using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  using External.DB.Extensions;
  using Convert.Geometry;

  [ComponentVersion(introduced: "1.6")]
  public class ElementClone : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("0EA8D61A-5FED-471D-A69D-B695DFBA5581");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public ElementClone() : base
    (
      name: "Clone Element",
      nickname: "Clone",
      description: "Clone document element on several locations",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Destination document",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Source element",
        }
      ),
      new ParamDefinition
      (
        new Param_Plane
        {
          Name = "Location",
          NickName = "L",
          Description = "Location to place the new element. Point and plane are accepted",
          Access = GH_ParamAccess.list,
        }
      ),
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "Target View",
        },
        ParamRelevance.Secondary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Source element",
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _Clones_,
          NickName = _Clones_.Substring(0, 1),
          Description = "Cloned elements",
          Access = GH_ParamAccess.list,
        }
      ),
    };

    const string _Clones_ = "Clones";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;

      if (!Params.GetData(DA, "Element", out Types.GraphicalElement element)) return;
      else Params.TrySetData(DA, "Element", () => element);

      if (!Params.GetDataList(DA, "Location", out IList<Plane?> locations) || locations is null) return;
      if (!Params.TryGetData(DA, "View", out Types.View view)) return;

      var clones = new List<Types.GraphicalElement>(locations.Count);
      foreach (var location in locations)
      {
        var clone = Types.GraphicalElement.FromElement
        (
          ReconstructElement<ARDB.Element>
          (
            doc.Value, _Clones_,
            x =>
            {
              if (element.Value is ARDB.Element elementValue && location.HasValue && location.Value.IsValid)
              {
                if(!Reuse(x, elementValue, view?.Value))
                {
                  if (view?.IsValid == true && element.ViewSpecific != true)
                  {
                    switch (FailureProcessingMode)
                    {
                      case ARDB.FailureProcessingResult.Continue:
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Cannot clone the specified element into '{view.Nomen}' view. {{{element.Id}}}");
                        return null;
                      case ARDB.FailureProcessingResult.ProceedWithCommit:
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Cannot paste the specified element into '{view.Nomen}' view. {{{element.Id}}}");
                        break;
                      case ARDB.FailureProcessingResult.WaitForUserInput:
                        using (var failure = new ARDB.FailureMessage(ARDB.BuiltInFailures.CopyPasteFailures.CannotPasteInView))
                        {
                          failure.SetFailingElement(view.Id);
                          failure.SetAdditionalElement(element.Id);
                          doc.Value.PostFailure(failure);
                        }
                        return null;

                      default: throw new Exceptions.RuntimeException();
                    }
                  }

                  x = elementValue.CloneElement(doc.Value, view?.Value);
                }

                return x;
              }

              return default;
            }
          )
        ) as Types.GraphicalElement;

        if (clone is object)
        {
          switch (element.Value.Location)
          {
            case ARDB.LocationCurve locationCurve:
              clone.SetCurve(locationCurve.Curve.ToCurve(), keepJoins: false);
              break;
          }

          if (location.HasValue && location.Value.IsValid && !clone.Location.EpsilonEquals(location.Value, GeometryTolerance.Model.VertexTolerance))
            clone.SetLocation(location.Value);
        }

        clones.Add(clone);
      }
      DA.SetDataList(_Clones_, clones);
    }

    bool Reuse(ARDB.Element target, ARDB.Element source, ARDB.View view)
    {
      if (target is null) return false;
      if (target.OwnerViewId != (view?.Id ?? source.OwnerViewId)) return false;

      if (target.GetType() != source.GetType()) return false;
      if (target.Category.Id != source.Category.Id) return false;
      if (target.ViewSpecific != source.ViewSpecific) return false;

      var targetLocation = target.Location;
      var sourceLocation = source.Location;

      if (targetLocation.GetType() != sourceLocation.GetType()) return false;
      if (targetLocation is ARDB.LocationCurve xCurve && !(sourceLocation as ARDB.LocationCurve).Curve.IsSameKindAs(xCurve.Curve)) return false;

      // TODO : Implement a DeepCopy here
      // Duplicate any missing type, material on demand
      //x.DeepCopyParametersFrom(element.Value);

      target.CopyParametersFrom(source);
      return true;
    }
  }
}

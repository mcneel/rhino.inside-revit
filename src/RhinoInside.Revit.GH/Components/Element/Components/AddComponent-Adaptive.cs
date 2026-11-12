using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using Grasshopper.Kernel.Parameters;

  public class AddComponentAdaptive : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("E8DDC0E4-97E9-4659-9945-E8C77114273D");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AddComponentAdaptive() : base
    (
      name: "Add Component (Adaptive)",
      nickname: "A-Component",
      description: "Given a collection of Points, it adds an adaptive component element to the active Revit document",
      category: "Revit",
      subCategory: "Component"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        Parameters.Document.AddTarget(), ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Points",
          NickName = "P",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = "Type",
          NickName = "T",
          Description = "Adaptive component type.",
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_GenericModel
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.FamilyInstance()
        {
          Name = _Component_,
          NickName = _Component_.Substring(0, 1),
          Description = $"Output {_Component_}",
        }
      )
    };

    const string _Component_ = "Component";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM
    };

    protected static void ChangeElementTypeId<T>(ref T element, ARDB.ElementId elementTypeId) where T : ARDB.Element
    {
      if (element is object && elementTypeId != element.GetTypeId())
      {
        var doc = element.Document;
        if (element.IsValidType(elementTypeId))
        {
          var newElmentId = element.ChangeTypeId(elementTypeId);
          if (newElmentId != ARDB.ElementId.InvalidElementId)
            element = (T) doc.GetElement(newElmentId);
        }
        else element = null;
      }
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.FamilyInstance>
      (
        doc.Value, _Component_, component =>
        {
          if (!Params.GetDataList(DA, "Points", out IList<Point3d?> points) || points.Any(x => !x.HasValue)) return null;
          if (points.Any(x => !x.HasValue)) return null;

          // Type
          if (!Params.GetData(DA, "Type", out Types.FamilySymbol type)) return null;
          type.AssertPlacementType(ARDB.FamilyPlacementType.Adaptive);
          if (!type.Value.IsActive) type.Value.Activate();
          ChangeElementTypeId(ref component, type.Id);

          var adaptivePoints = points.Select(x => x.Value.ToXYZ()).ToArray();
          if (component is object && ARDB.AdaptiveComponentInstanceUtils.IsAdaptiveComponentInstance(component))
          {
            var adaptivePointIds = ARDB.AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(component);
            if (adaptivePointIds.Count == adaptivePoints.Length)
            {
              int index = 0;
              foreach (var vertex in adaptivePointIds.Select(id => doc.Value.GetElement(id)).Cast<ARDB.ReferencePoint>())
              {
                var position = adaptivePoints[index++];
                if (!vertex.Position.AlmostEqualPoints(position))
                  vertex.Position = position;
              }

              DA.SetData(_Component_, component);
              return component;
            }
          }

          using (var create = doc.Value.Create())
          {
            var creationData = new List<Autodesk.Revit.Creation.FamilyInstanceCreationData>
            {
              doc.Value.Application.Create.NewFamilyInstanceCreationData(type.Value, adaptivePoints)
            };

            var newElementIds = create.NewFamilyInstances2(creationData);
            if (newElementIds.Count != 1)
              throw new Exceptions.RuntimeErrorException("Failed to create Family Instance element.");

            component = component.ReplaceElement(doc.Value.GetElement(newElementIds.First()) as ARDB.FamilyInstance, ExcludeUniqueProperties);
          }

          DA.SetData(_Component_, component);
          return component;
        }
      );
    }
  }
}

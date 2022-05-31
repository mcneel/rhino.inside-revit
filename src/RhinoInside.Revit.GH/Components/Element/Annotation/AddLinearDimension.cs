using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Annotation
{
  [ComponentVersion(introduced: "1.8")]
  public class AddLinearDimension : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("DF47C980-EF08-4BBE-A624-C956C07B04EC");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public AddLinearDimension() : base
    (
      name: "Add Linear Dimension",
      nickname: "LineDim",
      description: "Given a line, it adds a linear dimension to the given View",
      category: "Revit",
      subCategory: "Annotation"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to add a specific dimension"
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "References",
          NickName = "R",
          Description = "References to create a specific dimension",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Param_Line
        {
          Name = "Line",
          NickName = "L",
          Description = "Line to place a specific dimension",
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Element type of the given dimension",
          Optional = true
        }, ParamRelevance.Occasional
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Dimension()
        {
          Name = _Output_,
          NickName = _Output_.Substring(0, 1),
          Description = $"Output {_Output_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _Output_ = "Dimension";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;

      ReconstructElement<ARDB.Dimension>
      (
        view.Document, _Output_, dimension =>
        {
          // Input
          if (!Params.GetDataList(DA, "References", out IList<ARDB.Element> elements)) return null;
          if (!Params.GetData(DA, "Line", out Line? line)) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.DimensionType type, Types.Document.FromValue(view.Document), ARDB.ElementTypeGroup.LinearDimensionType)) return null;

          if
          (
            view.ViewType is ARDB.ViewType.Schedule ||
            view.ViewType is ARDB.ViewType.ColumnSchedule ||
            view.ViewType is ARDB.ViewType.PanelSchedule
          )
            throw new Exceptions.RuntimeArgumentException("View", "This view does not support detail items creation", view);

          var viewPlane = new Plane(view.Origin.ToPoint3d(), view.ViewDirection.ToVector3d());
          line = new Line(viewPlane.ClosestPoint(line.Value.From), viewPlane.ClosestPoint(line.Value.To));

          // Compute
          dimension = Reconstruct(dimension, view, line.Value.ToLine(), elements, type);

          DA.SetData(_Output_, dimension);
          return dimension;
        }
      );
    }

    static bool Contains(ARDB.ReferenceArray references, ARDB.ElementId value)
    {
      foreach (var reference in references.Cast<ARDB.Reference>())
        if (reference.ElementId == value)
          return true;

      return false;
    }

    bool Reuse(ARDB.Dimension dimension, ARDB.View view, ARDB.Line line, IList<ARDB.Element> elements, ARDB.DimensionType type)
    {
      if (dimension is null) return false;
      if (dimension.OwnerViewId != view.Id) return false;
      if (type != default && type.Id != dimension.GetTypeId()) return false;

      // Line
      if (!dimension.Curve.AlmostEquals(line, GeometryObjectTolerance.Internal.VertexTolerance)) return false;

      // Elements
      var currentReference = dimension.References;
      if (currentReference.Size != elements.Count) return false;

      foreach (var element in elements)
      {
        if (!Contains(currentReference, element.Id))
          return false;
      }

      return true;
    }

    ARDB.Dimension Create(ARDB.View view, ARDB.Line line, IList<ARDB.Element> elements, ARDB.DimensionType type)
    {
      var references = GetReferences(elements);

      if (view.Document.IsFamilyDocument)
      {
        if (type == default)
          return view.Document.FamilyCreate.NewDimension(view, line, references);
        else
          return view.Document.FamilyCreate.NewDimension(view, line, references, type);
      }
      else
      {
        if (type == default)
          return view.Document.Create.NewDimension(view, line, references);
        else
          return view.Document.Create.NewDimension(view, line, references, type);
      }
    }

    static ARDB.ReferenceArray GetReferences(IList<ARDB.Element> elements)
    {
      var referenceArray = new ARDB.ReferenceArray();

      foreach (var element in elements)
      {
        var reference = default(ARDB.Reference);
        switch (element)
        {
          case null: break;
#if REVIT_2018
          case ARDB.FamilyInstance instance:
            reference = instance.GetReferences(ARDB.FamilyInstanceReferenceType.CenterLeftRight).FirstOrDefault();
            break;
#endif
          default:
            using (var options = new ARDB.Options() { ComputeReferences = true, IncludeNonVisibleObjects = true })
            {
              var geometry = element.get_Geometry(options);
              reference = geometry.OfType<ARDB.Solid>().
                SelectMany(x => x.Faces.Cast<ARDB.Face>()).
                Select(x => x.Reference).
                OfType<ARDB.Reference>().
                FirstOrDefault();
            }
            break;
        }

        if (reference is object)
          referenceArray.Append(reference);
      }
      return referenceArray;

    }

    ARDB.Dimension Reconstruct(ARDB.Dimension dimension, ARDB.View view, ARDB.Line line, IList<ARDB.Element> elements, ARDB.DimensionType type)
    {
      if (!Reuse(dimension, view, line, elements, type))
        dimension = Create(view, line, elements, type);

      return dimension;
    }
  }
}


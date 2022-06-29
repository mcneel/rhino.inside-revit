using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
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
        new Parameters.DimensionType()
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
          if (!view.IsGraphicalView()) throw new Exceptions.RuntimeArgumentException("View", "This view does not support detail items creation", view);
          if (!Params.GetDataList(DA, "References", out IList<ARDB.Element> elements)) return null;
          if (!Params.GetData(DA, "Line", out Line? line)) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.DimensionType type, Types.Document.FromValue(view.Document), ARDB.ElementTypeGroup.LinearDimensionType)) return null;

          var viewPlane = new Plane(view.Origin.ToPoint3d(), view.ViewDirection.ToVector3d());
          line = new Line(viewPlane.ClosestPoint(line.Value.From), viewPlane.ClosestPoint(line.Value.To));

          // Compute
          var references = elements.Select(ElementExtension.GetDefaultReference).OfType<ARDB.Reference>().ToArray();
          dimension = Reconstruct(dimension, view, line.Value.ToLine(), references, type);

          DA.SetData(_Output_, dimension);
          return dimension;
        }
      );
    }

    bool Reuse(ARDB.Dimension dimension, ARDB.View view, ARDB.Line line, IList<ARDB.Reference> references, ARDB.DimensionType type)
    {
      if (dimension is null) return false;
      if (dimension.OwnerViewId != view.Id) return false;
      if (type != default && type.Id != dimension.GetTypeId()) return false;

      // Line
      if (!dimension.Curve.AlmostEquals(line, GeometryTolerance.Internal.VertexTolerance)) return false;

      // References
      var currentReferences = dimension.References;
      if (currentReferences.Size != references.Count) return false;

      var referenceEqualityComparer = ReferenceEqualityComparer.SameDocument(dimension.Document);
      foreach (var reference in references)
      {
        if (!currentReferences.Cast<ARDB.Reference>().Contains(reference, referenceEqualityComparer))
          return false;
      }

      return true;
    }

    ARDB.Dimension Create(ARDB.View view, ARDB.Line line, IList<ARDB.Reference> references, ARDB.DimensionType type)
    {
      var referenceArray = new ARDB.ReferenceArray();
      for (int r = 0; r < references.Count; ++r)
        referenceArray.Append(references[r]);

      if (view.Document.IsFamilyDocument)
      {
        if (type == default)
          return view.Document.FamilyCreate.NewDimension(view, line, referenceArray);
        else
          return view.Document.FamilyCreate.NewDimension(view, line, referenceArray, type);
      }
      else
      {
        if (type == default)
          return view.Document.Create.NewDimension(view, line, referenceArray);
        else
          return view.Document.Create.NewDimension(view, line, referenceArray, type);
      }
    }

    ARDB.Dimension Reconstruct(ARDB.Dimension dimension, ARDB.View view, ARDB.Line line, IList<ARDB.Reference> references, ARDB.DimensionType type)
    {
      if (!Reuse(dimension, view, line, references, type))
        dimension = Create(view, line, references, type);

      return dimension;
    }
  }
}


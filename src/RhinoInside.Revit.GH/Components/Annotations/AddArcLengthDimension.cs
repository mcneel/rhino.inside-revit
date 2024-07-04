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
  [ComponentVersion(introduced: "1.23"), ComponentRevitAPIVersion(min: "2025.0")]
  public class AddArcLengthDimension : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("3FD260D4-1E8D-4C03-89D0-3D7256C19520");
    public override GH_Exposure Exposure => SDKCompliancy(GH_Exposure.primary);
    protected override string IconTag => string.Empty;

    public AddArcLengthDimension() : base
    (
      name: "Add Arc Length Dimension",
      nickname: "AL-Dim",
      description: "Given an arc, it adds an arc length dimension to the given View",
      category: "Revit",
      subCategory: "Annotate"
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
        new Parameters.GeometryObject()
        {
          Name = "References",
          NickName = "R",
          Description = "References to create a specific dimension",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Param_Arc
        {
          Name = "Arc",
          NickName = "A",
          Description = "Arc to place a specific dimension",
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

#if REVIT_2025
    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;

      ReconstructElement<ARDB.Dimension>
      (
        view.Document, _Output_, dimension =>
        {
          // Input
          if (!view.IsAnnotationView()) throw new Exceptions.RuntimeArgumentException("View", $"View '{view.Name}' does not support detail items creation", view);
          if (!Params.GetDataList(DA, "References", out IList<Types.GeometryObject> geometries)) return null;
          if (!Params.GetData(DA, "Arc", out Arc? arc)) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.DimensionType type, Types.Document.FromValue(view.Document), ARDB.ElementTypeGroup.ArcLengthDimensionType)) return null;

          // Compute
          var references = geometries.Where(x => x.ReferenceDocument.IsEquivalent(view.Document)).Select(x => x?.GetDefaultReference()).OfType<ARDB.Reference>().ToArray();

          if (references.Length > 2) dimension = Reconstruct(dimension, view, arc.Value.ToArc(), references, type);
          else
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid number of references.");
            dimension = null;
          }

          DA.SetData(_Output_, dimension);
          return dimension;
        }
      );
    }

    bool Reuse(ARDB.Dimension dimension, ARDB.View view, ARDB.Arc arc, IList<ARDB.Reference> references, ARDB.DimensionType type)
    {
      if (dimension is null) return false;
      if (dimension.OwnerViewId != view.Id) return false;

      // Arc
      if (!dimension.Curve.AlmostEquals(arc, GeometryTolerance.Internal.VertexTolerance)) return false;

      // References
      var currentReferences = dimension.References;
      if (currentReferences.Size != references.Count) return false;

      var referenceEqualityComparer = External.DB.Extensions.ReferenceEqualityComparer.SameDocument(dimension.Document);
      foreach (var reference in references)
      {
        if (!currentReferences.Cast<ARDB.Reference>().Contains(reference, referenceEqualityComparer))
          return false;
      }

      if (type is object) dimension.ChangeTypeId(type.Id);
      return true;
    }

    ARDB.Dimension Reconstruct(ARDB.Dimension dimension, ARDB.View view, ARDB.Arc arc, IList<ARDB.Reference> references, ARDB.DimensionType type)
    {
      if (!Reuse(dimension, view, arc, references, type))
      {
        dimension = ARDB.ArcLengthDimension.Create(view.Document, view, arc, references.FirstOrDefault(), references.Skip(1).ToList());
        if (type is object) dimension.ChangeTypeId(type.Id);
      }

      return dimension;
    }
#else
    protected override void TrySolveInstance(IGH_DataAccess DA) { }
#endif
  }
}


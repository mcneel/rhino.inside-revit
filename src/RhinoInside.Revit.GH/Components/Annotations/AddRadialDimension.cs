using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  [ComponentVersion(introduced: "1.23"), ComponentRevitAPIVersion(min: "2025.0")]
  public class AddRadialDimension : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("194E7DE0-5049-4BDA-A0FD-322C0F62AA47");
    public override GH_Exposure Exposure => SDKCompliancy(GH_Exposure.primary);
    protected override string IconTag => string.Empty;

    public AddRadialDimension() : base
    (
      name: "Add Radial Dimension",
      nickname: "R-Dim",
      description: "Given an arc referece, it adds a radial dimension to the given View",
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
          Name = "Reference",
          NickName = "R",
          Description = "References to create a specific dimension",
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

      ReconstructElement<ARDB.RadialDimension>
      (
        view.Document, _Output_, dimension =>
        {
          // Input
          if (!view.IsAnnotationView()) throw new Exceptions.RuntimeArgumentException("View", $"View '{view.Name}' does not support detail items creation", view);
          if (!Params.GetData(DA, "Reference", out Types.GeometryObject geometry)) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.DimensionType type, Types.Document.FromValue(view.Document), ARDB.ElementTypeGroup.RadialDimensionType)) return null;

          // Compute
          var references = new Types.GeometryObject[] { geometry}.Where(x => x.ReferenceDocument.IsEquivalent(view.Document)).Select(x => x?.GetDefaultReference()).OfType<ARDB.Reference>().ToArray();

          if (references.Length > 0) dimension = Reconstruct(dimension, view, references, type);
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

    bool Reuse(ARDB.RadialDimension dimension, ARDB.View view, IList<ARDB.Reference> references, ARDB.DimensionType type)
    {
      if (dimension is null) return false;
      if (dimension.OwnerViewId != view.Id) return false;

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

    ARDB.RadialDimension Reconstruct(ARDB.RadialDimension dimension, ARDB.View view, IList<ARDB.Reference> references, ARDB.DimensionType type)
    {
      if (!Reuse(dimension, view, references, type))
      {
        dimension = ARDB.RadialDimension.Create(view.Document, view, references.FirstOrDefault(), isDiameter: false);
        if (type is object) dimension.ChangeTypeId(type.Id);
      }

      return dimension;
    }
#else
    protected override void TrySolveInstance(IGH_DataAccess DA) { }
#endif
  }
}


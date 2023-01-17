using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Views
{
  using System.Linq;
  using Convert.Geometry;
  using Grasshopper.Kernel.Parameters;
  using Rhino.Geometry;

  [ComponentVersion(introduced: "1.12")]
  public class ViewCropRegion : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("3AE4FA67-5673-4153-B43C-962AB7F8AFA1");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public ViewCropRegion() : base
    (
      name: "View Crop Region",
      nickname: "Crop Region",
      description: "View Get-Set crop region",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.View>
      (
        name: "View",
        nickname: "V",
        description: "View to access crop extents"
      ),
      ParamDefinition.Create<Param_Boolean>
      (
        name: "Crop View",
        nickname: "CV",
        description:  "Crop View",
        optional:  true,
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_Boolean>
      (
        name: "Crop Region Visible",
        nickname: "CRV",
        description:  "Crop Region Visible",
        optional:  true,
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_Curve>
      (
        name: "Crop Region",
        nickname: "CR",
        description:  "Crop Region in View near-plane coordinate system.",
        optional: true,
        relevance: ParamRelevance.Primary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.View>
      (
        name: "View",
        nickname: "V",
        description: "View to access crop extents",
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_Boolean>
      (
        name: "Crop View",
        nickname: "CV",
        description:  "Crop View",
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_Boolean>
      (
        name: "Crop Region Visible",
        nickname: "CRV",
        description:  "Crop Region Visible",
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_Curve>
      (
        name: "Crop Region",
        nickname: "CR",
        description:  "Crop Region in View near-plane coordinate system.",
        relevance: ParamRelevance.Primary
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      else Params.TrySetData(DA, "View", () => view);

      if (Params.GetData(DA, "Crop View", out bool? cropView))
      {
        StartTransaction(view.Document);
        view.CropBoxActive = cropView;
      }
      Params.TrySetData(DA, "Crop View", () => view.CropBoxActive);

      if (Params.GetData(DA, "Crop Region Visible", out bool? cropRegionVisible))
      {
        StartTransaction(view.Document);
        view.CropBoxVisible = cropRegionVisible;
      }
      Params.TrySetData(DA, "Crop Region Visible", () => view.CropBoxVisible);

      using (var cropManager = view.Value.GetCropRegionShapeManager())
      {
        if (Params.GetData(DA, "Crop Region", out Curve cropRegion))
        {
          if (!cropManager.CanHaveShape)
          {
            AddGeometryRuntimeError
            (
              GH_RuntimeMessageLevel.Error,
              $"View '{view.Nomen}' does not permit to have a non-rectangular shape.",
              cropRegion
            );

            return;
          }

          cropRegion = Curve.ProjectToPlane(cropRegion, view.Location);
          var curveLoop = cropRegion?.ToCurveLoop();

          if (curveLoop is null || !cropManager.IsCropRegionShapeValid(curveLoop))
          {
            AddGeometryRuntimeError
            (
              GH_RuntimeMessageLevel.Error,
              "Crop Region should be one closed curve loop without self-intersections, consisting of non-zero length straight lines in a plane parallel to the view plane.",
              cropRegion
            );

            return;
          }

          StartTransaction(view.Document);

          cropManager.SetCropShape(curveLoop);

          // Necessary to make GetCropShape below return updated geometry.
          view.Document.Regenerate();
        }
        Params.TrySetData(DA, "Crop Region", () =>
        {
          if (!cropManager.Split)
            return cropManager.GetCropShape().Select(GeometryDecoder.ToPolyCurve).FirstOrDefault();

          return null;
        });
      }
    }
  }
}

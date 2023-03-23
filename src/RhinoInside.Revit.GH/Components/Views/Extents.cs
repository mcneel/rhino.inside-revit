using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  using System.Linq;
  using External.DB.Extensions;
  using Grasshopper.Kernel.Parameters;
  using Grasshopper.Kernel.Types;
  using Rhino.Geometry;

  [ComponentVersion(introduced: "1.7")]
  public class ViewExtents : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("D4593785-9CAB-408E-B70C-1BA40A9B2B5E");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "E";

    public ViewExtents() : base
    (
      name: "View Extents",
      nickname: "Extents",
      description: "View Get-Set crop extents",
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
      ParamDefinition.Create<Param_Interval2D>
      (
        name: "Crop Extents",
        nickname: "CE",
        description:  "Crop extents in View near-plane coordinate system.",
        optional: true,
        relevance: ParamRelevance.Primary
      ),
      //ParamDefinition.Create<Param_Interval>
      //(
      //  name: "Depth",
      //  nickname: "D",
      //  description:  "View depth in View near-plane coordinate system",
      //  optional:  true,
      //  relevance: ParamRelevance.Secondary
      //),
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
      ParamDefinition.Create<Param_Interval2D>
      (
        name: "Crop Extents",
        nickname: "CE",
        description:  "Crop extents in View-Location coordinate system.",
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_Interval>
      (
        name: "Depth",
        nickname: "D",
        description:  "View depth in View-Location coordinate system",
        relevance: ParamRelevance.Secondary
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;

      if (!view.Value.GetOrderedParameters().Any(x => x.Id.ToBuiltInParameter() == ARDB.BuiltInParameter.VIEWER_CROP_REGION))
      {
        AddRuntimeMessage
        (
          GH_RuntimeMessageLevel.Error,
          $"View '{view.Value.Title}' can't be cropped."
        );
        return;
      }
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

      if (Params.GetData(DA, "Crop Extents", out GH_Interval2D cropExtents))
      {
        StartTransaction(view.Document);

        using (var cropManager = view.Value.GetCropRegionShapeManager())
          if (cropManager.CanHaveShape) cropManager.RemoveCropRegionShape();

        var cropBox = view.Value.CropBox;
        cropBox.Min = new ARDB.XYZ
        (
          cropExtents.Value.U.Min / Revit.ModelUnits,
          cropExtents.Value.V.Min / Revit.ModelUnits,
          cropBox.Min.Z
        );
        cropBox.Max = new ARDB.XYZ
        (
          cropExtents.Value.U.Max / Revit.ModelUnits,
          cropExtents.Value.V.Max / Revit.ModelUnits,
          cropBox.Max.Z
        );
        view.Value.CropBox = cropBox;
      }
      Params.TrySetData(DA, "Crop Extents", () =>
      {
        var (min, max) = view.Value.CropBox;
        var u = new Interval(min.X * Revit.ModelUnits, max.X * Revit.ModelUnits);
        var v = new Interval(min.Y * Revit.ModelUnits, max.Y * Revit.ModelUnits);
        return new GH_Interval2D(new UVInterval(u, v));
      });

      if (Params.GetData(DA, "Depth", out GH_Interval depth))
      {
        StartTransaction(view.Document);

        var origin = view.Value.Origin;
        var cropBox = view.Value.CropBox;
        var zFar = cropBox.Min.Z;
        var zNear = cropBox.Max.Z;
        cropBox.Min = new ARDB.XYZ
        (
          cropBox.Min.X,
          cropBox.Min.Y,
          cropBox.Min.Z
        );
        cropBox.Max = new ARDB.XYZ
        (
          cropBox.Max.X,
          cropBox.Max.Y,
          cropBox.Min.Z + depth.Value.Max / Revit.ModelUnits - depth.Value.Min / Revit.ModelUnits
        );

        try { view.Value.CropBox = cropBox; }
        catch { }

        view.Document.Regenerate();

        cropBox = view.Value.CropBox;

        cropBox.Min = new ARDB.XYZ
        (
          cropBox.Min.X,
          cropBox.Min.Y,
          zNear//+extentsZ.Value.Min / Revit.ModelUnits
        );
        cropBox.Max = new ARDB.XYZ
        (
          cropBox.Max.X,
          cropBox.Max.Y,
          zNear + cropBox.Min.Z /*+ extentsZ.Value.Max / Revit.ModelUnits*/ - depth.Value.Min / Revit.ModelUnits
        );

        view.Value.CropBox = cropBox;
      }
      Params.TrySetData(DA, "Depth", () =>
      {
        GetViewRangeOffsets(view, out var backOffset, out var frontOffset);
        return new GH_Interval(new Interval(backOffset, frontOffset));
      });
    }

    static void GetFrontAndBackClipOffsets(ARDB.View view, out double backOffset, out double frontOffset)
    {
      backOffset = -(view.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_FAR)?.AsInteger() == 1 ?
                    (view.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_OFFSET_FAR)?.AsDouble() ?? double.PositiveInfinity) : double.PositiveInfinity);

      frontOffset = +(view.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_NEAR)?.AsInteger() == 1 ?
                     (view.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_OFFSET_NEAR)?.AsDouble() ?? double.PositiveInfinity) : double.PositiveInfinity);
    }

    static void GetViewRangeOffsets(Types.View view, out double backOffset, out double frontOffset)
    {
      GetFrontAndBackClipOffsets(view.Value, out backOffset, out frontOffset);

      backOffset *= Revit.ModelUnits;
      frontOffset *= Revit.ModelUnits;

      switch (view.Value)
      {
        case ARDB.View3D view3D:
        {
          // `FilteredElementCollector` does not check near-plane on 3D-views. (Tested on Revit 2023.0)
          //if (view3D.IsPerspective)
          //  frontOffset = Math.Min(frontOffset, 0.0);
        }
        break;

        case ARDB.ViewPlan viewPlan:
          using (var viewRange = viewPlan.GetViewRange())
          {
            var bottom  = ViewRangeElevations.GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.ViewDepthPlane);
            var top     = ViewRangeElevations.GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.TopClipPlane);

            backOffset  = Math.Max(backOffset,  (bottom?.Elevation ?? double.NegativeInfinity) - view.Position.Z);
            frontOffset = Math.Min(frontOffset, (top?.Elevation    ?? double.PositiveInfinity) - view.Position.Z);
          }
          break;

        case ARDB.ViewSection viewSection:
          if (double.IsInfinity(frontOffset) || double.IsNaN(frontOffset))
            frontOffset = viewSection.CropBox.Max.Z * Revit.ModelUnits;

          break;
      }
    }
  }
}

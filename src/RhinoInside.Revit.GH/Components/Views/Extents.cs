using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.View
{
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
        description:  "Crop extents in View near-plane coordinate system.",
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_Interval>
      (
        name: "Depth",
        nickname: "D",
        description:  "View depth in View near-plane coordinate system",
        relevance: ParamRelevance.Secondary
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      else Params.TrySetData(DA, "View", () => view);

      if (Params.GetData(DA, "Crop View", out bool? cropView) && cropView.HasValue)
      {
        StartTransaction(view.Document);
        view.Value.CropBoxActive = cropView.Value;
      }
      Params.TrySetData(DA, "Crop View", () => view.Value.CropBoxActive);

      if (Params.GetData(DA, "Crop Region Visible", out bool? cropRegionVisible) && cropRegionVisible.HasValue)
      {
        StartTransaction(view.Document);
        view.Value.CropBoxVisible = cropRegionVisible.Value;
      }
      Params.TrySetData(DA, "Crop Region Visible", () => view.Value.CropBoxVisible);      

      if (Params.GetData(DA, "Crop Extents", out GH_Interval2D cropExtents))
      {
        StartTransaction(view.Document);

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
        var cropBox = view.Value.CropBox;
        var u = new Interval(cropBox.Min.X * Revit.ModelUnits, cropBox.Max.X * Revit.ModelUnits);
        var v = new Interval(cropBox.Min.Y * Revit.ModelUnits, cropBox.Max.Y * Revit.ModelUnits);
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
        var cropBox = view.Value.CropBox;
        var interval = new Interval(cropBox.Min.Z * Revit.ModelUnits, cropBox.Max.Z * Revit.ModelUnits);
        return new GH_Interval(interval);
      });
    }
  }
}

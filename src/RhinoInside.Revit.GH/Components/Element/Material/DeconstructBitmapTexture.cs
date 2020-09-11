using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.External.DB;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public class DeconstructBitmapTexture : AnalysisComponent
  {
    public override Guid ComponentGuid =>
      new Guid("77b391db-014a-4964-8810-5927df0dd7bb");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public DeconstructBitmapTexture() : base(
      name: "Deconstruct Bitmap Texture",
      nickname: "D-BMPTX",
      description: "Deconstruct given instance of bitmap texture",
      category: "Revit",
      subCategory: "Material"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(
        param: new Parameters.DataObject<UnifiedBitmapData>(),
        name: "Bitmap Texture",
        nickname: "BT",
        description: "Bitmap Texture Data",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter(
        name: "Source",
        nickname: "S",
        description: "Full path of bitmap texture source image file",
        access: GH_ParamAccess.item
      );
      pManager.AddBooleanParameter(
        name: "Invert",
        nickname: "I",
        description: "Invert source image colors",
        access: GH_ParamAccess.item
      );
      pManager.AddNumberParameter(
        name: "Brightness",
        nickname: "B",
        description: "Texture brightness",
        access: GH_ParamAccess.item
      );
      pManager.AddNumberParameter(
        name: "OffsetU",
        nickname: "OU",
        description: "Texture offset along U axis",
        access: GH_ParamAccess.item
      );
      pManager.AddNumberParameter(
        name: "OffsetV",
        nickname: "OV",
        description: "Texture offset along V axis",
        access: GH_ParamAccess.item
      );
      pManager.AddNumberParameter(
        name: "SizeU",
        nickname: "SU",
        description: "Texture size along U axis",
        access: GH_ParamAccess.item
      );
      pManager.AddNumberParameter(
        name: "SizeV",
        nickname: "SV",
        description: "Texture size along V axis",
        access: GH_ParamAccess.item
      );
      pManager.AddBooleanParameter(
        name: "RepeatU",
        nickname: "RU",
        description: "Texture repeat along U axis",
        access: GH_ParamAccess.item
      );
      pManager.AddBooleanParameter(
        name: "RepeatV",
        nickname: "RV",
        description: "Texture repeat along V axis",
        access: GH_ParamAccess.item
      );
      pManager.AddNumberParameter(
        name: "Angle",
        nickname: "A",
        description: "Texture angle",
        access: GH_ParamAccess.item
      );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // get input
      Types.DataObject<UnifiedBitmapData> dataObj = default;
      if (!DA.GetData("Bitmap Texture", ref dataObj))
        return;

      UnifiedBitmapData bitmapData = dataObj.Value;

      DA.SetData("Source", bitmapData.SourceFile);
      DA.SetData("Invert", bitmapData.Invert);
      DA.SetData("Brightness", bitmapData.Brightness);
      DA.SetData("OffsetU", bitmapData.TwoDMap.OffsetU);
      DA.SetData("OffsetV", bitmapData.TwoDMap.OffsetV);
      DA.SetData("SizeU", bitmapData.TwoDMap.SizeU);
      DA.SetData("SizeV", bitmapData.TwoDMap.SizeV);
      DA.SetData("RepeatU", bitmapData.TwoDMap.RepeatU);
      DA.SetData("RepeatV", bitmapData.TwoDMap.RepeatV);
      DA.SetData("Angle", bitmapData.TwoDMap.Angle);
    }
  }
}

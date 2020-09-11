using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public class ConstructBitmapTexture : DocumentComponent
  {
    public override Guid ComponentGuid =>
      new Guid("37b63660-c083-45e3-9b98-cfdd2b539055");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public ConstructBitmapTexture() : base(
      name: "Construct Bitmap Texture",
      nickname: "C-BMPTX",
      description: "Construct a new instance of bitmap texture",
      category: "Revit",
      subCategory: "Material"
    )
    {
    }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Param_String>(
        name: "Source",
        nickname: "S",
        description: "Full path of bitmap texture source image file",
        access: GH_ParamAccess.item
      ),
      ParamDefinition.Create<Param_Boolean>(
        name: "Invert",
        nickname: "I",
        description: "Invert source image colors",
        access: GH_ParamAccess.item,
        optional: true
      ),
      ParamDefinition.Create<Param_Number>(
        name: "Brightness",
        nickname: "B",
        description: "Texture brightness",
        access: GH_ParamAccess.item,
        optional: true
      ),
      ParamDefinition.Create<Param_Number>(
        name: "OffsetU",
        nickname: "OU",
        description: "Texture offset along U axis",
        access: GH_ParamAccess.item,
        optional: true
      ),
      ParamDefinition.Create<Param_Number>(
        name: "OffsetV",
        nickname: "OV",
        description: "Texture offset along V axis",
        access: GH_ParamAccess.item,
        optional: true
      ),
      ParamDefinition.Create<Param_Number>(
        name: "SizeU",
        nickname: "SU",
        description: "Texture size along U axis",
        access: GH_ParamAccess.item,
        optional: true
      ),
      ParamDefinition.Create<Param_Number>(
        name: "SizeV",
        nickname: "SV",
        description: "Texture size along V axis",
        access: GH_ParamAccess.item,
        optional: true
      ),
      ParamDefinition.Create<Param_Boolean>(
        name: "RepeatU",
        nickname: "RU",
        description: "Texture repeat along U axis",
        access: GH_ParamAccess.item,
        optional: true
      ),
      ParamDefinition.Create<Param_Boolean>(
        name: "RepeatV",
        nickname: "RV",
        description: "Texture repeat along V axis",
        access: GH_ParamAccess.item,
        optional: true
      ),
      ParamDefinition.Create<Param_Number>(
        name: "Angle",
        nickname: "A",
        description: "Texture angle",
        access: GH_ParamAccess.item,
        optional: true
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.DataObject<UnifiedBitmapData>>(
        name: "Bitmap Texture",
        nickname: "BT",
        description: "Bitmap Texture Data",
        access: GH_ParamAccess.item
        )
    };


    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      string source = default;
      if (!DA.GetData("Source", ref source))
        return;

      bool invert = false;
      DA.GetData("Invert", ref invert);
      double brightness = 1.0;
      DA.GetData("Brightness", ref brightness);
      double offsetU = 0;
      double offsetV = 0;
      DA.GetData("OffsetU", ref offsetU);
      DA.GetData("OffsetV", ref offsetV);
      // TODO: fix units
      double scaleU = 12.0;
      double scaleV = 12.0;
      DA.GetData("SizeU", ref scaleU);
      DA.GetData("SizeV", ref scaleV);
      bool repeatU = true;
      bool repeatV = true;
      DA.GetData("RepeatU", ref repeatU);
      DA.GetData("RepeatV", ref repeatV);
      double angle = 0;
      DA.GetData("Angle", ref angle);

      DA.SetData(
        "Bitmap Texture",
        new Types.DataObject<UnifiedBitmapData>(
          new UnifiedBitmapData
          {
            SourceFile = source,
            Invert = invert,
            Brightness = brightness,
            TwoDMap = new TwoDMapData
            {
              OffsetU = offsetU,
              OffsetV = offsetV,
              SizeU = scaleU,
              SizeV = scaleV,
              RepeatU = repeatU,
              RepeatV = repeatV,
              Angle = angle
            }
          },
          srcDocument: doc
          )
        );
    }
  }
}

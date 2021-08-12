using System;

using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Material
{
#if REVIT_2018
  public abstract class DeconstructTextureAsset<T>
    : BaseAssetComponent<T> where T : TextureData, new()
  {
    public override GH_Exposure Exposure => GH_Exposure.senary;

    public DeconstructTextureAsset() : base()
    {
      this.Name = $"Deconstruct {ComponentInfo.Name}";
      this.NickName = $"D-{ComponentInfo.NickName}";
      this.Description = $"Deconstruct {ComponentInfo.Description}";
    }

    protected override ParamDefinition[] Inputs => new ParamDefinition[]
    {
      ParamDefinition.Create<Parameters.TextureData>(
        name: ComponentInfo.Name,
        nickname: ComponentInfo.NickName,
        description: ComponentInfo.Description,
        access: GH_ParamAccess.item
        ),
    };
    protected override ParamDefinition[] Outputs => GetAssetDataAsOutputs();

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Types.TextureData ghTextureData = default;
      if (DA.GetData(ComponentInfo.Name, ref ghTextureData))
      {
        if (ghTextureData.Value is T textureData)
          SetOutputsFromAssetData(DA, textureData);

        else if (ghTextureData.Value is TextureData tdata)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Can not deconstruct {tdata.Schema} Asset");

        else
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Input is not a texture");
      }
    }
  }


  public class DeconstructBitmapTexture
    : DeconstructTextureAsset<UnifiedBitmapData>
  {
    public override Guid ComponentGuid
      => new Guid("77b391db-014a-4964-8810-5927df0dd7bb");
  }

  public class DeconstructCheckerTexture
    : DeconstructTextureAsset<CheckerData>
  {
    public override Guid ComponentGuid
      => new Guid("d44a1497-ae52-4e4d-ab8e-962195177fbb");
  }
#endif
}

using System;

using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Materials
{
#if REVIT_2018
  public abstract class ConstructTextureAsset<T>
    : BaseAssetComponent<T> where T : TextureData, new()
  {
    public override GH_Exposure Exposure => GH_Exposure.senary;

    public ConstructTextureAsset() : base()
    {
      Name = $"Construct {ComponentInfo.Name}";
      NickName = $"C-{ComponentInfo.NickName}";
      Description = $"Construct {ComponentInfo.Description}";
    }

    protected override ParamDefinition[] Inputs => GetAssetDataAsInputs();
    protected override ParamDefinition[] Outputs => new ParamDefinition[]
    {
      ParamDefinition.Create<Parameters.TextureData>(
        name: ComponentInfo.Name,
        nickname: ComponentInfo.NickName,
        description: ComponentInfo.Description,
        access: GH_ParamAccess.item
        ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
      => DA.SetData(
        ComponentInfo.Name,
        new Types.TextureData(CreateAssetDataFromInputs(DA))
        );
  }

  public class ConstructBitmapTexture
    : ConstructTextureAsset<UnifiedBitmapData>
  {
    public override Guid ComponentGuid
      => new Guid("37b63660-c083-45e3-9b98-cfdd2b539055");
  }

  public class ConstructCheckerTexture
    : ConstructTextureAsset<CheckerData>
  {
    public override Guid ComponentGuid
      => new Guid("2332c031-de18-43a9-b41f-6405e1460c06");
  }
#endif
}

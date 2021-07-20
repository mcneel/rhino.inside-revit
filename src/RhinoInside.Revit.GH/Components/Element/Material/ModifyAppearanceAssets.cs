using System;
using System.Collections.Generic;

using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Material
{
#if REVIT_2018
  public abstract class ModifyAppearanceAssets<T>
: BaseAssetComponent<T> where T : ShaderData, new()
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public ModifyAppearanceAssets()
    {
      Name = $"Modify {ComponentInfo.Name}";
      NickName = $"M-{ComponentInfo.NickName}";
      Description = $"Modify given {ComponentInfo.Description}";
    }

    protected override ParamDefinition[] Inputs => GetFieldsAsInputs();
    protected override ParamDefinition[] Outputs => new ParamDefinition[]
    {
      ParamDefinition.Create<Parameters.AppearanceAsset>(
        name: ComponentInfo.Name,
        nickname: ComponentInfo.NickName,
        description: ComponentInfo.Description,
        access: GH_ParamAccess.item
        ),
    };

    private ParamDefinition[] GetFieldsAsInputs()
    {
      var inputs = new List<ParamDefinition>();

      var param = new Parameters.AppearanceAsset
      {
        Name = ComponentInfo.Name,
        NickName = ComponentInfo.NickName,
        Description = ComponentInfo.Description,
      };

      inputs.Add(new ParamDefinition(param));
      inputs.AddRange(GetAssetDataAsInputs(skipUnchangable: true));
      return inputs.ToArray();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var appearanceAsset = default(Types.AppearanceAssetElement);
      if (!DA.GetData(ComponentInfo.Name, ref appearanceAsset) || appearanceAsset.Value is null)
        return;

      // lets process all the inputs into a data structure
      // this step also verifies the input data
      var assetData = CreateAssetDataFromInputs(DA);

      StartTransaction(appearanceAsset.Document);

      // update the asset parameters
      // update asset properties
      UpdateAssetElementFromInputs(appearanceAsset.Value, assetData);

      // send it to output
      DA.SetData(ComponentInfo.Name, appearanceAsset);
    }
  }

  public class ModifyGenericShader
  : ModifyAppearanceAssets<GenericData>
  {
    public override Guid ComponentGuid =>
      new Guid("73b2376b-d6c8-4095-b7ce-a66ef4c931e6");
  }
#endif
}

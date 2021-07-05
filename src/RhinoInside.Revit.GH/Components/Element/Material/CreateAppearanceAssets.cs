using System;
using System.Collections.Generic;

using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Material
{
#if REVIT_2018
  public abstract class CreateAppearanceAsset<T>
    : BaseAssetComponent<T> where T : ShaderData, new()
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public CreateAppearanceAsset() : base()
    {
      Name = $"Create {ComponentInfo.Name}";
      NickName = $"C-{ComponentInfo.NickName}";
      Description = $"Create a new instance of {ComponentInfo.Description} inside document";
    }

    protected override ParamDefinition[] Inputs => GetInputs();
    protected override ParamDefinition[] Outputs => new ParamDefinition[]
    {
      ParamDefinition.Create<Parameters.AppearanceAsset>(
        name: ComponentInfo.Name,
        nickname: ComponentInfo.NickName,
        description: ComponentInfo.Description,
        access: GH_ParamAccess.item
        ),
    };

    private ParamDefinition[] GetInputs()
    {
      // build a list of inputs based on the shader data type
      // add optional document parameter as first
      var inputs = new List<ParamDefinition>()
      {
        new ParamDefinition(new Parameters.Document() { Optional = true }, ParamRelevance.Occasional)
      };
      inputs.AddRange(GetAssetDataAsInputs());
      return inputs.ToArray();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      // lets process all the inputs into a data structure
      // this step also verifies the input data
      var assetData = CreateAssetDataFromInputs(DA);

      if (string.IsNullOrEmpty(assetData.Name))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Bad Name");
        return;
      }

      StartTransaction(doc);

      var assetElement = EnsureThisAsset(doc, assetData.Name);

      // update asset properties
      UpdateAssetElementFromInputs(assetElement, assetData);

      DA.SetData(ComponentInfo.Name, new Types.AppearanceAssetElement(assetElement));
    }
  }

  public class CreateGenericShader
    : CreateAppearanceAsset<GenericData>
  {
    public override Guid ComponentGuid
      => new Guid("0f251f87-317b-4669-bc70-22b29d3eba6a");
  }

#endif
}

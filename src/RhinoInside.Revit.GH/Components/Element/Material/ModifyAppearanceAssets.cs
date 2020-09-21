using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.GH.Parameters;
using RhinoInside.Revit.GH.Components.Element.Material;
using DB = Autodesk.Revit.DB;


namespace RhinoInside.Revit.GH.Components
{
#if REVIT_2019
  public abstract class ModifyAppearanceAssets<T>
: BaseAssetComponent<T> where T : ShaderData, new()
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public ModifyAppearanceAssets() : base()
    {
      this.Name = $"Modify {ComponentInfo.Name}";
      this.NickName = $"M-{ComponentInfo.NickName}";
      this.Description = $"Modify given {ComponentInfo.Description}";
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
      List<ParamDefinition> inputs = new List<ParamDefinition>();

      var param = new Parameters.AppearanceAsset();
      param.Name = ComponentInfo.Name;
      param.NickName = ComponentInfo.NickName;
      param.Description = ComponentInfo.Description;
      param.Access = GH_ParamAccess.item;

      inputs.Add(ParamDefinition.FromParam(param));
      inputs.AddRange(
        base.GetAssetDataAsInputs(skipUnchangable: true)
        );
      return inputs.ToArray();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var appearanceAsset = default(DB.AppearanceAssetElement);
      if (DA.GetData(ComponentInfo.Name, ref appearanceAsset))
      {
        // lets process all the inputs into a data structure
        // this step also verifies the input data
        var assetData = CreateAssetDataFromInputs(DA);


        var doc = Revit.ActiveDBDocument;
        using (var transaction = NewTransaction(doc))
        {
          transaction.Start();

          // update the asset parameters
          // update asset properties
          UpdateAssetElementFromInputs(appearanceAsset, assetData);

          transaction.Commit();
        }

        // send it to output
        DA.SetData(ComponentInfo.Name, appearanceAsset);
      }
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

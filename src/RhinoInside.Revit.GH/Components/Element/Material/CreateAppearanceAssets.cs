using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External;
using RhinoInside.Revit.External.DB;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
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

    protected override ParamDefinition[] Inputs => GetAssetDataAsInputs();
    protected override ParamDefinition[] Outputs => new ParamDefinition[]
    {
      ParamDefinition.Create<Parameters.AppearanceAsset>(
        name: ComponentInfo.Name,
        nickname: ComponentInfo.NickName,
        description: ComponentInfo.Description,
        access: GH_ParamAccess.item
        ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // lets process all the inputs into a data structure
      // this step also verifies the input data
      var assetData = CreateAssetDataFromInputs(DA);

      if (assetData.Name is null || assetData.Name == string.Empty)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Bad Name");
        return;
      }

      var doc = Revit.ActiveDBDocument;
      using (var transaction = NewTransaction(doc))
      {
        transaction.Start();

        var assetElement = EnsureThisAsset(doc, assetData.Name);
        if (assetElement is null)
        {
          transaction.RollBack();
          return;
        }

        // update asset properties
        UpdateAssetElementFromInputs(assetElement, assetData);

        DA.SetData(
          ComponentInfo.Name,
          new Types.AppearanceAsset(assetElement)
        );

        transaction.Commit();
      }
    }
  }

  public class CreateGenericShader
    : CreateAppearanceAsset<GenericData>
  {
    public override Guid ComponentGuid
      => new Guid("0f251f87-317b-4669-bc70-22b29d3eba6a");
  }
}

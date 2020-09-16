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
    : AssetComponent<T> where T : ShaderData, new()
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public CreateAppearanceAsset() : base()
    {
      this.Name = $"Create {ComponentInfo.Name}";
      this.NickName = $"C-{ComponentInfo.NickName}";
      this.Description = $"Create a new instance of {ComponentInfo.Description} inside document";
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
      => SetFieldsAsInputs(pManager);

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(
        param: new Parameters.AppearanceAsset(),
        name: ComponentInfo.Name,
        nickname: ComponentInfo.NickName,
        description: ComponentInfo.Description,
        access: GH_ParamAccess.item
      );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      T assetInfo = CreateAssetDataFromInputs(DA);

      var doc = Revit.ActiveDBDocument;

      // create new doc asset
      var assetElement = EnsureAsset(assetInfo.Schema, doc, assetInfo.Name);

      // open asset for editing
      var scope = new DB.Visual.AppearanceAssetEditScope(doc);
      var editableAsset = scope.Start(assetElement.Id);

      // commit the changes after all changes has been made
      scope.Commit(true);

      // and send it out
      DA.SetData("Appearance Asset", new Types.AppearanceAsset(assetElement));
    }
  }

  public class CreateGenericShader
    : CreateAppearanceAsset<GenericData>
  {
    public override Guid ComponentGuid
      => new Guid("0f251f87-317b-4669-bc70-22b29d3eba6a");
  }
}

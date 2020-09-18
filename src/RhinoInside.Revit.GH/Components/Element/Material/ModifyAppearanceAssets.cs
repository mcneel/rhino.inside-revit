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
using DB = Autodesk.Revit.DB;


namespace RhinoInside.Revit.GH.Components.Element.Material
{
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

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(
        param: new Parameters.AppearanceAsset(),
        name: ComponentInfo.Name,
        nickname: ComponentInfo.NickName,
        description: ComponentInfo.Description,
        access: GH_ParamAccess.item
      );

      SetFieldsAsInputs(pManager);
    }

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
      var appearanceAsset = default(DB.AppearanceAssetElement);
      if (DA.GetData(ComponentInfo.Name, ref appearanceAsset))
      {
        var asset = appearanceAsset.GetRenderingAsset();
        if (asset != null)
        {
          // TODO: modify asset here
        }
      }
    }
  }

  public class ModifyGenericShader
  : ModifyAppearanceAssets<GenericData>
  {
    public override Guid ComponentGuid =>
      new Guid("73b2376b-d6c8-4095-b7ce-a66ef4c931e6");
  }
}

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
      Name = $"Create {ComponentInfo.Name}";
      NickName = $"C-{ComponentInfo.NickName}";
      Description = $"Create a new instance of {ComponentInfo.Description} inside document";
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
      => DA.SetData(
        ComponentInfo.Name,
        new Types.AppearanceAsset(CreateAssetFromInputs(DA))
        );
  }

  public class CreateGenericShader
    : CreateAppearanceAsset<GenericData>
  {
    public override Guid ComponentGuid
      => new Guid("0f251f87-317b-4669-bc70-22b29d3eba6a");
  }
}

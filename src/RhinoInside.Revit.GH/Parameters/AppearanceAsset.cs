using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.GUI;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class AppearanceAsset : ElementIdWithoutPreviewParam<Types.AppearanceAsset, DB.AppearanceAssetElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("941b2ee3-5423-4fee-9df6-27c77fdb53c9");

    public AppearanceAsset() : base(
      "Shader Asset",
      "APAST",
      "Represents a Revit Shader (Appearance) Asset",
      "Params",
      "Revit Primitives"
      ) { }
  }
}

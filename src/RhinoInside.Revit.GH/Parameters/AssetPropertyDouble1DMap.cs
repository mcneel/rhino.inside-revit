using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using DBX = RhinoInside.Revit.External.DB;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
#if REVIT_2019
  public class AssetPropertyDouble1DMap : GH_Param<Types.AssetPropertyDouble1DMap>
  {
    public override Guid ComponentGuid
      => new Guid("49a94c44-26ec-4ee8-b9e7-37581968c3bf");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override System.Drawing.Bitmap Icon
      => ((System.Drawing.Bitmap)
          Properties.Resources.ResourceManager.GetObject(GetType().Name))
      ?? ImageBuilder.BuildIcon("D1D");

    public AssetPropertyDouble1DMap() : base(
      name: "AssetPropertyDouble1DMap",
      nickname: "AssetPropertyDouble1DMap",
      description: "Represents an asset property that can be connected to a texture map as well",
      category: "Params",
      subcategory: "Revit Primitives",
      access: GH_ParamAccess.item
      )
    { }
  }
#endif
}

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
  public class AssetPropertyDouble4DMap : GH_Param<Types.AssetPropertyDouble4DMap>
  {
    public override Guid ComponentGuid => new Guid("c2fc2e60-0336-465a-9ff0-1afc4b65d10d");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override System.Drawing.Bitmap Icon
      => ((System.Drawing.Bitmap)
          Properties.Resources.ResourceManager.GetObject(GetType().Name))
      ?? ImageBuilder.BuildIcon("D4D");

    public AssetPropertyDouble4DMap() : base(
      name: "AssetPropertyDouble4DMap",
      nickname: "AssetPropertyDouble4DMap",
      description: "Represents an asset property that can be connected to a texture map as well",
      category: "Params",
      subcategory: "Revit Primitives",
      access: GH_ParamAccess.item
      )
    { }
  }
#endif
}

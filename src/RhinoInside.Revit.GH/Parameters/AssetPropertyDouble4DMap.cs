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
#if REVIT_2018
  public class AssetPropertyDouble4DMap : GH_Param<Types.AssetPropertyDouble4DMap>
  {
    public override Guid ComponentGuid => new Guid("C2FC2E60-0336-465A-9FF0-1AFC4B65D10D");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override System.Drawing.Bitmap Icon
      => ((System.Drawing.Bitmap)
          Properties.Resources.ResourceManager.GetObject(GetType().Name))
      ?? ImageBuilder.BuildIcon("D4D");

    public AssetPropertyDouble4DMap() : base
    (
      name: "AssetPropertyDouble4DMap",
      nickname: "AssetPropertyDouble4DMap",
      description: "Contains a collection of Revit 4D appearance asset properties",
      category: "Params",
      subcategory: "Revit Primitives",
      access: GH_ParamAccess.item
    )
    { }
  }
#endif
}

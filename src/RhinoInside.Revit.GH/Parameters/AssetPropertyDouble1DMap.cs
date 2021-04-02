using System;

using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
#if REVIT_2018
  public class AssetPropertyDouble1DMap : GH_Param<Types.AssetPropertyDouble1DMap>
  {
    public override Guid ComponentGuid => new Guid("49A94C44-26EC-4EE8-B9E7-37581968C3BF");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override System.Drawing.Bitmap Icon
      => ((System.Drawing.Bitmap)
          Properties.Resources.ResourceManager.GetObject(GetType().Name))
      ?? ImageBuilder.BuildIcon("D1D");

    public AssetPropertyDouble1DMap() : base
    (
      name: "AssetPropertyDouble1DMap",
      nickname: "AssetPropertyDouble1DMap",
      description: "Contains a collection of Revit 1D appearance asset properties",
      category: "Params",
      subcategory: "Revit Primitives",
      access: GH_ParamAccess.item
    )
    { }
  }
#endif
}

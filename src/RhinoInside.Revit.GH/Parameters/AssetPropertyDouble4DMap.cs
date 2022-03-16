using System;

using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
#if REVIT_2018
  public class AssetPropertyDouble4DMap : Param<Types.AssetPropertyDouble4DMap>
  {
    public override Guid ComponentGuid => new Guid("C2FC2E60-0336-465A-9FF0-1AFC4B65D10D");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override string IconTag => "D4D";

    public AssetPropertyDouble4DMap() : base
    (
      name: "Asset Property Double 4D Map",
      nickname: "Asset Property Double 4D Map",
      description: "Contains a collection of Revit 4D appearance asset properties",
      category: "Params",
      subcategory: "Revit"
    )
    { }
  }
#endif
}

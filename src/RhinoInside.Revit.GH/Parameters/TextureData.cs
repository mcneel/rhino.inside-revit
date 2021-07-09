using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
#if REVIT_2018
  public class TextureData : GH_Param<Types.TextureData>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("a7c7ecef-066d-4b39-b2e8-01b6d53adfeb");
    protected override Bitmap Icon => (Bitmap) Properties.Resources.ResourceManager.
                                      GetObject(nameof(Components.Material.TextureData));

    public TextureData() : base
    (
      name: "TextureData",
      nickname: "TextureData",
      description: "Wraps Types.TextureData",
      category: string.Empty,
      subcategory: string.Empty,
      access: GH_ParamAccess.item
    )
    { }
  }
#endif
}

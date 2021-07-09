using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
#if REVIT_2018
  public class TextureData : Param<Types.TextureData>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("a7c7ecef-066d-4b39-b2e8-01b6d53adfeb");
    protected override string IconTag => "T";

    public TextureData() : base
    (
      name: "Texture Data",
      nickname: "Texture Data",
      description: "Wraps Types.TextureData",
      category: "Params",
      subcategory: "Revit Primitives"
    )
    { }
  }
#endif
}

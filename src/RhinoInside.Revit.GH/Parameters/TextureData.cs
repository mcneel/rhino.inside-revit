using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.Kernel;

using MAT = RhinoInside.Revit.GH.Components.Element.Material;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  class TextureData: GH_Param<Types.TextureData>
  {
    public override Guid ComponentGuid
      => new Guid("a7c7ecef-066d-4b39-b2e8-01b6d53adfeb");

    protected override Bitmap Icon
      => (Bitmap) Properties.Resources.ResourceManager
                            .GetObject(typeof(MAT.TextureData).Name);

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public TextureData() : base(
      name: "TextureData",
      nickname: "TextureData",
      description: "Wraps Types.TextureData",
      category: string.Empty,
      subcategory: string.Empty,
      access: GH_ParamAccess.item)
    { }
  }
}

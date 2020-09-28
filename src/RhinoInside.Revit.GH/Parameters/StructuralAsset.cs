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
#if REVIT_2019
  public class StructuralAsset : ElementIdWithoutPreviewParam<Types.StructuralAsset, DB.PropertySetElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid =>
      new Guid("dde6da63-87bc-4250-9455-5233bfad8683");

    public StructuralAsset() : base(
      "Physical Asset",
      "PHAST",
      "Represents a Revit Physical (Structural) Asset",
      "Params",
      "Revit Primitives"
      )
    { }
  }
#endif
}

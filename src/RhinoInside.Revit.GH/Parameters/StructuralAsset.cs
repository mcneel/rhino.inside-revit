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
  public class StructuralAsset : ElementIdWithoutPreviewParam<Types.PhysicalAsset, DB.PropertySetElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid =>
      new Guid("dde6da63-87bc-4250-9455-5233bfad8683");

    public StructuralAsset() : base(
      "Physical Asset",
      "PHAST",
      "Represents a Revit Physical Asset",
      "Params",
      "Revit Primitives"
      )
    { }
  }
}

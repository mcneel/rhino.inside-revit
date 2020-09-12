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
  public class ThermalAsset : ElementIdWithoutPreviewParam<Types.ThermalAsset, DB.PropertySetElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("b0a6689a-f2cd-4360-980c-d61a1f0c0453");

    public ThermalAsset() : base(
      "Thermal Asset",
      "THAST",
      "Represents a Revit Thermal Asset",
      "Params",
      "Revit Primitives"
      )
    { }
  }
}

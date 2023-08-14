using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.14")]
  public class AreaScheme : Element<Types.AreaScheme, ARDB.AreaScheme>
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("B042539F-21FB-4BB3-BCA8-F93708CD140E");

    public AreaScheme() : base
    (
      name: "Area Scheme",
      nickname: "A-Scheme",
      description: "Contains a collection of Revit area scheme elements",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }
  }
}

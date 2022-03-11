using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Family : Element<Types.Family, ARDB.Family>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary;
    public override Guid ComponentGuid => new Guid("3966ADD8-07C0-43E7-874B-6EFF95598EB0");

    public Family() : base("Family", "Family", "Contains a collection of Revit family elements", "Params", "Revit Primitives") { }
  }
}

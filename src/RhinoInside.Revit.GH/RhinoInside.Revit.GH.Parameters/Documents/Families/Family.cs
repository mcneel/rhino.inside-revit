using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using RhinoInside.Revit.GH.Parameters.Elements;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Documents.Families
{
  public class Family : ElementIdNonGeometryParam<Types.Documents.Families.Family, DB.Family>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("3966ADD8-07C0-43E7-874B-6EFF95598EB0");

    public Family() : base("Family", "Family", "Represents a Revit document family.", "Params", "Revit") { }
  }
}

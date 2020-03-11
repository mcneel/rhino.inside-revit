using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Elements.Grid
{
  public class Grid : GeometricElementT<Types.Elements.Grid.Grid, DB.Grid>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("7D2FB886-A184-41B8-A7D6-A6FDB85CF4E4");

    public Grid() : base("Grid", "Grid", "Represents a Revit document grid.", "Params", "Revit") { }
  }
}

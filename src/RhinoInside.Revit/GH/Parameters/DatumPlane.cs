using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Level : GeometricElementT<Types.Level, DB.Level>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("3238F8BC-8483-4584-B47C-48B4933E478E");

    public Level() : base("Level", "Level", "Represents a Revit document level.", "Params", "Revit") { }
  }

  public class Grid : GeometricElementT<Types.Grid, DB.Grid>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("7D2FB886-A184-41B8-A7D6-A6FDB85CF4E4");

    public Grid() : base("Grid", "Grid", "Represents a Revit document grid.", "Params", "Revit") { }
  }
}

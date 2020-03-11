using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.GH.Types.Elements.Level;
using RhinoInside.Revit.GH.Parameters.Elements.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Elements.Level
{
  public class Level : GeometricElementT<Types.Elements.Level.Level, DB.Level>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("3238F8BC-8483-4584-B47C-48B4933E478E");

    public Level() : base("Level", "Level", "Represents a Revit document level.", "Params", "Revit") { }
  }
}

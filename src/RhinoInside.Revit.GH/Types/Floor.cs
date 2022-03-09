using System;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Floor")]
  public class Floor : HostObject, ISketchAccess
  {
    protected override Type ValueType => typeof(ARDB.Floor);
    public new ARDB.Floor Value => base.Value as ARDB.Floor;

    public Floor() { }
    public Floor(ARDB.Floor floor) : base(floor) { }

    #region Location
    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Floor floor && Sketch is Sketch sketch)
        {
          var plane = sketch.ProfilesPlane;

          var center = plane.Origin;
          if (floor.Document.GetElement(floor.LevelId) is ARDB.Level level)
            center.Z = level.GetHeight() * Revit.ModelUnits;

          center.Z += Revit.ModelUnits * floor.get_Parameter(ARDB.BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)?.AsDouble() ?? 0.0;

          plane.Origin = center;
          return plane;
        }

        return base.Location;
      }
    }
    #endregion

    #region ISketchAccess
    public Sketch Sketch => Value is ARDB.Floor floor ?
      new Sketch(floor.GetSketch()) : default;
    #endregion
  }
}

using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Floor")]
  public class Floor : HostObject, ISketchAccess
  {
    protected override Type ValueType => typeof(ARDB.Floor);
    public new ARDB.Floor Value => base.Value as ARDB.Floor;

    public Floor() { }
    public Floor(ARDB.Floor floor) : base(floor) { }

    public double? LevelOffset =>
      Value?.get_Parameter(ARDB.BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).AsDouble() * Revit.ModelUnits;

    #region Location
    public override Plane Location
    {
      get
      {
        if (Sketch is Sketch sketch)
        {
          var plane = sketch.ProfilesPlane;

          var center = plane.Origin;
          center.Z = Level.Elevation + LevelOffset.Value;
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

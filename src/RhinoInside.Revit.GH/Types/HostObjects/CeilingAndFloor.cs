using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  public abstract class CeilingAndFloor : HostObject, ISketchAccess
  {
    protected override Type ValueType => typeof(ARDB.CeilingAndFloor);
    public new ARDB.CeilingAndFloor Value => base.Value as ARDB.CeilingAndFloor;

    public CeilingAndFloor() { }
    public CeilingAndFloor(ARDB.CeilingAndFloor element) : base(element) { }

    #region ISketchAccess
    public Sketch Sketch => GetElement<Sketch>(Value?.GetSketchId());
    #endregion

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

    public abstract double? LevelOffset { get; }
    #endregion
  }
}

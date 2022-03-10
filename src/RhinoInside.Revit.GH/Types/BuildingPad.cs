using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Building Pad")]
  public class BuildingPad : HostObject, ISketchAccess
  {
    protected override Type ValueType => typeof(ARDB.Architecture.BuildingPad);
    public new ARDB.Architecture.BuildingPad Value => base.Value as ARDB.Architecture.BuildingPad;

    public BuildingPad() { }
    public BuildingPad(ARDB.Architecture.BuildingPad floor) : base(floor) { }

    public double? LevelOffset =>
      Value?.get_Parameter(ARDB.BuiltInParameter.BUILDINGPAD_HEIGHTABOVELEVEL_PARAM).AsDouble() * Revit.ModelUnits;

    #region Location
    public override Plane Location
    {
      get
      {
        if (Sketch is Sketch sketch)
        {
          var plane = sketch.ProfilesPlane;

          var center = plane.Origin;
          center.Z = Level.Height + LevelOffset.Value;
          plane.Origin = center;

          return plane;
        }

        return base.Location;
      }
    }
    #endregion

    #region ISketchAccess
    public Sketch Sketch => Value is ARDB.Architecture.BuildingPad pad ?
      new Sketch(pad.GetSketch()) : default;
    #endregion
  }
}

using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Ceiling")]
  public class Ceiling : HostObject, ISketchAccess
  {
    protected override Type ValueType => typeof(ARDB.Ceiling);
    public new ARDB.Ceiling Value => base.Value as ARDB.Ceiling;

    public Ceiling() { }
    public Ceiling(ARDB.Ceiling ceiling) : base(ceiling) { }

    public double? LevelOffset =>
      Value?.get_Parameter(ARDB.BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM).AsDouble() * Revit.ModelUnits;

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
    public Sketch Sketch => Value is ARDB.Ceiling ceiling ?
      new Sketch(ceiling.GetSketch()) : default;
    #endregion
  }
}

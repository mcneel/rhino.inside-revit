using System;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Ceiling")]
  public class Ceiling : HostObject, ISketchAccess
  {
    protected override Type ValueType => typeof(ARDB.Ceiling);
    public new ARDB.Ceiling Value => base.Value as ARDB.Ceiling;

    public Ceiling() { }
    public Ceiling(ARDB.Ceiling ceiling) : base(ceiling) { }

    #region Location
    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Ceiling ceiling && Sketch is Sketch sketch)
        {
          var plane = sketch.ProfilesPlane;

          var center = plane.Origin;
          if (ceiling.Document.GetElement(ceiling.LevelId) is ARDB.Level level)
            center.Z = level.GetHeight() * Revit.ModelUnits;

          center.Z += Revit.ModelUnits * ceiling.get_Parameter(ARDB.BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM)?.AsDouble() ?? 0.0;

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

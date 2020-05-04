using System;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class CurtainGridMullion : Element
  {
    public override string TypeDescription => "Represents a Revit Curtain Grid Mullion Element";
    protected override Type ScriptVariableType => typeof(DB.Mullion);
    public static explicit operator DB.Mullion(CurtainGridMullion self) =>
      self.Document?.GetElement(self) as DB.Mullion;

    public CurtainGridMullion() { }
    public CurtainGridMullion(DB.Mullion mullion) : base(mullion) { }

    public override Rhino.Geometry.Curve Axis
    {
      get
      {
        var mullion = (DB.Mullion) this;
        var axisCurve = mullion?.LocationCurve?.ToCurve();

        // .LocationCurve might be null so let's return a zero-length curve for those
        // place the curve at mullion base point
        var basepoint = ((DB.LocationPoint) mullion.Location).Point.ToPoint3d();
        Rhino.Geometry.Curve zeroLengthCurve = new Rhino.Geometry.Line(basepoint, basepoint).ToNurbsCurve();

        return axisCurve ?? zeroLengthCurve;
      }
    }
  }
}

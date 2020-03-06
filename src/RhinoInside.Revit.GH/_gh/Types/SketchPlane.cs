using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class SketchPlane : Element
  {
    public override string TypeName => "Revit Sketch Plane";
    public override string TypeDescription => "Represents a Revit sketch plane";
    protected override Type ScriptVariableType => typeof(DB.SketchPlane);
    public static explicit operator DB.SketchPlane(SketchPlane self) =>
      self.Document?.GetElement(self) as DB.SketchPlane;

    public SketchPlane() : base() { }
    public SketchPlane(DB.SketchPlane sketchPlane) : base(sketchPlane) { }

    #region Location
    public override Rhino.Geometry.Point3d Location => Plane.Origin;
    public override Rhino.Geometry.Vector3d XAxis => Plane.XAxis;
    public override Rhino.Geometry.Vector3d YAxis => Plane.YAxis;
    public override Rhino.Geometry.Vector3d ZAxis => Plane.ZAxis;
    public override Rhino.Geometry.Plane Plane
    {
      get
      {
        var element = (Autodesk.Revit.DB.SketchPlane) this;
        if (element != null)
          return element.GetPlane().ToRhino().ChangeUnits(Revit.ModelUnits);

        return new Rhino.Geometry.Plane(new Rhino.Geometry.Point3d(double.NaN, double.NaN, double.NaN), Rhino.Geometry.Vector3d.Zero, Rhino.Geometry.Vector3d.Zero);
      }
    }
    #endregion
  }
}

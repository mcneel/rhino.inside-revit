using System;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Convert.Geometry;
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

    public override bool CastFrom(object source)
    {
      var value = source;

      if (source is IGH_Goo goo)
        value = goo.ScriptVariable();

      if (value is DB.View view)
        return view.SketchPlane is null ? false : SetValue(view.SketchPlane);

      return base.CastFrom(source);
    }

    #region Location
    public override Rhino.Geometry.Point3d Origin => Plane.Origin;
    public override Rhino.Geometry.Vector3d XAxis => Plane.XAxis;
    public override Rhino.Geometry.Vector3d YAxis => Plane.YAxis;
    public override Rhino.Geometry.Vector3d ZAxis => Plane.ZAxis;
    public override Rhino.Geometry.Plane Plane
    {
      get
      {
        var element = (DB.SketchPlane) this;
        return element?.GetPlane().ToPlane() ??
          new Rhino.Geometry.Plane(new Rhino.Geometry.Point3d(double.NaN, double.NaN, double.NaN), Rhino.Geometry.Vector3d.Zero, Rhino.Geometry.Vector3d.Zero);
      }
    }
    #endregion
  }
}

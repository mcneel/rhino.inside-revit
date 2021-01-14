using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Sketch Plane")]
  public class SketchPlane : GraphicalElement
  {
    protected override Type ScriptVariableType => typeof(DB.SketchPlane);
    public static explicit operator DB.SketchPlane(SketchPlane value) => value?.Value;
    public new DB.SketchPlane Value => base.Value as DB.SketchPlane;

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

    public override BoundingBox GetBoundingBox(Transform xform) => BoundingBox.Unset;

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var location = Location;
      if (!location.IsValid)
        return;

      GH_Plane.DrawPlane(args.Pipeline, location, Grasshopper.CentralSettings.PreviewPlaneRadius, 4, args.Color, System.Drawing.Color.DarkRed, System.Drawing.Color.DarkGreen);
    }
    #endregion

    #region Location
    public override BoundingBox BoundingBox => BoundingBox.Unset;

    public override Plane Location => Value?.GetPlane().ToPlane() ?? base.Location;
    #endregion
  }
}

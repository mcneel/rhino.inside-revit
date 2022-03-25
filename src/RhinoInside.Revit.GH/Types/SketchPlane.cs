using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Sketch Plane")]
  public class SketchPlane : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.SketchPlane);
    public new ARDB.SketchPlane Value => base.Value as ARDB.SketchPlane;

    public SketchPlane() : base() { }
    public SketchPlane(ARDB.SketchPlane sketchPlane) : base(sketchPlane) { }

    public override bool CastFrom(object source)
    {
      var value = source;

      if (source is IGH_Goo goo)
        value = goo.ScriptVariable();

      if (value is ARDB.View view)
        return view.SketchPlane is null ? false : SetValue(view.SketchPlane);

      return base.CastFrom(source);
    }

    public override BoundingBox GetBoundingBox(Transform xform) => NaN.BoundingBox;

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
    public override Plane Location => Value?.GetPlane().ToPlane() ?? base.Location;
    #endregion
  }
}

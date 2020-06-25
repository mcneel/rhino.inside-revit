using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class SketchPlane : GraphicalElement
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

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var location = Location;
      if (!location.IsValid)
        return;

      GH_Plane.DrawPlane(args.Pipeline, Location, Grasshopper.CentralSettings.PreviewPlaneRadius, 4, args.Color, System.Drawing.Color.DarkRed, System.Drawing.Color.DarkGreen);
    }

    public override void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion

    #region Location
    public override Rhino.Geometry.Plane Location
    {
      get
      {
        var sketchPlane = (DB.SketchPlane) this;
        return sketchPlane?.GetPlane().ToPlane() ?? base.Location;
      }
    }
    #endregion
  }
}

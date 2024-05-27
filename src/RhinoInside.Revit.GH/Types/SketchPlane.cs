using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using ARDB_SketchPlaneGrid = ARDB.Element;

  [Kernel.Attributes.Name("Work Plane")]
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
        return SetValue(view.SketchPlane);

      if (value is ARDB.CurveElement curveElement)
        return SetValue(curveElement.SketchPlane);

      if (value is ARDB.DatumPlane datum)
        return SetValue(datum.GetSketchPlane());

      return base.CastFrom(source);
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      var location = Location;
      if (location.IsValid)
      {
        var radius = Grasshopper.CentralSettings.PreviewPlaneRadius;
        var origin = location.Origin;
        origin.Transform(xform);
        return new BoundingBox
        (
          origin - new Vector3d(radius, radius, radius),
          origin + new Vector3d(radius, radius, radius)
        );
      }

      return NaN.BoundingBox;
    }

    #region IGH_PreviewData
    protected override bool GetClippingBox(out BoundingBox clippingBox)
    {
      clippingBox = GetBoundingBox(Transform.Identity);
      return !clippingBox.IsValid;
    }

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
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

  [Kernel.Attributes.Name("Work Plane Grid")]
  public class SketchPlaneGrid : GeometricElement
  {
    protected override Type ValueType => typeof(ARDB_SketchPlaneGrid);
    public new ARDB_SketchPlaneGrid Value => base.Value as ARDB_SketchPlaneGrid;

    protected override bool SetValue(ARDB_SketchPlaneGrid element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB_SketchPlaneGrid element)
    {
      return element.GetType() == typeof(ARDB_SketchPlaneGrid) &&
             element.Category?.ToBuiltInCategory() == ARDB.BuiltInCategory.OST_IOSSketchGrid;
    }

    public SketchPlaneGrid() : base() { }
    public SketchPlaneGrid(ARDB_SketchPlaneGrid grid) : base(grid)
    {
      if (!IsValidElement(grid))
        throw new ArgumentException("Invalid Element", nameof(grid));
    }

    public override bool CastFrom(object source)
    {
      var value = source;

      if (source is IGH_Goo goo)
        value = goo.ScriptVariable();

      if (value is ARDB.View view)
        return SetValue(view.Document.GetElement(view.GetSketchGridId()));

      return base.CastFrom(source);
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      return base.GetBoundingBox(xform);
    }

    #region IGH_PreviewData
    protected override bool GetClippingBox(out BoundingBox clippingBox)
    {
      clippingBox = GetBoundingBox(Transform.Identity);
      return clippingBox.IsValid;
    }

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var location = Location;
      if (!location.IsValid)
        return;

      base.DrawViewportWires(args);
    }
    #endregion

    #region Location
    public override Plane Location => base.Location;
    #endregion

    #region Properties
    public double? GridSpacing
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.SKETCH_GRID_SPACING_PARAM)?.AsDouble() * Revit.ModelUnits;
    }
    #endregion
  }
}

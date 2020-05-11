using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.Geometry.Raw;
using RhinoInside.Revit.Convert.System.Drawing;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit
{
  /// <summary>
  /// This class is here to help port code that was previous calling Convert class extension methods
  /// </summary>
  public static partial class Convert_OBSOLETE
  {
    #region ToHost
    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.System.Drawing namespace")]
    public static DB.Color ToHost(this System.Drawing.Color c) => ColorConverter.ToColor(c);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawEncoder.ToHost\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static DB.XYZ ToHost(this Point3f p) => p.ToXYZ(UnitConverter.NoScale);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawEncoder.ToHost\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static DB.XYZ ToHost(this Point3d p) => p.ToXYZ(UnitConverter.NoScale);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawEncoder.ToHost\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static DB.XYZ ToHost(this Vector3f p) => p.ToXYZ(UnitConverter.NoScale);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawEncoder.ToHost\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static DB.XYZ ToHost(this Vector3d p) => p.ToXYZ(UnitConverter.NoScale);

    [Obsolete("\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static DB.Line ToHost(this Line line) => line.ToLine(UnitConverter.NoScale);

    [Obsolete("\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static IEnumerable<DB.Line> ToHostMultiple(this Polyline polyline) => polyline.ToLines(UnitConverter.NoScale);

    [Obsolete("\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static DB.Arc ToHost(this Arc arc) => arc.ToArc(UnitConverter.NoScale);

    [Obsolete("\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static DB.Arc ToHost(this Circle circle) => circle.ToArc(UnitConverter.NoScale);

    [Obsolete("\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static DB.Curve ToHost(this Ellipse ellipse) => ellipse.ToCurve(new Interval(0.0, 2.0 * Math.PI * 2.0), UnitConverter.NoScale);
    public static DB.Curve ToHost(this Ellipse ellipse, Interval interval) => ellipse.ToCurve(interval, UnitConverter.NoScale);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawEncoder.ToHost\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static DB.Plane ToHost(this Plane plane) => plane.ToPlane(UnitConverter.NoScale);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawEncoder.ToHost\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static DB.Transform ToHost(this Transform transform) => transform.ToTransform(UnitConverter.NoScale);

    [Obsolete("\rThis method will be removed")]
    public static IEnumerable<DB.XYZ> ToHost(this IEnumerable<Point3d> points) => points.Select(p => RawEncoder.AsXYZ(p));

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawEncoder.ToHost\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static DB.Point ToHost(this Point point) => point.ToPoint(UnitConverter.NoScale);

    [Obsolete("\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static IEnumerable<DB.Point> ToHostMultiple(this PointCloud pointCloud) => pointCloud.ToPointMany(UnitConverter.NoScale);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawEncoder.ToHost\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static DB.Line ToHost(this LineCurve curve) => RawEncoder.ToHost(curve);

    [Obsolete("\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static DB.Arc ToHost(this ArcCurve curve) => RawEncoder.ToHost(curve);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawEncoder.ToHost\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static DB.Curve ToHost(this NurbsCurve curve) => curve.ToCurve(UnitConverter.NoScale);

    [Obsolete("\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static DB.Curve ToHost(this Curve curve) => curve.ToCurve(UnitConverter.NoScale);

    [Obsolete("\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static IEnumerable<DB.Curve> ToHostMultiple(this Curve curve) => curve.ToCurveMany(UnitConverter.NoScale);

    [Obsolete("\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static DB.Solid ToHost(this Brep brep) => brep.ToSolid(UnitConverter.NoScale);

    [Obsolete("\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static IEnumerable<DB.GeometryObject> ToHostMultiple(this Brep brep) => brep.ToShape(UnitConverter.NoScale);

    [Obsolete("\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static IEnumerable<DB.GeometryObject> ToHostMultiple(this Mesh mesh) => mesh.ToShape(UnitConverter.NoScale);

    [Obsolete("\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryEncoder extension methods")]
    public static IEnumerable<DB.GeometryObject> ToHostMultiple(this GeometryBase geometry, double scaleFactor) => geometry.ToShape(scaleFactor);

    [Obsolete("\rThis method will be removed")]
    public static IEnumerable<IList<DB.GeometryObject>> ToHost(this IEnumerable<GeometryBase> geometries)
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;
      return geometries.Select(x => x.ToShape(scaleFactor)).Where(x => x.Length > 0);
    }
    #endregion
  }
}

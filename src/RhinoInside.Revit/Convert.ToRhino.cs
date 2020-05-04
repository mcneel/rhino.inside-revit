using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Units;
using RhinoInside.Revit.Convert.Display;
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
    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Units namespace")]
    public static Rhino.UnitSystem ToRhinoLengthUnits(this DB.DisplayUnitType value) => value.ToUnitSystem();

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.System.Drawing namespace")]
    public static System.Drawing.Color ToRhino(this DB.Color c) => ColorConverter.ToColor(c);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Display namespace")]
    public static Rhino.Display.DisplayMaterial ToRhino(this DB.Material material, Rhino.Display.DisplayMaterial parentMaterial) => material.ToDisplayMaterial(parentMaterial);

    #region Geometry values
    [Obsolete("\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static Point3d ToRhino(this DB.XYZ p) => RawDecoder.ToRhino(p);

    [Obsolete("\rThis method will be removed")]
    public static IEnumerable<Point3d> ToRhino(this IEnumerable<DB.XYZ> points) => points.Select(x => RawDecoder.ToRhino(x));

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static BoundingBox ToRhino(this DB.BoundingBoxXYZ bbox) => RawDecoder.ToRhino(bbox);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static Transform ToRhino(this DB.Transform transform) => RawDecoder.ToRhino(transform);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static Point ToRhino(this DB.Point point) => RawDecoder.ToRhino(point);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static Plane ToRhino(this DB.Plane plane) => RawDecoder.ToRhino(plane);
    #endregion

    #region Curves
    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static LineCurve ToRhino(this DB.Line line) => RawDecoder.ToRhino(line);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static ArcCurve ToRhino(this DB.Arc arc) => RawDecoder.ToRhino(arc);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static NurbsCurve ToRhino(this DB.Ellipse ellipse) => RawDecoder.ToRhino(ellipse);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static NurbsCurve ToRhino(this DB.HermiteSpline hermite) => RawDecoder.ToRhino(hermite);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static NurbsCurve ToRhino(this DB.NurbSpline nurb) => RawDecoder.ToRhino(nurb);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static NurbsCurve ToRhino(this DB.CylindricalHelix helix) => RawDecoder.ToRhino(helix);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static Curve ToRhino(this DB.Curve curve) => RawDecoder.ToRhino(curve);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static PolylineCurve ToRhino(this DB.PolyLine polyline) => RawDecoder.ToRhino(polyline);


    /// <summary>
    /// Convert array of curve arrays into a list of Rhino curves
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<IEnumerable<Curve>> ToRhino(this DB.CurveArrArray curveArrayArray)
    {
      var curveLists = new List<List<Curve>>();
      foreach (DB.CurveArray curveArray in curveArrayArray)
      {
        var curves = new List<Curve>();
        foreach (DB.Curve c in curveArray)
          curves.Add(c.ToCurve());
        curveLists.Add(curves);
      }
      return curveLists;
    }
    #endregion

    #region Surfaces
    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static PlaneSurface ToRhinoSurface(this DB.PlanarFace face, double relativeTolerance) => RawDecoder.ToRhinoSurface(face, relativeTolerance);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static PlaneSurface ToRhino(this DB.Plane surface, DB.BoundingBoxUV bboxUV) => RawDecoder.ToRhino(surface, bboxUV);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static RevSurface ToRhinoSurface(this DB.ConicalFace face, double relativeTolerance) => RawDecoder.ToRhinoSurface(face, relativeTolerance);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static RevSurface ToRhino(this DB.ConicalSurface surface, DB.BoundingBoxUV bboxUV) => RawDecoder.ToRhino(surface, bboxUV);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static RevSurface ToRhinoSurface(this DB.CylindricalFace face, double relativeTolerance) => RawDecoder.ToRhinoSurface(face, relativeTolerance);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static RevSurface ToRhino(this DB.CylindricalSurface surface, DB.BoundingBoxUV bboxUV) => RawDecoder.ToRhino(surface, bboxUV);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static RevSurface ToRhinoSurface(this DB.RevolvedFace face, double relativeTolerance) => RawDecoder.ToRhinoSurface(face, relativeTolerance);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static RevSurface ToRhino(this DB.RevolvedSurface surface, DB.BoundingBoxUV bboxUV) => RawDecoder.ToRhino(surface, bboxUV);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static Surface ToRhinoSurface(this DB.RuledFace face, double relativeTolerance) => RawDecoder.ToRhinoSurface(face, relativeTolerance);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static Surface ToRhino(this DB.RuledSurface surface, DB.BoundingBoxUV bboxUV) => RawDecoder.ToRhino(surface, bboxUV);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static NurbsSurface ToRhinoSurface(this DB.HermiteFace face, double relativeTolerance) => RawDecoder.ToRhinoSurface(face, relativeTolerance);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static NurbsSurface ToRhino(this DB.NurbsSurfaceData surface, DB.BoundingBoxUV bboxUV) => RawDecoder.ToRhino(surface, bboxUV);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static Surface ToRhinoSurface(this DB.Face face, double relativeTolerance = 0.0) => RawDecoder.ToRhinoSurface(face, relativeTolerance);
    #endregion

    #region Brep
    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static Brep ToRhino(this DB.Face face) => RawDecoder.ToRhino(face);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static Brep ToRhino(this DB.Solid solid) => RawDecoder.ToRhino(solid);
    #endregion

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.Raw.RawDecoder.ToRhino\r - For a full conversion including units consider RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static Mesh ToRhino(this DB.Mesh mesh) => RawDecoder.ToRhino(mesh);

    [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Geometry.GeometryDecoder extension methods")]
    public static IEnumerable<GeometryBase> ToRhino(this DB.GeometryObject geometry) => geometry.ToGeometryBaseMany();
  }
}

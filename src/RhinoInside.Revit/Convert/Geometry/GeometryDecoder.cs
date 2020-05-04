using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  using Raw;
  using RhinoInside.Revit.External.DB.Extensions;

  /// <summary>
  /// Methods in this class do a full geometry conversion.
  /// <para>It converts geometry from Revit internal units to Active Rhino model units.</para>
  /// <para>For direct conversion methods see <see cref="Raw.RawDecoder"/> class.</para>
  /// </summary>
  public static class GeometryDecoder
  {
    #region Geometry values
    public static Point3d ToPoint3d(this DB.XYZ value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }
    public static Vector3d ToVector3d(this DB.XYZ value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(ref rhino, UnitConverter.ToRhinoUnits); return (Vector3d) rhino; }

    public static Transform ToTransform(this DB.Transform value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }

    public static BoundingBox ToBoundingBox(this DB.BoundingBoxXYZ value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }

    public static Plane ToPlane(this DB.Plane value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }
    #endregion

    #region GeometryBase
    public static Point ToPoint(this DB.Point value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    public static Curve ToCurve(this DB.Line value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    public static Curve ToCurve(this DB.Arc value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    public static Curve ToCurve(this DB.Ellipse value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    public static Curve ToCurve(this DB.NurbSpline value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    public static Curve ToCurve(this DB.HermiteSpline value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    public static Curve ToCurve(this DB.CylindricalHelix value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    public static Curve ToCurve(this DB.Curve value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    public static PolylineCurve ToPolylineCurve(this DB.PolyLine value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    public static Brep ToBrep(this DB.Face value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    public static Brep ToBrep(this DB.Solid value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    public static Mesh ToMesh(this DB.Mesh value)
    { var rhino = RawDecoder.ToRhino(value); UnitConverter.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }
    #endregion

    /// <summary>
    /// Converts a <see cref="DB.CurveLoop"/> into a Rhino <see cref="Curve"/>
    /// </summary>
    public static Curve ToCurve(this DB.CurveLoop value)
    {
      if (value.NumberOfCurves() == 1)
        return value.First().ToCurve();

      var polycurve = new PolyCurve();

      foreach (var curve in value)
        polycurve.AppendSegment(curve.ToCurve());

      return polycurve;
    }

    /// <summary>
    /// Converts a <see cref="DB.CurveArray"/> into a Rhino <see cref="Curve"/>[]
    /// </summary>
    /// <seealso cref="ToPolyCurves(DB.CurveArrArray)"/>
    public static Curve[] ToCurves(this DB.CurveArray value)
    {
      var count = value.Size;
      var curves = new Curve[count];

      int index = 0;
      foreach (var curve in value.Cast<DB.Curve>())
        curves[index++] = curve.ToCurve();

      return curves;
    }

    /// <summary>
    /// Converts a <see cref="DB.CurveArrArray"/> into a <see cref="PolyCurve"/>[]
    /// </summary>
    /// <seealso cref="ToCurves(DB.CurveArrArray)"/>
    public static PolyCurve[] ToPolyCurves(this DB.CurveArrArray value)
    {
      var count = value.Size;
      var list = new PolyCurve[count];

      int index = 0;
      foreach (var curveArray in value.Cast<DB.CurveArray>())
      {
        var polycurve = new PolyCurve();

        foreach (var curve in curveArray.Cast<DB.Curve>())
          polycurve.AppendSegment(curve.ToCurve());

        list[index++] = polycurve;
      }

      return list;
    }

    public static IEnumerable<GeometryBase> ToGeometryBaseMany(this DB.GeometryObject geometry)
    {
      switch (geometry)
      {
        case DB.GeometryElement element:
        foreach (var g in element.SelectMany(x => x.ToGeometryBaseMany()))
            yield return g;

            yield break;
        case DB.GeometryInstance instance:
            var xform = ToTransform(instance.Transform);
            foreach (var g in instance.SymbolGeometry.ToGeometryBaseMany())
            {
                g?.Transform(xform);
                yield return g;
            }
            break;
        case DB.Mesh mesh:

            yield return mesh.ToMesh();
            yield break;
        case DB.Solid solid:

            yield return solid.ToBrep();
            yield break;
        case DB.Curve curve:

            yield return curve.ToCurve();
            yield break;
        case DB.PolyLine polyline:

            yield return polyline.ToPolylineCurve();
            yield break;
      }
    }
  }
}

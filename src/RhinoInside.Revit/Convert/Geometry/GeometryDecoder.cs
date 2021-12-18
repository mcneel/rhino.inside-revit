using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  using Raw;
  using RhinoInside.Revit.External.DB.Extensions;

  /// <summary>
  /// Converts a Revit geometry type to an equivalent Rhino geometry type.
  /// </summary>
  public static class GeometryDecoder
  {
    #region Context
    internal sealed class Context : State<Context>
    {
      public ARDB.Element Element = default;
      public ARDB.Visibility Visibility = ARDB.Visibility.Invisible;
      public ARDB.Category Category = default;
      public ARDB.Material Material = default;
      public ARDB.ElementId[] FaceMaterialId;
    }
    #endregion

    #region Points and Vectors
    /// <summary>
    /// Converts the specified UV point to an equivalent Rhino Point2d.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Point2d that is equivalent to the provided value.</returns>
    public static Point2d ToPoint2d(this ARDB.UV value)
    { var rhino = RawDecoder.AsPoint2d(value); UnitConvertible.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified UV vector to an equivalent Rhino Vector2d.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Vector2d that is equivalent to the provided value.</returns>
    public static Vector2d ToVector2d(this ARDB.UV value)
    { return new Vector2d(value.U, value.V); }

    /// <summary>
    /// Converts the specified XYZ point to an equivalent Rhino Point3d.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Point3d that is equivalent to the provided value.</returns>
    public static Point3d ToPoint3d(this ARDB.XYZ value)
    { var rhino = RawDecoder.AsPoint3d(value); UnitConvertible.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified XYZ vector to an equivalent Rhino Vector3d.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Vector3d that is equivalent to the provided value.</returns>
    public static Vector3d ToVector3d(this ARDB.XYZ value)
    { return new Vector3d(value.X, value.Y, value.Z); }

    /// <summary>
    /// Converts the specified Plane to an equivalent Rhino Plane.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Plane that is equivalent to the provided value.</returns>
    public static Plane ToPlane(this ARDB.Plane value)
    { var rhino = RawDecoder.AsPlane(value); UnitConvertible.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified Plane to an equivalent Rhino Transform.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Transform that is equivalent to the provided value.</returns>
    public static Transform ToTransform(this ARDB.Transform value)
    { var rhino = RawDecoder.AsTransform(value); UnitConvertible.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified BoundingBoxXYZ to an equivalent Rhino BoundingBox.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino BoundingBox that is equivalent to the provided value.</returns>
    public static BoundingBox ToBoundingBox(this ARDB.BoundingBoxXYZ value)
    { var rhino = RawDecoder.AsBoundingBox(value); UnitConvertible.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified BoundingBoxXYZ to an equivalent Rhino BoundingBox.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <param name="transform"></param>
    /// <returns>A Rhino BoundingBox that is equivalent to the provided value.</returns>
    public static BoundingBox ToBoundingBox(this ARDB.BoundingBoxXYZ value, out Transform transform)
    {
      var rhino = RawDecoder.AsBoundingBox(value, out transform);
      UnitConvertible.Scale(ref rhino, UnitConverter.ToRhinoUnits);
      UnitConvertible.Scale(ref transform, UnitConverter.ToRhinoUnits);
      return rhino;
    }

    internal static Interval[] ToIntervals(ARDB.BoundingBoxUV value)
    {
      return !value.IsUnset() ?
      new Interval[]
      {
        new Interval(value.Min.U, value.Max.U),
        new Interval(value.Min.V, value.Max.V)
      } :
      new Interval[]
      {
        NaN.Interval,
        NaN.Interval
      };
    }

    /// <summary>
    /// Converts the specified Outline to an equivalent Rhino BoundingBox.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino BoundingBox that is equivalent to the provided value.</returns>
    public static BoundingBox ToBoundingBox(this ARDB.Outline value)
    {
      return new BoundingBox(value.MinimumPoint.ToPoint3d(), value.MaximumPoint.ToPoint3d());
    }

    /// <summary>
    /// Converts the specified BoundingBoxXYZ to an equivalent Rhino Box.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Box that is equivalent to the provided value.</returns>
    public static Box ToBox(this ARDB.BoundingBoxXYZ value)
    {
      var rhino = RawDecoder.AsBoundingBox(value, out var transform);
      UnitConvertible.Scale(ref rhino, UnitConverter.ToRhinoUnits);
      UnitConvertible.Scale(ref transform, UnitConverter.ToRhinoUnits);

      return new Box
      (
        new Plane
        (
          origin :    new Point3d (transform.M03, transform.M13, transform.M23),
          xDirection: new Vector3d(transform.M00, transform.M10, transform.M20),
          yDirection: new Vector3d(transform.M01, transform.M11, transform.M21)
        ),
        xSize: new Interval(rhino.Min.X, rhino.Max.X),
        ySize: new Interval(rhino.Min.Y, rhino.Max.Y),
        zSize: new Interval(rhino.Min.Z, rhino.Max.Z)
      );
    }
    #endregion

    #region GeometryBase
    /// <summary>
    /// Converts the specified Point to an equivalent Rhino Point.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Point that is equivalent to the provided value.</returns>
    public static Point ToPoint(this ARDB.Point value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified Line to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    public static Curve ToCurve(this ARDB.Line value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified Arc to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    public static Curve ToCurve(this ARDB.Arc value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified Ellipse to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    public static Curve ToCurve(this ARDB.Ellipse value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified NurbSpline to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    public static Curve ToCurve(this ARDB.NurbSpline value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified HermiteSpline to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    public static Curve ToCurve(this ARDB.HermiteSpline value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified CylindricalHelix to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    public static Curve ToCurve(this ARDB.CylindricalHelix value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified Curve to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    public static Curve ToCurve(this ARDB.Curve value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified PolyLine to an equivalent Rhino PolylineCurve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino PolylineCurve that is equivalent to the provided value.</returns>
    public static PolylineCurve ToPolylineCurve(this ARDB.PolyLine value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified Face to an equivalent Rhino Brep.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Brep that is equivalent to the provided value.</returns>
    public static Brep ToBrep(this ARDB.Face value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified Solid to an equivalent Rhino Brep.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Brep that is equivalent to the provided value.</returns>
    public static Brep ToBrep(this ARDB.Solid value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified Mesh to an equivalent Rhino Mesh.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Mesh that is equivalent to the provided value.</returns>
    public static Mesh ToMesh(this ARDB.Mesh value) =>
      MeshDecoder.FromRawMesh(MeshDecoder.ToRhino(value), UnitConverter.ToRhinoUnits);
    #endregion

    /// <summary>
    /// Converts the specified CurveLoop to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    public static Curve ToCurve(this ARDB.CurveLoop value)
    {
      if (value.NumberOfCurves() == 1)
        return value.First().ToCurve();

      var polycurve = new PolyCurve();

      foreach (var curve in value)
        polycurve.AppendSegment(curve.ToCurve());

      if(!value.IsOpen())
        polycurve.MakeClosed(GeometryObjectTolerance.Model.VertexTolerance);

      return polycurve;
    }

    /// <summary>
    /// Converts the specified CurveArray to an equivalent Rhino Curve array.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve array that is equivalent to the provided value.</returns>
    public static Curve[] ToCurves(this ARDB.CurveArray value)
    {
      var count = value.Size;
      var curves = new Curve[count];

      int index = 0;
      foreach (var curve in value.Cast<ARDB.Curve>())
        curves[index++] = curve.ToCurve();

      return curves;
    }

    /// <summary>
    /// Converts the specified CurveArrArray to an equivalent PolyCurve array.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino PolyCurve array that is equivalent to the provided value.</returns>
    public static PolyCurve[] ToPolyCurves(this ARDB.CurveArrArray value)
    {
      var count = value.Size;
      var list = new PolyCurve[count];

      int index = 0;
      foreach (var curveArray in value.Cast<ARDB.CurveArray>())
      {
        var polycurve = new PolyCurve();

        foreach (var curve in curveArray.Cast<ARDB.Curve>())
          polycurve.AppendSegment(curve.ToCurve());

        polycurve.MakeClosed(GeometryObjectTolerance.Model.VertexTolerance);

        list[index++] = polycurve;
      }

      return list;
    }

    /// <summary>
    /// Update Context from <see cref="ARDB.GeometryObject"/> <paramref name="geometryObject"/>
    /// </summary>
    /// <param name="geometryObject"></param>
    internal static void UpdateGraphicAttributes(ARDB.GeometryObject geometryObject)
    {
      if (geometryObject is object)
      {
        var context = Context.Peek;
        if (context.Element is ARDB.Element element)
        {
          context.Visibility = geometryObject.Visibility;

          if (geometryObject is ARDB.GeometryInstance instance)
          {
            context.Element = instance.Symbol;
            context.Category = instance.Symbol.Category ?? context.Category;
            context.Material = context.Category?.Material;
          }
          else if (geometryObject is ARDB.GeometryElement geometry)
          {
            context.Material = geometry.MaterialElement;
          }
          else if (geometryObject is ARDB.Solid solid)
          {
            if (!solid.Faces.IsEmpty)
            {
              var faces = solid.Faces;
              var count = faces.Size;

              context.FaceMaterialId = new ARDB.ElementId[count];
              for (int f = 0; f < count; ++f)
                context.FaceMaterialId[f] = faces.get_Item(f).MaterialElementId;
            }
          }
          else if (geometryObject is ARDB.Mesh mesh)
          {
            context.FaceMaterialId = new ARDB.ElementId[] { mesh.MaterialElementId };
          }

          if (geometryObject.GraphicsStyleId != ARDB.ElementId.InvalidElementId)
            context.Category = (element.Document.GetElement(geometryObject.GraphicsStyleId) as ARDB.GraphicsStyle).GraphicsStyleCategory;
        }
      }
    }

    /// <summary>
    /// Set graphic attributes to <see cref="GeometryBase"/> <paramref name="geometry"/> from Context
    /// </summary>
    /// <param name="geometry"></param>
    /// <returns><paramref name="geometry"/> with graphic attributes</returns>
    static GeometryBase SetGraphicAttributes(GeometryBase geometry)
    {
      if (geometry is object)
      {
        var context = Context.Peek;
        if (context.Element is object)
        {
          if (context.Category is ARDB.Category category)
            geometry.TrySetUserString(ARDB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY.ToString(), category.Id);

          if (context.Material is ARDB.Material material)
            geometry.TrySetUserString(ARDB.BuiltInParameter.MATERIAL_ID_PARAM.ToString(), material.Id);
        }
      }

      return geometry;
    }

    internal static IEnumerable<GeometryBase> ToGeometryBaseMany(this ARDB.GeometryObject geometry) =>
      ToGeometryBaseMany(geometry, _ => true);

    internal static IEnumerable<GeometryBase> ToGeometryBaseMany(this ARDB.GeometryObject geometry, Func<ARDB.GeometryObject, bool> visitor)
    {
      UpdateGraphicAttributes(geometry);

      if (visitor(geometry))
      {
        switch (geometry)
        {
          case ARDB.GeometryElement element:
            foreach (var g in element.SelectMany(x => x.ToGeometryBaseMany(visitor)))
              yield return g;
            yield break;

          case ARDB.GeometryInstance instance:
            foreach (var g in instance.GetInstanceGeometry().ToGeometryBaseMany(visitor))
              yield return g;
            yield break;

          case ARDB.Mesh mesh:
            yield return SetGraphicAttributes(mesh.ToMesh());
            yield break;

          case ARDB.Solid solid:
            yield return SetGraphicAttributes(solid.ToBrep());
            yield break;

          case ARDB.Curve curve:
            yield return SetGraphicAttributes(curve.ToCurve());
            yield break;

          case ARDB.PolyLine polyline:
            yield return SetGraphicAttributes(polyline.ToPolylineCurve());
            yield break;
        }
      }
    }
  }
}

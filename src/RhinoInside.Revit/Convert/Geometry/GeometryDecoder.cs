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

    #region Static Properties
    /// <summary>
    /// Default scale factor applied during the geometry decoding to change
    /// from Revit internal units to active Rhino document model units.
    /// </summary>
    /// <remarks>
    /// This factor should be applied to Revit internal length values
    /// in order to obtain Rhino model length values.
    /// <code>
    /// RhinoModelLength = RevitInternalLength * <see cref="GeometryDecoder.ModelScaleFactor"/>
    /// </code>
    /// </remarks>
    /// <since>1.4</since>
    internal static double ModelScaleFactor => UnitConverter.ToModelLength;
    #endregion

    #region Length
    /// <summary>
    /// Converts the specified length to an equivalent Rhino model length.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino model length that is equivalent to the provided value.</returns>
    /// <since>1.4</since>
    public static double ToModelLength(double value) => ToModelLength(value, ModelScaleFactor);
    internal static double ToModelLength(double value, double factor) => value * factor;
    #endregion

    #region Points and Vectors
    /// <summary>
    /// Converts the specified UV point to an equivalent Rhino Point2d.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Point2d that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Point2d ToPoint2d(this ARDB.UV value)
    { var rhino = RawDecoder.AsPoint2d(value); UnitConvertible.Scale(ref rhino, ModelScaleFactor); return rhino; }

    /// <summary>
    /// Converts the specified UV vector to an equivalent Rhino Vector2d.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Vector2d that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Vector2d ToVector2d(this ARDB.UV value)
    { return new Vector2d(value.U, value.V); }

    /// <summary>
    /// Converts the specified XYZ point to an equivalent Rhino Point3d.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Point3d that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Point3d ToPoint3d(this ARDB.XYZ value)
    { var rhino = RawDecoder.AsPoint3d(value); UnitConvertible.Scale(ref rhino, ModelScaleFactor); return rhino; }

    /// <summary>
    /// Converts the specified XYZ vector to an equivalent Rhino Vector3d.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Vector3d that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Vector3d ToVector3d(this ARDB.XYZ value)
    { return new Vector3d(value.X, value.Y, value.Z); }

    /// <summary>
    /// Converts the specified Plane to an equivalent Rhino Plane.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Plane that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Plane ToPlane(this ARDB.Plane value)
    { var rhino = RawDecoder.AsPlane(value); UnitConvertible.Scale(ref rhino, ModelScaleFactor); return rhino; }

    /// <summary>
    /// Converts the specified Plane to an equivalent Rhino Transform.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Transform that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Transform ToTransform(this ARDB.Transform value)
    { var rhino = RawDecoder.AsTransform(value); UnitConvertible.Scale(ref rhino, ModelScaleFactor); return rhino; }

    /// <summary>
    /// Converts the specified BoundingBoxXYZ to an equivalent Rhino BoundingBox.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino BoundingBox that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static BoundingBox ToBoundingBox(this ARDB.BoundingBoxXYZ value)
    { var rhino = RawDecoder.AsBoundingBox(value); UnitConvertible.Scale(ref rhino, ModelScaleFactor); return rhino; }

    /// <summary>
    /// Converts the specified BoundingBoxXYZ to an equivalent Rhino BoundingBox.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <param name="transform"></param>
    /// <returns>A Rhino BoundingBox that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static BoundingBox ToBoundingBox(this ARDB.BoundingBoxXYZ value, out Transform transform)
    {
      var rhino = RawDecoder.AsBoundingBox(value, out transform);
      UnitConvertible.Scale(ref rhino, ModelScaleFactor);
      UnitConvertible.Scale(ref transform, ModelScaleFactor);
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
    /// <since>1.0</since>
    public static BoundingBox ToBoundingBox(this ARDB.Outline value)
    {
      return new BoundingBox(value.MinimumPoint.ToPoint3d(), value.MaximumPoint.ToPoint3d());
    }

    /// <summary>
    /// Converts the specified BoundingBoxXYZ to an equivalent Rhino Box.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Box that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Box ToBox(this ARDB.BoundingBoxXYZ value)
    {
      var rhino = RawDecoder.AsBoundingBox(value, out var transform);
      UnitConvertible.Scale(ref rhino, ModelScaleFactor);
      UnitConvertible.Scale(ref transform, ModelScaleFactor);

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

    #region Points
    /// <summary>
    /// Converts the specified Point to an equivalent Rhino Point.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Point that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Point ToPoint(this ARDB.Point value) => ToPoint(value, ModelScaleFactor);
    internal static Point ToPoint(this ARDB.Point value, double factor)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, factor); return rhino; }
    #endregion

    #region Curves
    /// <summary>
    /// Converts the specified Line to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Curve ToCurve(this ARDB.Line value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, ModelScaleFactor); return rhino; }

    /// <summary>
    /// Converts the specified Arc to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Curve ToCurve(this ARDB.Arc value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, ModelScaleFactor); return rhino; }

    /// <summary>
    /// Converts the specified Ellipse to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Curve ToCurve(this ARDB.Ellipse value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, ModelScaleFactor); return rhino; }

    /// <summary>
    /// Converts the specified NurbSpline to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Curve ToCurve(this ARDB.NurbSpline value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, ModelScaleFactor); return rhino; }

    /// <summary>
    /// Converts the specified HermiteSpline to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Curve ToCurve(this ARDB.HermiteSpline value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, ModelScaleFactor); return rhino; }

    /// <summary>
    /// Converts the specified CylindricalHelix to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Curve ToCurve(this ARDB.CylindricalHelix value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, ModelScaleFactor); return rhino; }

    /// <summary>
    /// Converts the specified Curve to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Curve ToCurve(this ARDB.Curve value) => ToCurve(value, ModelScaleFactor);
    internal static Curve ToCurve(this ARDB.Curve value, double factor)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, factor); return rhino; }

    /// <summary>
    /// Converts the specified PolyLine to an equivalent Rhino PolylineCurve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino PolylineCurve that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static PolylineCurve ToPolylineCurve(this ARDB.PolyLine value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, ModelScaleFactor); return rhino; }
    #endregion

    #region Solids
    /// <summary>
    /// Converts the specified Face to an equivalent Rhino Brep.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Brep that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Brep ToBrep(this ARDB.Face value)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, ModelScaleFactor); return rhino; }

    /// <summary>
    /// Converts the specified Solid to an equivalent Rhino Brep.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Brep that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Brep ToBrep(this ARDB.Solid value) => ToBrep(value, ModelScaleFactor);
    internal static Brep ToBrep(this ARDB.Solid value, double factor)
    { var rhino = RawDecoder.ToRhino(value); UnitConvertible.Scale(rhino, factor); return rhino; }
    #endregion

    #region Meshes
    /// <summary>
    /// Converts the specified Mesh to an equivalent Rhino Mesh.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Mesh that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Mesh ToMesh(this ARDB.Mesh value) => ToMesh(value, ModelScaleFactor);
    internal static Mesh ToMesh(this ARDB.Mesh value, double factor) =>
      MeshDecoder.FromRawMesh(MeshDecoder.ToRhino(value), factor);
    #endregion

    /// <summary>
    /// Converts the specified GeomertyObject to an equivalent Revit GeometryBase object.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino GeometryBase object that is equivalent to the provided value.</returns>
    /// <since>1.4</since>
    public static GeometryBase ToGeometryBase(this ARDB.GeometryObject value) => ToGeometryBase(value, ModelScaleFactor);
    internal static GeometryBase ToGeometryBase(this ARDB.GeometryObject value, double factor)
    {
      switch (value)
      {
        case ARDB.Point point: return point.ToPoint(factor);
        case ARDB.Curve curve: return curve.ToCurve(factor);
        case ARDB.Solid solid: return solid.ToBrep(factor);
        case ARDB.Mesh mesh:   return mesh.ToMesh(factor);
      }

      throw new ConversionException($"Unable to convert {value} to ${nameof(GeometryBase)}");
    }
    #endregion

    #region CurveLoop
    internal static TOutput[] ToArray<TOutput>
    (
      this ARDB.CurveLoop value,
      Converter<ARDB.Curve, TOutput> converter
    )
    {
      var count = value.NumberOfCurves();
      var curves = new TOutput[count];

      var index = 0;
      foreach (var curve in value)
        curves[index++] = converter(curve);

      return curves;
    }

    /// <summary>
    /// Converts the specified CurveLoop to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static Curve ToCurve(this ARDB.CurveLoop value)
    {
      var count = value.NumberOfCurves();
      if (count == 0) return default;
      if (count == 1) return value.First().ToCurve();

      return ToPolyCurve(value);
    }

    /// <summary>
    /// Converts the specified CurveLoop to an equivalent Rhino PolyCurve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino PolyCurve that is equivalent to the provided value.</returns>
    /// <since>1.0</since>
    public static PolyCurve ToPolyCurve(this ARDB.CurveLoop value)
    {
      var polycurve = new PolyCurve();
      foreach (var curve in value)
        polycurve.AppendSegment(curve.ToCurve());

      if (!value.IsOpen())
        polycurve.MakeClosed(GeometryObjectTolerance.Model.VertexTolerance);

      return polycurve;
    }
    #endregion

    #region CurveArray
    internal static TOuput[] ToArray<TOuput>(this ARDB.CurveArray value, Converter<ARDB.Curve, TOuput> converter)
    {
      var count = value.Size;
      var curves = new TOuput[count];

      int index = 0;
      foreach (var curve in value)
        curves[index++] = converter((ARDB.Curve) curve);

      return curves;
    }

    /// <summary>
    /// Converts the specified CurveArrArray to a Rhino Curve IEnumerable.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve IEnumerable that is equivalent to the provided value.</returns>
    /// <since>1.4</since>
    public static IEnumerable<Curve> ToCurveMany(this ARDB.CurveArray value)
    {
      foreach (object curve in value)
        yield return ToCurve((ARDB.Curve) curve);
    }

    /// <summary>
    /// Converts the specified CurveArray to an equivalent Rhino Curve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino Curve that is equivalent to the provided value.</returns>
    /// <since>1.4</since>
    public static Curve ToCurve(this ARDB.CurveArray value)
    {
      var count = value.Size;
      if (count == 0) return null;
      if (count == 1) return value.get_Item(0).ToCurve();

      return ToPolyCurve(value);
    }

    /// <summary>
    /// Converts the specified CurveArray to an equivalent Rhino PolyCurve.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino PolyCurve that is equivalent to the provided value.</returns>
    /// <since>1.4</since>
    public static PolyCurve ToPolyCurve(this ARDB.CurveArray value)
    {
      var count = value.Size;
      var polycurve = new PolyCurve();
      for (int c = 0; c < count; ++c)
        polycurve.AppendSegment(value.get_Item(c).ToCurve());

      polycurve.MakeClosed(GeometryObjectTolerance.Model.VertexTolerance);
      return polycurve;
    }
    #endregion

    #region CurveArrArray
    internal static TOutput[] ToArray<TOutput>
    (
      this ARDB.CurveArrArray value,
      Converter<ARDB.CurveArray, TOutput> converter
    )
    {
      var array = new TOutput[value.Size];
      int index = 0;
      foreach (var item in value)
        array[index++] = converter((ARDB.CurveArray) item);

      return array;
    }

    /// <summary>
    /// Converts the specified CurveArrArray to a Rhino PolyCurve IEnumerable.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Rhino PolyCurve IEnumerable that is equivalent to the provided value.</returns>
    /// <since>1.4</since>
    public static IEnumerable<Curve> ToCurveMany(this ARDB.CurveArrArray value)
    {
      foreach (object curve in value)
        yield return ToCurve((ARDB.CurveArray) curve);
    }
    #endregion

    #region Graphics
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
    #endregion
  }
}

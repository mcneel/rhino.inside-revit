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
    #region Context
    public sealed class Context : State<Context>
    {
      public DB.Element Element = default;
      public DB.Visibility Visibility = DB.Visibility.Invisible;
      public DB.ElementId GraphicsStyleId = DB.ElementId.InvalidElementId;
      public DB.ElementId MaterialId = DB.ElementId.InvalidElementId;
      public DB.ElementId[] FaceMaterialId;
    }
    #endregion

    #region Geometry values
    public static Point2d ToPoint2d(this DB.UV value)
    { var rhino = RawDecoder.AsPoint2d(value); UnitConverter.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }
    public static Vector2d ToVector2d(this DB.UV value)
    { return new Vector2d(value.U, value.V); }

    public static Point3d ToPoint3d(this DB.XYZ value)
    { var rhino = RawDecoder.AsPoint3d(value); UnitConverter.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }
    public static Vector3d ToVector3d(this DB.XYZ value)
    { return new Vector3d(value.X, value.Y, value.Z); }

    public static Plane ToPlane(this DB.Plane value)
    { var rhino = RawDecoder.AsPlane(value); UnitConverter.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }

    public static Transform ToTransform(this DB.Transform value)
    { var rhino = RawDecoder.AsTransform(value); UnitConverter.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }

    public static BoundingBox ToBoundingBox(this DB.BoundingBoxXYZ value)
    { var rhino = RawDecoder.AsBoundingBox(value); UnitConverter.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }

    public static BoundingBox ToBoundingBox(this DB.BoundingBoxXYZ value, out Transform transform)
    {
      var rhino = RawDecoder.AsBoundingBox(value, out transform);
      UnitConverter.Scale(ref rhino, UnitConverter.ToRhinoUnits);
      UnitConverter.Scale(ref transform, UnitConverter.ToRhinoUnits);
      return rhino;
    }

    public static Interval[] ToIntervals(DB.BoundingBoxUV value)
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

    public static BoundingBox ToBoundingBox(this DB.Outline value)
    {
      return new BoundingBox(value.MinimumPoint.ToPoint3d(), value.MaximumPoint.ToPoint3d());
    }

    public static Box ToBox(this DB.BoundingBoxXYZ value)
    {
      var rhino = RawDecoder.AsBoundingBox(value, out var transform);
      UnitConverter.Scale(ref rhino, UnitConverter.ToRhinoUnits);
      UnitConverter.Scale(ref transform, UnitConverter.ToRhinoUnits);

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

    public static Mesh ToMesh(this DB.Mesh value) =>
      MeshDecoder.FromRawMesh(MeshDecoder.ToRhino(value), UnitConverter.ToRhinoUnits);
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

      if(!value.IsOpen())
        polycurve.MakeClosed(Revit.VertexTolerance * Revit.ModelUnits);

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

        polycurve.MakeClosed(Revit.VertexTolerance * Revit.ModelUnits);

        list[index++] = polycurve;
      }

      return list;
    }

    /// <summary>
    /// Update Context from <see cref="DB.GeometryObject"/> <paramref name="geometryObject"/>
    /// </summary>
    /// <param name="geometryObject"></param>
    internal static void UpdateGraphicAttributes(DB.GeometryObject geometryObject)
    {
      if (geometryObject is object)
      {
        var context = Context.Peek;
        if (context.Element is object)
        {
          context.Visibility = geometryObject.Visibility;

          if (geometryObject is DB.GeometryInstance instance)
          {
            context.Element = instance.Symbol;
            context.GraphicsStyleId = instance.Symbol.Category?.GetGraphicsStyle(DB.GraphicsStyleType.Projection)?.Id ?? DB.ElementId.InvalidElementId;
            context.MaterialId = instance.Symbol.Category?.Material?.Id ?? DB.ElementId.InvalidElementId;
          }
          else if (geometryObject is DB.GeometryElement element)
          {
            context.MaterialId = element.MaterialElement?.Id ?? DB.ElementId.InvalidElementId;
          }
          else if (geometryObject is DB.Solid solid)
          {
            if (!solid.Faces.IsEmpty)
            {
              var faces = solid.Faces;
              var count = faces.Size;

              context.FaceMaterialId = new DB.ElementId[count];
              for (int f = 0; f < count; ++f)
                context.FaceMaterialId[f] = faces.get_Item(f).MaterialElementId;
            }
          }
          else if (geometryObject is DB.Mesh mesh)
          {
            context.FaceMaterialId = new DB.ElementId[] { mesh.MaterialElementId };
          }

          if (geometryObject.GraphicsStyleId != DB.ElementId.InvalidElementId)
            context.GraphicsStyleId = geometryObject.GraphicsStyleId;
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
          if (context.GraphicsStyleId.IsValid() && context.Element.Document.GetElement(context.GraphicsStyleId) is DB.GraphicsStyle graphicsStyle)
          {
            var category = graphicsStyle.GraphicsStyleCategory;
            geometry.TrySetUserString(DB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY.ToString(), category.Id);
          }

          if (context.MaterialId.IsValid() && context.Element.Document.GetElement(context.MaterialId) is DB.Material material)
          {
            geometry.TrySetUserString(DB.BuiltInParameter.MATERIAL_ID_PARAM.ToString(), material.Id);
          }
        }
      }

      return geometry;
    }

    public static IEnumerable<GeometryBase> ToGeometryBaseMany(this DB.GeometryObject geometry)
    {
      UpdateGraphicAttributes(geometry);

      switch (geometry)
      {
        case DB.GeometryElement element:
          foreach (var g in element.SelectMany(x => x.ToGeometryBaseMany()))
            yield return g;
          yield break;

        case DB.GeometryInstance instance:
          foreach (var g in instance.GetInstanceGeometry().ToGeometryBaseMany())
            yield return g;
          yield break;

        case DB.Mesh mesh:
          yield return SetGraphicAttributes(mesh.ToMesh());
          yield break;

        case DB.Solid solid:
          yield return SetGraphicAttributes(solid.ToBrep());
          yield break;

        case DB.Curve curve:
          yield return SetGraphicAttributes(curve.ToCurve());
          yield break;

        case DB.PolyLine polyline:
          yield return SetGraphicAttributes(polyline.ToPolylineCurve());
          yield break;
      }
    }
  }
}

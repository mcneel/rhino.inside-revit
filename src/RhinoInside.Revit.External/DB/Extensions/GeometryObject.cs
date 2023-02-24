using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  using static NumericTolerance;

  struct GeometryObjectEqualityComparer :
    IEqualityComparer<double>,
    IEqualityComparer<UV>,
    IEqualityComparer<XYZ>,
    IEqualityComparer<Point>,
    IEqualityComparer<PolyLine>,
    IEqualityComparer<Line>,
    IEqualityComparer<Arc>,
    IEqualityComparer<Ellipse>,
    IEqualityComparer<HermiteSpline>,
    IEqualityComparer<NurbSpline>,
    IEqualityComparer<CylindricalHelix>,
    IEqualityComparer<Curve>,
    IEqualityComparer<GeometryObject>
  {
    static int CombineHash(params int[] values)
    {
      int hash = 0;
      for (int h = 0; h < values.Length; h++)
        hash = hash * -1521134295 + values[h];

      return hash;
    }

    readonly double Tolerance;

    GeometryObjectEqualityComparer(double tolerance) => Tolerance = Math.Max(tolerance, Upsilon);

    struct ParamComparer : IEqualityComparer<double>, IEqualityComparer<IList<double>>, IEqualityComparer<DoubleArray>
    {
      public const double ParamTolerance = DefaultTolerance;
      const double ReciprocalParamTolerance = 1.0 / DefaultTolerance;

      public bool Equals(double x, double y) => AlmostEquals(x, y, ParamTolerance);
      public int GetHashCode(double value) => (int) Math.Round(value * ReciprocalParamTolerance);

      public bool Equals(IList<double> left, IList<double> right)
      {
        if (left.Count != right.Count) return false;

        for (int i = 0; i < left.Count; i++)
          if (!Equals(left[i], right[i])) return false;

        return true;
      }

      public int GetHashCode(IList<double> values)
      {
        int hash = values.Count;
        for (int h = 0; h < values.Count; h++)
          hash = hash * -1521134295 + GetHashCode(values[h]);

        return hash;
      }

      public bool Equals(DoubleArray left, DoubleArray right)
      {
        if (left.Size != right.Size) return false;

        for (int i = 0; i < left.Size; i++)
          if (!Equals(left.get_Item(i), right.get_Item(i))) return false;

        return true;
      }

      public int GetHashCode(DoubleArray values)
      {
        int hash = values.Size;
        for (int h = 0; h < values.Size; h++)
          hash = hash * -1521134295 + GetHashCode(values.get_Item(h));

        return hash;
      }
    }

    public static IEqualityComparer<double>         Parameter = default(ParamComparer);
    public static IEqualityComparer<IList<double>>  Parameters = default(ParamComparer);
    public static IEqualityComparer<DoubleArray>    DoubleArray = default(ParamComparer);

    /// <summary>
    /// IEqualityComparer for <see cref="{T}"/> that compares geometrically using <see cref="DefaultTolerance"/> value.
    /// </summary>
    /// <param name="tolerance"></param>
    /// <returns>A geometry comparer.</returns>
    public static readonly GeometryObjectEqualityComparer Default = new GeometryObjectEqualityComparer(DefaultTolerance);

    /// <summary>
    /// IEqualityComparer for <see cref="{T}"/> that compares geometrically.
    /// </summary>
    /// <param name="tolerance"></param>
    /// <returns>A geometry comparer.</returns>
    public static GeometryObjectEqualityComparer Comparer(double tolerance) => new GeometryObjectEqualityComparer(tolerance);

    #region Length
    public bool Equals(double x, double y) => Norm(x - y) < Tolerance;
    public int GetHashCode(double value) => Math.Round(value / Tolerance).GetHashCode();
    #endregion

    #region UV
    public bool Equals(UV x, UV y) => Norm(x.U - y.U, x.V - y.V) < Tolerance;
    public int GetHashCode(UV obj) => CombineHash
    (
      GetHashCode(obj.U),
      GetHashCode(obj.V)
    );
    #endregion

    #region BoundingBoxUV
    public bool Equals(BoundingBoxUV x, BoundingBoxUV y)
    {
      return Default.Equals(x.Min, y.Max);
    }

    public int GetHashCode(BoundingBoxUV value) => CombineHash
    (
      GetHashCode(value.Min),
      GetHashCode(value.Max)
    );
    #endregion

    #region XYZ
    public bool Equals(XYZ left, XYZ right) => Norm(left.X - right.X, left.Y - right.Y, left.Z - right.Z) < Tolerance;
    public int GetHashCode(XYZ obj) => CombineHash
    (
      GetHashCode(obj.X),
      GetHashCode(obj.Y),
      GetHashCode(obj.Z)
    );

    public bool Equals(IList<XYZ> left, IList<XYZ> right)
    {
      if (left.Count != right.Count) return false;

      for (int i = 0; i < left.Count; i++)
        if (!Equals(left[i], right[i])) return false;

      return true;
    }

    int GetHashCode(IList<XYZ> values, int maxSamples = int.MaxValue)
    {
      var valuesCount = values.Count;
      int hash = valuesCount.GetHashCode();

      if (valuesCount < maxSamples)
      {
        for (int h = 0; h < valuesCount; h++)
          hash = hash * -1521134295 + GetHashCode(values[h]);
      }
      else
      {
        for (int h = 0; h < maxSamples; h++)
          hash = hash * -1521134295 + GetHashCode(values[(int) Math.Round(((double) valuesCount / maxSamples) * h)]);
      }

      return hash;
    }
    #endregion

    #region BoundingBoxXYZ
    public bool Equals(BoundingBoxXYZ x, BoundingBoxXYZ y)
    {
      if (!Equals(x.Min, y.Max)) return false;

      using (var xT = x.Transform) using (var yT = y.Transform)
      {
        if (!Equals(xT.Origin, yT.Origin)) return false;
        if (!Default.Equals(xT.BasisX, yT.BasisX)) return false;
        if (!Default.Equals(xT.BasisY, yT.BasisY)) return false;
        if (!Default.Equals(xT.BasisZ, yT.BasisZ)) return false;
      }

      return true;
    }

    public int GetHashCode(BoundingBoxXYZ value) => CombineHash
    (
      GetHashCode(value.Min),
      GetHashCode(value.Max),
      GetHashCode(value.Transform.Origin)
    );
    #endregion

    #region Point
    public bool Equals(Point left, Point right) =>
      Equals(left.Coord, right.Coord);

    public int GetHashCode(Point value) => CombineHash
    (
      value.GetType().GetHashCode(),
      GetHashCode(value.Coord)
    );
    #endregion

    #region PolyLine
    public bool Equals(PolyLine left, PolyLine right) =>
      left.NumberOfCoordinates == right.NumberOfCoordinates &&
      Equals(left.GetCoordinates(), right.GetCoordinates());

    public int GetHashCode(PolyLine value) => CombineHash
    (
      value.GetType().GetHashCode(),
      GetHashCode(value.GetCoordinates(), 16)
    );
    #endregion

    #region Mesh
    public bool Equals(Mesh left, Mesh right)
    {
      var trianglesCount = left.NumTriangles;
      if (trianglesCount != right.NumTriangles) return false;

      for (int t = 0; t < trianglesCount; ++t)
      {
        var lTriangle = left.get_Triangle(t);
        var rTriangle = right.get_Triangle(t);
        if (lTriangle.get_Index(0) != rTriangle.get_Index(0)) return false;
        if (lTriangle.get_Index(1) != rTriangle.get_Index(1)) return false;
        if (lTriangle.get_Index(2) != rTriangle.get_Index(2)) return false;
      }

      return Equals(left.Vertices, right.Vertices);
    }

    public int GetHashCode(Mesh value) => CombineHash
    (
      value.GetType().GetHashCode(),
      GetHashCode(value.Vertices, 32)
    );
    #endregion

    #region Line
    public bool Equals(Line left, Line right) =>
      left.IsBound == right.IsBound &&
      (
        left.IsBound ?
        (
          Equals(left.GetEndPoint(CurveEnd.Start), right.GetEndPoint(CurveEnd.Start)) &&
          Equals(left.GetEndPoint(CurveEnd.End),   right.GetEndPoint(CurveEnd.End))
        ):
        (
          Equals(left.Origin, right.Origin) &&
          Equals(left.Direction, right.Direction)
        )
      );

    public int GetHashCode(Line value) => CombineHash
    (
      value.GetType().GetHashCode(),
      value.IsBound.GetHashCode(),
      GetHashCode(value.IsBound ? value.GetEndPoint(CurveEnd.Start) : value.Origin),
      GetHashCode(value.IsBound ? value.GetEndPoint(CurveEnd.End)   : value.Direction)
    );
    #endregion

    #region Arc
    public bool Equals(Arc left, Arc right) =>
      left.IsBound == right.IsBound &&
      Equals(left.Center, right.Center) &&
      Equals(left.XDirection, right.XDirection) &&
      Equals(left.YDirection, right.YDirection) &&
      Equals(left.Normal, right.Normal) &&
      (
        left.IsBound ?
        (
          Equals(left.Evaluate(0.0 / 2.0, true), right.Evaluate(0.0 / 2.0, true)) &&
          Equals(left.Evaluate(1.0 / 2.0, true), right.Evaluate(1.0 / 2.0, true)) &&
          Equals(left.Evaluate(2.0 / 2.0, true), right.Evaluate(2.0 / 2.0, true))
        ) :
        (
          Equals(left.Evaluate(left.Period * 0.0 / 4.0, false), right.Evaluate(right.Period * 0.0 / 4.0, false)) &&
          Equals(left.Evaluate(left.Period * 1.0 / 4.0, false), right.Evaluate(right.Period * 1.0 / 4.0, false)) &&
          Equals(left.Evaluate(left.Period * 2.0 / 4.0, false), right.Evaluate(right.Period * 2.0 / 4.0, false)) &&
          Equals(left.Evaluate(left.Period * 3.0 / 4.0, false), right.Evaluate(right.Period * 3.0 / 4.0, false))
        )
      );

    public int GetHashCode(Arc value) => CombineHash
    (
      value.GetType().GetHashCode(),
      value.IsBound.GetHashCode(),
      GetHashCode(value.Center),
      GetHashCode(value.XDirection),
      GetHashCode(value.YDirection),
      GetHashCode(value.Normal),
      value.IsBound ?
      CombineHash
      (
        GetHashCode(value.Evaluate(0.0 / 2.0, true)),
        GetHashCode(value.Evaluate(1.0 / 2.0, true)),
        GetHashCode(value.Evaluate(2.0 / 2.0, true))
      ) :
      CombineHash
      (
        GetHashCode(value.Evaluate(value.Period * 0.0 / 4.0, false)),
        GetHashCode(value.Evaluate(value.Period * 1.0 / 4.0, false)),
        GetHashCode(value.Evaluate(value.Period * 2.0 / 4.0, false)),
        GetHashCode(value.Evaluate(value.Period * 3.0 / 4.0, false))
      )
    );
    #endregion

    #region Ellipse
    public bool Equals(Ellipse left, Ellipse right) =>
      left.IsBound == right.IsBound &&
      Equals(left.Center, right.Center) &&
      Equals(left.XDirection, right.XDirection) &&
      Equals(left.YDirection, right.YDirection) &&
      Equals(left.Normal, right.Normal) &&
      (
        left.IsBound ?
        (
          Equals(left.Evaluate(0.0 / 2.0, true), right.Evaluate(0.0 / 2.0, true)) &&
          Equals(left.Evaluate(1.0 / 2.0, true), right.Evaluate(1.0 / 2.0, true)) &&
          Equals(left.Evaluate(2.0 / 2.0, true), right.Evaluate(2.0 / 2.0, true))
        ) :
        (
          Equals(left.Evaluate(left.Period * 0.0 / 4.0, false), right.Evaluate(right.Period * 0.0 / 4.0, false)) &&
          Equals(left.Evaluate(left.Period * 1.0 / 4.0, false), right.Evaluate(right.Period * 1.0 / 4.0, false)) &&
          Equals(left.Evaluate(left.Period * 2.0 / 4.0, false), right.Evaluate(right.Period * 2.0 / 4.0, false)) &&
          Equals(left.Evaluate(left.Period * 3.0 / 4.0, false), right.Evaluate(right.Period * 3.0 / 4.0, false))
        )
      );

    public int GetHashCode(Ellipse value) => CombineHash
    (
      value.GetType().GetHashCode(),
      value.IsBound.GetHashCode(),
      GetHashCode(value.Center),
      GetHashCode(value.XDirection),
      GetHashCode(value.YDirection),
      GetHashCode(value.Normal),
      value.IsBound ?
      CombineHash
      (
        GetHashCode(value.Evaluate(0.0 / 2.0, true)),
        GetHashCode(value.Evaluate(1.0 / 2.0, true)),
        GetHashCode(value.Evaluate(2.0 / 2.0, true))
      ) :
      CombineHash
      (
        GetHashCode(value.Evaluate(value.Period * 0.0 / 4.0, false)),
        GetHashCode(value.Evaluate(value.Period * 1.0 / 4.0, false)),
        GetHashCode(value.Evaluate(value.Period * 2.0 / 4.0, false)),
        GetHashCode(value.Evaluate(value.Period * 3.0 / 4.0, false))
      )
    );
    #endregion

    #region HermiteSpline
    public bool Equals(HermiteSpline left, HermiteSpline right) =>
      left.IsBound == right.IsBound &&
      left.IsCyclic == right.IsCyclic &&
      Equals(left.ControlPoints, right.ControlPoints) &&
      Equals(left.Tangents, right.Tangents) &&
      DoubleArray.Equals(left.Parameters, right.Parameters);

    public int GetHashCode(HermiteSpline value) => CombineHash
    (
      value.GetType().GetHashCode(),
      value.IsBound.GetHashCode(),
      GetHashCode(value.ControlPoints, 16),
      GetHashCode(value.Tangents, 16),
      DoubleArray.GetHashCode(value.Parameters)
    );
    #endregion

    #region NurbSpline
    public bool Equals(NurbSpline left, NurbSpline right) =>
      left.IsBound == right.IsBound &&
      left.IsCyclic == right.IsCyclic &&
      Equals(left.CtrlPoints, right.CtrlPoints) &&
      DoubleArray.Equals(left.Weights, right.Weights) &&
      DoubleArray.Equals(left.Knots, right.Knots);

    public int GetHashCode(NurbSpline value) => CombineHash
    (
      value.GetType().GetHashCode(),
      value.IsBound.GetHashCode(),
      GetHashCode(value.CtrlPoints, 16),
      DoubleArray.GetHashCode(value.Weights),
      DoubleArray.GetHashCode(value.Knots)
    );
    #endregion

    #region CylindricalHelix
    public bool Equals(CylindricalHelix left, CylindricalHelix right) =>
      left.IsBound == right.IsBound &&
      left.IsCyclic == right.IsCyclic &&
      left.IsRightHanded == right.IsRightHanded &&
      Equals(left.Height, right.Height) &&
      Equals(left.Pitch, right.Pitch) &&
      Equals(left.Radius, right.Radius) &&
      Equals(left.BasePoint, right.BasePoint) &&
      Equals(left.XVector, right.XVector) &&
      Equals(left.YVector, right.YVector) &&
      Equals(left.ZVector, right.ZVector);

    public int GetHashCode(CylindricalHelix value) => CombineHash
    (
      value.GetType().GetHashCode(),
      value.IsBound.GetHashCode(),
      value.IsRightHanded.GetHashCode(),
      GetHashCode(value.Height),
      GetHashCode(value.Pitch),
      GetHashCode(value.Radius),
      GetHashCode(value.BasePoint),
      GetHashCode(value.XVector),
      GetHashCode(value.YVector),
      GetHashCode(value.ZVector)
    );
    #endregion

    #region Curve
    public bool Equals(Curve left, Curve right) => Equals((GeometryObject)left, right);
    public int GetHashCode(Curve value) => GetHashCode((GeometryObject) value);
    #endregion

    #region Face
    public bool Equals(Face left, Face right)
    {
      if (left.GetType() != right.GetType()) return false;

      var uv = left.GetBoundingBox();
      if (!Equals(uv, right.GetBoundingBox())) return false;

      var (min, max) = uv;
      var mid = (max + min) / 2.0;
      if (!Equals(left.Evaluate(mid), right.Evaluate(mid))) return false;

      var leftEdges = left.EdgeLoops;
      var rightEdges = right.EdgeLoops;
      if (leftEdges.Size != rightEdges.Size) return false;

      return Equals(left.Triangulate(), right.Triangulate());
    }

    public int GetHashCode(Face value)
    {
      using (var uv = value.GetBoundingBox())
      {
        var (min, max) = uv;
        var mid = (max + min) / 2.0;
        return CombineHash
        (
          value.GetType().GetHashCode(),
          GetHashCode(uv),
          GetHashCode(value.Evaluate(mid)),
          GetHashCode(value.Evaluate(new UV(min.U, min.V))),
          GetHashCode(value.Evaluate(new UV(max.U, min.V))),
          GetHashCode(value.Evaluate(new UV(min.U, max.V))),
          GetHashCode(value.Evaluate(new UV(max.U, max.V)))
        );
      }
    }
    #endregion

    #region Solid
    public bool Equals(Solid left, Solid right)
    {
      if (!Equals(left.GetBoundingBox(), right.GetBoundingBox())) return false;

      var leftFaces = left.Faces;
      var rightFaces = right.Faces;

      var faceCoumt = leftFaces.Size;
      if (faceCoumt != rightFaces.Size) return false;

      for(int f = 0; f < faceCoumt; ++f)
        if (!Equals(leftFaces.get_Item(f), rightFaces.get_Item(f))) return false;

      return true;
    }

    public int GetHashCode(Solid value) => CombineHash
    (
      value.GetType().GetHashCode(),
      value.Faces.Size.GetHashCode(),
      GetHashCode(value.GetBoundingBox())
    );
    #endregion

    #region GeometryObject
    public bool Equals(GeometryObject left, GeometryObject right)
    {
      if (ReferenceEquals(left, right))       return true;
      if (left is null || right is null)      return false;
      if (left.GetType() != right.GetType())  return false;

      switch (left)
      {
        case Point point:             return Equals(point,    (Point)             right);
        case PolyLine polyLine:       return Equals(polyLine, (PolyLine)          right);
        case Line line:               return Equals(line,     (Line)              right);
        case Arc arc:                 return Equals(arc,      (Arc)               right);
        case Ellipse ellipse:         return Equals(ellipse,  (Ellipse)           right);
        case HermiteSpline hermite:   return Equals(hermite,  (HermiteSpline)     right);
        case NurbSpline spline:       return Equals(spline,   (NurbSpline)        right);
        case CylindricalHelix helix:  return Equals(helix,    (CylindricalHelix)  right);
        case Solid solid:             return Equals(solid,    (Solid)             right);
        case Face face:               return Equals(face,     (Face)              right);
      }

      throw new NotImplementedException($"{nameof(GeometryObjectEqualityComparer)} is not implemented for {left.GetType()}.");
    }
    public int GetHashCode(GeometryObject value)
    {
      switch (value)
      {
        case null:                    return 0;
        case Line line:               return GetHashCode(line);
        case Arc arc:                 return GetHashCode(arc);
        case Ellipse ellipse:         return GetHashCode(ellipse);
        case HermiteSpline hermite:   return GetHashCode(hermite);
        case NurbSpline spline:       return GetHashCode(spline);
        case CylindricalHelix helix:  return GetHashCode(helix);
        case Solid solid:             return GetHashCode(solid);
        case Face face:               return GetHashCode(face);
      }

      throw new NotImplementedException($"{nameof(GeometryObjectEqualityComparer)} is not implemented for {value.GetType()}.");
    }
    #endregion
  }

  public static class GeometryObjectExtension
  {
    internal static bool IsValid(this GeometryObject geometry) => geometry?.IsValidObject() ?? false;

    internal static bool IsValidObject(this GeometryObject geometry)
    {
#if REVIT_2021
      try { return geometry.Id >= 0; }
#else
      // TODO : Test this, type by type, and use a faster fail check.
      try { return geometry.TryGetLocation(out var _, out var _, out var _);}
#endif
      catch { return false; }
    }

    public static bool AlmostEquals<G>(this G left, G right)
      where G : GeometryObject
    {
      return GeometryObjectEqualityComparer.Default.Equals(left, right);
    }

    public static bool AlmostEquals<G>(this G left, G right, double tolerance)
      where G : GeometryObject
    {
      return GeometryObjectEqualityComparer.Comparer(tolerance).Equals(left, right);
    }

    public static IEnumerable<GeometryObject> ToDirectShapeGeometry(this GeometryObject geometry)
    {
      switch (geometry)
      {
        case Point p: yield return p; yield break;
        case Curve c: foreach (var unbounded in c.ToBoundedCurves()) yield return unbounded; yield break;
        case Solid s: yield return s; yield break;
        case Mesh m: yield return m; yield break;
        case GeometryInstance i: yield return i; yield break;
        case GeometryElement e: foreach (var g in e) yield return g; yield break;
        default: throw new ArgumentException("DirectShape only supports Point, Curve, Solid, Mesh and GeometryInstance.");
      }
    }

    /// <summary>
    /// Computes an arbitrary object oriented coord system for <paramref name="geometry"/>.
    /// </summary>
    /// <param name="geometry"></param>
    /// <param name="origin"></param>
    /// <param name="basisX"></param>
    /// <param name="basisY"></param>
    /// <returns>True on success, False on fail.</returns>
    public static bool TryGetLocation(this GeometryObject geometry, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      switch (geometry)
      {
        case GeometryElement element:
          foreach (var geo in element)
            if (TryGetLocation(geo, out origin, out basisX, out basisY)) return true;
          break;

        case GeometryInstance instance:
          origin = instance.Transform.Origin;
          basisX = instance.Transform.BasisX;
          basisY = instance.Transform.BasisY;
          return true;

        case Point point:
          origin = point.Coord;
          basisX = XYZExtension.BasisX;
          basisY = XYZExtension.BasisY;
          return true;

        case PolyLine polyline:
          return polyline.TryGetLocation(out origin, out basisX, out basisY);

        case Curve curve:
          return curve.TryGetLocation(out origin, out basisX, out basisY);

        case Edge edge:
          return edge.AsCurve().TryGetLocation(out origin, out basisX, out basisY);

        case Face face:
          using (var derivatives = face.ComputeDerivatives(new UV(0.5, 0.5), normalized: true))
          {
            origin = derivatives.Origin;
            basisX = derivatives.BasisX;
            basisY = derivatives.BasisY;

            // Make sure is orthonormal.
            var basisZ = basisX.CrossProduct(basisY).Normalize(0D);
            basisX = basisX.Normalize(0D);
            basisY = basisZ.CrossProduct(basisX);
          }
          return true;

        case Solid solid:
          if (!solid.Faces.IsEmpty)
            return TryGetLocation(solid.Faces.get_Item(0), out origin, out basisX, out basisY);

          break;

        case Mesh mesh:
          return mesh.TryGetLocation(out origin, out basisX, out basisY);
      }

      origin = basisX = basisY = default;
      return false;
    }

    /// <summary>
    /// Retrieves a box that encloses the geometry object.
    /// </summary>
    /// <param name="geometry"></param>
    /// <returns>The geometry bounding box.</returns>
    public static BoundingBoxXYZ GetBoundingBox(this GeometryObject geometry)
    {
      switch (geometry)
      {
        case null:
          return null;

        case GeometryElement element:
          return element.GetBoundingBox();

        case GeometryInstance instance:
          {
            var bbox = instance.SymbolGeometry.GetBoundingBox();
            bbox.Transform = instance.Transform;
            return bbox;
          }

        case Point point:
          return new BoundingBoxXYZ() { Min = point.Coord, Max = point.Coord };

        case PolyLine polyline:
          {
            if (XYZExtension.TryGetBoundingBox(polyline.GetCoordinates(), out var bbox))
              return bbox;
          }
          break;

        case Curve curve:
          {
            if (XYZExtension.TryGetBoundingBox(curve.Tessellate(), out var bbox))
              return bbox;
          }
          break;

        case Edge edge:
          {
            if (XYZExtension.TryGetBoundingBox(edge.Tessellate(), out var bbox))
              return bbox;
          }
          break;

        case Face face:
          {
            using (var mesh = face.Triangulate())
              if (XYZExtension.TryGetBoundingBox(mesh.Vertices, out var bbox))
                return bbox;
          }
          break;

        case Solid solid:
          {
            if (!solid.Faces.IsEmpty)
              return solid.GetBoundingBox();
          }
          break;

        case Mesh mesh:
          {
            if (XYZExtension.TryGetBoundingBox(mesh.Vertices, out var bbox))
              return bbox;
          }
          break;

        default:
          throw new NotImplementedException($"{nameof(GetBoundingBox)} is not implemented for {geometry.GetType()}.");
      }

      return BoundingBoxXYZExtension.Empty;
    }

    /// <summary>
    /// Retrieves the symbol element that this instance is referring to.
    /// </summary>
    /// <param name="instance"></param>
    /// <returns>The Symbol element.</returns>
    public static Element GetSymbol(this GeometryInstance instance)
    {
#if REVIT_2023
      return instance.GetDocument().GetElement(instance.GetSymbolGeometryId().SymbolId);
#else
      return instance.Symbol;
#endif
    }

    #region References
    static IEnumerable<string> GetStableFaceReferences(this GeometryObject geometry, Document document, string uniqueId)
    {
      int instanceIndex = 0;
      switch (geometry)
      {
        case GeometryElement element:
          foreach (var g in element.SelectMany(x => GetStableFaceReferences(x, document, uniqueId)))
            yield return g;
          yield break;

        case GeometryInstance instance:
          foreach (var g in GetStableFaceReferences(instance.GetInstanceGeometry(), document, uniqueId))
            yield return $"{uniqueId}:{instanceIndex}:INSTANCE:{g}";

          instanceIndex++;
          yield break;

        case Mesh mesh:
          yield break;

        case Solid solid:
          foreach (var face in solid.Faces.Cast<Face>())
          {
            if (face.Reference is Reference reference)
              yield return reference.ConvertToStableRepresentation(document);
          }

          yield break;

        case Curve curve:
          yield break;

        case PolyLine polyline:
          yield break;
      }
    }

    static IEnumerable<string> GetStableEdgeReferences(GeometryObject geometry, Document document, string uniqueId)
    {
      int instanceIndex = 0;
      switch (geometry)
      {
        case GeometryElement element:
          foreach (var g in element.SelectMany(x => GetStableEdgeReferences(x, document, uniqueId)))
            yield return g;
          yield break;

        case GeometryInstance instance:
          foreach (var g in GetStableEdgeReferences(instance.GetInstanceGeometry(), document, uniqueId))
            yield return $"{uniqueId}:{instanceIndex}:INSTANCE:{g}";

          instanceIndex++;
          yield break;

        case Mesh mesh:
          yield break;

        case Solid solid:
          foreach (var edge in solid.Edges.Cast<Edge>())
          {
            if (edge.Reference is Reference reference)
            {
              if (reference.ElementReferenceType != ElementReferenceType.REFERENCE_TYPE_LINEAR)
                continue;

              var stable = reference.ConvertToStableRepresentation(document);
              yield return stable;
            }
          }

          yield break;

        case Curve curve:
          yield break;

        case PolyLine polyline:
          yield break;
      }
    }

    public static IEnumerable<Reference> GetFaceReferences(this GeometryObject geometry, Element element)
    {
      var document = element.Document;
      var uniqueId = element.UniqueId;

      foreach(var stable in GetStableFaceReferences(geometry, document, uniqueId))
        yield return Reference.ParseFromStableRepresentation(document, stable);
    }

    public static IEnumerable<Reference> GetEdgeReferences(this GeometryObject geometry, Element element)
    {
      var document = element.Document;
      var uniqueId = element.UniqueId;

      foreach (var stable in GetStableEdgeReferences(geometry, document, uniqueId))
        yield return Reference.ParseFromStableRepresentation(document, stable);
    }

    public static IEnumerable<Reference> GetEdgeEndPointReferences(this GeometryObject geometry, Element element)
    {
      var document = element.Document;
      var uniqueId = element.UniqueId;

      foreach (var stable in GetStableEdgeReferences(geometry, document, uniqueId))
      {
        yield return Reference.ParseFromStableRepresentation(document, $"{stable}/0");
        yield return Reference.ParseFromStableRepresentation(document, $"{stable}/1");
      }
    }
    #endregion
  }
}

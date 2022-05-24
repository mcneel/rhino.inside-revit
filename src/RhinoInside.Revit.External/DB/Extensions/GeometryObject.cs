using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  using static NumericTolerance;

  struct GeometryObjectEqualityComparer :
    IEqualityComparer<double>,
    IEqualityComparer<GeometryObject>,
    IEqualityComparer<Curve>,
    IEqualityComparer<Line>,
    IEqualityComparer<Arc>,
    IEqualityComparer<Ellipse>,
    IEqualityComparer<HermiteSpline>,
    IEqualityComparer<NurbSpline>,
    IEqualityComparer<CylindricalHelix>
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
      public const double Tolerance = 1e-9;

      public bool Equals(double x, double y) => NumericTolerance.AlmostEquals(x, y, Tolerance);
      public int GetHashCode(double value) => (int) Math.Round(value * 1e+9);

      public bool Equals(IList<double> left, IList<double> right)
      {
        if (left.Count != right.Count) return false;

        for (int i = 0; i < left.Count; i++)
          if (!Equals(left[i], right[i])) return false;

        return true;
      }

      public int GetHashCode(IList<double> values)
      {
        int hash = 0;
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
        int hash = 0;
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
    public bool Equals(double x, double y) => NumericTolerance.AlmostEquals(x, y, Tolerance);
    public int GetHashCode(double value) => Math.Round(value / Tolerance).GetHashCode();
    #endregion

    #region XYZ
    public bool Equals(XYZ x, XYZ y) => x.AlmostEquals(y, Tolerance);
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

    public int GetHashCode(IList<XYZ> values)
    {
      int hash = 0;
      for (int h = 0; h < values.Count; h++)
        hash = hash * -1521134295 + GetHashCode(values[h]);

      return hash;
    }
    #endregion

    #region Line
    public bool Equals(Line left, Line right) =>
      left.IsBound == right.IsBound &&
      Equals(left.Origin, right.Origin) &&
      Equals(left.Direction, right.Direction);

    public int GetHashCode(Line value) => CombineHash
    (
      value.GetType().GetHashCode(),
      value.IsBound.GetHashCode(),
      GetHashCode(value.Origin),
      GetHashCode(value.Direction)
    );
    #endregion

    #region Arc
    public bool Equals(Arc left, Arc right) =>
      left.IsBound == right.IsBound &&
      Equals(left.Center, right.Center) &&
      Equals(left.XDirection, right.XDirection) &&
      Equals(left.YDirection, right.YDirection) &&
      Equals(left.Normal, right.Normal) &&
      left.IsBound ?
      (
        Equals(left.Evaluate(0.0 / 2.0, true), right.Evaluate(0.0 / 3.0, true)) &&
        Equals(left.Evaluate(1.0 / 2.0, true), right.Evaluate(1.0 / 3.0, true)) &&
        Equals(left.Evaluate(2.0 / 2.0, true), right.Evaluate(2.0 / 3.0, true))
      ) :
      (
        Equals(left.Evaluate(left.Period * 0.0 / 4.0, false), right.Evaluate(right.Period * 0.0 / 4.0, false)) &&
        Equals(left.Evaluate(left.Period * 1.0 / 4.0, false), right.Evaluate(right.Period * 1.0 / 4.0, false)) &&
        Equals(left.Evaluate(left.Period * 2.0 / 4.0, false), right.Evaluate(right.Period * 2.0 / 4.0, false)) &&
        Equals(left.Evaluate(left.Period * 3.0 / 4.0, false), right.Evaluate(right.Period * 3.0 / 4.0, false))
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
      left.IsBound ?
      (
        Equals(left.Evaluate(0.0 / 2.0, true), right.Evaluate(0.0 / 3.0, true)) &&
        Equals(left.Evaluate(1.0 / 2.0, true), right.Evaluate(1.0 / 3.0, true)) &&
        Equals(left.Evaluate(2.0 / 2.0, true), right.Evaluate(2.0 / 3.0, true))
      ) :
      (
        Equals(left.Evaluate(left.Period * 0.0 / 4.0, false), right.Evaluate(right.Period * 0.0 / 4.0, false)) &&
        Equals(left.Evaluate(left.Period * 1.0 / 4.0, false), right.Evaluate(right.Period * 1.0 / 4.0, false)) &&
        Equals(left.Evaluate(left.Period * 2.0 / 4.0, false), right.Evaluate(right.Period * 2.0 / 4.0, false)) &&
        Equals(left.Evaluate(left.Period * 3.0 / 4.0, false), right.Evaluate(right.Period * 3.0 / 4.0, false))
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
      GetHashCode(value.ControlPoints),
      GetHashCode(value.Tangents),
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
      GetHashCode(value.CtrlPoints),
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

    #region GeometryObject
    public bool Equals(GeometryObject left, GeometryObject right)
    {
      if (ReferenceEquals(left, right))       return true;
      if (left is null || right is null)      return false;
      if (left.GetType() != right.GetType())  return false;

      switch (left)
      {
        case Line line:               return Equals(line,     (Line)              right);
        case Arc arc:                 return Equals(arc,      (Arc)               right);
        case Ellipse ellipse:         return Equals(ellipse,  (Ellipse)           right);
        case HermiteSpline hermite:   return Equals(hermite,  (HermiteSpline)     right);
        case NurbSpline spline:       return Equals(spline,   (NurbSpline)        right);
        case CylindricalHelix helix:  return Equals(helix,    (CylindricalHelix)  right);
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
      }

      throw new NotImplementedException($"{nameof(GeometryObjectEqualityComparer)} is not implemented for {value.GetType()}.");
    }
    #endregion
  }

  public static class GeometryObjectExtension
  {
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
    /// <returns></returns>
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
          basisX = XYZ.BasisX;
          basisY = XYZ.BasisY;
          return true;

        case PolyLine polyline:
          return polyline.TryGetLocation(out origin, out basisX, out basisY);

        case Curve curve:
          return curve.TryGetLocation(out origin, out basisX, out basisY);

        case Edge edge:
          return edge.AsCurve().TryGetLocation(out origin, out basisX, out basisY);

        case Face face:
          using (var derivatives = face.ComputeDerivatives(new UV(0.5, 0.5), true))
          {
            origin = derivatives.Origin;
            basisX = derivatives.BasisX;
            basisY = derivatives.BasisY;
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
  }
}

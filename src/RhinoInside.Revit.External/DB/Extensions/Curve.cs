using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  using static NumericTolerance;

  public static class CurveExtension
  {
    public static bool IsSameKindAs(this Curve self, Curve other)
    {
      return self.IsBound == other.IsBound && self.GetType() == other.GetType();
    }

    #region IsAlmostEqualTo
    private static bool AreAlmostEqual(IList<XYZ> x, IList<XYZ> y, double toleance)
    {
      var count = x.Count;
      if (count != y.Count) return false;
      for (int p = 0; p < count; ++p)
      {
        if (!x[p].IsAlmostEqualTo(y[p], toleance))
          return false;
      }

      return true;
    }

    private static bool AreAlmostEqual(DoubleArray x, DoubleArray y, double toleance)
    {
      var count = x.Size;
      if (count != y.Size) return false;
      for (int p = 0; p < count; ++p)
      {
        if (!NumericTolerance.AreAlmostEqual(x.get_Item(p), y.get_Item(p), toleance))
          return false;
      }

      return true;
    }

    public static bool IsAlmostEqualTo(this Line self, Line other, double tolerance = DefaultTolerance) =>
      self.IsBound == other.IsBound &&
      self.Origin.IsAlmostEqualTo(other.Origin, tolerance) &&
      self.Direction.IsAlmostEqualTo(other.Direction, tolerance) &&
      (!self.IsBound || NumericTolerance.AreAlmostEqual(self.GetEndParameter(0), other.GetEndParameter(0), tolerance)) &&
      (!self.IsBound || NumericTolerance.AreAlmostEqual(self.GetEndParameter(1), other.GetEndParameter(1), tolerance));

    public static bool IsAlmostEqualTo(this Arc self, Arc other, double tolerance = DefaultTolerance) =>
      self.IsBound == other.IsBound &&
      self.IsCyclic == other.IsCyclic &&
      NumericTolerance.AreAlmostEqual(self.Radius, other.Radius, tolerance) &&
      (!self.IsBound || NumericTolerance.AreAlmostEqual(self.GetEndParameter(0), other.GetEndParameter(0), tolerance)) &&
      (!self.IsBound || NumericTolerance.AreAlmostEqual(self.GetEndParameter(1), other.GetEndParameter(1), tolerance)) &&
      self.Center.IsAlmostEqualTo(other.Center, tolerance) &&
      self.Normal.IsAlmostEqualTo(other.Normal, tolerance) &&
      self.XDirection.IsAlmostEqualTo(other.XDirection, tolerance) &&
      self.YDirection.IsAlmostEqualTo(other.YDirection, tolerance);

    public static bool IsAlmostEqualTo(this Ellipse self, Ellipse other, double tolerance = DefaultTolerance) =>
      self.IsBound == other.IsBound &&
      self.IsCyclic == other.IsCyclic &&
      self.Center.IsAlmostEqualTo(other.Center, tolerance) &&
      self.Normal.IsAlmostEqualTo(other.Normal, tolerance) &&
      self.XDirection.IsAlmostEqualTo(other.XDirection, tolerance) &&
      self.YDirection.IsAlmostEqualTo(other.YDirection, tolerance) &&
      (!self.IsBound || NumericTolerance.AreAlmostEqual(self.GetEndParameter(0), other.GetEndParameter(0), tolerance)) &&
      (!self.IsBound || NumericTolerance.AreAlmostEqual(self.GetEndParameter(1), other.GetEndParameter(1), tolerance)) &&
      NumericTolerance.AreAlmostEqual(self.RadiusX, other.RadiusX, tolerance) &&
      NumericTolerance.AreAlmostEqual(self.RadiusY, other.RadiusY, tolerance);

    public static bool IsAlmostEqualTo(this HermiteSpline self, HermiteSpline other, double tolerance = DefaultTolerance) =>
      self.IsBound == other.IsBound &&
      self.IsCyclic == other.IsCyclic &&
      AreAlmostEqual(self.ControlPoints, other.ControlPoints, tolerance) &&
      AreAlmostEqual(self.Tangents, other.Tangents, tolerance) &&
      AreAlmostEqual(self.Parameters, other.Parameters, tolerance);

    public static bool IsAlmostEqualTo(this NurbSpline self, NurbSpline other, double tolerance = DefaultTolerance) =>
      self.IsBound == other.IsBound &&
      self.IsCyclic == other.IsCyclic &&
      self.Degree == other.Degree &&
      self.isRational == other.isRational &&
      AreAlmostEqual(self.CtrlPoints, other.CtrlPoints, tolerance) &&
      AreAlmostEqual(self.Knots, other.Knots, tolerance) &&
      AreAlmostEqual(self.Weights, other.Weights, tolerance);

    public static bool IsAlmostEqualTo(this CylindricalHelix self, CylindricalHelix other, double tolerance = DefaultTolerance) =>
      self.IsBound == other.IsBound &&
      self.IsCyclic == other.IsCyclic &&
      self.IsRightHanded == other.IsRightHanded &&
      NumericTolerance.AreAlmostEqual(self.Height, other.Height, tolerance) &&
      NumericTolerance.AreAlmostEqual(self.Pitch, other.Pitch, tolerance) &&
      NumericTolerance.AreAlmostEqual(self.Radius, other.Radius, tolerance) &&
      NumericTolerance.AreAlmostEqual(self.GetEndParameter(0), other.GetEndParameter(0), tolerance) &&
      NumericTolerance.AreAlmostEqual(self.GetEndParameter(1), other.GetEndParameter(1), tolerance) &&
      self.BasePoint.IsAlmostEqualTo(other.BasePoint, tolerance) &&
      self.XVector.IsAlmostEqualTo(other.XVector, tolerance) &&
      self.YVector.IsAlmostEqualTo(other.YVector, tolerance) &&
      self.ZVector.IsAlmostEqualTo(other.ZVector, tolerance);

    public static bool IsAlmostEqualTo(this Curve self, Curve other, double tolerance = DefaultTolerance)
    {
      if (!IsSameKindAs(self, other)) return false;

      switch (self)
      {
        case Line selfLine: return IsAlmostEqualTo(selfLine, (Line) other, tolerance);
        case Arc selfArc: return IsAlmostEqualTo(selfArc, (Arc) other, tolerance);
        case Ellipse selfEllipse: return IsAlmostEqualTo(selfEllipse, (Ellipse) other, tolerance);
        case HermiteSpline selfHermite: return IsAlmostEqualTo(selfHermite, (HermiteSpline) other, tolerance);
        case NurbSpline selfNurb: return IsAlmostEqualTo(selfNurb, (NurbSpline) other, tolerance);
        case CylindricalHelix selfHelix: return IsAlmostEqualTo(selfHelix, (CylindricalHelix) other, tolerance);
      }

      throw new NotImplementedException();
    }
    #endregion

    public static IEnumerable<Curve> ToBoundedCurves(this Curve curve)
    {
      switch (curve)
      {
        case Arc arc:
          if (!arc.IsBound)
          {
            yield return Arc.Create(arc.Center, arc.Radius, 0.0, Math.PI, arc.XDirection, arc.YDirection);
            yield return Arc.Create(arc.Center, arc.Radius, Math.PI, Math.PI * 2.0, arc.XDirection, arc.YDirection);
          }
          else yield return arc;
          yield break;
        case Ellipse ellipse:
          if (!ellipse.IsBound)
          {
#if REVIT_2018
            yield return Ellipse.CreateCurve(ellipse.Center, ellipse.RadiusX, ellipse.RadiusY, ellipse.XDirection, ellipse.YDirection, 0.0, Math.PI);
            yield return Ellipse.CreateCurve(ellipse.Center, ellipse.RadiusX, ellipse.RadiusY, ellipse.XDirection, ellipse.YDirection, Math.PI, Math.PI * 2.0);
#else
            yield return Ellipse.Create(ellipse.Center, ellipse.RadiusX, ellipse.RadiusY, ellipse.XDirection, ellipse.YDirection, 0.0, Math.PI);
            yield return Ellipse.Create(ellipse.Center, ellipse.RadiusX, ellipse.RadiusY, ellipse.XDirection, ellipse.YDirection, Math.PI, Math.PI * 2.0);
#endif
          }
          else yield return ellipse;
          yield break;
        case Curve c: yield return c; yield break;
      }
    }

    public static CurveArray ToCurveArray(this IEnumerable<Curve> curves)
    {
      var curveArray = new CurveArray();
      foreach (var curve in curves)
        curveArray.Append(curve);

      return curveArray;
    }

    #region TryGetLocation
    public static bool TryGetLocation(this Line curve, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      var curveDirection = curve.Direction;
      if (!curveDirection.IsAlmostEqualTo(XYZ.Zero, DefaultTolerance))
      {
        if (curve.IsBound)
        {
          origin = curve.Evaluate(0.5, true);
          basisX = curveDirection.Normalize(0D);
          basisY = basisX.PerpVector().Normalize(0D);
          return true;
        }
        else
        {
          origin = curve.Origin;
          basisX = curveDirection.Normalize(0D);
          basisY = basisX.PerpVector().Normalize(0D);
          return true;
        }
      }

      origin = basisX = basisY = default;
      return false;
    }

    public static bool TryGetLocation(this Arc curve, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      if (curve.IsBound)
      {
        var start = curve.GetEndPoint(0);
        var end = curve.GetEndPoint(1);
        var curveDirection = end - start;

        if (!curveDirection.IsAlmostEqualTo(XYZ.Zero, DefaultTolerance))
        {
          origin = start + (curveDirection * 0.5);
          basisX = curveDirection.Normalize(0D);
          basisY = curve.Normal.CrossProduct(basisX).Normalize(0D);
          return true;
        }
      }
      else
      {
        origin = curve.Center;
        basisX = curve.XDirection;
        basisY = curve.YDirection;
        return true;
      }

      origin = basisX = basisY = default;
      return false;
    }

    public static bool TryGetLocation(this Ellipse curve, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      if (curve.IsBound)
      {
        var start = curve.GetEndPoint(0);
        var end = curve.GetEndPoint(1);
        var curveDirection = end - start;

        if (!curveDirection.IsAlmostEqualTo(XYZ.Zero, DefaultTolerance))
        {
          origin = start + (curveDirection * 0.5);
          basisX = curveDirection.Normalize(0D);
          basisY = curve.Normal.CrossProduct(basisX).Normalize(0D);
          return true;
        }
      }
      else
      {
        origin = curve.Center;
        basisX = curve.XDirection;
        basisY = curve.YDirection;
        return true;
      }

      origin = basisX = basisY = default;
      return false;
    }

    public static bool TryGetLocation(this CylindricalHelix curve, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      origin = curve.BasePoint;
      basisX = curve.XVector;
      basisY = curve.YVector;

      return true;
    }

    public static bool TryGetLocation(this NurbSpline curve, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      if (curve.IsBound)
      {
        var start = curve.GetEndPoint(0);
        var end = curve.GetEndPoint(1);
        var curveDirection = end - start;

        if (!curveDirection.IsAlmostEqualTo(XYZ.Zero, DenormalUpperBound))
        {
          origin = start + (curveDirection * 0.5);
          basisX = curveDirection.Normalize(0D);

          var normal = XYZ.Zero;
          {
            // Create the covariance matrix
            var cov = XYZExtension.ComputeCovariance(curve.CtrlPoints);
            bool planar = !cov.TryGetInverse(out var inverse);
            if (planar)
              inverse = cov;

            normal = inverse.GetPrincipalComponent(0D);

            if(planar)
              normal = basisX.CrossProduct(normal).Normalize(0D);
          }

          basisY = normal.CrossProduct(basisX).Normalize(0D);
          return true;
        }
      }
      else throw new NotImplementedException();

      origin = basisX = basisY = default;
      return false;
    }

    public static bool TryGetLocation(this PolyLine curve, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      switch (curve.NumberOfCoordinates)
      {
        case 0:
          origin = basisX = basisY = default;
          return false;

        case 1:
          origin = curve.GetCoordinate(0);
          basisX = XYZ.BasisX;
          basisY = XYZ.BasisY;
          return true;

        default:
          var start = curve.GetCoordinate(0);
          var end = curve.GetCoordinate(curve.NumberOfCoordinates - 1);
          if (start.IsAlmostEqualTo(end))
          {
            origin = XYZExtension.ComputeMeanPoint(curve.GetCoordinates());
            var axis = start - origin;
            basisX = axis.Normalize(0D);
            basisY = basisX.PerpVector();
          }
          else
          {
            var axis = end - start;
            origin = start + (axis * 0.5);
            basisX = axis.Normalize(0D);
            basisY = basisX.PerpVector();
          }
          return true;
      }
    }

    public static bool TryGetLocation(this Curve curve, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      switch (curve)
      {
        case Line line:               return line.TryGetLocation(out origin, out basisX, out basisY);
        case Arc arc:                 return arc.TryGetLocation(out origin, out basisX, out basisY);
        case Ellipse ellipse:         return ellipse.TryGetLocation(out origin, out basisX, out basisY);
        case CylindricalHelix helix:  return helix.TryGetLocation(out origin, out basisX, out basisY);
        case NurbSpline spline:       return spline.TryGetLocation(out origin, out basisX, out basisY);
        default: throw new NotImplementedException();
      }
    }
    #endregion

    #region TryGetPlane
    public static bool TryGetPlane(this Curve curve, out Plane plane)
    {
      using(var loop = new CurveLoop())
      {
        loop.Append(curve);
        if (loop.HasPlane())
        {
          plane = loop.GetPlane();
          return true;
        }
      }

      plane = default;
      return default;
    }
    #endregion

    #region TryGetCentroid
    public static bool TryGetCentroid(this IEnumerable<Curve> curves, out XYZ centroid)
    {
      centroid = XYZ.Zero;
      var count = 0;
      foreach (var curve in curves)
      {
        var t0 = curve.IsBound ? curve.GetEndParameter(CurveEnd.Start) : 0.0;
        var t1 = curve.IsBound ? curve.GetEndParameter(CurveEnd.End) : curve.IsCyclic ? curve.Period : 1.0;
        centroid += curve.Evaluate(t0, normalized: false);
        centroid += curve.Evaluate(t1, normalized: false);
        count += 2;
      }

      centroid /= count;
      return count > 0;
    }
    #endregion
  }
}

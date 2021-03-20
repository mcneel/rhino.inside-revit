using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class CurveExtension
  {
    public static bool IsSameKindAs(this Curve self, Curve other)
    {
      return self.IsBound == other.IsBound && self.GetType() == other.GetType();
    }

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
      if (!curveDirection.IsAlmostEqualTo(XYZ.Zero, XYZExtension.DefaultTolerance))
      {
        if (curve.IsBound)
        {
          origin = curve.Evaluate(0.5, true);
          basisX = curveDirection.Normalize(0D);
          basisY = basisX.PerpVector(0D).Normalize(0D);
          return true;
        }
        else
        {
          origin = curve.Origin;
          basisX = curveDirection.Normalize(0D);
          basisY = basisX.PerpVector(0D).Normalize(0D);
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

        if (!curveDirection.IsAlmostEqualTo(XYZ.Zero, XYZExtension.DefaultTolerance))
        {
          origin = start + (curveDirection * 0.5);
          basisX = curveDirection.Normalize(0D);
          basisY = curve.Normal.CrossProduct(basisX, 0D).Normalize(0D);
          return true;
        }
      }
      else
      {
        origin = curve.Center;
        basisX = curve.XDirection.Normalize(0D);
        basisY = curve.YDirection.Normalize(0D);
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

        if (!curveDirection.IsAlmostEqualTo(XYZ.Zero, XYZExtension.DefaultTolerance))
        {
          origin = start + (curveDirection * 0.5);
          basisX = curveDirection.Normalize(0D);
          basisY = curve.Normal.CrossProduct(basisX, 0D);
          return true;
        }
      }
      else
      {
        origin = curve.Center;
        basisX = curve.XDirection.Normalize(0D);
        basisY = curve.YDirection.Normalize(0D);
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

        if (!curveDirection.IsAlmostEqualTo(XYZ.Zero, XYZExtension.DenormalUpperBound))
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
              normal = basisX.CrossProduct(normal, 0D).Normalize(0D);
          }

          basisY = normal.CrossProduct(basisX, 0D).Normalize(0D);
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
            basisY = basisX.PerpVector(0D);
          }
          else
          {
            var axis = end - start;
            origin = start + (axis * 0.5);
            basisX = axis.Normalize(0D);
            basisY = basisX.PerpVector(0D);
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
    //public static bool TryGetPlane(this Curve curve, out Plane plane, double maxDeviation = 1e-9)
    //{
    //  if (TryGetLocation(curve, out var origin, out var basisX, out var basisY))
    //  {
    //    if (curve.IsInPlane(origin, basisX, basisY, maxDeviation))
    //    {
    //      plane = Plane.CreateByOriginAndBasis(origin, basisX, basisY);
    //      return true;
    //    }
    //  }

    //  plane = default;
    //  return false;
    //}
    #endregion
  }
}

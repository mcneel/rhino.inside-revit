using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class SolidExtension
  {
    /// <summary>
    /// Identifies if the solid is watertight. Is watertight if all edges are shared by two faces.
    /// </summary>
    /// <param name="solid"></param>
    /// <returns>It true, the solid is watertight and defines a closed volume.</returns>
    public static bool IsWatertight(this Solid solid)
    {
      foreach (var face in solid.Faces.Cast<Face>())
      {
        foreach (var loop in face.EdgeLoops.Cast<EdgeArray>())
        {
          foreach (var edge in loop.Cast<Edge>())
          {
            if (edge.GetFace(1) is null) return false;
          }
        }
      }

      return !solid.Faces.IsEmpty;
    }

    /// <summary>
    /// Identifies if the solid is watertight. Is watertight if all edges are shared by two faces.
    /// </summary>
    /// <param name="solid"></param>
    /// <param name="brepType"></param>
    /// <returns>It true, the solid is watertight and defines a closed volume.</returns>
    public static bool IsWatertight(this Solid solid, out BRepType brepType)
    {
      brepType = BRepType.OpenShell;
      var volume = solid.Volume;
      var watertight = IsWatertight(solid);

      if(watertight || System.Math.Abs(volume) > 1e-9)
        brepType = volume < 0.0 ? BRepType.Void : BRepType.Solid;

      return watertight;
    }

    public static bool TryGetNakedEdges(this Solid solid, out List<Edge> nakedEdges)
    {
      nakedEdges = default;
      foreach (var face in solid.Faces.Cast<Face>())
      {
        foreach (var loop in face.EdgeLoops.Cast<EdgeArray>())
        {
          foreach (var edge in loop.Cast<Edge>())
          {
            if (edge.GetFace(1) is null)
            {
              if (nakedEdges is null) nakedEdges = new List<Edge>();
              nakedEdges.Add(edge);
            }
          }
        }
      }

      return nakedEdges is object;
    }

    /// <summary>
    /// Indicates whether the specified point is within this solid.
    /// </summary>
    /// <param name="solid"></param>
    /// <param name="point"></param>
    /// <returns>True if within this solid or on its boundary, otherwise False.</returns>
    public static bool IsInside(this Solid solid, XYZ point)
    {
      if (!IsInside(solid, point, out var result)) return false;
      using (result) return true;
    }

    /// <summary>
    /// Indicates whether the specified point is within this solid.
    /// </summary>
    /// <param name="solid"></param>
    /// <param name="point"></param>
    /// <param name="result"></param>
    /// <returns>True if within this solid or on its boundary, otherwise False.</returns>
    public static bool IsInside(this Solid solid, XYZ point, out SolidCurveIntersection result)
    {
      using (var bbox = solid.GetBoundingBox())
      {
        result = null;
        if (!bbox.IsInside(point)) return false;

        var vector = bbox.Transform.Origin - point;
        if (vector.IsZeroLength(0D)) vector = XYZExtension.BasisZ;
        else vector = vector.Unitize();

        using (var line = Line.CreateBound(point, point + vector * 0.1))
        {
          try
          {
            using (var options = new SolidCurveIntersectionOptions() { ResultType = SolidCurveIntersectionMode.CurveSegmentsOutside })
            {
              result = solid.IntersectWithCurve(line, options);

              if (result?.SegmentCount > 0)
                return result.GetCurveSegmentExtents(0).StartParameter != 0.0;
              else
                return true;
            }
          }
          catch (Autodesk.Revit.Exceptions.ArgumentException) { }
        }
      }

      return false;
    }

    /// <summary>
    /// Projects the specified point on the solid.
    /// </summary>
    /// <param name="solid"></param>
    /// <param name="point"></param>
    /// <returns>Geometric information if projection is successful; if projection fails returns null</returns>
    public static IntersectionResult Project(this Solid solid, XYZ point)
    {
      // Project on faces
      var intersection = default(IntersectionResult);
      intersection = solid.Faces.Cast<Face>().
        Select(x => x.Project(point)).
        Where(x => x is object).
        OrderBy(x => x.Distance).
        FirstOrDefault();

      if (intersection is object) return intersection;

      // Project on edges
      intersection = solid.Edges.Cast<Edge>().
        Select(x => x.Project(point)).
        Where(x => x is object).
        OrderBy(x => x.Distance).
        FirstOrDefault();

      return intersection;
    }

    /// <summary>
    /// Projects the specified point on the solid.
    /// </summary>
    /// <param name="solid"></param>
    /// <param name="point"></param>
    /// <param name="face"></param>
    /// <returns>Geometric information if projection is successful; if projection fails returns null</returns>
    public static IntersectionResult Project(this Solid solid, XYZ point, out Face face)
    {
      // Project on faces
      var intersection = default(IntersectionResult);
      (intersection, face) = solid.Faces.Cast<Face>().
        Select(x => (Intersection: x.Project(point), Face: x)).
        Where(x => x.Intersection is object).
        OrderBy(x => x.Intersection.Distance).
        FirstOrDefault();

      if (face is object) return intersection;

      // Project on edges
      (intersection, face) = solid.Edges.Cast<Edge>().
        Select(x => (Intersection: x.Project(point, out var f), Face: f)).
        Where(x => x.Intersection is object).
        OrderBy(x => x.Intersection.Distance).
        FirstOrDefault();

      return intersection;
    }
  }

  public static class FaceExtension
  {
    public static bool MatchesSurfaceOrientation(this Face face)
    {
#if REVIT_2018
      return face.OrientationMatchesSurfaceOrientation;
#else
      return true;
#endif
    }

#if !REVIT_2018
    public static Surface GetSurface(this Face face)
    {
      switch(face)
      {
        case PlanarFace planarFace:
          return Plane.CreateByOriginAndBasis(planarFace.Origin, planarFace.XVector, planarFace.YVector);

        case ConicalFace conicalFace:
        {
          var basisX = conicalFace.get_Radius(0).Normalize();
          var basisY = conicalFace.get_Radius(1).Normalize();
          var basisZ = conicalFace.Axis.Normalize();
          return ConicalSurface.Create(new Frame(conicalFace.Origin, basisX, basisY, basisZ), conicalFace.HalfAngle);
        }

        case CylindricalFace cylindricalFace:
        {
          double radius = cylindricalFace.get_Radius(0).GetLength();
          var basisX = cylindricalFace.get_Radius(0).Normalize();
          var basisY = cylindricalFace.get_Radius(1).Normalize();
          var basisZ = cylindricalFace.Axis.Normalize();
          return CylindricalSurface.Create(new Frame(cylindricalFace.Origin, basisX, basisY, basisZ), radius);
        }

        case RevolvedFace revolvedFace:
        {
          var ECStoWCS = new Transform(Transform.Identity)
          {
            Origin = revolvedFace.Origin,
            BasisX = revolvedFace.get_Radius(0).Normalize(),
            BasisY = revolvedFace.get_Radius(1).Normalize(),
            BasisZ = revolvedFace.Axis.Normalize()
          };

          var profileInWCS = revolvedFace.Curve.CreateTransformed(ECStoWCS);

          return RevolvedSurface.Create(new Frame(ECStoWCS.Origin, ECStoWCS.BasisX, ECStoWCS.BasisY, ECStoWCS.BasisZ), profileInWCS);
        }
        case RuledFace ruledFace:
        {
          var profileCurve0 = ruledFace.get_Curve(0);
          var profileCurve1 = ruledFace.get_Curve(1);
          return RuledSurface.Create(profileCurve0, profileCurve1);
        }
      }

      return null;
    }

    public static Curve GetProfileCurveInWorldCoordinates(this RevolvedSurface revolvedSurface)
    {
      var profileCurve = revolvedSurface.GetProfileCurve();
      var ECStoWCS = new Transform(Transform.Identity)
      {
        Origin = revolvedSurface.Origin,
        BasisX = revolvedSurface.XDir.Normalize(),
        BasisY = revolvedSurface.YDir.Normalize(),
        BasisZ = revolvedSurface.Axis.Normalize()
      };

      return profileCurve.CreateTransformed(ECStoWCS);
    }

    public static bool HasFirstProfilePoint(this RuledSurface ruledSurface)
    {
      return ruledSurface.GetFirstProfilePoint() is object;
    }

    public static bool HasSecondProfilePoint(this RuledSurface ruledSurface)
    {
      return ruledSurface.GetSecondProfilePoint() is object;
    }
#endif

    public static XYZ Evaluate(this Face face, UV param, bool normalized)
    {
      return normalized ?
        face.Evaluate(face.GetBoundingBox().Evaluate(param)) :
        face.Evaluate(param);
    }

    public static XYZ ComputeNormal(this Face face, UV uv, bool normalized)
    {
      return normalized ?
        face.ComputeNormal(face.GetBoundingBox().Evaluate(uv)) :
        face.ComputeNormal(uv);
    }

    public static Transform ComputeDerivatives(this Face face, UV uv, bool normalized)
    {
      return normalized ?
        face.ComputeDerivatives(face.GetBoundingBox().Evaluate(uv)) :
        face.ComputeDerivatives(uv);
    }

    public static FaceSecondDerivatives ComputeSecondDerivatives(this Face face, UV uv, bool normalized)
    {
      return normalized ?
        face.ComputeSecondDerivatives(face.GetBoundingBox().Evaluate(uv)) :
        face.ComputeSecondDerivatives(uv);
    }

    /// <summary>
    /// Indicates whether the specified point is within this face.
    /// </summary>
    /// <param name="face"></param>
    /// <param name="uv"></param>
    /// <param name="normalized"></param>
    /// <returns>True if within this face or on its boundary, otherwise False.</returns>
    public static bool IsInside(this Face face, UV uv, bool normalized)
    {
      return normalized ?
        face.IsInside(face.GetBoundingBox().Evaluate(uv)) :
        face.IsInside(uv);
    }

    /// <summary>
    /// Indicates whether the specified point is within this face.
    /// </summary>
    /// <param name="face"></param>
    /// <param name="uv"></param>
    /// <param name="normalized"></param>
    /// <param name="result"></param>
    /// <returns>True if within this face or on its boundary, otherwise False.</returns>
    public static bool IsInside(this Face face, UV uv, bool normalized, out IntersectionResult result)
    {
      return normalized ?
        face.IsInside(face.GetBoundingBox().Evaluate(uv), out result) :
        face.IsInside(uv, out result);
    }
  }

  public static class EdgeExtension
  {
    /// <summary>
    /// Projects the specified point on the edge.
    /// </summary>
    /// <param name="edge"></param>
    /// <param name="point"></param>
    /// <returns>Geometric information if projection is successful; if projection fails returns null</returns>
    public static IntersectionResult Project(this Edge edge, XYZ point)
    {
      try
      {
        var curve = edge.AsCurve();
        var intersection = curve.Project(point);
        intersection.SetEdgeObject(edge);
        intersection.SetEdgeParameter(curve.GetNormalizedParameter(intersection.Parameter));

        return intersection;
      }
      catch { return default; }
    }

    /// <summary>
    /// Projects the specified point on the edge.
    /// </summary>
    /// <param name="edge"></param>
    /// <param name="point"></param>
    /// <param name="face"></param>
    /// <returns>Geometric information if projection is successful; if projection fails returns null</returns>
    public static IntersectionResult Project(this Edge edge, XYZ point, out Face face)
    {
      face = default;

      try
      {
        var intersection = edge.Project(point);
        var vector = point - intersection.XYZPoint;

        var faces = new Face[] { edge.GetFace(0), edge.GetFace(1) };
        if (vector.IsZeroLength(0D))
        {
          face = faces[0] ?? faces[1];
        }
        else
        {
          vector = vector.Unitize();

          var dot0 = double.PositiveInfinity;
          if (faces[0] is object)
          {
            var uv = edge.EvaluateOnFace(intersection.EdgeParameter, faces[0]);
            var normal = faces[0].ComputeNormal(uv);
            dot0 = normal.AngleTo(vector);
          }

          var dot1 = double.PositiveInfinity;
          if (faces[1] is object)
          {
            var uv = edge.EvaluateOnFace(intersection.EdgeParameter, faces[1]);
            var normal = faces[1].ComputeNormal(uv);
            dot1 = normal.AngleTo(vector);
          }

          face = dot1 < dot0 ? faces[1] : faces[0];
        }

        var distance = intersection.Distance;
        var parameter = intersection.Parameter;
        intersection = face.Project(intersection.XYZPoint) ?? intersection;

        // Update Distance and Parameter
        intersection.SetDistance(distance);
        intersection.SetParameter(parameter);
        return intersection;
      }
      catch { return default; }
    }
  }

  static class IntersectionResultExtension
  {
    static void SetPropertyValue(this IntersectionResult intersection, string name, object value)
    {
      var IntersectionResultType = typeof(IntersectionResult);
      const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty;
      if (IntersectionResultType.GetProperty(name, bindingFlags) is PropertyInfo property)
        property.SetValue(intersection, value);
      else
        throw new ArgumentOutOfRangeException(nameof(name), $"Property {name} was not found in Type {IntersectionResultType}");
    }

    public static void SetEdgeParameter(this IntersectionResult intersection, double edgeParameter) =>
      SetPropertyValue(intersection, nameof(IntersectionResult.EdgeParameter), edgeParameter);

    public static void SetEdgeObject(this IntersectionResult intersection, Edge edge) =>
      SetPropertyValue(intersection, nameof(IntersectionResult.EdgeObject), edge);

    public static void SetDistance(this IntersectionResult intersection, double distance) =>
      SetPropertyValue(intersection, nameof(IntersectionResult.Distance), distance);

    public static void SetParameter(this IntersectionResult intersection, double parameter) =>
      SetPropertyValue(intersection, nameof(IntersectionResult.Parameter), parameter);
  }
}

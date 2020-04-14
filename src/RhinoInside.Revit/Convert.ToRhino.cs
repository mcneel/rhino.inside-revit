using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rhino.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit
{
  public static partial class Convert
  {
    static class ToRhinoLengthUnitsStatic
    {
      static ToRhinoLengthUnitsStatic()
      {
        foreach (var unit in DB.UnitUtils.GetValidDisplayUnits(DB.UnitType.UT_Length))
        {
          var revit = DB.UnitUtils.Convert(1.0, DB.DisplayUnitType.DUT_METERS, unit);
          var rhino = Rhino.RhinoMath.UnitScale(Rhino.UnitSystem.Meters, unit.ToRhinoLengthUnits());
          //Debug.Assert(Rhino.RhinoMath.EpsilonEquals(revit, rhino, Rhino.RhinoMath.ZeroTolerance), $"ToRhinoLengthUnits({unit}) fails!!");
        }
      }

      [Conditional("DEBUG")]
      internal static void Assert() { }
    }

    public static Rhino.UnitSystem ToRhinoLengthUnits(this DB.DisplayUnitType value)
    {
      ToRhinoLengthUnitsStatic.Assert();

      if (!DB.UnitUtils.IsValidDisplayUnit(DB.UnitType.UT_Length, value))
        throw new ConversionException($"{value} is not a length unit");

      switch (value)
      {
        case DB.DisplayUnitType.DUT_METERS:                 return Rhino.UnitSystem.Meters;
        case DB.DisplayUnitType.DUT_METERS_CENTIMETERS:     return Rhino.UnitSystem.Meters;
        case DB.DisplayUnitType.DUT_DECIMETERS:             return Rhino.UnitSystem.Decimeters;
        case DB.DisplayUnitType.DUT_CENTIMETERS:            return Rhino.UnitSystem.Centimeters;
        case DB.DisplayUnitType.DUT_MILLIMETERS:            return Rhino.UnitSystem.Millimeters;

        case DB.DisplayUnitType.DUT_FRACTIONAL_INCHES:      return Rhino.UnitSystem.Inches;
        case DB.DisplayUnitType.DUT_DECIMAL_INCHES:         return Rhino.UnitSystem.Inches;
        case DB.DisplayUnitType.DUT_FEET_FRACTIONAL_INCHES: return Rhino.UnitSystem.Feet;
        case DB.DisplayUnitType.DUT_DECIMAL_FEET:           return Rhino.UnitSystem.Feet;
#if REVIT_2021
        case DB.DisplayUnitType.DUT_DECIMAL_US_SURVEY_FEET: return Rhino.UnitSystem.Feet;
#endif
      }

      Debug.Fail($"{value} conversion is not implemented");
      return Rhino.UnitSystem.Unset;
    }

    public static System.Drawing.Color ToRhino(this DB.Color c)
    {
      return System.Drawing.Color.FromArgb(c.Red, c.Green, c.Blue);
    }

    static readonly Rhino.Display.DisplayMaterial defaultMaterial = new Rhino.Display.DisplayMaterial(System.Drawing.Color.WhiteSmoke);
    public static Rhino.Display.DisplayMaterial ToRhino(this DB.Material material, Rhino.Display.DisplayMaterial parentMaterial)
    {
      return (material is null) ? parentMaterial ?? defaultMaterial :
        new Rhino.Display.DisplayMaterial()
        {
          Diffuse = material.Color.ToRhino(),
          Transparency = material.Transparency / 100.0,
          Shine = material.Shininess / 128.0
        };
    }

    public static Point3d ToRhino(this DB.XYZ p)
    {
      return new Point3d(p.X, p.Y, p.Z);
    }

    public static IEnumerable<Point3d> ToRhino(this IEnumerable<DB.XYZ> points)
    {
      foreach (var p in points)
        yield return p.ToRhino();
    }

    public static BoundingBox ToRhino(this DB.BoundingBoxXYZ bbox)
    {
      if (bbox?.Enabled ?? false)
      {
        var box = new BoundingBox(bbox.Min.ToRhino(), bbox.Max.ToRhino());
        return bbox.Transform.ToRhino().TransformBoundingBox(box);
      }

      return BoundingBox.Empty;
    }

    public static Transform ToRhino(this DB.Transform transform)
    {
      var value = new Transform
      {
        M00 = transform.BasisX.X,
        M10 = transform.BasisX.Y,
        M20 = transform.BasisX.Z,
        M30 = 0.0,

        M01 = transform.BasisY.X,
        M11 = transform.BasisY.Y,
        M21 = transform.BasisY.Z,
        M31 = 0.0,

        M02 = transform.BasisZ.X,
        M12 = transform.BasisZ.Y,
        M22 = transform.BasisZ.Z,
        M32 = 0.0,

        M03 = transform.Origin.X,
        M13 = transform.Origin.Y,
        M23 = transform.Origin.Z,
        M33 = 1.0
      };

      return value;
    }

    public static Point ToRhino(this DB.Point point)
    {
      return new Point(point.Coord.ToRhino());
    }

    public static Plane ToRhino(this DB.Plane plane)
    {
      return new Plane(plane.Origin.ToRhino(), (Vector3d) plane.XVec.ToRhino(), (Vector3d) plane.YVec.ToRhino());
    }

    #region Curves
    public static LineCurve ToRhino(this DB.Line line)
    {
      var l = new Line(line.GetEndPoint(0).ToRhino(), line.GetEndPoint(1).ToRhino());
      return line.IsBound ?
        new LineCurve(l, line.GetEndParameter(0), line.GetEndParameter(1)) :
        null;
    }

    public static ArcCurve ToRhino(this DB.Arc arc)
    {
      return arc.IsBound ?
        new ArcCurve
        (
          new Arc(arc.GetEndPoint(0).ToRhino(), arc.Evaluate(0.5, true).ToRhino(), arc.GetEndPoint(1).ToRhino()),
          arc.GetEndParameter(0),
          arc.GetEndParameter(1)
        ) :
        new ArcCurve
        (
          new Circle(new Plane(arc.Center.ToRhino(), new Vector3d(arc.XDirection.ToRhino()), new Vector3d(arc.YDirection.ToRhino())), arc.Radius),
          0.0,
          2.0 * Math.PI
        );
    }

    public static NurbsCurve ToRhino(this DB.Ellipse ellipse)
    {
      var plane = new Plane(ellipse.Center.ToRhino(), new Vector3d(ellipse.XDirection.ToRhino()), new Vector3d(ellipse.YDirection.ToRhino()));
      var e = new Ellipse(plane, ellipse.RadiusX, ellipse.RadiusY);
      var nurbsCurve = e.ToNurbsCurve();

      if (ellipse.IsBound)
      {
        nurbsCurve.ClosestPoint(ellipse.GetEndPoint(0).ToRhino(), out var param0);
        if (!nurbsCurve.ChangeClosedCurveSeam(param0))
          nurbsCurve.Domain = new Interval(param0, param0 + nurbsCurve.Domain.Length);

        nurbsCurve.ClosestPoint(ellipse.GetEndPoint(1).ToRhino(), out var param1);
        nurbsCurve = nurbsCurve.Trim(param0, param1) as NurbsCurve;
        nurbsCurve.Domain = new Interval(ellipse.GetEndParameter(0), ellipse.GetEndParameter(1));
      }

      return nurbsCurve;
    }

    public static NurbsCurve ToRhino(this DB.HermiteSpline hermite)
    {
      var nurbsCurve = DB.NurbSpline.Create(hermite).ToRhino();
      nurbsCurve.Domain = new Interval(hermite.GetEndParameter(0), hermite.GetEndParameter(1));
      return nurbsCurve;
    }

    public static NurbsCurve ToRhino(this DB.NurbSpline nurb)
    {
      var controlPoints = nurb.CtrlPoints;
      var n = new NurbsCurve(3, nurb.isRational, nurb.Degree + 1, controlPoints.Count);

      if (nurb.isRational)
      {
        using (var Weights = nurb.Weights)
        {
          var weights = Weights.Cast<double>().ToArray();
          int index = 0;
          foreach (var pt in controlPoints)
          {
            var w = weights[index];
            n.Points.SetPoint(index++, pt.X * w, pt.Y * w, pt.Z * w, w);
          }
        }
      }
      else
      {
        int index = 0;
        foreach (var pt in controlPoints)
          n.Points.SetPoint(index++, pt.X, pt.Y, pt.Z);
      }

      using (var Knots = nurb.Knots)
      {
        int index = 0;
        foreach (var w in Knots.Cast<double>().Skip(1).Take(n.Knots.Count))
          n.Knots[index++] = w;
      }

      return n;
    }

    public static NurbsCurve ToRhino(this DB.CylindricalHelix helix)
    {
      var nurbsCurve = NurbsCurve.CreateSpiral
      (
        helix.BasePoint.ToRhino(),
        (Vector3d) helix.ZVector.ToRhino(),
        helix.BasePoint.ToRhino() + ((Vector3d) helix.XVector.ToRhino()),
        helix.Pitch,
        helix.IsRightHanded ? +1 : -1,
        helix.Radius,
        helix.Radius
      );

      nurbsCurve.Domain = new Interval(helix.GetEndParameter(0), helix.GetEndParameter(1));
      return nurbsCurve;
    }

    public static Curve ToRhino(this DB.Curve curve)
    {
      switch (curve)
      {
        case null:                      return null;
        case DB.Line line:              return line.ToRhino();
        case DB.Arc arc:                return arc.ToRhino();
        case DB.Ellipse ellipse:        return ellipse.ToRhino();
        case DB.HermiteSpline hermite:  return hermite.ToRhino();
        case DB.NurbSpline nurb:        return nurb.ToRhino();
        case DB.CylindricalHelix helix: return helix.ToRhino();
        default: throw new NotImplementedException();
      }
    }

    public static PolylineCurve ToRhino(this DB.PolyLine polyline)
    {
      return new PolylineCurve(polyline.GetCoordinates().ToRhino());
    }
    #endregion

    #region Surfaces
    static PlaneSurface FromPlane(DB.XYZ origin, DB.XYZ xDir, DB.XYZ yDir, DB.XYZ zDir, DB.BoundingBoxUV bboxUV, double relativeTolerance)
    {
      var ctol = relativeTolerance * Revit.ShortCurveTolerance;
      var uu = new Interval(bboxUV.Min.U - ctol, bboxUV.Max.U + ctol);
      var vv = new Interval(bboxUV.Min.V - ctol, bboxUV.Max.V + ctol);

      var plane = new Plane
      (
        origin.ToRhino(),
        (Vector3d) xDir.ToRhino(),
        (Vector3d) yDir.ToRhino()
      );

      return new PlaneSurface(plane, uu, vv);
    }

    public static PlaneSurface ToRhinoSurface(this DB.PlanarFace face, double relativeTolerance) => FromPlane
    (
      face.Origin,
      face.XVector,
      face.YVector,
      face.FaceNormal,
      face.GetBoundingBox(),
      relativeTolerance
    );

    public static PlaneSurface ToRhino(this DB.Plane surface, DB.BoundingBoxUV bboxUV) => FromPlane
    (
      surface.Origin,
      surface.XVec,
      surface.YVec,
      surface.Normal,
      bboxUV,
      0.0
    );

    static RevSurface FromConicalSurface(DB.XYZ origin, DB.XYZ xDir, DB.XYZ yDir, DB.XYZ zDir, double halfAngle, DB.BoundingBoxUV bboxUV, double relativeTolerance)
    {
      var atol = relativeTolerance * Revit.AngleTolerance * 10.0;
      var ctol = relativeTolerance * Revit.ShortCurveTolerance;
      var uu = new Interval(bboxUV.Min.U - atol, bboxUV.Max.U + atol);
      var vv = new Interval(bboxUV.Min.V - ctol, bboxUV.Max.V + ctol);

      var plane = new Plane
      (
        origin.ToRhino(),
        (Vector3d) xDir.ToRhino(),
        (Vector3d) yDir.ToRhino()
      );
      var axisDir = (Vector3d) zDir.ToRhino();

      var dir = axisDir + Math.Tan(halfAngle) * plane.XAxis;
      dir.Unitize();

      var curve = new LineCurve
      (
        new Line
        (
          plane.Origin + (vv.Min * dir),
          plane.Origin + (vv.Max * dir)
        ),
        vv.Min,
        vv.Max
      );

      var axis = new Line(plane.Origin, plane.Normal);
      return RevSurface.Create(curve, axis, uu.Min, uu.Max);
    }

    public static RevSurface ToRhinoSurface(this DB.ConicalFace face, double relativeTolerance) => FromConicalSurface
    (
      face.Origin,
      face.get_Radius(0),
      face.get_Radius(1),
      face.Axis,
      face.HalfAngle,
      face.GetBoundingBox(),
      relativeTolerance
    );

    public static RevSurface ToRhino(this DB.ConicalSurface surface, DB.BoundingBoxUV bboxUV) => FromConicalSurface
    (
      surface.Origin,
      surface.XDir,
      surface.YDir,
      surface.Axis,
      surface.HalfAngle,
      bboxUV,
      0.0
    );

    static RevSurface FromCylindricalSurface(DB.XYZ origin, DB.XYZ xDir, DB.XYZ yDir, DB.XYZ zDir, double radius, DB.BoundingBoxUV bboxUV, double relativeTolerance)
    {
      var atol = relativeTolerance * Revit.AngleTolerance;
      var ctol = relativeTolerance * Revit.ShortCurveTolerance;
      var uu = new Interval(bboxUV.Min.U - atol, bboxUV.Max.U + atol);
      var vv = new Interval(bboxUV.Min.V - ctol, bboxUV.Max.V + ctol);

      var plane = new Plane
      (
        origin.ToRhino(),
        (Vector3d) xDir.ToRhino(),
        (Vector3d) yDir.ToRhino()
      );
      var axisDir = (Vector3d) zDir.ToRhino();

      var curve = new LineCurve
      (
        new Line
        (
          plane.Origin + (radius * plane.XAxis) + (vv.Min * axisDir),
          plane.Origin + (radius * plane.XAxis) + (vv.Max * axisDir)
        ),
        vv.Min,
        vv.Max
      );

      var axis = new Line(plane.Origin, plane.Normal);
      return RevSurface.Create(curve, axis, uu.Min, uu.Max);
    }

    public static RevSurface ToRhinoSurface(this DB.CylindricalFace face, double relativeTolerance) => FromCylindricalSurface
    (
      face.Origin,
      face.get_Radius(0),
      face.get_Radius(1),
      face.Axis,
      face.get_Radius(0).GetLength(),
      face.GetBoundingBox(),
      relativeTolerance
    );

    public static RevSurface ToRhino(this DB.CylindricalSurface surface, DB.BoundingBoxUV bboxUV) => FromCylindricalSurface
    (
      surface.Origin,
      surface.XDir,
      surface.YDir,
      surface.Axis,
      surface.Radius,
      bboxUV,
      0.0
    );

    static RevSurface FromRevolvedSurface(DB.XYZ origin, DB.XYZ xDir, DB.XYZ yDir, DB.XYZ zDir, DB.Curve curve, DB.BoundingBoxUV bboxUV, double relativeTolerance)
    {
      var atol = relativeTolerance * Revit.AngleTolerance;
      var ctol = relativeTolerance * Revit.ShortCurveTolerance;
      var uu = new Interval(bboxUV.Min.U - atol, bboxUV.Max.U + atol);

      var plane = new Plane
      (
        origin.ToRhino(),
        (Vector3d) xDir.ToRhino(),
        (Vector3d) yDir.ToRhino()
      );
      var axisDir = (Vector3d) zDir.ToRhino();

      using (var ECStoWCS = new DB.Transform(DB.Transform.Identity) {Origin = origin, BasisX = xDir.Normalize(), BasisY = yDir.Normalize(), BasisZ = zDir.Normalize() })
      {
        var c = curve.CreateTransformed(ECStoWCS).ToRhino();
        c = ctol == 0.0 ? c : c.Extend(CurveEnd.Both, ctol, CurveExtensionStyle.Smooth);

        var axis = new Line(plane.Origin, plane.Normal);
        return RevSurface.Create(c, axis, uu.Min, uu.Max);
      }
    }

    public static RevSurface ToRhinoSurface(this DB.RevolvedFace face, double relativeTolerance) => FromRevolvedSurface
    (
      face.Origin,
      face.get_Radius(0),
      face.get_Radius(1),
      face.Axis,
      face.Curve,
      face.GetBoundingBox(),
      relativeTolerance
    );

    public static RevSurface ToRhino(this DB.RevolvedSurface surface, DB.BoundingBoxUV bboxUV) => FromRevolvedSurface
    (
      surface.Origin,
      surface.XDir,
      surface.YDir,
      surface.Axis,
      surface.GetProfileCurve(),
      bboxUV,
      0.0
    );

    static Surface FromRuledSurface(IList<DB.Curve> curves, DB.XYZ start, DB.XYZ end, DB.BoundingBoxUV bboxUV, double relativeTolerance)
    {
      var ctol = relativeTolerance * Revit.ShortCurveTolerance;

      var cs = curves.Where(x => x is object).Select
      (
        x =>
        {
          var c = x.ToRhino(); c.Reverse();
          return ctol == 0.0 ? c : c.Extend(CurveEnd.Both, ctol, CurveExtensionStyle.Smooth);
        }
      );

      Point3d p0 = start?.ToRhino() ?? Point3d.Unset,
              pN = end?.ToRhino()   ?? Point3d.Unset;

      var lofts = Brep.CreateFromLoft(cs, p0, pN, LoftType.Straight, false);
      if (lofts.Length == 1 && lofts[0].Faces.Count == 1)
        return lofts[0].Faces[0].DuplicateSurface();

      return null;
    }

    public static Surface ToRhinoSurface(this DB.RuledFace face, double relativeTolerance)
    {
      using (var surface = face.GetSurface() as DB.RuledSurface)
      {
        return FromRuledSurface
        (
          new DB.Curve[] { surface.GetFirstProfileCurve(), surface.GetSecondProfileCurve() },
          surface.HasFirstProfilePoint() ? surface.GetFirstProfilePoint() : null,
          surface.HasSecondProfilePoint() ? surface.GetSecondProfilePoint() : null,
          face.GetBoundingBox(),
          relativeTolerance
        );
      }
    }

    public static Surface ToRhino(this DB.RuledSurface surface, DB.BoundingBoxUV bboxUV) => FromRuledSurface
    (
      new DB.Curve[] { surface.GetFirstProfileCurve(), surface.GetSecondProfileCurve() },
      surface.HasFirstProfilePoint()  ? surface.GetFirstProfilePoint() : null,
      surface.HasSecondProfilePoint() ? surface.GetSecondProfilePoint() : null,
      bboxUV,
      0.0
    );

    static NurbsSurface FromHermiteSurface
    (
      IList<DB.XYZ> points, IList<DB.XYZ> mixedDerivs,
      IList<double> paramsU, IList<double> paramsV,
      IList<DB.XYZ> tangentsU, IList<DB.XYZ> tangentsV
    )
    {
      return null;
      //throw new NotImplementedException();
      //return NurbsSurface.CreateHermiteSurface
      //(
      //  points.Select(x => x.ToRhino()),
      //  mixedDerivs.Select(x => (Vector3d) x.ToRhino()),
      //  paramsU, paramsV,
      //  tangentsU.Select(x => (Vector3d) x.ToRhino()),
      //  tangentsV.Select(x => (Vector3d) x.ToRhino())
      //);
    }

    public static NurbsSurface ToRhinoSurface(this DB.HermiteFace face, double relativeTolerance)
    {
      NurbsSurface nurbsSurface = default;
      try
      {
#if REVIT_2021
        using (var surface = DB.ExportUtils.GetNurbsSurfaceDataForSurface(face.GetSurface()))
          nurbsSurface = surface.ToRhino(face.GetBoundingBox());
#else
        using (var surface = DB.ExportUtils.GetNurbsSurfaceDataForFace(face))
          nurbsSurface = surface.ToRhino(face.GetBoundingBox());
#endif
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }

      if (nurbsSurface is null)
      {
        nurbsSurface = FromHermiteSurface
        (
          face.Points,
          face.MixedDerivs,
          face.get_Params(0).Cast<double>().ToArray(),
          face.get_Params(1).Cast<double>().ToArray(),
          face.get_Tangents(0),
          face.get_Tangents(1)
        );
      }

      if (nurbsSurface is object)
      {
        double ctol = relativeTolerance * Revit.ShortCurveTolerance * 5.0;
        if (ctol != 0.0)
        {
          // Extend using smooth way avoids creating C2 discontinuities
          nurbsSurface = nurbsSurface.Extend(IsoStatus.West, ctol, true) as NurbsSurface ?? nurbsSurface;
          nurbsSurface = nurbsSurface.Extend(IsoStatus.East, ctol, true) as NurbsSurface ?? nurbsSurface;
          nurbsSurface = nurbsSurface.Extend(IsoStatus.South, ctol, true) as NurbsSurface ?? nurbsSurface;
          nurbsSurface = nurbsSurface.Extend(IsoStatus.North, ctol, true) as NurbsSurface ?? nurbsSurface;
        }
      }

      return nurbsSurface;
    }

    public static NurbsSurface ToRhino(this DB.NurbsSurfaceData surface, DB.BoundingBoxUV bboxUV)
    {
      var degreeU = surface.DegreeU;
      var degreeV = surface.DegreeV;

      var knotsU = surface.GetKnotsU();
      var knotsV = surface.GetKnotsV();

      int controlPointCountU = knotsU.Count - degreeU - 1;
      int controlPointCountV = knotsV.Count - degreeV - 1;

      var nurbsSurface = NurbsSurface.Create(3, surface.IsRational, degreeU + 1, degreeV + 1, controlPointCountU, controlPointCountV);

      var controlPoints = surface.GetControlPoints();
      var weights = surface.GetWeights();

      var points = nurbsSurface.Points;
      for (int u = 0; u < controlPointCountU; u++)
      {
        int u_offset = u * controlPointCountV;
        for (int v = 0; v < controlPointCountV; v++)
        {
          var pt = controlPoints[u_offset + v];
          if (surface.IsRational)
          {
            double w = weights[u_offset + v];
            points.SetPoint(u, v, pt.X * w, pt.Y * w, pt.Z * w, w);
          }
          else
          {
            points.SetPoint(u, v, pt.X, pt.Y, pt.Z);
          }
        }
      }

      {
        var knots = nurbsSurface.KnotsU;
        int index = 0;
        foreach (var w in knotsU.Skip(1).Take(knots.Count))
          knots[index++] = w;
      }

      {
        var knots = nurbsSurface.KnotsV;
        int index = 0;
        foreach (var w in knotsV.Skip(1).Take(knots.Count))
          knots[index++] = w;
      }

      return nurbsSurface;
    }

    public static Surface ToRhinoSurface(this DB.Face face, double relativeTolerance = 0.0)
    {
      switch (face)
      {
        case null:                            return null;
        case DB.PlanarFace planar:            return planar.ToRhinoSurface(relativeTolerance);
        case DB.ConicalFace conical:          return conical.ToRhinoSurface(relativeTolerance);
        case DB.CylindricalFace cylindrical:  return cylindrical.ToRhinoSurface(relativeTolerance);
        case DB.RevolvedFace revolved:        return revolved.ToRhinoSurface(relativeTolerance);
        case DB.RuledFace ruled:              return ruled.ToRhinoSurface(relativeTolerance);
        case DB.HermiteFace hermite:          return hermite.ToRhinoSurface(relativeTolerance);
        default:                              throw new NotImplementedException();
      }
    }
    #endregion

    #region Brep
#if NOTDEFINED
    static IEnumerable<Curve> ToRhino(this IEnumerable<DB.CurveLoop> loops)
    {
      foreach (var loop in loops)
      {
        var curves = Curve.JoinCurves(loop.Select(x => x.ToRhino()), Revit.ShortCurveTolerance, false);
        if (curves.Length != 1)
          throw new ConversionException("Failed to found one and only one closed loop.");

        yield return curves[0];
      }
    }

    static Brep JoinAndMerge(this ICollection<Brep> brepFaces, double tolerance)
    {
      if (brepFaces.Count == 0)
        return null;

      if (brepFaces.Count == 1)
        return brepFaces.First();

      var joinedBreps = Brep.JoinBreps(brepFaces.OfType<Brep>(), tolerance) ?? brepFaces;
      if (joinedBreps.Count == 1)
        return joinedBreps.First();

      var merged = Brep.MergeBreps(joinedBreps, Rhino.RhinoMath.UnsetValue);
      if (merged?.IsValid == false)
        merged.Repair(tolerance);

      return merged;
    }

    static Brep TrimFaces(this Brep brep, IEnumerable<Curve> loops)
    {
      var brepFaces = new List<Brep>();

      foreach (var brepFace in brep?.Faces ?? Enumerable.Empty<BrepFace>())
      {
        var trimmedBrep = brepFace.Split(loops, Revit.VertexTolerance);

        if (trimmedBrep is object)
        {
          // Remove ears, faces with edges not over 'loops'
          foreach (var trimmedFace in trimmedBrep.Faces.OrderByDescending(x => x.FaceIndex))
          {
            var boundaryEdges = trimmedFace.Loops.
                                SelectMany(loop => loop.Trims).
                                Where(trim => trim.TrimType == BrepTrimType.Boundary).
                                Select(trim => trim.Edge);

            foreach (var edge in boundaryEdges)
            {
              var midPoint = edge.PointAt(edge.Domain.Mid);

              var midPointOnAnyLoop = loops.Where(x => x.ClosestPoint(midPoint, out var _, Revit.VertexTolerance)).Any();
              if (!midPointOnAnyLoop)
              {
                trimmedBrep.Faces.RemoveAt(trimmedFace.FaceIndex);
                break;
              }
            }
          }

          // Remove holes, faces with no boundary edges
          foreach (var trimmedFace in trimmedBrep.Faces.OrderByDescending(x => x.FaceIndex))
          {
            var boundaryTrims = trimmedFace.Loops.
                                SelectMany(loop => loop.Trims).
                                Where(trim => trim.TrimType == BrepTrimType.Boundary);

            if (!boundaryTrims.Any())
            {
              trimmedBrep.Faces.RemoveAt(trimmedFace.FaceIndex);
              continue;
            }
          }

          if (!trimmedBrep.IsValid)
            trimmedBrep.Repair(Revit.VertexTolerance);

          trimmedBrep.Compact();
          brepFaces.Add(trimmedBrep);
        }
      }

      return brepFaces.Count == 0 ?
             brep.DuplicateBrep() :
             brepFaces.JoinAndMerge(Revit.VertexTolerance);
    }

    public static Brep ToRhino(this DB.Face face)
    {
      var surface = face.ToRhinoSurface(1.0);
      if (surface is null)
        return null;

      var brep = Brep.CreateFromSurface(surface);
      if (brep is null)
        return null;

      if (!face.OrientationMatchesSurface())
        brep.Flip();

      var loops = face.GetEdgesAsCurveLoops().ToRhino().ToArray();

      try { return brep.TrimFaces(loops); }
      finally { brep.Dispose(); }
    }

    public static Brep ToRhino(this DB.Solid solid)
    {
      return solid.Faces.
             Cast<DB.Face>().
             Select(x => x.ToRhino()).
             ToArray().
             JoinAndMerge(Revit.VertexTolerance);
    }
#else

    struct BrepBoundary
    {
      public BrepLoopType type;
      public List<BrepEdge> edges;
      public PolyCurve trims;
      public List<int> orientation;
    }

    static int AddSurface(this Brep brep, DB.Face face, out List<BrepBoundary>[] shells, Dictionary<DB.Edge, BrepEdge> brepEdges = null)
    {
      // Extract base surface
      if(face.ToRhinoSurface() is Surface surface)
      {
        double trimTolerance = Revit.VertexTolerance * 0.1;
        int si = brep.AddSurface(surface);

        if (surface is PlaneSurface planar)
        {
          var nurbs = planar.ToNurbsSurface();
          nurbs.KnotsU.InsertKnot(surface.Domain(0).Mid);
          nurbs.KnotsV.InsertKnot(surface.Domain(1).Mid);
          surface = nurbs;
        }

        // Extract and classify Edge Loops
        var edgeLoops = new List<BrepBoundary>();
        foreach (var edgeLoop in face.EdgeLoops.Cast<DB.EdgeArray>())
        {
          if (edgeLoop.IsEmpty)
            continue;

          var edges = edgeLoop.Cast<DB.Edge>();
          if (!face.OrientationMatchesSurface())
            edges = edges.Reverse();

          var loop = new BrepBoundary()
          {
            type = BrepLoopType.Unknown,
            edges = new List<BrepEdge>(),
            trims = new PolyCurve(),
            orientation = new List<int>()
          };

          foreach (var edge in edges)
          {
            var brepEdge = default(BrepEdge);
            if (brepEdges?.TryGetValue(edge, out brepEdge) != true)
            {
              var curve = edge.AsCurve();
              if (curve is null)
                continue;

              brepEdge = brep.Edges.Add(brep.AddEdgeCurve(curve.ToRhino()));
              brepEdges?.Add(edge, brepEdge);
            }

            loop.edges.Add(brepEdge);
            var segment = edge.AsCurveFollowingFace(face).ToRhino();

            if (!face.OrientationMatchesSurface())
              segment.Reverse();

            loop.orientation.Add(segment.TangentAt(segment.Domain.Mid).IsParallelTo(brepEdge.TangentAt(brepEdge.Domain.Mid)));

            var trim = surface.Pullback(segment, trimTolerance);
            loop.trims.Append(trim);
          }

          loop.trims.MakeClosed(Revit.VertexTolerance);

          switch (loop.trims.ClosedCurveOrientation())
          {
            case CurveOrientation.Undefined:        loop.type = BrepLoopType.Unknown; break;
            case CurveOrientation.CounterClockwise: loop.type = BrepLoopType.Outer;   break;
            case CurveOrientation.Clockwise:        loop.type = BrepLoopType.Inner;   break;
          }

          edgeLoops.Add(loop);
        }

        var outerLoops = edgeLoops.Where(x => x.type == BrepLoopType.Outer);
        var innerLoops = edgeLoops.Where(x => x.type == BrepLoopType.Inner);

        // Group Edge loops in shells with the outer loop as the first one
        shells = outerLoops.
                 Select(x => new List<BrepBoundary>() { x }).
                 ToArray();

        if (shells.Length == 1)
        {
          shells[0].AddRange(innerLoops);
        }
        else
        {
          foreach (var edgeLoop in innerLoops)
          {
            foreach (var shell in shells)
            {
              var containment = Curve.PlanarClosedCurveRelationship(edgeLoop.trims, shell[0].trims, Plane.WorldXY, Revit.VertexTolerance);
              if (containment == RegionContainment.AInsideB)
              {
                shell.Add(edgeLoop);
                break;
              }
            }
          }
        }

        return si;
      }

      shells = default;
      return -1;
    }

    static void TrimSurface(this Brep brep, int surface, bool orientationIsReversed, List<BrepBoundary>[] shells)
    {
      foreach (var shell in shells)
      {
        var brepFace = brep.Faces.Add(surface);
        brepFace.OrientationIsReversed = orientationIsReversed;

        foreach (var loop in shell)
        {
          var brepLoop = brep.Loops.Add(loop.type, brepFace);

          var edgeCount = loop.edges.Count;
          for (int e = 0; e < edgeCount; ++e)
          {
            var brepEdge = loop.edges[e];

            int orientation = loop.orientation[e];
            if (orientation == 0)
              continue;

            if (loop.trims.SegmentCurve(e) is Curve trim)
            {
              var ti = brep.AddTrimCurve(trim);
              brep.Trims.Add(brepEdge, orientation < 0, brepLoop, ti);
            }
          }

          brep.Trims.MatchEnds(brepLoop);
        }
      }
    }

    public static Brep ToRhino(this DB.Face face)
    {
      var brep = new Brep();

      // Set surface
      var si = brep.AddSurface(face, out var shells);
      if (si < 0)
        return null;

      // Set edges & trims
      brep.TrimSurface(si, !face.OrientationMatchesSurface(), shells);

      // Set vertices
      brep.SetVertices();

      // Set flags
      brep.SetTolerancesBoxesAndFlags
      (
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true
      );

      if (!brep.IsValid)
      {
#if DEBUG
        brep.IsValidWithLog(out var log);
#endif
        brep.Repair(Revit.VertexTolerance);
      }

      return brep;
    }

    public static Brep ToRhino(this DB.Solid solid)
    {
      if (solid.Faces.IsEmpty)
        return null;

      var brep = new Brep();
      var brepEdges = new Dictionary<DB.Edge, BrepEdge>();

      foreach (var face in solid.Faces.Cast<DB.Face>())
      {
        // Set surface
        var si = brep.AddSurface(face, out var shells, brepEdges);
        if (si < 0)
          continue;

        // Set edges & trims
        brep.TrimSurface(si, !face.OrientationMatchesSurface(), shells);
      }

      // Set vertices
      brep.SetVertices();

      // Set flags
      brep.SetTolerancesBoxesAndFlags
      (
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true
      );

      if (!brep.IsValid)
      {
#if DEBUG
        brep.IsValidWithLog(out var log);
#endif
        brep.Repair(Revit.VertexTolerance);
      }

      return brep;
    }
#endif
    #endregion

    public static Mesh ToRhino(this DB.Mesh mesh)
    {
      if (mesh.NumTriangles < 1)
        return null;

      var result = new Mesh();

      result.Vertices.AddVertices(mesh.Vertices.ToRhino());

      for (int t = 0; t < mesh.NumTriangles; ++t)
      {
        var triangle = mesh.get_Triangle(t);

        var meshFace = new MeshFace
        (
          (int) triangle.get_Index(0),
          (int) triangle.get_Index(1),
          (int) triangle.get_Index(2)
        );

        result.Faces.AddFace(meshFace);
      }

      return result;
    }

    public static IEnumerable<GeometryBase> ToRhino(this DB.GeometryObject geometry)
    {
      var scaleFactor = Revit.ModelUnits;
      switch (geometry)
      {
          case DB.GeometryElement element:
            foreach (var g in element.SelectMany(x => x.ToRhino()))
              yield return g;

            break;
          case DB.GeometryInstance instance:
            var xform = instance.Transform.ToRhino().ChangeUnits(scaleFactor);
            foreach (var g in instance.SymbolGeometry.ToRhino())
            {
              g?.Transform(xform);
              yield return g;
            }
            break;
          case DB.Mesh mesh:
            var m = mesh.ToRhino();

            yield return m?.ChangeUnits(scaleFactor);
            break;
          case DB.Solid solid:
            var s = solid.ToRhino();

            yield return s?.ChangeUnits(scaleFactor);
            break;
          case DB.Curve curve:
            var c = curve.ToRhino();

            yield return c?.ChangeUnits(scaleFactor);
            break;
          case DB.PolyLine polyline:
            var p = polyline.ToRhino();

            yield return p?.ChangeUnits(scaleFactor);
            break;
        }
    }
  };
}

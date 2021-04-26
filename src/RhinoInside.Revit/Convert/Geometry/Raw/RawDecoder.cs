using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry.Raw
{
  /// <summary>
  /// Methods in this class convert Revit geometry to "Raw" form.
  /// <para>Raw form is Rhino geometry in Revit internal units</para>
  /// </summary>
  static class RawDecoder
  {
    #region Values
    public static Point2d AsPoint2d(DB.UV p)
    {
      return new Point2d(p.U, p.V);
    }
    public static Vector2d AsVector2d(DB.UV p)
    {
      return new Vector2d(p.U, p.V);
    }

    public static Point3d AsPoint3d(DB.XYZ p)
    {
      return new Point3d(p.X, p.Y, p.Z);
    }
    public static Vector3d AsVector3d(DB.XYZ p)
    {
      return new Vector3d(p.X, p.Y, p.Z);
    }

    public static Transform AsTransform(DB.Transform transform)
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

    public static BoundingBox AsBoundingBox(DB.BoundingBoxXYZ bbox)
    {
      if (bbox?.Enabled ?? false)
      {
        var box = new BoundingBox(AsPoint3d(bbox.Min), AsPoint3d(bbox.Max));
        return AsTransform(bbox.Transform).TransformBoundingBox(box);
      }

      return BoundingBox.Unset;
    }

    public static BoundingBox AsBoundingBox(DB.BoundingBoxXYZ bbox, out Transform transform)
    {
      if (bbox?.Enabled ?? false)
      {
        var box = new BoundingBox(AsPoint3d(bbox.Min), AsPoint3d(bbox.Max));
        transform = AsTransform(bbox.Transform);
        return box;
      }

      transform = Transform.Identity;
      return BoundingBox.Unset;
    }

    public static Box AsBox(DB.BoundingBoxXYZ bbox)
    {
      return new Box
      (
        new Plane
        (
          origin :    AsPoint3d(bbox.Transform.Origin),
          xDirection: AsVector3d(bbox.Transform.BasisX),
          yDirection: AsVector3d(bbox.Transform.BasisY)
        ),
        xSize: new Interval(bbox.Min.X, bbox.Max.X),
        ySize: new Interval(bbox.Min.Y, bbox.Max.Y),
        zSize: new Interval(bbox.Min.Z, bbox.Max.Z)
      );
    }

    public static Plane AsPlane(DB.Plane plane)
    {
      return new Plane(AsPoint3d(plane.Origin), AsVector3d(plane.XVec), AsVector3d(plane.YVec));
    }
    #endregion

    #region Point
    public static Point ToRhino(DB.Point point)
    {
      return new Point(AsPoint3d(point.Coord));
    }
    #endregion

    #region Curve
    public static LineCurve ToRhino(DB.Line line)
    {
      return line.IsBound ?
        new LineCurve
        (
          new Line(AsPoint3d(line.GetEndPoint(0)), AsPoint3d(line.GetEndPoint(1))),
          line.GetEndParameter(0),
          line.GetEndParameter(1)
        ) :
        new LineCurve
        (
          new Line(AsPoint3d(line.Origin) - (15000.0 * AsVector3d(line.Direction)), AsPoint3d(line.Origin) + (15000.0 * AsVector3d(line.Direction))),
          -15000,
          +15000
        );
    }

    public static ArcCurve ToRhino(DB.Arc arc)
    {
      return arc.IsBound ?
        new ArcCurve
        (
          new Arc(AsPoint3d(arc.GetEndPoint(0)), AsPoint3d(arc.Evaluate(0.5, true)), AsPoint3d(arc.GetEndPoint(1))),
          arc.GetEndParameter(0),
          arc.GetEndParameter(1)
        ) :
        new ArcCurve
        (
          new Circle(new Plane(AsPoint3d(arc.Center), AsVector3d(arc.XDirection), AsVector3d(arc.YDirection)), arc.Radius),
          0.0,
          2.0 * Math.PI
        );
    }

    public static NurbsCurve ToRhino(DB.Ellipse ellipse)
    {
      var plane = new Plane(AsPoint3d(ellipse.Center), AsVector3d(ellipse.XDirection), AsVector3d(ellipse.YDirection));
      var e = new Ellipse(plane, ellipse.RadiusX, ellipse.RadiusY);
      var nurbsCurve = e.ToNurbsCurve();

      if (ellipse.IsBound)
      {
        nurbsCurve.ClosestPoint(AsPoint3d(ellipse.GetEndPoint(0)), out var param0);
        if (!nurbsCurve.ChangeClosedCurveSeam(param0))
          nurbsCurve.Domain = new Interval(param0, param0 + nurbsCurve.Domain.Length);

        nurbsCurve.ClosestPoint(AsPoint3d(ellipse.GetEndPoint(1)), out var param1);
        nurbsCurve = nurbsCurve.Trim(param0, param1) as NurbsCurve;
        nurbsCurve.Domain = new Interval(ellipse.GetEndParameter(0), ellipse.GetEndParameter(1));
      }

      return nurbsCurve;
    }

    public static NurbsCurve ToRhino(DB.HermiteSpline hermite)
    {
      var nurbsCurve = ToRhino(DB.NurbSpline.Create(hermite));
      nurbsCurve.Domain = new Interval(hermite.GetEndParameter(0), hermite.GetEndParameter(1));
      return nurbsCurve;
    }

    public static NurbsCurve ToRhino(DB.NurbSpline nurb)
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

    public static NurbsCurve ToRhino(DB.CylindricalHelix helix)
    {
      var nurbsCurve = NurbsCurve.CreateSpiral
      (
        AsPoint3d(helix.BasePoint),
        AsVector3d(helix.ZVector),
        AsPoint3d(helix.BasePoint) + AsVector3d(helix.XVector),
        helix.Pitch,
        helix.IsRightHanded ? +1 : -1,
        helix.Radius,
        helix.Radius
      );

      nurbsCurve.Domain = new Interval(helix.GetEndParameter(0), helix.GetEndParameter(1));
      return nurbsCurve;
    }

    public static Curve ToRhino(DB.Curve curve)
    {
      switch (curve)
      {
        case null: return null;
        case DB.Line line: return ToRhino(line);
        case DB.Arc arc: return ToRhino(arc);
        case DB.Ellipse ellipse: return ToRhino(ellipse);
        case DB.HermiteSpline hermite: return ToRhino(hermite);
        case DB.NurbSpline nurb: return ToRhino(nurb);
        case DB.CylindricalHelix helix: return ToRhino(helix);
        default: throw new NotImplementedException();
      }
    }

    public static PolylineCurve ToRhino(DB.PolyLine polyline)
    {
      return new PolylineCurve(polyline.GetCoordinates().Select(x => AsPoint3d(x)));
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
        AsPoint3d(origin),
        AsVector3d(xDir),
        AsVector3d(yDir)
      );

      return new PlaneSurface(plane, uu, vv);
    }

    public static PlaneSurface ToRhinoSurface(DB.PlanarFace face, double relativeTolerance) => FromPlane
    (
      face.Origin,
      face.XVector,
      face.YVector,
      face.FaceNormal,
      face.GetBoundingBox(),
      relativeTolerance
    );

    public static PlaneSurface ToRhino(DB.Plane surface, DB.BoundingBoxUV bboxUV) => FromPlane
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
        AsPoint3d(origin),
        AsVector3d(xDir),
        AsVector3d(yDir)
      );
      var axisDir = AsVector3d(zDir);

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

    public static RevSurface ToRhinoSurface(DB.ConicalFace face, double relativeTolerance) => FromConicalSurface
    (
      face.Origin,
      face.get_Radius(0),
      face.get_Radius(1),
      face.Axis,
      face.HalfAngle,
      face.GetBoundingBox(),
      relativeTolerance
    );

    public static RevSurface ToRhino(DB.ConicalSurface surface, DB.BoundingBoxUV bboxUV) => FromConicalSurface
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
        AsPoint3d(origin),
        AsVector3d(xDir),
        AsVector3d(yDir)
      );
      var axisDir = AsVector3d(zDir);

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

    public static RevSurface ToRhinoSurface(DB.CylindricalFace face, double relativeTolerance) => FromCylindricalSurface
    (
      face.Origin,
      face.get_Radius(0),
      face.get_Radius(1),
      face.Axis,
      face.get_Radius(0).GetLength(),
      face.GetBoundingBox(),
      relativeTolerance
    );

    public static RevSurface ToRhino(DB.CylindricalSurface surface, DB.BoundingBoxUV bboxUV) => FromCylindricalSurface
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
        AsPoint3d(origin),
        AsVector3d(xDir),
        AsVector3d(yDir)
      );
      var axisDir = AsVector3d(zDir);

      using (var ECStoWCS = new DB.Transform(DB.Transform.Identity) { Origin = origin, BasisX = xDir.Normalize(), BasisY = yDir.Normalize(), BasisZ = zDir.Normalize() })
      {
        var c = ToRhino(curve.CreateTransformed(ECStoWCS));
        c = ctol == 0.0 ? c : c.Extend(CurveEnd.Both, ctol, CurveExtensionStyle.Smooth);

        var axis = new Line(plane.Origin, plane.Normal);
        return RevSurface.Create(c, axis, uu.Min, uu.Max);
      }
    }

    public static RevSurface ToRhinoSurface(DB.RevolvedFace face, double relativeTolerance) => FromRevolvedSurface
    (
      face.Origin,
      face.get_Radius(0),
      face.get_Radius(1),
      face.Axis,
      face.Curve,
      face.GetBoundingBox(),
      relativeTolerance
    );

    public static RevSurface ToRhino(DB.RevolvedSurface surface, DB.BoundingBoxUV bboxUV) => FromRevolvedSurface
    (
      surface.Origin,
      surface.XDir,
      surface.YDir,
      surface.Axis,
      surface.GetProfileCurve(),
      bboxUV,
      0.0
    );

    static Surface FromExtrudedSurface(IList<DB.Curve> curves, DB.BoundingBoxUV bboxUV, double relativeTolerance)
    {
      var ctol = relativeTolerance * Revit.ShortCurveTolerance;

      var axis = new LineCurve
      (
        new Line(AsPoint3d(curves[0].GetEndPoint(0)), AsPoint3d(curves[1].GetEndPoint(0))),
        0.0,
        1.0
      );

      Curve curveU = ToRhino(curves[0]);
      curveU.Translate(axis.PointAt(bboxUV.Min.V) - curveU.PointAtStart);

      Curve curveV = new LineCurve(new Line(axis.PointAt(bboxUV.Min.V), axis.PointAt(bboxUV.Max.V)));

      if (ctol != 0.0)
      {
        curveU = curveU.Extend(CurveEnd.Both, ctol, CurveExtensionStyle.Smooth);
        curveV = curveV.Extend(CurveEnd.Both, ctol, CurveExtensionStyle.Smooth);
      }

      return SumSurface.Create(curveU, curveV);
    }

    static Surface FromRuledSurface(IList<DB.Curve> curves, DB.XYZ start, DB.XYZ end, DB.BoundingBoxUV bboxUV, double relativeTolerance)
    {
      var ctol = relativeTolerance * Revit.ShortCurveTolerance;

      var cs = curves.Where(x => x is object).Select
      (
        x =>
        {
          var c = ToRhino(x);
          return ctol == 0.0 ? c : c.Extend(CurveEnd.Both, ctol, CurveExtensionStyle.Smooth);
        }
      );

      Point3d p0 = start is null ? Point3d.Unset : AsPoint3d(start),
              pN = end   is null ? Point3d.Unset : AsPoint3d(end);

      var lofts = Brep.CreateFromLoft(cs, p0, pN, LoftType.Straight, false);
      if (lofts.Length == 1 && lofts[0].Faces.Count == 1)
      {
        // Surface.Transpose is necessary since Brep.CreateFromLoft places the input curves along V,
        // while Revit Ruled Surface has those Curves along U axis.
        // This subtle thing also results in the correct normal of the resulting surface.
        // Transpose duplicates the underlaying surface, what is a desired side effect of calling Transpose here.
        return lofts[0].Faces[0].Transpose();
      }

      return null;
    }

    public static Surface ToRhinoSurface(DB.RuledFace face, double relativeTolerance)
    {
//      NurbsSurface nurbsSurface = default;
//      try
//      {
//#if REVIT_2021
//        using (var surface = DB.ExportUtils.GetNurbsSurfaceDataForSurface(face.GetSurface()))
//          nurbsSurface = ToRhino(surface, face.GetBoundingBox());
//#else
//        using (var surface = DB.ExportUtils.GetNurbsSurfaceDataForFace(face))
//          nurbsSurface = ToRhino(surface, face.GetBoundingBox());
//#endif
//      }
//      catch (Autodesk.Revit.Exceptions.ApplicationException) { }

//      if (nurbsSurface is null)
      {
        return face.IsExtruded ?
          FromExtrudedSurface
          (
            new DB.Curve[] { face.get_Curve(0), face.get_Curve(1) },
            face.GetBoundingBox(),
            relativeTolerance
          ) :
          FromRuledSurface
          (
            new DB.Curve[] { face.get_Curve(0), face.get_Curve(1) },
            face.get_Curve(0) is null ? face.get_Point(0) : null,
            face.get_Curve(1) is null ? face.get_Point(1) : null,
            face.GetBoundingBox(),
            relativeTolerance
          );
      }
      //else
      //{
      //  double ctol = relativeTolerance * Revit.ShortCurveTolerance * 5.0;
      //  if (ctol != 0.0)
      //  {
      //    // Extend using smooth way avoids creating C2 discontinuities
      //    nurbsSurface = nurbsSurface.Extend(IsoStatus.West, ctol, true) as NurbsSurface ?? nurbsSurface;
      //    nurbsSurface = nurbsSurface.Extend(IsoStatus.East, ctol, true) as NurbsSurface ?? nurbsSurface;
      //    nurbsSurface = nurbsSurface.Extend(IsoStatus.South, ctol, true) as NurbsSurface ?? nurbsSurface;
      //    nurbsSurface = nurbsSurface.Extend(IsoStatus.North, ctol, true) as NurbsSurface ?? nurbsSurface;
      //  }
      //}

      //return nurbsSurface;
    }

    public static Surface ToRhino(DB.RuledSurface surface, DB.BoundingBoxUV bboxUV) => FromRuledSurface
    (
      new DB.Curve[] { surface.GetFirstProfileCurve(), surface.GetSecondProfileCurve() },
      surface.HasFirstProfilePoint() ? surface.GetFirstProfilePoint() : null,
      surface.HasSecondProfilePoint() ? surface.GetSecondProfilePoint() : null,
      bboxUV,
      0.0
    );

    static NurbsSurface FromHermiteSurface
    (
      DB.DoubleArray paramsU, DB.DoubleArray paramsV,
      IList<DB.XYZ> points, IList<DB.XYZ> mixedDerivs,
      IList<DB.XYZ> tangentsU, IList<DB.XYZ> tangentsV
    )
    {
      var uCount = paramsU.Size;
      var vCount = paramsV.Size;

      var hermiteSurface = new HermiteSurface(uCount, vCount);
      {
        for (int u = 0; u < uCount; ++u)
          hermiteSurface.SetUParameterAt(u, paramsU.get_Item(u));

        for (int v = 0; v < vCount; ++v)
          hermiteSurface.SetVParameterAt(v, paramsV.get_Item(v));

        for (int v = 0; v < vCount; ++v)
        {
          for (int u = 0; u < uCount; ++u)
          {
            hermiteSurface.SetPointAt(u, v, AsPoint3d(points[v * uCount + u]));
            hermiteSurface.SetTwistAt(u, v, AsVector3d(mixedDerivs[v * uCount + u]));
            hermiteSurface.SetUTangentAt(u, v, AsVector3d(tangentsU[v * uCount + u]));
            hermiteSurface.SetVTangentAt(u, v, AsVector3d(tangentsV[v * uCount + u]));
          }
        }
      }

      return hermiteSurface.ToNurbsSurface();
    }

    public static NurbsSurface ToRhinoSurface(DB.HermiteFace face, double relativeTolerance)
    {
      NurbsSurface nurbsSurface = default;
      try
      {
#if REVIT_2021
        using (var surface = DB.ExportUtils.GetNurbsSurfaceDataForSurface(face.GetSurface()))
          nurbsSurface = ToRhino(surface, face.GetBoundingBox());
#else
        using (var surface = DB.ExportUtils.GetNurbsSurfaceDataForFace(face))
          nurbsSurface = ToRhino(surface, face.GetBoundingBox());
#endif
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }

      if (nurbsSurface is null)
      {
        using (var paramsU = face.get_Params(0))
        using (var paramsV = face.get_Params(1))
        {
          nurbsSurface = FromHermiteSurface
          (
            paramsU,
            paramsV,
            face.Points,
            face.MixedDerivs,
            face.get_Tangents(0),
            face.get_Tangents(1)
          );
        }
      }

      using (var bboxUV = face.GetBoundingBox())
      {
        nurbsSurface = nurbsSurface.Trim
        (
          new Interval(bboxUV.Min.U, bboxUV.Max.U),
          new Interval(bboxUV.Min.V, bboxUV.Max.V)
        ) as NurbsSurface;
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

    public static NurbsSurface ToRhino(DB.NurbsSurfaceData surface, DB.BoundingBoxUV bboxUV)
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

    public static Surface ToRhinoSurface(DB.Face face, out bool parametricOrientation, double relativeTolerance = 0.0)
    {
      using (var surface = face.GetSurface())
        parametricOrientation = surface.MatchesParametricOrientation();

      switch (face)
      {
        case null: return null;
        case DB.PlanarFace planar:            return ToRhinoSurface(planar, relativeTolerance);
        case DB.ConicalFace conical:          return ToRhinoSurface(conical, relativeTolerance);
        case DB.CylindricalFace cylindrical:  return ToRhinoSurface(cylindrical, relativeTolerance);
        case DB.RevolvedFace revolved:        return ToRhinoSurface(revolved, relativeTolerance);
        case DB.RuledFace ruled:              return ToRhinoSurface(ruled, relativeTolerance);
        case DB.HermiteFace hermite:          return ToRhinoSurface(hermite, relativeTolerance);
        default: throw new NotImplementedException();
      }
    }
    #endregion

    #region Brep
    struct BrepBoundary
    {
      public BrepLoopType type;
      public List<int> orientation;
      public List<BrepEdge> edges;
      public PolyCurve trims;
    }

    static int AddSurface(Brep brep, DB.Face face, out List<BrepBoundary>[] shells, Dictionary<DB.Edge, BrepEdge> brepEdges = null)
    {
      // Extract base surface
      if (ToRhinoSurface(face, out var parametricOrientation) is Surface surface)
      {
        if (!parametricOrientation)
          surface.Transpose(true);

        int si = brep.AddSurface(surface);

        if (surface is PlaneSurface planar)
        {
          var nurbs = planar.ToNurbsSurface();
          nurbs.KnotsU.InsertKnot(surface.Domain(0).Mid);
          nurbs.KnotsV.InsertKnot(surface.Domain(1).Mid);
          surface = nurbs;
        }
        else if (surface is SumSurface sum)
        {
          surface = sum.ToNurbsSurface();
        }

        // Extract and classify Edge Loops
        var edgeLoops = new List<BrepBoundary>(face.EdgeLoops.Size);
        foreach (var edgeLoop in face.EdgeLoops.Cast<DB.EdgeArray>())
        {
          if (edgeLoop.IsEmpty)
            continue;

          var edges = edgeLoop.Cast<DB.Edge>();
          if (!face.MatchesSurfaceOrientation())
            edges = edges.Reverse();

          var loop = new BrepBoundary()
          {
            type = BrepLoopType.Unknown,
            orientation = new List<int>(edgeLoop.Size),
            edges = new List<BrepEdge>(edgeLoop.Size),
            trims = new PolyCurve()
          };

          var trims = new List<Curve>(edgeLoop.Size);
          foreach (var edge in edges)
          {
            var brepEdge = default(BrepEdge);
            if (brepEdges?.TryGetValue(edge, out brepEdge) != true)
            {
              var curve = edge.AsCurve();
              if (curve is null)
                continue;

              brepEdge = brep.Edges.Add(brep.AddEdgeCurve(ToRhino(curve)));
              brepEdges?.Add(edge, brepEdge);
            }

            var segment = ToRhino(edge.AsCurveFollowingFace(face));
            if (!face.MatchesSurfaceOrientation())
              segment.Reverse();

            if (surface.Pullback(segment, 1e-5) is Curve trim)
            {
              trims.Add(trim);
              loop.edges.Add(brepEdge);

              loop.orientation.Add(segment.TangentAt(segment.Domain.Mid).IsParallelTo(brepEdge.TangentAt(brepEdge.Domain.Mid)));
            }
          }

          // Add singular edges
          for (int ti = 0, ei = 0; ti < trims.Count; ++ti, ++ei)
          {
            int tA = ti;
            int tB = (ti + 1) % trims.Count;

            loop.trims.AppendSegment(trims[tA]);

            int eA = ei;
            int eB = (ei + 1) % loop.edges.Count;

            var start = loop.orientation[eA] > 0 ? (loop.edges[eA]).PointAtEnd   : (loop.edges[eA]).PointAtStart;
            var end   = loop.orientation[eB] > 0 ? (loop.edges[eB]).PointAtStart : (loop.edges[eB]).PointAtEnd;

            if (start.EpsilonEquals(end, Revit.VertexTolerance))
            {
              var startTrim = new Point2d(trims[tA].PointAtEnd);
              var endTrim = new Point2d(trims[tB].PointAtStart);
              var midTrim = (endTrim + startTrim) * 0.5;

              if (surface.IsAtSingularity(midTrim.X, midTrim.Y, false))
              {
                loop.orientation.Insert(eA + 1, 0);
                loop.edges.Insert(eA + 1, default);
                loop.trims.AppendSegment(new LineCurve(startTrim, endTrim));

                ei++;
              }
            }
            else // Missing edge
            {
              Debug.WriteLine("Missing edge detected");
            }
          }

          loop.trims.MakeClosed(Revit.VertexTolerance);

          switch (loop.trims.ClosedCurveOrientation())
          {
            case CurveOrientation.Undefined: loop.type = BrepLoopType.Unknown; break;
            case CurveOrientation.CounterClockwise: loop.type = BrepLoopType.Outer; break;
            case CurveOrientation.Clockwise: loop.type = BrepLoopType.Inner; break;
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

    static void TrimSurface(Brep brep, int surface, bool orientationIsReversed, List<BrepBoundary>[] shells)
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

            if (loop.trims.SegmentCurve(e) is Curve trim)
            {
              var ti = brep.AddTrimCurve(trim);

              int orientation = loop.orientation[e];
              if (orientation == 0)
                brep.Trims.Add(false, brepLoop, ti).TrimType = BrepTrimType.Singular;
              else
                brep.Trims.Add(brepEdge, orientation < 0, brepLoop, ti);
            }
          }

          brep.Trims.MatchEnds(brepLoop);
        }
      }
    }

    public static Brep ToRhino(DB.Face face)
    {
      if (face is null)
        return null;

      var brep = new Brep();

      // Set surface
      var si = AddSurface(brep, face, out var shells);
      if (si < 0)
        return null;

      // Set edges & trims
      TrimSurface(brep, si, !face.MatchesSurfaceOrientation(), shells);

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
        Debug.WriteLine($"{MethodInfo.GetCurrentMethod().DeclaringType.FullName}.{MethodInfo.GetCurrentMethod().Name}()\n{log}");
#endif
        brep.Repair(Revit.VertexTolerance);
      }

      return brep;
    }

    public static Brep ToRhino(DB.Solid solid)
    {
      if (solid is null)
        return null;

      var brep = new Brep();

      if (!solid.Faces.IsEmpty)
      {
        var brepEdges = new Dictionary<DB.Edge, BrepEdge>();

        foreach (var face in solid.Faces.Cast<DB.Face>())
        {
          // Set surface
          var si = AddSurface(brep, face, out var shells, brepEdges);
          if (si < 0)
            continue;

          // Set edges & trims
          TrimSurface(brep, si, !face.MatchesSurfaceOrientation(), shells);
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
          Debug.WriteLine($"{MethodInfo.GetCurrentMethod().DeclaringType.FullName}.{MethodInfo.GetCurrentMethod().Name}()\n{log}");
#endif
          brep.Repair(Revit.VertexTolerance);
        }
      }

      return brep;
    }
    #endregion

    #region Mesh
    public static Mesh ToRhino(DB.Mesh mesh)
    {
      if (mesh is null)
        return null;

      var result = new Mesh();

      result.Vertices.Capacity = mesh.Vertices.Count;
      result.Vertices.AddVertices(mesh.Vertices.Convert(AsPoint3d));

      var faceCount = mesh.NumTriangles;
      result.Faces.Capacity = faceCount;

      for (int t = 0; t < faceCount; ++t)
      {
        var triangle = mesh.get_Triangle(t);

        result.Faces.AddFace
        (
          (int) triangle.get_Index(0),
          (int) triangle.get_Index(1),
          (int) triangle.get_Index(2)
        );
      }

#if REVIT_2021
      switch (mesh.DistributionOfNormals)
      {
        case DB.DistributionOfNormals.AtEachPoint:
          if (mesh.NumberOfNormals == result.Vertices.Count)
          {
            foreach (var xyz in mesh.GetNormals())
              result.Normals.Add(xyz.X, xyz.Y, xyz.Z);
          }
          break;
        case DB.DistributionOfNormals.OnePerFace:
          result.FaceNormals.ComputeFaceNormals();
          break;
        case DB.DistributionOfNormals.OnEachFacet:
          if (mesh.NumberOfNormals == result.Faces.Count)
          {
            foreach (var xyz in mesh.GetNormals())
              result.FaceNormals.AddFaceNormal(xyz.X, xyz.Y, xyz.Z);
          }
          break;
      }
#else
      result.FaceNormals.ComputeFaceNormals();
#endif

      return result;
    }
    #endregion
  };
}

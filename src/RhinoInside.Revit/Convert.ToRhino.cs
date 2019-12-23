using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit
{
  public static partial class Convert
  {
    #region ToRhino
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
          arc.GetEndParameter(0),
          arc.GetEndParameter(1)
        );
    }

    public static NurbsCurve ToRhino(this DB.Ellipse ellipse)
    {
      var plane = new Plane(ellipse.Center.ToRhino(), new Vector3d(ellipse.XDirection.ToRhino()), new Vector3d(ellipse.YDirection.ToRhino()));
      var e = new Ellipse(plane, ellipse.RadiusX, ellipse.RadiusY);
      var nurbsCurve = e.ToNurbsCurve();
      return ellipse.IsBound ?
        nurbsCurve.Trim(ellipse.GetEndParameter(0), ellipse.GetEndParameter(1)) as NurbsCurve :
        nurbsCurve;
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

    public static IEnumerable<Curve> ToRhino(this IEnumerable<DB.CurveLoop> loops)
    {
      foreach (var loop in loops)
      {
        var curves = Curve.JoinCurves(loop.Select(x => x.ToRhino()), Revit.ShortCurveTolerance, false);
        if (curves.Length != 1)
          throw new ConversionException("Failed to found one and only one closed loop.");

        yield return curves[0];
      }
    }

    public static PlaneSurface ToRhino(this DB.Plane surface, DB.BoundingBoxUV bboxUV)
    {
      var ctol = Revit.ShortCurveTolerance;
      var uu = new Interval(bboxUV.Min.U - ctol, bboxUV.Max.U + ctol);
      var vv = new Interval(bboxUV.Min.V - ctol, bboxUV.Max.V + ctol);

      return new PlaneSurface
      (
        new Plane(surface.Origin.ToRhino(), (Vector3d) surface.XVec.ToRhino(), (Vector3d) surface.YVec.ToRhino()),
        uu,
        vv
      );
    }

    public static RevSurface ToRhino(this DB.ConicalSurface surface, DB.BoundingBoxUV bboxUV)
    {
      var ctol = Revit.ShortCurveTolerance;
      var atol = Revit.AngleTolerance * 10.0;
      var uu = new Interval(bboxUV.Min.U - atol, bboxUV.Max.U + atol);
      var vv = new Interval(bboxUV.Min.V - ctol, bboxUV.Max.V + ctol);

      var origin = surface.Origin.ToRhino();
      var xdir = (Vector3d) surface.XDir.ToRhino();
      var zdir = (Vector3d) surface.Axis.ToRhino();

      var axis = new Line(origin, origin + zdir);

      var dir = zdir + xdir * Math.Tan(surface.HalfAngle);
      dir.Unitize();

      var curve = new LineCurve
      (
        new Line
        (
          surface.Origin.ToRhino() + (dir * vv.Min),
          surface.Origin.ToRhino() + (dir * vv.Max)
        ),
        vv.Min,
        vv.Max
      );

      return RevSurface.Create(curve, axis, uu.Min, uu.Max);
    }

    public static RevSurface ToRhino(this DB.CylindricalSurface surface, DB.BoundingBoxUV bboxUV)
    {
      var ctol = Revit.ShortCurveTolerance;
      var atol = Revit.AngleTolerance;
      var uu = new Interval(bboxUV.Min.U - atol, bboxUV.Max.U + atol);
      var vv = new Interval(bboxUV.Min.V - ctol, bboxUV.Max.V + ctol);

      var origin = surface.Origin.ToRhino();
      var xdir = (Vector3d) surface.XDir.ToRhino();
      var zdir = (Vector3d) surface.Axis.ToRhino();

      var axis = new Line(origin, origin + zdir);
      var curve = new LineCurve
      (
        new Line
        (
          origin + (xdir * surface.Radius) + (zdir * vv.Min),
          origin + (xdir * surface.Radius) + (zdir * vv.Max)
        ),
        vv.Min,
        vv.Max
      );

      return RevSurface.Create(curve, axis, uu.Min, uu.Max);
    }

    public static RevSurface ToRhino(this DB.RevolvedSurface surface, DB.BoundingBoxUV bboxUV)
    {
      var ctol = Revit.ShortCurveTolerance;
      var atol = Revit.AngleTolerance;
      var uu = new Interval(bboxUV.Min.U - atol, bboxUV.Max.U + atol);

      var axis = new Line
      (
        surface.Origin.ToRhino(),
        surface.Origin.ToRhino() + (Vector3d) surface.Axis.ToRhino()
      );

      var curve = surface.GetProfileCurveInWorldCoordinates().ToRhino();
      curve = curve.Extend(CurveEnd.Both, ctol, CurveExtensionStyle.Line);

      return RevSurface.Create(curve, axis, uu.Min, uu.Max);
    }

    public static Surface ToRhino(this DB.RuledSurface surface, DB.BoundingBoxUV bboxUV)
    {
      var ctol = Revit.ShortCurveTolerance;

      var curves = new List<Curve>();
      Point3d start = Point3d.Unset, end = Point3d.Unset;

      if (surface.HasFirstProfilePoint())
        start = surface.GetFirstProfilePoint().ToRhino();
      else
        curves.Add(surface.GetFirstProfileCurve().ToRhino());

      if (surface.HasSecondProfilePoint())
        end = surface.GetSecondProfilePoint().ToRhino();
      else
        curves.Add(surface.GetSecondProfileCurve().ToRhino());

      // Revit Ruled surface Parametric Orientation is opposite to Rhino
      for (var c = 0; c < curves.Count; ++c)
      {
        curves[c].Reverse();
        curves[c] = curves[c].Extend(CurveEnd.Both, ctol, CurveExtensionStyle.Line);
      }

      var lofts = Brep.CreateFromLoft(curves, start, end, LoftType.Straight, false);
      if (lofts.Length == 1 && lofts[0].Surfaces.Count == 1)
        return lofts[0].Surfaces[0];

      return null;
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

      double ctol = Revit.ShortCurveTolerance * 5.0;
      // Extend using smooth way avoids creating C2 discontinuities
      nurbsSurface = nurbsSurface.Extend(IsoStatus.West, ctol, true) as NurbsSurface ?? nurbsSurface;
      nurbsSurface = nurbsSurface.Extend(IsoStatus.East, ctol, true) as NurbsSurface ?? nurbsSurface;
      nurbsSurface = nurbsSurface.Extend(IsoStatus.South, ctol, true) as NurbsSurface ?? nurbsSurface;
      nurbsSurface = nurbsSurface.Extend(IsoStatus.North, ctol, true) as NurbsSurface ?? nurbsSurface;

      return nurbsSurface;
    }

    static Surface ToRhinoSurface(this DB.Face face)
    {
      Surface surface = default;

      using (var faceSurface = face.GetSurface())
      {
        var bboxUV = face.GetBoundingBox();

        switch (faceSurface)
        {
          case DB.Plane planeSurface:                     surface = planeSurface.ToRhino(bboxUV);       break;
          case DB.ConicalSurface conicalSurface:          surface = conicalSurface.ToRhino(bboxUV);     break;
          case DB.CylindricalSurface cylindricalSurface:  surface = cylindricalSurface.ToRhino(bboxUV); break;
          case DB.RevolvedSurface revolvedSurface:        surface = revolvedSurface.ToRhino(bboxUV);    break;
          case DB.RuledSurface ruledSurface:              surface = ruledSurface.ToRhino(bboxUV);       break;
          case DB.HermiteSurface hermiteSurface:
            try
            {
              using (var nurbsData = DB.ExportUtils.GetNurbsSurfaceDataForFace(face))
                surface = nurbsData.ToRhino(bboxUV);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException) { }
            break;
          default: throw new NotImplementedException();
        }
      }

      return surface;
    }

    static Brep JoinAndMerge(this ICollection<Brep> brepFaces, double tolerance)
    {
      if (brepFaces.Count == 0)
        return null;

      if (brepFaces.Count == 1)
        return brepFaces.First();

      ICollection<Brep> joinedBreps = Brep.JoinBreps(brepFaces.Where(x => x?.IsValid == true), tolerance);
      if (joinedBreps is null)
        joinedBreps = brepFaces;
      else if (joinedBreps.Count == 1)
        return joinedBreps.First();

      return Brep.MergeBreps(joinedBreps, Rhino.RhinoMath.UnsetValue);
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

    public static Brep ToRhino(this DB.Face face, bool untrimmed = false)
    {
      var surface = face.ToRhinoSurface();
      if (surface is null)
        return null;

      var brep = Brep.CreateFromSurface(surface);
      if (brep is null)
        return null;

#if REVIT_2018
      if (!face.OrientationMatchesSurfaceOrientation)
        brep.Flip();
#endif
      if (untrimmed)
        return brep;

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

    public static Mesh ToRhino(this DB.Mesh mesh)
    {
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
            var p = new PolylineCurve(polyline.GetCoordinates().ToRhino());

            yield return p?.ChangeUnits(scaleFactor);
            break;
        }
    }
    #endregion
  };
}

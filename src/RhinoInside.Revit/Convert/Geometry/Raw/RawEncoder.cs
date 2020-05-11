using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry.Raw
{
  /// <summary>
  /// Methods in this class convert Revit geometry from "Raw" form.
  /// <para>The input geometry is granted not to be modified on any way, no copies are necessary before calling this methods.</para>
  /// <para>Raw form is Rhino geometry in Revit internal units</para>
  /// </summary>
  static class RawEncoder
  {
    #region Values
    public static DB::UV AsUV(Point2f value)
    {
      return new DB::UV(value.X, value.Y);
    }
    public static DB::UV AsUV(Point2d value)
    {
      return new DB::UV(value.X, value.Y);
    }
    public static DB::UV AsUV(Vector2d value)
    {
      return new DB::UV(value.X, value.Y);
    }
    public static DB::UV AsUV(Vector2f value)
    {
      return new DB::UV(value.X, value.Y);
    }

    public static DB::XYZ AsXYZ(Point3f value)
    {
      return new DB::XYZ(value.X, value.Y, value.Z);
    }
    public static DB::XYZ AsXYZ(Point3d value)
    {
      return new DB::XYZ(value.X, value.Y, value.Z);
    }
    public static DB::XYZ AsXYZ(Vector3d value)
    {
      return new DB::XYZ(value.X, value.Y, value.Z);
    }
    public static DB::XYZ AsXYZ(Vector3f value)
    {
      return new DB::XYZ(value.X, value.Y, value.Z);
    }

    public static DB.Transform ToHost(Transform value)
    {
      Debug.Assert(value.IsAffine);

      var result = DB.Transform.CreateTranslation(new DB.XYZ(value.M03, value.M13, value.M23));

      result.BasisX = new DB.XYZ(value.M00, value.M10, value.M20);
      result.BasisY = new DB.XYZ(value.M01, value.M11, value.M21);
      result.BasisZ = new DB.XYZ(value.M02, value.M12, value.M22);
      return result;
    }

    public static DB.Plane ToHost(Plane value)
    {
      return DB.Plane.CreateByOriginAndBasis(AsXYZ(value.Origin), AsXYZ((Point3d) value.XAxis), AsXYZ((Point3d) value.YAxis));
    }

    public static DB.PolyLine ToHost(Polyline value)
    {
      int count = value.Count;
      var points = new DB.XYZ[count];

      for (int p = 0; p < count; ++p)
        points[p] = AsXYZ(value[p]);

      return DB.PolyLine.Create(points);
    }
    #endregion

    #region Point
    public static DB.Point ToHost(Point value)
    {
      return DB.Point.Create(AsXYZ(value.Location));
    }
    #endregion

    #region Curve
    public static DB.Line ToHost(LineCurve value)
    {
      var line = value.Line;
      return DB.Line.CreateBound(AsXYZ(line.From), AsXYZ(line.To));
    }

    public static DB.Arc ToHost(ArcCurve value)
    {
      var arc = value.Arc;
      if (value.Arc.IsCircle)
        return DB.Arc.Create(ToHost(arc.Plane), value.Radius, 0.0, 2.0 * Math.PI);
      else
        return DB.Arc.Create(AsXYZ(arc.StartPoint), AsXYZ(arc.EndPoint), AsXYZ(arc.MidPoint));
    }

    public static double[] ToHost(NurbsCurveKnotList list)
    {
      var count = list.Count;
      var knots = new double[count + 2];

      int j = 0, k = 0;
      while (j < count)
        knots[++k] = list[j++];

      knots[0] = knots[1];
      knots[count + 1] = knots[count];

      return knots;
    }

    public static DB.XYZ[] ToHost(NurbsCurvePointList list)
    {
      var count = list.Count;
      var points = new DB.XYZ[count];

      for(int p = 0; p < count; ++p)
      {
        var location = list[p].Location;
        points[p] = new DB::XYZ(location.X, location.Y, location.Z);
      }

      return points;
    }

    public static DB.Curve ToHost(NurbsCurve value)
    {
      var degree = value.Degree;
      var knots = ToHost(value.Knots);
      var controlPoints = ToHost(value.Points);

      Debug.Assert(degree > 2 || value.SpanCount == 1);
      Debug.Assert(degree >= 1);
      Debug.Assert(controlPoints.Length > degree);
      Debug.Assert(knots.Length == degree + controlPoints.Length + 1);

      if (value.IsRational)
      {
        var weights = value.Points.Select(p => p.Weight).ToArray();
        return DB.NurbSpline.CreateCurve(value.Degree, knots, controlPoints, weights);
      }
      else
      {
        var c = DB.NurbSpline.CreateCurve(value.Degree, knots, controlPoints);
        return c;
      }
    }
    #endregion

    #region Brep
    public static IEnumerable<DB.BRepBuilderEdgeGeometry> ToHost(BrepEdge edge)
    {
      var edgeCurve = edge.EdgeCurve.Trim(edge.Domain);

      if (edge.ProxyCurveIsReversed)
        edgeCurve.Reverse();

      switch (edgeCurve)
      {
        case LineCurve line:
          yield return DB.BRepBuilderEdgeGeometry.Create(ToHost(line));
          yield break;
        case ArcCurve arc:
          yield return DB.BRepBuilderEdgeGeometry.Create(ToHost(arc));
          yield break;
        case NurbsCurve nurbs:
          yield return DB.BRepBuilderEdgeGeometry.Create(ToHost(nurbs));
          yield break;
        default:
          Debug.Fail($"{edgeCurve} is not supported as a Solid Edge");
          yield break;
      }
    }

    public static double[] ToHost(NurbsSurfaceKnotList list)
    {
      var count = list.Count;
      var knots = new double[count + 2];

      int j = 0, k = 0;
      while (j < count)
        knots[++k] = list[j++];

      knots[0] = knots[1];
      knots[count + 1] = knots[count];

      return knots;
    }

    public static DB.XYZ[] ToHost(NurbsSurfacePointList list)
    {
      var count = list.CountU * list.CountV;
      var points = new DB.XYZ[count];

      int p = 0;
      foreach (var point in list)
      {
        var location = point.Location;
        points[p++] = new DB::XYZ(location.X, location.Y, location.Z);
      }

      return points;
    }

    public static DB.BRepBuilderSurfaceGeometry ToHost(BrepFace face)
    {
      // TODO: Implement conversion from other Rhino surface types like PlaneSurface, RevSurface and SumSurface.

      using (var nurbsSurface = face.ToNurbsSurface())
      {
        var domainU = nurbsSurface.Domain(0);
        var domainV = nurbsSurface.Domain(1);
        var degreeU = nurbsSurface.Degree(0);
        var degreeV = nurbsSurface.Degree(1);
        var knotsU = ToHost(nurbsSurface.KnotsU);
        var knotsV = ToHost(nurbsSurface.KnotsV);
        var controlPoints = ToHost(nurbsSurface.Points);
        var bboxUV = new DB.BoundingBoxUV(domainU.Min, domainV.Min, domainU.Max, domainV.Max);

        Debug.Assert(!nurbsSurface.IsClosed(0));
        Debug.Assert(!nurbsSurface.IsClosed(1));
        Debug.Assert(degreeU >= 1);
        Debug.Assert(degreeV >= 1);
        Debug.Assert(knotsU.Length >= 2 * (degreeU + 1));
        Debug.Assert(knotsV.Length >= 2 * (degreeV + 1));
        Debug.Assert(controlPoints.Length == (knotsU.Length - degreeU - 1) * (knotsV.Length - degreeV - 1));

        if (nurbsSurface.IsRational)
        {
          var weights = nurbsSurface.Points.Select(p => p.Weight).ToList();

          return DB.BRepBuilderSurfaceGeometry.CreateNURBSSurface
          (
            degreeU, degreeV, knotsU, knotsV, controlPoints, weights, false, bboxUV
          );
        }
        else
        {
          return DB.BRepBuilderSurfaceGeometry.CreateNURBSSurface
          (
            degreeU, degreeV, knotsU, knotsV, controlPoints, false, bboxUV
          );
        }
      }
    }

    public static DB.Solid ToHost(Brep brep)
    {
      var brepType = DB.BRepType.OpenShell;
      switch (brep.SolidOrientation)
      {
        case BrepSolidOrientation.Inward: brepType = DB.BRepType.Void; break;
        case BrepSolidOrientation.Outward: brepType = DB.BRepType.Solid; break;
      }

      using (var builder = new DB.BRepBuilder(brepType))
      {
        var brepEdges = new List<DB.BRepBuilderGeometryId>[brep.Edges.Count];
        foreach (var face in brep.Faces)
        {
          var faceId = builder.AddFace(ToHost(face), face.OrientationIsReversed);
          builder.SetFaceMaterialId(faceId, GeometryEncoder.Context.Peek.MaterialId);

          foreach (var loop in face.Loops)
          {
            var loopId = builder.AddLoop(faceId);

            IEnumerable<BrepTrim> trims = loop.Trims;
            if (face.OrientationIsReversed)
              trims = trims.Reverse();

            foreach (var trim in trims)
            {
              if (trim.TrimType != BrepTrimType.Boundary && trim.TrimType != BrepTrimType.Mated)
                continue;

              var edge = trim.Edge;
              if (edge is null)
                continue;

              var edgeIds = brepEdges[edge.EdgeIndex];
              if (edgeIds is null)
              {
                edgeIds = brepEdges[edge.EdgeIndex] = new List<DB.BRepBuilderGeometryId>();
                edgeIds.AddRange(ToHost(edge).Select(e => builder.AddEdge(e)));
              }

              bool trimReversed = face.OrientationIsReversed ?
                                  !trim.IsReversed() :
                                   trim.IsReversed();

              if (trimReversed)
              {
                for (int e = edgeIds.Count - 1; e >= 0; --e)
                  builder.AddCoEdge(loopId, edgeIds[e], true);
              }
              else
              {
                for (int e = 0; e < edgeIds.Count; ++e)
                  builder.AddCoEdge(loopId, edgeIds[e], false);
              }
            }

            builder.FinishLoop(loopId);
          }

          builder.FinishFace(faceId);
        }

        var brepBuilderOutcome = builder.Finish();
        if (builder.IsResultAvailable())
          return builder.GetResult();
      }

      return null;
    }
    #endregion

    #region Mesh
    public static DB.Mesh ToHost(Mesh mesh)
    {
      if (mesh is null)
        return null;

      using
      (
        var builder = new DB.TessellatedShapeBuilder()
        {
          GraphicsStyleId = GeometryEncoder.Context.Peek.GraphicsStyleId,
          Target = DB.TessellatedShapeBuilderTarget.Mesh,
          Fallback = DB.TessellatedShapeBuilderFallback.Salvage
        }
      )
      {
        var isSolid = mesh.SolidOrientation() != 0;
        builder.OpenConnectedFaceSet(isSolid);

        var vertices = mesh.Vertices.ToPoint3dArray();
        var triangle = new DB.XYZ[3];
        var quad = new DB.XYZ[4];

        foreach (var face in mesh.Faces)
        {
          if (face.IsQuad)
          {
            quad[0] = AsXYZ(vertices[face.A]);
            quad[1] = AsXYZ(vertices[face.B]);
            quad[2] = AsXYZ(vertices[face.C]);
            quad[3] = AsXYZ(vertices[face.D]);

            builder.AddFace(new DB.TessellatedFace(quad, GeometryEncoder.Context.Peek.MaterialId));
          }
          else
          {
            triangle[0] = AsXYZ(vertices[face.A]);
            triangle[1] = AsXYZ(vertices[face.B]);
            triangle[2] = AsXYZ(vertices[face.C]);

            builder.AddFace(new DB.TessellatedFace(triangle, GeometryEncoder.Context.Peek.MaterialId));
          }
        }
        builder.CloseConnectedFaceSet();

        builder.Build();
        using (var result = builder.GetBuildResult())
        {
          if (result.Outcome != DB.TessellatedShapeBuilderOutcome.Nothing)
          {
            var geometries = result.GetGeometricalObjects();
            if (geometries.Count == 1)
            {
              return geometries[0] as DB.Mesh;
            }
          }
        }
      }

      return null;
    }
    #endregion
  }
}

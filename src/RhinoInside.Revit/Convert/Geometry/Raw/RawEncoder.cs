using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry.Raw
{
  using Convert.System.Collections.Generic;

  /// <summary>
  /// Methods in this class convert Revit geometry from "Raw" form.
  /// <para>The input geometry is granted not to be modified on any way, no copies are necessary before calling this methods.</para>
  /// <para>Raw form is Rhino geometry in Revit internal units</para>
  /// </summary>
  static class RawEncoder
  {
    #region Values
    public static ARDB::UV AsUV(Point2f value)
    {
      return new ARDB::UV(value.X, value.Y);
    }
    public static ARDB::UV AsUV(Point2d value)
    {
      return new ARDB::UV(value.X, value.Y);
    }
    public static ARDB::UV AsUV(Vector2d value)
    {
      return new ARDB::UV(value.X, value.Y);
    }
    public static ARDB::UV AsUV(Vector2f value)
    {
      return new ARDB::UV(value.X, value.Y);
    }

    public static ARDB::XYZ AsXYZ(Point3f value)
    {
      return new ARDB::XYZ(value.X, value.Y, value.Z);
    }
    public static ARDB::XYZ AsXYZ(Point3d value)
    {
      return new ARDB::XYZ(value.X, value.Y, value.Z);
    }
    public static ARDB::XYZ AsXYZ(Vector3d value)
    {
      return new ARDB::XYZ(value.X, value.Y, value.Z);
    }
    public static ARDB::XYZ AsXYZ(Vector3f value)
    {
      return new ARDB::XYZ(value.X, value.Y, value.Z);
    }

    public static ARDB.Transform AsTransform(Transform value)
    {
      Debug.Assert(value.IsAffine);

      var result = ARDB.Transform.CreateTranslation(new ARDB.XYZ(value.M03, value.M13, value.M23));

      result.BasisX = new ARDB.XYZ(value.M00, value.M10, value.M20);
      result.BasisY = new ARDB.XYZ(value.M01, value.M11, value.M21);
      result.BasisZ = new ARDB.XYZ(value.M02, value.M12, value.M22);
      return result;
    }

    public static ARDB.BoundingBoxXYZ AsBoundingBoxXYZ(BoundingBox value)
    {
      return new ARDB.BoundingBoxXYZ
      {
        Min = AsXYZ(value.Min),
        Max = AsXYZ(value.Max),
        Enabled = value.IsValid
      };
    }

    public static ARDB.BoundingBoxXYZ AsBoundingBoxXYZ(Box value)
    {
      return new ARDB.BoundingBoxXYZ
      {
        Transform = AsTransform(Transform.PlaneToPlane(Plane.WorldXY, value.Plane)),
        Min = new ARDB.XYZ(value.X.Min, value.Y.Min, value.Z.Min),
        Max = new ARDB.XYZ(value.X.Max, value.Y.Max, value.Z.Max),
        Enabled = value.IsValid
      };
    }

    public static ARDB.Plane AsPlane(Plane value)
    {
      return ARDB.Plane.CreateByOriginAndBasis(AsXYZ(value.Origin), AsXYZ((Point3d) value.XAxis), AsXYZ((Point3d) value.YAxis));
    }

    public static ARDB.PolyLine AsPolyLine(Polyline value)
    {
      int count = value.Count;
      var points = new ARDB.XYZ[count];

      for (int p = 0; p < count; ++p)
        points[p] = AsXYZ(value[p]);

      return ARDB.PolyLine.Create(points);
    }
    #endregion

    #region Point
    public static ARDB.Point ToHost(Point value)
    {
      return ARDB.Point.Create(AsXYZ(value.Location));
    }
    #endregion

    #region Curve
    public static ARDB.Line ToHost(LineCurve value)
    {
      var line = value.Line;
      return ARDB.Line.CreateBound(AsXYZ(line.From), AsXYZ(line.To));
    }

    public static ARDB.Arc ToHost(ArcCurve value)
    {
      var arc = value.Arc;
      if (value.Arc.IsCircle)
        return ARDB.Arc.Create(AsPlane(arc.Plane), value.Radius, 0.0, 2.0 * Math.PI);
      else
        return ARDB.Arc.Create(AsXYZ(arc.StartPoint), AsXYZ(arc.EndPoint), AsXYZ(arc.MidPoint));
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

    public static ARDB.XYZ[] ToHost(NurbsCurvePointList list)
    {
      var count = list.Count;
      var points = new ARDB.XYZ[count];

      for(int p = 0; p < count; ++p)
      {
        var location = list[p].Location;
        points[p] = new ARDB::XYZ(location.X, location.Y, location.Z);
      }

      return points;
    }

    public static ARDB.Curve ToHost(NurbsCurve value)
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
        var weights = value.Points.ConvertAll(p => p.Weight);
        return ARDB.NurbSpline.CreateCurve(value.Degree, knots, controlPoints, weights);
      }
      else
      {
        return ARDB.NurbSpline.CreateCurve(value.Degree, knots, controlPoints);
      }
    }
    #endregion

    #region Brep
    public static IEnumerable<ARDB.BRepBuilderEdgeGeometry> ToHost(BrepEdge edge)
    {
      var edgeCurve = edge.EdgeCurve.Trim(edge.Domain);

      if (edge.ProxyCurveIsReversed)
        edgeCurve.Reverse();

      switch (edgeCurve)
      {
        case LineCurve line:
          yield return ARDB.BRepBuilderEdgeGeometry.Create(ToHost(line));
          yield break;
        case ArcCurve arc:
          yield return ARDB.BRepBuilderEdgeGeometry.Create(ToHost(arc));
          yield break;
        case NurbsCurve nurbs:
          yield return ARDB.BRepBuilderEdgeGeometry.Create(ToHost(nurbs));
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

    public static ARDB.XYZ[] ToHost(NurbsSurfacePointList list)
    {
      var count = list.CountU * list.CountV;
      var points = new ARDB.XYZ[count];

      int p = 0;
      foreach (var point in list)
      {
        var location = point.Location;
        points[p++] = new ARDB::XYZ(location.X, location.Y, location.Z);
      }

      return points;
    }

    public static ARDB.BRepBuilderSurfaceGeometry ToHost(BrepFace face)
    {
      // TODO: Implement conversion from other Rhino surface types like PlaneSurface, RevSurface and SumSurface.

      var isNurbs = face.UnderlyingSurface() is NurbsSurface;

      using (var nurbsSurface = face.ToNurbsSurface())
      {
        var degreeU = nurbsSurface.Degree(0);
        var degreeV = nurbsSurface.Degree(1);
        var knotsU = ToHost(nurbsSurface.KnotsU);
        var knotsV = ToHost(nurbsSurface.KnotsV);
        var controlPoints = ToHost(nurbsSurface.Points);
        var bboxUV = new ARDB.BoundingBoxUV(knotsU[0], knotsV[0], knotsU[knotsU.Length - 1], knotsV[knotsV.Length - 1]);

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

          return ARDB.BRepBuilderSurfaceGeometry.CreateNURBSSurface
          (
            degreeU, degreeV, knotsU, knotsV, controlPoints, weights, false, isNurbs ? bboxUV : default
          );
        }
        else
        {
          return ARDB.BRepBuilderSurfaceGeometry.CreateNURBSSurface
          (
            degreeU, degreeV, knotsU, knotsV, controlPoints, false, isNurbs ? bboxUV : default
          );
        }
      }
    }

    public static ARDB.Solid ToHost(Brep brep)
    {
      var brepType = ARDB.BRepType.OpenShell;
      switch (brep.SolidOrientation)
      {
        case BrepSolidOrientation.Inward: brepType = ARDB.BRepType.Void; break;
        case BrepSolidOrientation.Outward: brepType = ARDB.BRepType.Solid; break;
      }

      using (var builder = new ARDB.BRepBuilder(brepType))
      {
        var brepEdges = new List<ARDB.BRepBuilderGeometryId>[brep.Edges.Count];
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
                edgeIds = brepEdges[edge.EdgeIndex] = new List<ARDB.BRepBuilderGeometryId>();
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
    public static ARDB.Mesh ToHost(Mesh mesh)
    {
      if (mesh is null)
        return null;

      using
      (
        var builder = new ARDB.TessellatedShapeBuilder()
        {
          GraphicsStyleId = GeometryEncoder.Context.Peek.GraphicsStyleId,
          Target = ARDB.TessellatedShapeBuilderTarget.Mesh,
          Fallback = ARDB.TessellatedShapeBuilderFallback.Salvage
        }
      )
      {
        var isSolid = mesh.SolidOrientation() != 0;
        builder.OpenConnectedFaceSet(isSolid);

        var vertices = mesh.Vertices.ToPoint3dArray();
        var triangle = new ARDB.XYZ[3];
        var quad = new ARDB.XYZ[4];

        foreach (var face in mesh.Faces)
        {
          if (face.IsQuad)
          {
            quad[0] = AsXYZ(vertices[face.A]);
            quad[1] = AsXYZ(vertices[face.B]);
            quad[2] = AsXYZ(vertices[face.C]);
            quad[3] = AsXYZ(vertices[face.D]);

            builder.AddFace(new ARDB.TessellatedFace(quad, GeometryEncoder.Context.Peek.MaterialId));
          }
          else
          {
            triangle[0] = AsXYZ(vertices[face.A]);
            triangle[1] = AsXYZ(vertices[face.B]);
            triangle[2] = AsXYZ(vertices[face.C]);

            builder.AddFace(new ARDB.TessellatedFace(triangle, GeometryEncoder.Context.Peek.MaterialId));
          }
        }
        builder.CloseConnectedFaceSet();

        builder.Build();
        using (var result = builder.GetBuildResult())
        {
          if (result.Outcome != ARDB.TessellatedShapeBuilderOutcome.Nothing)
          {
            var geometries = result.GetGeometricalObjects();
            if (geometries.Count == 1)
            {
              return geometries[0] as ARDB.Mesh;
            }
          }
        }
      }

      return null;
    }
    #endregion
  }
}

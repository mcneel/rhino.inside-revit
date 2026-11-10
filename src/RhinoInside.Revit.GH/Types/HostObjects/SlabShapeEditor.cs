using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  internal sealed class SlabShapeEditor : IDisposable
  {
    readonly HostObject Host;
    readonly ARDB.SlabShapeEditor Editor;
    public SlabShapeEditor(HostObject host)
    {
      Host = host;
      Editor = Host.Value?.GetSlabShapeEditor();
    }
    public void Dispose() => (Editor as IDisposable).Dispose();

    public bool IsAvailable => Editor is object;
    public bool IsEnabled => Editor?.IsEnabled is true;
    public bool Enable()
    {
      if (!IsAvailable) return false;
      Editor.Enable();
      return true;
    }

    public void ResetSlabShape() => Editor?.ResetSlabShape();

    private static double GetBoundaryElevation(IList<Curve> boundary2d, IList<Curve> boundary3d, Point3d point)
    {
      var point2d = new Point3d(point.X, point.Y, 0.0);
      var closestPoint = NaN.Point3d;
      double minDistance = double.PositiveInfinity;
      for (int c = 0; c < boundary2d.Count; ++c)
      {
        if (!boundary2d[c].ClosestPoint(point, out var t, minDistance)) continue;
        var distance = boundary2d[c].PointAt(t).DistanceTo(point2d);
        if (distance < minDistance)
        {
          minDistance = distance;
          closestPoint = boundary3d[c].PointAt(t);
        }
      }

      return closestPoint.Z;
    }

    public bool SetSlabShape
    (
      IList<Curve> boundary2d,
      IList<Curve> boundary3d,
      IList<Point3d> points,
      IList<Line> creases,
      out IList<Point3d> skipedPoints,
      out IList<Line> skipedCreases,
      AbortToken token = default
    )
    {
      var hostLocation = Host.Location;
      var elevation = hostLocation.OriginZ;
      if (!double.IsNaN(elevation))
      {
        var compoundStructure = Host.Type.CompoundStructure;
        var minElevation = elevation - (compoundStructure.VariableLayerThickness ?? double.PositiveInfinity);

        elevation = GeometryEncoder.ToInternalLength(elevation);
        minElevation = GeometryEncoder.ToInternalLength(minElevation);

        skipedPoints = new List<Point3d>();
        skipedCreases = new List<Line>();

#if REVIT_2025
        var addInPlace = true;
#elif REVIT_2024
        // Move the points after crashes in Revit 2024.3.
        var addInPlace = host.Value is ARDB.Toposolid; 
#else
        // Before Revit 2024 is much faster to draw the points on the plane and move them after.
        var addInPlace = false; 
#endif

        var vertices = new Dictionary<Point3d, ARDB.SlabShapeVertex>(Editor.SlabShapeVertices.Size + (points?.Count ?? 0) + (creases?.Count ?? 0));
        ARDB.SlabShapeVertex AddVertex(Point3d point)
        {
          var x = GeometryEncoder.ToInternalLength(point.X);
          var y = GeometryEncoder.ToInternalLength(point.Y);
          var z = Math.Max(GeometryEncoder.ToInternalLength(point.Z), minElevation);

          var xyz = new Point3d(x, y, z);
          if (!vertices.TryGetValue(xyz, out var vertex))
          {
            try
            {
              if (addInPlace)
              {
                if ((vertex = Editor.AddPoint(new ARDB.XYZ(x, y, z))) is object)
                  vertices.Add(xyz, vertex);
              }
              else
              {
                if ((vertex = Editor.AddPoint(new ARDB.XYZ(x, y, elevation))) is object)
                  vertices.Add(xyz, vertex);
              }
            }
            catch { }
          }

          return vertex?.VertexType == ARDB.SlabShapeVertexType.Invalid ? null : vertex;
        }

        if (boundary2d is object && boundary3d is object)
        {
          for (int b = 0; b < boundary2d.Count; ++b)
            boundary2d[b] = boundary2d[b].ProjectToPlane(hostLocation);

          foreach (var vertex in Editor.SlabShapeVertices.Cast<ARDB.SlabShapeVertex>().TakeWhileNotAborted(token))
          {
            if (vertex.VertexType == ARDB.SlabShapeVertexType.Invalid) continue;
            if (vertex.VertexType == ARDB.SlabShapeVertexType.Interior) continue;

            var xyz = vertex.Position.ToPoint3d();
            xyz.Z = GetBoundaryElevation(boundary2d, boundary3d, xyz);

            if (addInPlace)
              Editor.ModifySubElement(vertex, GeometryEncoder.ToInternalLength(xyz.Z) - elevation);
            else
              vertices.Add(xyz / Revit.ModelUnits, vertex);
          }
        }

        if (points is object)
        {
          foreach (var point in points.TakeWhileNotAborted(token))
          {
            if (AddVertex(point) is null)
              skipedPoints.Add(point);
          }
        }

        if (creases is object)
        {
          foreach (var crease in creases.TakeWhileNotAborted(token))
          {
            if (crease.IsValid)
            {
              try
              {
                var from = AddVertex(crease.From);
                var to = AddVertex(crease.To);
                if (from is object && to is object && Editor.AddSplitLine(from, to) is IList<ARDB.SlabShapeCrease> lines)
                {
                  if (!addInPlace && lines.Count > 1)
                  {
                    foreach (var line in lines)
                    {
                      foreach (ARDB.SlabShapeVertex vertex in line.EndPoints)
                        AddVertex(vertex.Position.ToPoint3d());
                    }
                  }
                }
                else skipedCreases.Add(crease);
              }
              catch { skipedCreases.Add(crease); }
            }
            else skipedCreases.Add(crease);
          }
        }

        if (!addInPlace)
        {
          foreach (var vertex in vertices.OrderBy(x => x.Key.Z))
            Editor.ModifySubElement(vertex.Value, vertex.Key.Z - elevation);
        }

        return true;
      }

      skipedPoints = null;
      skipedCreases = null;
      return false;
    }
  }
}

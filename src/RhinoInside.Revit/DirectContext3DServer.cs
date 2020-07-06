#if REVIT_2018
using System;
using System.Diagnostics;
using System.Collections.Generic;

using DB = Autodesk.Revit.DB;
using DBES = Autodesk.Revit.DB.ExternalService;
using DB3D = Autodesk.Revit.DB.DirectContext3D;

using Rhino;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry.Raw;

namespace RhinoInside.Revit
{
  public abstract class DirectContext3DServer : DB3D.IDirectContext3DServer
  {
    #region IExternalServer
    public abstract string GetDescription();
    public abstract string GetName();
    string DBES.IExternalServer.GetVendorId() => "RMA";
    DBES.ExternalServiceId DBES.IExternalServer.GetServiceId() => DBES.ExternalServices.BuiltInExternalServices.DirectContext3DService;
    public abstract Guid GetServerId();
    #endregion

    #region IDirectContext3DServer
    string DB3D.IDirectContext3DServer.GetApplicationId() => string.Empty;
    string DB3D.IDirectContext3DServer.GetSourceId() => string.Empty;
    bool DB3D.IDirectContext3DServer.UsesHandles() => false;
    public virtual bool UseInTransparentPass(DB.View dBView) => false;
    public abstract bool CanExecute(DB.View dBView);
    public abstract DB.Outline GetBoundingBox(DB.View dBView);
    public abstract void RenderScene(DB.View dBView, DB.DisplayStyle displayStyle);
    #endregion

    virtual public void Register()
    {
      using (var service = DBES.ExternalServiceRegistry.GetService(DBES.ExternalServices.BuiltInExternalServices.DirectContext3DService) as DBES.MultiServerService)
      {
        service.AddServer(this);

        var activeServerIds = service.GetActiveServerIds();
        activeServerIds.Add(GetServerId());
        service.SetActiveServers(activeServerIds);
      }
    }

    virtual public void Unregister()
    {
      using (var service = DBES.ExternalServiceRegistry.GetService(DBES.ExternalServices.BuiltInExternalServices.DirectContext3DService) as DBES.MultiServerService)
      {
        var activeServerIds = service.GetActiveServerIds();
        activeServerIds.Remove(GetServerId());
        service.SetActiveServers(activeServerIds);

        service.RemoveServer(GetServerId());
      }
    }

    protected static bool IsModelView(DB.View dBView)
    {
      if
      (
        dBView.ViewType == DB.ViewType.FloorPlan ||
        dBView.ViewType == DB.ViewType.CeilingPlan ||
        dBView.ViewType == DB.ViewType.Elevation ||
        dBView.ViewType == DB.ViewType.Section ||
        dBView.ViewType == DB.ViewType.ThreeD
      )
        return true;

      return false;
    }

    public const int VertexThreshold = ushort.MaxValue + 1;

    static DB3D.IndexBuffer indexPointsBuffer;
    static DB3D.IndexBuffer IndexPointsBuffer(int pointsCount)
    {
      Debug.Assert(pointsCount <= VertexThreshold);

      if (indexPointsBuffer == null)
      {
        indexPointsBuffer = new DB3D.IndexBuffer(VertexThreshold * DB3D.IndexPoint.GetSizeInShortInts());
        indexPointsBuffer.Map(VertexThreshold * DB3D.IndexPoint.GetSizeInShortInts());
        using (var istream = indexPointsBuffer.GetIndexStreamPoint())
        {
          for (int vi = 0; vi < VertexThreshold; ++vi)
            istream.AddPoint(new DB3D.IndexPoint(vi));
        }
        indexPointsBuffer.Unmap();
      }

      Debug.Assert(indexPointsBuffer.IsValid());
      return indexPointsBuffer;
    }

    static DB3D.IndexBuffer indexLinesBuffer;
    static DB3D.IndexBuffer IndexLinesBuffer(int pointsCount)
    {
      Debug.Assert(pointsCount <= VertexThreshold);

      if (indexLinesBuffer == null)
      {
        indexLinesBuffer = new DB3D.IndexBuffer(VertexThreshold * DB3D.IndexLine.GetSizeInShortInts());
        indexLinesBuffer.Map(VertexThreshold * DB3D.IndexLine.GetSizeInShortInts());
        using (var istream = indexLinesBuffer.GetIndexStreamLine())
        {
          for (int vi = 0; vi < VertexThreshold - 1; ++vi)
            istream.AddLine(new DB3D.IndexLine(vi, vi + 1));
        }
        indexLinesBuffer.Unmap();
      }

      Debug.Assert(indexLinesBuffer.IsValid());
      return indexLinesBuffer;
    }


    protected static DB3D.VertexBuffer ToVertexBuffer
    (
      Mesh mesh,
      Primitive.Part part,
      out DB3D.VertexFormatBits vertexFormatBits,
      System.Drawing.Color color = default
    )
    {
      int verticesCount = part.EndVertexIndex - part.StartVertexIndex;
      int normalCount = mesh.Normals.Count == mesh.Vertices.Count ? verticesCount : 0;
      int colorsCount = color.IsEmpty ? (mesh.VertexColors.Count == mesh.Vertices.Count ? verticesCount : 0) : verticesCount;

      bool hasVertices = verticesCount > 0;
      bool hasNormals  = normalCount > 0;
      bool hasColors   = colorsCount > 0;

      if (hasVertices)
      {
        var vertices = mesh.Vertices;
        if (hasNormals)
        {
          var normals = mesh.Normals;
          if (hasColors)
          {
            vertexFormatBits = DB3D.VertexFormatBits.PositionNormalColored;
            var colors = mesh.VertexColors;
            var vb = new DB3D.VertexBuffer(verticesCount * DB3D.VertexPositionNormalColored.GetSizeInFloats());
            vb.Map(verticesCount * DB3D.VertexPositionNormalColored.GetSizeInFloats());
            using (var stream = vb.GetVertexStreamPositionNormalColored())
            {
              for (int v = part.StartVertexIndex; v < part.EndVertexIndex; ++v)
              {
                var c = !color.IsEmpty ? color : colors[v];
                uint T = Math.Max(1, 255u - c.A);
                stream.AddVertex(new DB3D.VertexPositionNormalColored(RawEncoder.AsXYZ(vertices[v]), RawEncoder.AsXYZ(normals[v]), new DB.ColorWithTransparency(c.R, c.G, c.B, T)));
              }
            }
            vb.Unmap();
            return vb;
          }
          else
          {
            vertexFormatBits = DB3D.VertexFormatBits.PositionNormal;
            var sizeInFloats = DB3D.VertexPositionNormal.GetSizeInFloats();
            var vb = new DB3D.VertexBuffer(verticesCount * sizeInFloats);
            vb.Map(verticesCount * sizeInFloats);

            using (var stream = vb.GetVertexStreamPositionNormal())
            {
              for (int v = part.StartVertexIndex; v < part.EndVertexIndex; ++v)
                stream.AddVertex(new DB3D.VertexPositionNormal(RawEncoder.AsXYZ(vertices[v]), RawEncoder.AsXYZ(normals[v])));
            }

            vb.Unmap();
            return vb;
          }
        }
        else
        {
          if (hasColors)
          {
            vertexFormatBits = DB3D.VertexFormatBits.PositionColored;
            var colors = mesh.VertexColors;
            var vb = new DB3D.VertexBuffer(verticesCount * DB3D.VertexPositionColored.GetSizeInFloats());
            vb.Map(verticesCount * DB3D.VertexPositionColored.GetSizeInFloats());
            using (var stream = vb.GetVertexStreamPositionColored())
            {
              for (int v = part.StartVertexIndex; v < part.EndVertexIndex; ++v)
              {
                var c = !color.IsEmpty ? color : colors[v];
                uint T = Math.Max(1, 255u - c.A);
                stream.AddVertex(new DB3D.VertexPositionColored(RawEncoder.AsXYZ(vertices[v]), new DB.ColorWithTransparency(c.R, c.G, c.B, T)));
              }
            }
            vb.Unmap();
            return vb;
          }
          else
          {
            vertexFormatBits = DB3D.VertexFormatBits.Position;
            var vb = new DB3D.VertexBuffer(verticesCount * DB3D.VertexPosition.GetSizeInFloats());
            vb.Map(verticesCount * DB3D.VertexPosition.GetSizeInFloats());
            using (var stream = vb.GetVertexStreamPosition())
            {
              for (int v = part.StartVertexIndex; v < part.EndVertexIndex; ++v)
                stream.AddVertex(new DB3D.VertexPosition(RawEncoder.AsXYZ(vertices[v])));
            }
            vb.Unmap();
            return vb;
          }
        }
      }

      vertexFormatBits = 0;
      return null;
    }

    protected static DB3D.IndexBuffer ToTrianglesBuffer
    (
      Mesh mesh, Primitive.Part part,
      out int triangleCount
      )
    {
      triangleCount = part.FaceCount;
      {
        var faces = mesh.Faces;
        for (int f = part.StartFaceIndex; f < part.EndFaceIndex; ++f)
        {
          if (faces[f].IsQuad)
            triangleCount++;
        }
      }

      if (triangleCount > 0)
      {
        var ib = new DB3D.IndexBuffer(triangleCount * DB3D.IndexTriangle.GetSizeInShortInts());
        ib.Map(triangleCount * DB3D.IndexTriangle.GetSizeInShortInts());

        using (var istream = ib.GetIndexStreamTriangle())
        {
          var faces = mesh.Faces;
          for (int f = part.StartFaceIndex; f < part.EndFaceIndex; ++f)
          {
            var face = faces[f];

            istream.AddTriangle(new DB3D.IndexTriangle(face.A - part.StartVertexIndex, face.B - part.StartVertexIndex, face.C - part.StartVertexIndex));
            if (face.IsQuad)
              istream.AddTriangle(new DB3D.IndexTriangle(face.C - part.StartVertexIndex, face.D - part.StartVertexIndex, face.A - part.StartVertexIndex));
          }
        }

        ib.Unmap();
        return ib;
      }

      return null;
    }

    protected static DB3D.IndexBuffer ToWireframeBuffer(Mesh mesh, out int linesCount)
    {
      linesCount = (mesh.Faces.Count * 3) + mesh.Faces.QuadCount;
      if (linesCount > 0)
      {
        var ib = new DB3D.IndexBuffer(linesCount * DB3D.IndexLine.GetSizeInShortInts());
        ib.Map(linesCount * DB3D.IndexLine.GetSizeInShortInts());

        using (var istream = ib.GetIndexStreamLine())
        {
          foreach (var face in mesh.Faces)
          {
            istream.AddLine(new DB3D.IndexLine(face.A, face.B));
            istream.AddLine(new DB3D.IndexLine(face.B, face.C));
            istream.AddLine(new DB3D.IndexLine(face.C, face.D));
            if (face.IsQuad)
              istream.AddLine(new DB3D.IndexLine(face.D, face.A));
          }
        }

        ib.Unmap();
        return ib;
      }

      return null;
    }

    protected static DB3D.IndexBuffer ToEdgeBuffer
    (
      Mesh mesh,
      Primitive.Part part,
      out int linesCount
    )
    {
      if (part.VertexCount != mesh.Vertices.Count)
      {
        if (part.VertexCount > 0)
        {
          linesCount = -part.VertexCount;
          return IndexPointsBuffer(part.VertexCount);
        }

        linesCount = 0;
      }
      else
      {
        var edgeIndices = new List<IndexPair>();
        if (mesh.Ngons.Count > 0)
        {
          foreach (var ngon in mesh.Ngons)
          {
            var boundary = ngon.BoundaryVertexIndexList();
            if ((boundary?.Length ?? 0) > 1)
            {
              for (int b = 0; b < boundary.Length - 1; ++b)
                edgeIndices.Add(new IndexPair((int) boundary[b], (int) boundary[b + 1]));

              edgeIndices.Add(new IndexPair((int) boundary[boundary.Length - 1], (int) boundary[0]));
            }
          }
        }
        else
        {
          var vertices = mesh.TopologyVertices;
          var edges = mesh.TopologyEdges;
          var edgeCount = edges.Count;
          for (int e = 0; e < edgeCount; ++e)
          {
            if (edges.IsEdgeUnwelded(e) || edges.GetConnectedFaces(e).Length < 2)
            {
              var pair = edges.GetTopologyVertices(e);
              pair.I = vertices.MeshVertexIndices(pair.I)[0];
              pair.J = vertices.MeshVertexIndices(pair.J)[0];
              edgeIndices.Add(pair);
            }
          }
        }

        linesCount = edgeIndices.Count;
        if (linesCount > 0)
        {
          var ib = new DB3D.IndexBuffer(linesCount * DB3D.IndexLine.GetSizeInShortInts());
          ib.Map(linesCount * DB3D.IndexLine.GetSizeInShortInts());
          using (var istream = ib.GetIndexStreamLine())
          {
            foreach (var edge in edgeIndices)
            {
              Debug.Assert(0 <= edge.I && edge.I < part.VertexCount);
              Debug.Assert(0 <= edge.J && edge.J < part.VertexCount);
              istream.AddLine(new DB3D.IndexLine(edge.I, edge.J));
            }
          }
          ib.Unmap();

          return ib;
        }
      }

      return null;
    }

    protected static int ToPolylineBuffer
    (
      Polyline polyline,
      out DB3D.VertexFormatBits vertexFormatBits,
      out DB3D.VertexBuffer vb, out int vertexCount,
      out DB3D.IndexBuffer ib
    )
    {
      int linesCount = 0;

      if (polyline.SegmentCount > 0)
      {
        linesCount = polyline.SegmentCount;
        vertexCount = polyline.Count;

        vertexFormatBits = DB3D.VertexFormatBits.Position;
        vb = new DB3D.VertexBuffer(vertexCount * DB3D.VertexPosition.GetSizeInFloats());
        vb.Map(vertexCount * DB3D.VertexPosition.GetSizeInFloats());
        using (var vstream = vb.GetVertexStreamPosition())
        {
          foreach (var v in polyline)
            vstream.AddVertex(new DB3D.VertexPosition(RawEncoder.AsXYZ(v)));
        }
        vb.Unmap();

        ib = IndexLinesBuffer(vertexCount);
      }
      else
      {
        vertexFormatBits = 0;
        vb = null; vertexCount = 0;
        ib = null;
      }

      return linesCount;
    }

    protected static int ToPointsBuffer
    (
      Point point,
      out DB3D.VertexFormatBits vertexFormatBits,
      out DB3D.VertexBuffer vb, out int vertexCount,
      out DB3D.IndexBuffer ib
    )
    {
      int pointsCount = 0;

      if (point.Location.IsValid)
      {
        pointsCount = 1;
        vertexCount = 1;

        vertexFormatBits = DB3D.VertexFormatBits.Position;
        vb = new DB3D.VertexBuffer(pointsCount * DB3D.VertexPosition.GetSizeInFloats());
        vb.Map(pointsCount * DB3D.VertexPosition.GetSizeInFloats());
        using (var vstream = vb.GetVertexStreamPosition())
        {
          vstream.AddVertex(new DB3D.VertexPosition(RawEncoder.AsXYZ(point.Location)));
        }
        vb.Unmap();

        ib = IndexPointsBuffer(pointsCount);
      }
      else
      {
        vertexFormatBits = 0;
        vb = null; vertexCount = 0;
        ib = null;
      }

      return pointsCount;
    }

    protected static int ToPointsBuffer
    (
      PointCloud pointCloud,
      Primitive.Part part,
      out DB3D.VertexFormatBits vertexFormatBits,
      out DB3D.VertexBuffer vb, out int vertexCount,
      out DB3D.IndexBuffer ib
    )
    {
      int pointsCount = part.VertexCount;
      int normalCount = pointCloud.ContainsNormals ? pointsCount : 0;
      int colorsCount = pointCloud.ContainsColors  ? pointsCount : 0;

      bool hasPoints  = pointsCount > 0;
      bool hasNormals = normalCount == pointsCount;
      bool hasColors  = colorsCount == pointsCount;

      if (hasPoints)
      {
        if (hasNormals)
        {
          if(hasColors)
          {
            vertexFormatBits = DB3D.VertexFormatBits.PositionNormalColored;
            vb = new DB3D.VertexBuffer(pointsCount * DB3D.VertexPositionNormalColored.GetSizeInFloats());
            vb.Map(pointsCount * DB3D.VertexPositionNormalColored.GetSizeInFloats());

            using (var vstream = vb.GetVertexStreamPositionNormalColored())
            {
              for(int p = part.StartVertexIndex; p < part.EndVertexIndex; ++p)
              {
                var point = pointCloud[p];
                var c = new DB.ColorWithTransparency(point.Color.R, point.Color.G, point.Color.B, 255u - point.Color.A);
                vstream.AddVertex(new DB3D.VertexPositionNormalColored(RawEncoder.AsXYZ(point.Location), RawEncoder.AsXYZ(point.Normal), c));
              }
            }

            vb.Unmap();
          }
          else
          {
            vertexFormatBits = DB3D.VertexFormatBits.PositionNormal;
            vb = new DB3D.VertexBuffer(pointsCount * DB3D.VertexPositionNormal.GetSizeInFloats());
            vb.Map(pointsCount * DB3D.VertexPositionNormal.GetSizeInFloats());

            using (var vstream = vb.GetVertexStreamPositionNormal())
            {
              for (int p = part.StartVertexIndex; p < part.EndVertexIndex; ++p)
              {
                var point = pointCloud[p];
                vstream.AddVertex(new DB3D.VertexPositionNormal(RawEncoder.AsXYZ(point.Location), RawEncoder.AsXYZ(point.Normal)));
              }
            }

            vb.Unmap();
          }
        }
        else
        {
          if (hasColors)
          {
            vertexFormatBits = DB3D.VertexFormatBits.PositionColored;
            vb = new DB3D.VertexBuffer(pointsCount * DB3D.VertexPositionColored.GetSizeInFloats());
            vb.Map(pointsCount * DB3D.VertexPositionColored.GetSizeInFloats());

            using (var vstream = vb.GetVertexStreamPositionColored())
            {
              for (int p = part.StartVertexIndex; p < part.EndVertexIndex; ++p)
              {
                var point = pointCloud[p];
                var c = new DB.ColorWithTransparency(point.Color.R, point.Color.G, point.Color.B, 255u - point.Color.A);
                vstream.AddVertex(new DB3D.VertexPositionColored(RawEncoder.AsXYZ(point.Location), c));
              }
            }

            vb.Unmap();
          }
          else
          {
            vertexFormatBits = DB3D.VertexFormatBits.Position;
            vb = new DB3D.VertexBuffer(pointsCount * DB3D.VertexPosition.GetSizeInFloats());
            vb.Map(pointsCount * DB3D.VertexPosition.GetSizeInFloats());

            using (var vstream = vb.GetVertexStreamPosition())
            {
              for (int p = part.StartVertexIndex; p < part.EndVertexIndex; ++p)
              {
                var point = pointCloud[p];
                vstream.AddVertex(new DB3D.VertexPosition(RawEncoder.AsXYZ(point.Location)));
              }
            }

            vb.Unmap();
          }
        }

        ib = IndexPointsBuffer(pointsCount);
      }
      else
      {
        vertexFormatBits = 0;
        vb = null;
        ib = null;
      }

      vertexCount = pointsCount;
      return pointsCount;
    }

#region Utils
    public static bool ShowsEdges(DB.DisplayStyle displayStyle)
    {
      return displayStyle == DB.DisplayStyle.Wireframe ||
             displayStyle == DB.DisplayStyle.HLR ||
             displayStyle == DB.DisplayStyle.ShadingWithEdges ||
             displayStyle == DB.DisplayStyle.FlatColors ||
             displayStyle == DB.DisplayStyle.RealisticWithEdges;
    }

    public static bool ShowsVertexColors(DB.DisplayStyle displayStyle)
    {
      return displayStyle == DB.DisplayStyle.Shading ||
             displayStyle == DB.DisplayStyle.ShadingWithEdges ||
             displayStyle == DB.DisplayStyle.Realistic ||
             displayStyle == DB.DisplayStyle.RealisticWithEdges;
    }

    public static bool HasVertexNormals(DB3D.VertexFormatBits vertexFormatBits) => (((int) vertexFormatBits) & 2) != 0;
    public static bool HasVertexColors (DB3D.VertexFormatBits vertexFormatBits) => (((int) vertexFormatBits) & 4) != 0;
#endregion

#region Primitive
    protected class Primitive : IDisposable
    {
      protected DB3D.VertexFormatBits vertexFormatBits;
      protected int vertexCount;
      protected DB3D.VertexBuffer vertexBuffer;
      protected DB3D.VertexFormat vertexFormat;

      protected int triangleCount;
      protected DB3D.IndexBuffer triangleBuffer;

      protected int linesCount;
      protected DB3D.IndexBuffer linesBuffer;

      protected DB3D.EffectInstance effectInstance;
      protected GeometryBase geometry;
      public struct Part
      {
        public readonly int StartVertexIndex;
        public readonly int EndVertexIndex;
        public readonly int StartFaceIndex;
        public readonly int EndFaceIndex;

        public int VertexCount => EndVertexIndex - StartVertexIndex;
        public int FaceCount   => EndFaceIndex - StartFaceIndex;

        public Part(int startVertexIndex, int endVertexIndex, int startFaceIndex, int endFaceIndex)
        {
          StartVertexIndex = startVertexIndex;
          EndVertexIndex   = endVertexIndex;
          StartFaceIndex   = startFaceIndex;
          EndFaceIndex     = endFaceIndex;
        }

        public Part(int startVertexIndex, int endVertexIndex) : this(startVertexIndex, endVertexIndex, 0, -1) { }

        public static implicit operator Part(PointCloud pc)
        {
          return new Part(0, pc?.Count ?? -1);
        }
        public static implicit operator Part(Mesh m)
        {
          return new Part(0, m?.Vertices.Count ?? -1, 0, m?.Faces.Count ?? -1);
        }
        public static implicit operator Part(MeshPart p)
        {
          return new Part(p?.StartVertexIndex ?? 0, p?.EndVertexIndex ?? -1, p?.StartFaceIndex ?? 0, p?.EndFaceIndex ?? -1);
        }
      }
      protected Part part;

      public readonly BoundingBox ClippingBox;

      public Primitive(Point p)               { geometry = p; ClippingBox = geometry.GetBoundingBox(false); }
      public Primitive(PointCloud pc)         { geometry = pc; ClippingBox = geometry.GetBoundingBox(false); part = pc; }
      public Primitive(PointCloud pc, Part p) { geometry = pc; ClippingBox = geometry.GetBoundingBox(false); part = p; }
      public Primitive(Curve c)               { geometry = c; ClippingBox = geometry.GetBoundingBox(false); }
      public Primitive(Mesh m)                { geometry = m; ClippingBox = geometry.GetBoundingBox(false); part = m; }
      public Primitive(Mesh m, Part p)        { geometry = m; ClippingBox = geometry.GetBoundingBox(false); part = p; }

      void IDisposable.Dispose()
      {
        effectInstance?.Dispose(); effectInstance = null;
        if(linesBuffer != indexLinesBuffer && linesBuffer != indexPointsBuffer)
        linesBuffer?.Dispose();    linesBuffer = null; linesCount = 0;
        triangleBuffer?.Dispose(); triangleBuffer = null; triangleCount = 0;
        vertexFormat?.Dispose();   vertexFormat = null;
        vertexBuffer?.Dispose();   vertexBuffer = null; vertexCount = 0;
      }

      public virtual DB3D.EffectInstance EffectInstance(DB.DisplayStyle displayStyle, bool IsShadingPass)
      {
        if (effectInstance == null)
          effectInstance = new DB3D.EffectInstance(vertexFormatBits);

        return effectInstance;
      }

      public virtual bool Regen()
      {
        if (geometry != null)
        {
          if (!BeginRegen())
            return false;

          if (geometry.IsValid)
          {
            if (geometry is Mesh mesh)
            {
              vertexBuffer = ToVertexBuffer(mesh, part, out vertexFormatBits);
              vertexCount = part.VertexCount;

              triangleBuffer = ToTrianglesBuffer(mesh, part, out triangleCount);
              linesBuffer = ToEdgeBuffer(mesh, part, out linesCount);
            }
            else if (geometry is Curve curve)
            {
              using (var polyline = curve.ToPolyline(Revit.ShortCurveTolerance * Revit.ModelUnits, Revit.AngleTolerance * 100.0, Revit.ShortCurveTolerance * Revit.ModelUnits, 0.0))
              {
                var pline = polyline.ToPolyline();

                // Reduce too complex polylines.
                {
                  var tol = Revit.VertexTolerance * Revit.ModelUnits;
                  while (pline.Count > 0x4000)
                  {
                    tol *= 2.0;
                    if (pline.ReduceSegments(tol) == 0)
                      break;
                  }
                }

                linesCount = ToPolylineBuffer(pline, out vertexFormatBits, out vertexBuffer, out vertexCount, out linesBuffer);
              }
            }
            else if (geometry is Point point)
            {
              linesCount = -ToPointsBuffer(point, out vertexFormatBits, out vertexBuffer, out vertexCount, out linesBuffer);
            }
            else if (geometry is PointCloud pointCloud)
            {
              linesCount = -ToPointsBuffer(pointCloud, part, out vertexFormatBits, out vertexBuffer, out vertexCount, out linesBuffer);
            }

            vertexFormat = new DB3D.VertexFormat(vertexFormatBits);
          }

          geometry = null;

          EndRegen();
        }

        return true;
      }

      public virtual void Draw(DB.DisplayStyle displayStyle)
      {
        if (!Regen())
          return;

        var vc = HasVertexColors(vertexFormatBits) && ShowsVertexColors(displayStyle);
        if (DB3D.DrawContext.IsTransparentPass() != vc)
        {
          if (vertexCount > 0)
          {
            var ei = EffectInstance(displayStyle, true);

            if (triangleCount > 0)
            {
              DB3D.DrawContext.FlushBuffer
              (
                vertexBuffer, vertexCount,
                triangleBuffer, triangleCount * 3,
                vertexFormat, ei,
                DB3D.PrimitiveType.TriangleList,
                0, triangleCount
              );
            }
            else if (linesBuffer != null)
            {
              DB3D.DrawContext.FlushBuffer
              (
                vertexBuffer, vertexCount,
                linesBuffer, vertexCount,
                vertexFormat, ei,
                DB3D.PrimitiveType.PointList,
                0, vertexCount
              );
            }
          }
        }

        if(!DB3D.DrawContext.IsTransparentPass())
        {
          if (linesCount != 0)
          {
            if (triangleBuffer != null && !ShowsEdges(displayStyle))
              return;

            if (linesCount > 0)
            {
              DB3D.DrawContext.FlushBuffer
              (
                vertexBuffer, vertexCount,
                linesBuffer, linesCount * 2,
                vertexFormat, EffectInstance(displayStyle, false),
                DB3D.PrimitiveType.LineList,
                0, linesCount
              );
            }
            else if(triangleCount == 0)
            {
              DB3D.DrawContext.FlushBuffer
              (
                vertexBuffer, vertexCount,
                linesBuffer, vertexCount,
                vertexFormat, EffectInstance(displayStyle, false),
                DB3D.PrimitiveType.PointList,
                0, vertexCount
              );
            }
          }
        }
      }
    }

    static Stopwatch RegenTime = new Stopwatch();
    public static long RegenThreshold = 200;

    static bool BeginRegen()
    {
      if (RegenTime.ElapsedMilliseconds > RegenThreshold)
        return false;

      RegenTime.Start();
      return true;
    }
    static void EndRegen()
    {
      RegenTime.Stop();
    }

    public static bool RegenComplete()
    {
      var ms = RegenTime.ElapsedMilliseconds;
      RegenTime.Reset();
      return ms == 0;
    }
#endregion
  }
}
#else
namespace RhinoInside.Revit
{
  public abstract class DirectContext3DServer
  {
    public static bool RegenComplete() => true;
    public static long RegenThreshold = 200;
  }
}
#endif

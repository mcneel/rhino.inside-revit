#if REVIT_2018
using System;
using System.Diagnostics;
using System.Collections.Generic;

using ARDB = Autodesk.Revit.DB;
using ARDBES = Autodesk.Revit.DB.ExternalService;
using ARDB3D = Autodesk.Revit.DB.DirectContext3D;

using Rhino;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry.Raw;

namespace RhinoInside.Revit
{
  internal abstract class DirectContext3DServer : ARDB3D.IDirectContext3DServer
  {
    #region IExternalServer
    public abstract string GetDescription();
    public abstract string GetName();
    string ARDBES.IExternalServer.GetVendorId() => "com.mcneel";
    ARDBES.ExternalServiceId ARDBES.IExternalServer.GetServiceId() => ARDBES.ExternalServices.BuiltInExternalServices.DirectContext3DService;
    public abstract Guid GetServerId();
    #endregion

    #region IDirectContext3DServer
    string ARDB3D.IDirectContext3DServer.GetApplicationId() => string.Empty;
    string ARDB3D.IDirectContext3DServer.GetSourceId() => string.Empty;
    bool ARDB3D.IDirectContext3DServer.UsesHandles() => false;
    public virtual bool UseInTransparentPass(ARDB.View dBView) => false;
    public abstract bool CanExecute(ARDB.View dBView);
    public abstract ARDB.Outline GetBoundingBox(ARDB.View dBView);
    public abstract void RenderScene(ARDB.View dBView, ARDB.DisplayStyle displayStyle);
    #endregion

    public virtual void Register()
    {
      using (var service = ARDBES.ExternalServiceRegistry.GetService(ARDBES.ExternalServices.BuiltInExternalServices.DirectContext3DService) as ARDBES.MultiServerService)
      {
        service.AddServer(this);

        var activeServerIds = service.GetActiveServerIds();
        activeServerIds.Add(GetServerId());
        service.SetActiveServers(activeServerIds);
      }
    }

    public virtual void Unregister()
    {
      using (var service = ARDBES.ExternalServiceRegistry.GetService(ARDBES.ExternalServices.BuiltInExternalServices.DirectContext3DService) as ARDBES.MultiServerService)
      {
        var activeServerIds = service.GetActiveServerIds();
        activeServerIds.Remove(GetServerId());
        service.SetActiveServers(activeServerIds);

        service.RemoveServer(GetServerId());
      }
    }

    protected static bool IsModelView(ARDB.View dBView)
    {
      if
      (
        dBView.ViewType == ARDB.ViewType.FloorPlan ||
        dBView.ViewType == ARDB.ViewType.CeilingPlan ||
        dBView.ViewType == ARDB.ViewType.Elevation ||
        dBView.ViewType == ARDB.ViewType.Section ||
        dBView.ViewType == ARDB.ViewType.ThreeD
      )
        return true;

      return false;
    }

    public const int VertexThreshold = ushort.MaxValue + 1;

    static ARDB3D.IndexBuffer indexPointsBuffer;
    static ARDB3D.IndexBuffer IndexPointsBuffer(int pointsCount)
    {
      Debug.Assert(pointsCount <= VertexThreshold);

      if (indexPointsBuffer == null)
      {
        indexPointsBuffer = new ARDB3D.IndexBuffer(VertexThreshold * ARDB3D.IndexPoint.GetSizeInShortInts());
        indexPointsBuffer.Map(VertexThreshold * ARDB3D.IndexPoint.GetSizeInShortInts());
        using (var istream = indexPointsBuffer.GetIndexStreamPoint())
        {
          using (var point = new ARDB3D.IndexPoint(0))
          {
            for (int vi = 0; vi < VertexThreshold; ++vi)
            {
              point.Index = vi;
              istream.AddPoint(point);
            }
          }
        }
        indexPointsBuffer.Unmap();
      }

      Debug.Assert(indexPointsBuffer.IsValid());
      return indexPointsBuffer;
    }

    static ARDB3D.IndexBuffer indexLinesBuffer;
    static ARDB3D.IndexBuffer IndexLinesBuffer(int pointsCount)
    {
      Debug.Assert(pointsCount <= VertexThreshold);

      if (indexLinesBuffer == null)
      {
        indexLinesBuffer = new ARDB3D.IndexBuffer(VertexThreshold * ARDB3D.IndexLine.GetSizeInShortInts());
        indexLinesBuffer.Map(VertexThreshold * ARDB3D.IndexLine.GetSizeInShortInts());
        using (var istream = indexLinesBuffer.GetIndexStreamLine())
        {
          for (int vi = 0; vi < VertexThreshold - 1; ++vi)
            istream.AddLine(new ARDB3D.IndexLine(vi, vi + 1));
        }
        indexLinesBuffer.Unmap();
      }

      Debug.Assert(indexLinesBuffer.IsValid());
      return indexLinesBuffer;
    }

    /// <summary>
    /// Convert Alpha value into a Transparency value
    /// </summary>
    /// <remarks>Since we are drawing in TransparentPass seems no pixel should be opaque</remarks>
    /// <param name="alpha"></param>
    /// <returns></returns>
    static uint AlphaToTransparency(byte alpha) => Math.Max(1u, 255u - alpha);

    protected static ARDB3D.VertexBuffer ToVertexBuffer
    (
      Mesh mesh,
      Primitive.Part part,
      out ARDB3D.VertexFormatBits vertexFormatBits,
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
            vertexFormatBits = ARDB3D.VertexFormatBits.PositionNormalColored;
            var colors = mesh.VertexColors;
            var vb = new ARDB3D.VertexBuffer(verticesCount * ARDB3D.VertexPositionNormalColored.GetSizeInFloats());
            vb.Map(verticesCount * ARDB3D.VertexPositionNormalColored.GetSizeInFloats());
            using (var stream = vb.GetVertexStreamPositionNormalColored())
            {
              using (var clr = new ARDB.ColorWithTransparency(color.R, color.G, color.B, AlphaToTransparency(color.A)))
              {
                using (var vtx = new ARDB3D.VertexPositionNormalColored(ARDB.XYZ.Zero, ARDB.XYZ.Zero, clr))
                {
                  for (int v = part.StartVertexIndex; v < part.EndVertexIndex; ++v)
                  {
                    if (color.IsEmpty)
                    {
                      var c = colors[v];
                      clr.SetRed(c.R);
                      clr.SetGreen(c.G);
                      clr.SetBlue(c.B);
                      clr.SetTransparency(AlphaToTransparency(c.A));
                      vtx.SetColor(clr);
                    }

                    vtx.Position = RawEncoder.AsXYZ(vertices[v]);
                    vtx.Normal = RawEncoder.AsXYZ(normals[v]);
                    stream.AddVertex(vtx);
                  }
                }
              }
            }
            vb.Unmap();
            return vb;
          }
          else
          {
            vertexFormatBits = ARDB3D.VertexFormatBits.PositionNormal;
            var sizeInFloats = ARDB3D.VertexPositionNormal.GetSizeInFloats();
            var vb = new ARDB3D.VertexBuffer(verticesCount * sizeInFloats);
            vb.Map(verticesCount * sizeInFloats);

            using (var stream = vb.GetVertexStreamPositionNormal())
            {
              using (var vtx = new ARDB3D.VertexPositionNormal(ARDB.XYZ.Zero, ARDB.XYZ.Zero))
              {
                for (int v = part.StartVertexIndex; v < part.EndVertexIndex; ++v)
                {
                  vtx.Position = RawEncoder.AsXYZ(vertices[v]);
                  vtx.Normal = RawEncoder.AsXYZ(normals[v]);
                  stream.AddVertex(vtx);
                }
              }
            }

            vb.Unmap();
            return vb;
          }
        }
        else
        {
          if (hasColors)
          {
            vertexFormatBits = ARDB3D.VertexFormatBits.PositionColored;
            var colors = mesh.VertexColors;
            var vb = new ARDB3D.VertexBuffer(verticesCount * ARDB3D.VertexPositionColored.GetSizeInFloats());
            vb.Map(verticesCount * ARDB3D.VertexPositionColored.GetSizeInFloats());
            using (var stream = vb.GetVertexStreamPositionColored())
            {
              using (var clr = new ARDB.ColorWithTransparency(color.R, color.G, color.B, AlphaToTransparency(color.A)))
              {
                using (var vtx = new ARDB3D.VertexPositionColored(ARDB.XYZ.Zero, clr))
                {
                  for (int v = part.StartVertexIndex; v < part.EndVertexIndex; ++v)
                  {
                    if (color.IsEmpty)
                    {
                      var c = colors[v];
                      clr.SetRed(c.R);
                      clr.SetGreen(c.G);
                      clr.SetBlue(c.B);
                      clr.SetTransparency(AlphaToTransparency(c.A));
                      vtx.SetColor(clr);
                    }

                    vtx.Position = RawEncoder.AsXYZ(vertices[v]);
                    stream.AddVertex(vtx);
                  }
                }
              }
            }
            vb.Unmap();
            return vb;
          }
          else
          {
            vertexFormatBits = ARDB3D.VertexFormatBits.Position;
            var vb = new ARDB3D.VertexBuffer(verticesCount * ARDB3D.VertexPosition.GetSizeInFloats());
            vb.Map(verticesCount * ARDB3D.VertexPosition.GetSizeInFloats());
            using (var stream = vb.GetVertexStreamPosition())
            {
              using (var vtx = new ARDB3D.VertexPosition(ARDB.XYZ.Zero))
              {
                for (int v = part.StartVertexIndex; v < part.EndVertexIndex; ++v)
                {
                  vtx.Position = RawEncoder.AsXYZ(vertices[v]);
                  stream.AddVertex(vtx);
                }
              }
            }
            vb.Unmap();
            return vb;
          }
        }
      }

      vertexFormatBits = 0;
      return null;
    }

    protected static ARDB3D.IndexBuffer ToTrianglesBuffer
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
        var ib = new ARDB3D.IndexBuffer(triangleCount * ARDB3D.IndexTriangle.GetSizeInShortInts());
        ib.Map(triangleCount * ARDB3D.IndexTriangle.GetSizeInShortInts());

        using (var istream = ib.GetIndexStreamTriangle())
        {
          var faces = mesh.Faces;
          using (var triangle = new ARDB3D.IndexTriangle(0, 0, 0))
          {
            for (int f = part.StartFaceIndex; f < part.EndFaceIndex; ++f)
            {
              var face = faces[f];

              triangle.Index0 = face.A - part.StartVertexIndex;
              triangle.Index1 = face.B - part.StartVertexIndex;
              triangle.Index2 = face.C - part.StartVertexIndex;
              istream.AddTriangle(triangle);

              if (face.IsQuad)
              {
                triangle.Index0 = face.C - part.StartVertexIndex;
                triangle.Index1 = face.D - part.StartVertexIndex;
                triangle.Index2 = face.A - part.StartVertexIndex;
                istream.AddTriangle(triangle);
              }
            }
          }
        }

        ib.Unmap();
        return ib;
      }

      return null;
    }

    protected static ARDB3D.IndexBuffer ToWireframeBuffer(Mesh mesh, out int linesCount)
    {
      linesCount = (mesh.Faces.Count * 3) + mesh.Faces.QuadCount;
      if (linesCount > 0)
      {
        var ib = new ARDB3D.IndexBuffer(linesCount * ARDB3D.IndexLine.GetSizeInShortInts());
        ib.Map(linesCount * ARDB3D.IndexLine.GetSizeInShortInts());

        using (var istream = ib.GetIndexStreamLine())
        {
          using (var line = new ARDB3D.IndexLine(0, 0))
          {
            foreach (var face in mesh.Faces)
            {
              line.Index0 = face.A; line.Index1 = face.B;
              istream.AddLine(line);
              line.Index0 = face.B; line.Index1 = face.C;
              istream.AddLine(line);
              line.Index0 = face.C; line.Index1 = face.D;
              istream.AddLine(line);

              if (face.IsQuad)
              {
                line.Index0 = face.D; line.Index1 = face.A;
                istream.AddLine(line);
              }
            }
          }
        }

        ib.Unmap();
        return ib;
      }

      return null;
    }

    protected static ARDB3D.IndexBuffer ToEdgeBuffer
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
          var ib = new ARDB3D.IndexBuffer(linesCount * ARDB3D.IndexLine.GetSizeInShortInts());
          ib.Map(linesCount * ARDB3D.IndexLine.GetSizeInShortInts());
          using (var istream = ib.GetIndexStreamLine())
          {
            using (var line = new ARDB3D.IndexLine(0, 0))
            {
              foreach (var edge in edgeIndices)
              {
                Debug.Assert(0 <= edge.I && edge.I < part.VertexCount);
                Debug.Assert(0 <= edge.J && edge.J < part.VertexCount);
                line.Index0 = edge.I; line.Index1 = edge.J;
                istream.AddLine(line);
              }
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
      out ARDB3D.VertexFormatBits vertexFormatBits,
      out ARDB3D.VertexBuffer vb, out int vertexCount,
      out ARDB3D.IndexBuffer ib
    )
    {
      int linesCount = 0;

      if (polyline.SegmentCount > 0)
      {
        linesCount = polyline.SegmentCount;
        vertexCount = polyline.Count;

        vertexFormatBits = ARDB3D.VertexFormatBits.Position;
        vb = new ARDB3D.VertexBuffer(vertexCount * ARDB3D.VertexPosition.GetSizeInFloats());
        vb.Map(vertexCount * ARDB3D.VertexPosition.GetSizeInFloats());
        using (var vstream = vb.GetVertexStreamPosition())
        {
          using (var vtx = new ARDB3D.VertexPosition(ARDB.XYZ.Zero))
          {
            foreach (var v in polyline)
            {
              vtx.Position = RawEncoder.AsXYZ(v);
              vstream.AddVertex(vtx);
            }
          }
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
      out ARDB3D.VertexFormatBits vertexFormatBits,
      out ARDB3D.VertexBuffer vb, out int vertexCount,
      out ARDB3D.IndexBuffer ib
    )
    {
      int pointsCount = 0;

      if (point.Location.IsValid)
      {
        pointsCount = 1;
        vertexCount = 1;

        vertexFormatBits = ARDB3D.VertexFormatBits.Position;
        vb = new ARDB3D.VertexBuffer(pointsCount * ARDB3D.VertexPosition.GetSizeInFloats());
        vb.Map(pointsCount * ARDB3D.VertexPosition.GetSizeInFloats());
        using (var vstream = vb.GetVertexStreamPosition())
        {
          using (var vtx = new ARDB3D.VertexPosition(RawEncoder.AsXYZ(point.Location)))
            vstream.AddVertex(vtx);
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
      out ARDB3D.VertexFormatBits vertexFormatBits,
      out ARDB3D.VertexBuffer vb, out int vertexCount,
      out ARDB3D.IndexBuffer ib
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
            vertexFormatBits = ARDB3D.VertexFormatBits.PositionNormalColored;
            vb = new ARDB3D.VertexBuffer(pointsCount * ARDB3D.VertexPositionNormalColored.GetSizeInFloats());
            vb.Map(pointsCount * ARDB3D.VertexPositionNormalColored.GetSizeInFloats());

            using (var vstream = vb.GetVertexStreamPositionNormalColored())
            {
              using (var clr = new ARDB.ColorWithTransparency())
              {
                using (var vtx = new ARDB3D.VertexPositionNormalColored(ARDB.XYZ.Zero, ARDB.XYZ.Zero, clr))
                {
                  for (int p = part.StartVertexIndex; p < part.EndVertexIndex; ++p)
                  {
                    var point = pointCloud[p];
                    clr.SetRed(point.Color.R);
                    clr.SetGreen(point.Color.G);
                    clr.SetBlue(point.Color.B);
                    clr.SetTransparency(AlphaToTransparency(point.Color.A));
                    vtx.SetColor(clr);
                    vtx.Normal = RawEncoder.AsXYZ(point.Normal);
                    vtx.Position = RawEncoder.AsXYZ(point.Location);
                    vstream.AddVertex(vtx);
                  }
                }
              }
            }

            vb.Unmap();
          }
          else
          {
            vertexFormatBits = ARDB3D.VertexFormatBits.PositionNormal;
            vb = new ARDB3D.VertexBuffer(pointsCount * ARDB3D.VertexPositionNormal.GetSizeInFloats());
            vb.Map(pointsCount * ARDB3D.VertexPositionNormal.GetSizeInFloats());

            using (var vstream = vb.GetVertexStreamPositionNormal())
            {
              using (var vtx = new ARDB3D.VertexPositionNormal(ARDB.XYZ.Zero, ARDB.XYZ.Zero))
              {
                for (int p = part.StartVertexIndex; p < part.EndVertexIndex; ++p)
                {
                  var point = pointCloud[p];
                  vtx.Normal = RawEncoder.AsXYZ(point.Normal);
                  vtx.Position = RawEncoder.AsXYZ(point.Location);
                  vstream.AddVertex(vtx);
                }
              }
            }

            vb.Unmap();
          }
        }
        else
        {
          if (hasColors)
          {
            vertexFormatBits = ARDB3D.VertexFormatBits.PositionColored;
            vb = new ARDB3D.VertexBuffer(pointsCount * ARDB3D.VertexPositionColored.GetSizeInFloats());
            vb.Map(pointsCount * ARDB3D.VertexPositionColored.GetSizeInFloats());

            using (var vstream = vb.GetVertexStreamPositionColored())
            {
              using (var clr = new ARDB.ColorWithTransparency())
              {
                using (var vtx = new ARDB3D.VertexPositionColored(ARDB.XYZ.Zero, clr))
                {
                  for (int p = part.StartVertexIndex; p < part.EndVertexIndex; ++p)
                  {
                    var point = pointCloud[p];
                    clr.SetRed(point.Color.R);
                    clr.SetGreen(point.Color.G);
                    clr.SetBlue(point.Color.B);
                    clr.SetTransparency(AlphaToTransparency(point.Color.A));
                    vtx.SetColor(clr);
                    vtx.Position = RawEncoder.AsXYZ(point.Location);
                    vstream.AddVertex(vtx);
                  }
                }
              }
            }

            vb.Unmap();
          }
          else
          {
            vertexFormatBits = ARDB3D.VertexFormatBits.Position;
            vb = new ARDB3D.VertexBuffer(pointsCount * ARDB3D.VertexPosition.GetSizeInFloats());
            vb.Map(pointsCount * ARDB3D.VertexPosition.GetSizeInFloats());

            using (var vstream = vb.GetVertexStreamPosition())
            {
              using (var vtx = new ARDB3D.VertexPosition(ARDB.XYZ.Zero))
              {
                for (int p = part.StartVertexIndex; p < part.EndVertexIndex; ++p)
                {
                  var point = pointCloud[p];
                  vtx.Position = RawEncoder.AsXYZ(point.Location);
                  vstream.AddVertex(vtx);
                }
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
    public static bool ShowsEdges(ARDB.DisplayStyle displayStyle)
    {
      return displayStyle == ARDB.DisplayStyle.Wireframe ||
             displayStyle == ARDB.DisplayStyle.HLR ||
             displayStyle == ARDB.DisplayStyle.ShadingWithEdges ||
             displayStyle == ARDB.DisplayStyle.FlatColors ||
             displayStyle == ARDB.DisplayStyle.RealisticWithEdges;
    }

    public static bool ShowsVertexColors(ARDB.DisplayStyle displayStyle)
    {
      return displayStyle == ARDB.DisplayStyle.Shading ||
             displayStyle == ARDB.DisplayStyle.ShadingWithEdges ||
             displayStyle == ARDB.DisplayStyle.Realistic ||
             displayStyle == ARDB.DisplayStyle.RealisticWithEdges;
    }

    public static bool IsAvailable(ARDB.View view)
    {
      if (view is null) return false;
      if (view.Document.IsFamilyDocument) return false;
      if (!IsModelView(view)) return false;

      var displayStyle = view.DisplayStyle;
      return
       displayStyle == ARDB.DisplayStyle.Wireframe ||
       displayStyle == ARDB.DisplayStyle.HLR ||
       displayStyle == ARDB.DisplayStyle.Shading ||
       displayStyle == ARDB.DisplayStyle.ShadingWithEdges ||
       displayStyle == ARDB.DisplayStyle.FlatColors;
    }

    public static bool HasVertexNormals(ARDB3D.VertexFormatBits vertexFormatBits) => (((int) vertexFormatBits) & 2) != 0;
    public static bool HasVertexColors (ARDB3D.VertexFormatBits vertexFormatBits) => (((int) vertexFormatBits) & 4) != 0;
#endregion

#region Primitive
    protected class Primitive : IDisposable
    {
      protected ARDB3D.VertexFormatBits vertexFormatBits;
      protected int vertexCount;
      protected ARDB3D.VertexBuffer vertexBuffer;
      protected ARDB3D.VertexFormat vertexFormat;

      protected int triangleCount;
      protected ARDB3D.IndexBuffer triangleBuffer;

      protected int linesCount;
      protected ARDB3D.IndexBuffer linesBuffer;

      protected ARDB3D.EffectInstance effectInstance;
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

      public virtual ARDB3D.EffectInstance EffectInstance(ARDB.DisplayStyle displayStyle, bool IsShadingPass)
      {
        if (effectInstance == null)
          effectInstance = new ARDB3D.EffectInstance(vertexFormatBits);

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
              var tol = Convert.Geometry.GeometryObjectTolerance.Model;
              using (var polyline = curve.ToPolyline(tol.ShortCurveTolerance, tol.AngleTolerance * 100.0, tol.ShortCurveTolerance, 0.0))
              {
                if (polyline?.ToPolyline() is Polyline pline)
                {

                  // Reduce too complex polylines.
                  {
                    var ctol = tol.VertexTolerance;
                    while (pline.Count > 0x4000)
                    {
                      ctol *= 2.0;
                      if (pline.ReduceSegments(ctol) == 0)
                        break;
                    }
                  }

                  linesCount = ToPolylineBuffer(pline, out vertexFormatBits, out vertexBuffer, out vertexCount, out linesBuffer);
                }
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

            if (vertexFormatBits != default)
              vertexFormat = new ARDB3D.VertexFormat(vertexFormatBits);
          }

          geometry = null;

          EndRegen();
        }

        return true;
      }

      public virtual void Draw(ARDB.DisplayStyle displayStyle)
      {
        if (!Regen())
          return;

        if (ARDB3D.DrawContext.IsTransparentPass())
        {
          if (vertexCount > 0)
          {
            var ei = EffectInstance(displayStyle, true);

            if (triangleCount > 0)
            {
              ARDB3D.DrawContext.FlushBuffer
              (
                vertexBuffer, vertexCount,
                triangleBuffer, triangleCount * 3,
                vertexFormat, ei,
                ARDB3D.PrimitiveType.TriangleList,
                0, triangleCount
              );
            }
            else if (linesBuffer != null)
            {
              ARDB3D.DrawContext.FlushBuffer
              (
                vertexBuffer, vertexCount,
                linesBuffer, vertexCount,
                vertexFormat, ei,
                ARDB3D.PrimitiveType.PointList,
                0, vertexCount
              );
            }
          }
        }

        if(!ARDB3D.DrawContext.IsTransparentPass())
        {
          if (linesCount != 0)
          {
            if (triangleBuffer != null && !ShowsEdges(displayStyle))
              return;

            if (linesCount > 0)
            {
              ARDB3D.DrawContext.FlushBuffer
              (
                vertexBuffer, vertexCount,
                linesBuffer, linesCount * 2,
                vertexFormat, EffectInstance(displayStyle, false),
                ARDB3D.PrimitiveType.LineList,
                0, linesCount
              );
            }
            else if(triangleCount == 0)
            {
              ARDB3D.DrawContext.FlushBuffer
              (
                vertexBuffer, vertexCount,
                linesBuffer, vertexCount,
                vertexFormat, EffectInstance(displayStyle, false),
                ARDB3D.PrimitiveType.PointList,
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

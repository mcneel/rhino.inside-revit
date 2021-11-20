#if REVIT_2018
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color;

using DB = Autodesk.Revit.DB;
using DBES = Autodesk.Revit.DB.ExternalService;
using DB3D = Autodesk.Revit.DB.DirectContext3D;

using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;

using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Drawing;

namespace RhinoInside.Revit
{
  static partial class Rhinoceros
  {
    internal class PreviewServer : DirectContext3DServer
    {
      static PreviewServer()
      {
        RhinoDoc.CloseDocument += RhinoDoc_CloseDocument;
        RhinoDoc.AddRhinoObject += RhinoDoc_AddRhinoObject;
        RhinoDoc.DeleteRhinoObject += RhinoDoc_DeleteRhinoObject;
        RhinoDoc.UndeleteRhinoObject += RhinoDoc_AddRhinoObject;
        RhinoDoc.ModifyObjectAttributes += RhinoDoc_ModifyObjectAttributes;
        RhinoDoc.LayerTableEvent += RhinoDoc_LayerTableEvent;
        RhinoDoc.MaterialTableEvent += RhinoDoc_MaterialTableEvent;
      }

      static void RhinoDoc_CloseDocument(object sender, DocumentEventArgs e)
      {
        if (e.Document == ActiveDocument)
          ActiveDocument = null;
      }

      static void RhinoDoc_AddRhinoObject(object sender, RhinoObjectEventArgs e)
      {
        if (e.TheObject.Document == ActiveDocument && ObjectPrimitive.IsSupportedObject(e.TheObject, true))
        {
          Revit.EnqueueReadAction((doc, canceled) => new PreviewServer(e.TheObject).Register());
          Revit.RefreshActiveView();
        }
      }

      static void RhinoDoc_DeleteRhinoObject(object sender, RhinoObjectEventArgs e)
      {
        if (e.TheObject.Document == ActiveDocument && ObjectPrimitive.IsSupportedObject(e.TheObject, false))
        {
          Revit.EnqueueReadAction((doc, canceled) => objectPreviews[e.TheObject.Id]?.Unregister());
          Revit.RefreshActiveView();
        }
      }

      static void RhinoDoc_ModifyObjectAttributes(object sender, RhinoModifyObjectAttributesEventArgs e)
      {
        if (e.Document == ActiveDocument) Revit.RefreshActiveView();
      }

      static void RhinoDoc_MaterialTableEvent(object sender, MaterialTableEventArgs e)
      {
        if (e.Document == ActiveDocument) Revit.RefreshActiveView();
      }

      static void RhinoDoc_LayerTableEvent(object sender, LayerTableEventArgs e)
      {
        if (e.Document == ActiveDocument) Revit.RefreshActiveView();
      }

      static RhinoDoc document;
      public static RhinoDoc ActiveDocument
      {
        get { return document; }
        set
        {
          if (document != value)
          {
            if (document != null) Stop();
            document = value;
            if (value != null) Start();

            ActiveDocumentChanged?.Invoke(null, EventArgs.Empty);
          }
        }
      }

      public static event EventHandler ActiveDocumentChanged;

      public static void Toggle()
      {
        ActiveDocument = ActiveDocument == null ? RhinoDoc.ActiveDoc : null;
      }

      static Dictionary<Guid, PreviewServer> objectPreviews;
      readonly RhinoObject rhinoObject;
      PreviewServer(RhinoObject o) { rhinoObject = o; }

      public override void Register()
      {
        objectPreviews.Add(rhinoObject.Id, this);
        base.Register();
      }

      public override void Unregister()
      {
        base.Unregister();
        objectPreviews.Remove(rhinoObject.Id);

        ClearPrimitives();
      }

      void ClearPrimitives()
      {
        foreach (var buffer in primitives ?? Enumerable.Empty<Primitive>())
          ((IDisposable) buffer).Dispose();

        primitives = null;
      }

      static void Start()
      {
        objectPreviews = new Dictionary<Guid, PreviewServer>();

        using (var service = DBES.ExternalServiceRegistry.GetService(DBES.ExternalServices.BuiltInExternalServices.DirectContext3DService) as DBES.MultiServerService)
        {
          var activeServerIds = service.GetActiveServerIds();
          foreach (var o in ActiveDocument.Objects)
          {
            if (!ObjectPrimitive.IsSupportedObject(o, true))
              continue;

            var preview = new PreviewServer(o);
            var serverId = preview.GetServerId();
            objectPreviews.Add(serverId, preview);
            service.AddServer(preview);
            activeServerIds.Add(serverId);
          }
          service.SetActiveServers(activeServerIds);
        }

        Revit.RefreshActiveView();
      }

      static void Stop()
      {
        using (var service = DBES.ExternalServiceRegistry.GetService(DBES.ExternalServices.BuiltInExternalServices.DirectContext3DService) as DBES.MultiServerService)
        {
          var activeServerIds = service.GetActiveServerIds();
          foreach (var preview in objectPreviews)
            activeServerIds.Remove(preview.Key);
          service.SetActiveServers(activeServerIds);

          foreach (var preview in objectPreviews)
          {
            service.RemoveServer(preview.Key);
            preview.Value.ClearPrimitives();
          }
        }

        objectPreviews = null;

        Revit.RefreshActiveView();
      }

      #region IExternalServer
      public override string GetName() => "Rhino object";
      public override string GetDescription() => "Rhino object preview.";
      public override Guid GetServerId() => rhinoObject.Id;
      #endregion

      #region IDirectContext3DServer
      public override bool UseInTransparentPass(DB.View dBView) => rhinoObject.IsMeshable(MeshType.Render);

      bool collected = false;
      public override bool CanExecute(DB.View dBView)
      {
        if (collected)
          return false;

        try
        {
          if (rhinoObject.Document != ActiveDocument)
            return false;
        }
        catch (Rhino.Runtime.DocumentCollectedException)
        {
          collected = true;
          return false;
        }

        if (!rhinoObject.Visible)
          return false;

        return IsModelView(dBView);
      }

      public override DB.Outline GetBoundingBox(DB.View dBView) => rhinoObject.Geometry.GetBoundingBox(false).ToOutline();

      class ObjectPrimitive : Primitive
      {
        readonly RhinoObject rhinoObject;
        public ObjectPrimitive(RhinoObject o, Point p) : base(p) { rhinoObject = o; }
        public ObjectPrimitive(RhinoObject o, PointCloud pc) : base(pc) { rhinoObject = o; }
        public ObjectPrimitive(RhinoObject o, PointCloud pc, Part p) : base(pc, p) { rhinoObject = o; }
        public ObjectPrimitive(RhinoObject o, Curve c) : base(c) { rhinoObject = o; }
        public ObjectPrimitive(RhinoObject o, Mesh m) : base(m) { rhinoObject = o; }
        public ObjectPrimitive(RhinoObject o, Mesh m, Part p) : base(m, p) { rhinoObject = o; }

        public static bool IsSupportedObject(
          RhinoObject rhinoObject, bool add)
        {
          if (rhinoObject.IsInstanceDefinitionGeometry)
            return false;

          if (add && !rhinoObject.IsValid)
            return false;

          if (rhinoObject is PointObject po)
            return !add || po.PointGeometry.IsValid;

          if (rhinoObject is PointCloudObject pco)
            return !add || pco.PointCloudGeometry.Count > 0;

          if (rhinoObject is CurveObject co)
            return !add || !co.CurveGeometry.IsShort(Revit.ShortCurveTolerance * Revit.ModelUnits);

          if (rhinoObject is MeshObject mo)
            return !add || mo.MeshGeometry.Faces.Count > 0;

          if (rhinoObject is BrepObject bo)
            return !add || bo.BrepGeometry.Faces.Count > 0;

          if (rhinoObject.IsMeshable(MeshType.Render))
            return true;

          return false;
        }

        public override DB3D.EffectInstance EffectInstance(DB.DisplayStyle displayStyle, bool IsShadingPass)
        {
          var ei = base.EffectInstance(displayStyle, IsShadingPass);

          bool hlr = displayStyle == DB.DisplayStyle.HLR;
          bool flatColors = displayStyle == DB.DisplayStyle.FlatColors || displayStyle <= DB.DisplayStyle.HLR;
          bool useMaterials = displayStyle > DB.DisplayStyle.HLR && displayStyle != DB.DisplayStyle.FlatColors;
          bool useTextures = displayStyle > DB.DisplayStyle.Rendering && displayStyle != DB.DisplayStyle.FlatColors;

          var ambient = Color.Black;
          var color = Color.Black;
          var diffuse = Color.Black;
          var emissive = Color.Black;
          var glossiness = 1.0;
          var specular = Color.Black;
          var transparency = 0.0;

          if (IsShadingPass)
          {
            if (DB3D.DrawContext.IsTransparentPass())
            {
              transparency = displayStyle == DB.DisplayStyle.Wireframe ? 0.8 : 0.5;
              var previewColor = Color.Silver;
              if (flatColors) emissive = previewColor;
              else
              {
                diffuse = previewColor;
                ambient = Color.FromArgb(diffuse.R / 2, diffuse.G / 2, diffuse.B / 2);
                if (useTextures)
                {
                  glossiness = 0.5;
                  specular = Color.White;
                }
              }
            }
            else
            {
              var drawColor = rhinoObject.IsLocked ?
              Rhino.ApplicationSettings.AppearanceSettings.LockedObjectColor :
              hlr ?
              Color.White :
              useMaterials ?
              Material.DefaultMaterial.DiffuseColor :
              rhinoObject.Attributes.DrawColor(ActiveDocument);

              if (drawColor == Color.Black)
                drawColor = Color.Gray;

              if (displayStyle >= DB.DisplayStyle.HLR)
              {
                if (flatColors) emissive = drawColor;
                else
                {
                  var material = rhinoObject.GetMaterial(true);
                  ambient = Color.FromArgb(material.DiffuseColor.R / 2, material.DiffuseColor.G / 2, material.DiffuseColor.B / 2);
                  diffuse = material.DiffuseColor;
                  emissive = material.EmissionColor;
                  if (material.Shine != 0.0)
                  {
                    double s = material.Shine / Material.MaxShine;
                    double _s = 1.0 - s;
                    specular = Color.FromArgb
                    (
                      material.SpecularColor.A,
                      (int) (material.SpecularColor.R * s + material.DiffuseColor.R * _s),
                      (int) (material.SpecularColor.G * s + material.DiffuseColor.G * _s),
                      (int) (material.SpecularColor.B * s + material.DiffuseColor.B * _s)
                    );
                    glossiness = s;
                  }

                  transparency = material.Transparency;
                }
              }
            }
          }
          else
          {
            if (part.FaceCount == -1)
            {
              var previewColor = Color.Silver;
              diffuse = previewColor;
              ambient = Color.FromArgb(diffuse.R / 2, diffuse.G / 2, diffuse.B / 2);
              if (useTextures)
              {
                glossiness = 0.5;
                specular = Color.White;
              }
            }
          }

          ei.SetAmbientColor(ambient.ToColor());
          ei.SetColor(color.ToColor());
          ei.SetDiffuseColor(diffuse.ToColor());
          ei.SetEmissiveColor(emissive.ToColor());
          ei.SetGlossiness(glossiness * 100.0);
          ei.SetSpecularColor(specular.ToColor());
          ei.SetTransparency(transparency);

          return ei;
        }
      }

      Primitive[] primitives;

      void AddPointCloudPreviews(PointCloud previewCloud)
      {
        int verticesCount = previewCloud.Count;
        if (verticesCount > VertexThreshold)
        {
          primitives = new Primitive[(verticesCount / VertexThreshold) + ((verticesCount % VertexThreshold) > 0 ? 1 : 0)];
          for (int c = 0; c < verticesCount / VertexThreshold; ++c)
          {
            var part = new Primitive.Part(c * VertexThreshold, (c + 1) * VertexThreshold);
            primitives[c] = new ObjectPrimitive(rhinoObject, previewCloud, part);
          }

          if ((verticesCount % VertexThreshold) > 0)
          {
            var part = new Primitive.Part((primitives.Length - 1) * VertexThreshold, verticesCount);
            primitives[primitives.Length - 1] = new ObjectPrimitive(rhinoObject, previewCloud, part);
          }
        }
        else primitives = new Primitive[] { new ObjectPrimitive(rhinoObject, previewCloud) };
      }

      void AddMeshPreviews(Mesh previewMesh)
      {
        int verticesCount = previewMesh.Vertices.Count;
        if (verticesCount > VertexThreshold || previewMesh.Faces.Count > VertexThreshold)
        {
          // If it's insane big show as point clouds
          if (previewMesh.Faces.Count > (VertexThreshold - 1) * 16)
          {
            primitives = new Primitive[(verticesCount / VertexThreshold) + ((verticesCount % VertexThreshold) > 0 ? 1 : 0)];
            for (int c = 0; c < verticesCount / VertexThreshold; ++c)
            {
              var part = new Primitive.Part(c * VertexThreshold, (c + 1) * VertexThreshold);
              primitives[c] = new ObjectPrimitive(rhinoObject, previewMesh, part);
            }

            if ((verticesCount % VertexThreshold) > 0)
            {
              var part = new Primitive.Part((primitives.Length - 1) * VertexThreshold, verticesCount);
              primitives[primitives.Length - 1] = new ObjectPrimitive(rhinoObject, previewMesh, part);
            }

            // Mesh.Reduce is slow in this case
            //previewMesh = previewMesh.DuplicateMesh();
            //previewMesh.Reduce((BigMeshThreshold - 1) * 16, true, 5, true);
          }

          // Split the mesh into partitions
          else if (previewMesh.CreatePartitions(VertexThreshold, VertexThreshold))
          {
            if (!previewMesh.IsValid)
              previewMesh.Vertices.UseDoublePrecisionVertices = true;

            int partitionCount = previewMesh.PartitionCount;
            primitives = new Primitive[partitionCount];
            for (int p = 0; p < partitionCount; ++p)
              primitives[p] = new ObjectPrimitive(rhinoObject, previewMesh, previewMesh.GetPartition(p));
          }
        }
        else primitives = new Primitive[] { new ObjectPrimitive(rhinoObject, previewMesh) };
      }

      public override void RenderScene(DB.View dBView, Autodesk.Revit.DB.DisplayStyle displayStyle)
      {
        try
        {
          if (primitives == null)
          {
            if (rhinoObject is PointObject pointObject)
            {
              primitives = new Primitive[] { new ObjectPrimitive(pointObject, pointObject.PointGeometry) };
            }
            else if (rhinoObject is PointCloudObject pointCloudObject)
            {
              AddPointCloudPreviews(pointCloudObject.PointCloudGeometry);
            }
            else if (rhinoObject is CurveObject curveObject)
            {
              primitives = new Primitive[] { new ObjectPrimitive(curveObject, curveObject.CurveGeometry) };
            }
            else if (rhinoObject is MeshObject meshObject)
            {
              AddMeshPreviews(meshObject.MeshGeometry);
            }
            else if (rhinoObject.IsMeshable(MeshType.Render))
            {
              var meshingParameters = rhinoObject.GetRenderMeshParameters();
              if (rhinoObject.MeshCount(MeshType.Render, meshingParameters) == 0)
                rhinoObject.CreateMeshes(MeshType.Render, meshingParameters, false);

              var renderMeshes = rhinoObject.GetMeshes(MeshType.Render);
              if (renderMeshes?.Length > 0)
              {
                int vertexCount = renderMeshes.Select((x) => x.Vertices.Count).Sum();

                if (vertexCount > VertexThreshold)
                {
                  foreach (var m in renderMeshes)
                    AddMeshPreviews(m);
                }
                else
                {
                  var previewMesh = renderMeshes.Length == 1 ? renderMeshes[0] : null;
                  if (previewMesh == null)
                  {
                    previewMesh = new Mesh();
                    previewMesh.Append(renderMeshes);
                  }

                  AddMeshPreviews(previewMesh);
                }
              }
            }
          }

          if (primitives != null)
          {
            DB3D.DrawContext.SetWorldTransform(Autodesk.Revit.DB.Transform.Identity.ScaleBasis(1.0 / Revit.ModelUnits));

            foreach (var primitive in primitives)
            {
              if (DB3D.DrawContext.IsInterrupted())
                return;

              primitive.Draw(displayStyle);
            }
          }
        }
        catch (Exception e)
        {
          Debug.Fail(e.Source, e.Message);
        }
      }
      #endregion
    }
  }
}
#endif

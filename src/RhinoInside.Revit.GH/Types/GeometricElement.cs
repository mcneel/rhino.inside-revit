using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Display;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Drawing;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Geometric Element")]
  public interface IGH_GeometricElement : IGH_GraphicalElement { }

  [Kernel.Attributes.Name("Geometric Element")]
  public class GeometricElement : GraphicalElement, IGH_GeometricElement, IGH_PreviewMeshData, Bake.IGH_BakeAwareElement
  {
    public override string DisplayName
    {
      get
      {
        if (Value is DB.Element element)
        {
          if (element.get_Parameter(DB.BuiltInParameter.ALL_MODEL_MARK) is DB.Parameter parameter && parameter.HasValue)
          {
            var mark = parameter.AsString();
            if (!string.IsNullOrEmpty(mark))
              return $"{base.DisplayName} [{mark}]";
          }
        }

        return base.DisplayName;
      }
    }

    public GeometricElement() { }
    public GeometricElement(DB.Element element) : base(element) { }

    public static new bool IsValidElement(DB.Element element)
    {
      if (!GraphicalElement.IsValidElement(element))
        return false;

      using (var options = new DB.Options())
        return !(element.get_Geometry(options) is null);
    }

    protected override void SubInvalidateGraphics()
    {
      GeometryPreview = null;

      base.SubInvalidateGraphics();
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is DB.Element element)
      {
        if (!xform.IsIdentity)
        {
          var meshes = TryGetPreviewMeshes();
          var wires = TryGetPreviewWires();
          if (meshes is null && wires is null)
            BuildPreview(element, default, DB.ViewDetailLevel.Medium, out var _, out meshes, out wires);

          if (meshes?.Length > 0 || wires?.Length > 0)
          {
            var bbox = BoundingBox.Empty;

            foreach (var mesh in meshes)
              bbox.Union(mesh.GetBoundingBox(xform));

            foreach (var wire in wires)
              bbox.Union(wire.GetBoundingBox(xform));

            return bbox;
          }
        }
      }

      return base.GetBoundingBox(xform);
    }

    #region Preview
    internal static void BuildPreview
    (
      DB.Element element, MeshingParameters meshingParameters, DB.ViewDetailLevel detailLevel,
      out DB.Material[] materials, out Mesh[] meshes, out Curve[] wires
    )
    {
      using (var options = new DB.Options() { DetailLevel = detailLevel == DB.ViewDetailLevel.Undefined ? DB.ViewDetailLevel.Medium : detailLevel })
      using (var geometry = element?.GetGeometry(options))
      {
        if (geometry is null)
        {
          materials = null;
          meshes = null;
          wires = null;
        }
        else
        {
          var categoryMaterial = element.Category?.Material;
          var elementMaterial = geometry.MaterialElement ?? categoryMaterial;

          wires = geometry.GetPreviewWires().Where(x => x is object).ToArray();
          meshes = geometry.GetPreviewMeshes(element.Document, meshingParameters).ToArray();
          materials = geometry.GetPreviewMaterials(element.Document, elementMaterial).ToArray();

          if (wires.Length == 0 && meshes.Length == 0 && element.get_BoundingBox(null) is DB.BoundingBoxXYZ)
          {
            var subMeshes = new List<Mesh>();
            var subWires = new List<Curve>();
            var subMaterials = new List<DB.Material>();

            foreach (var dependent in element.GetDependentElements(null).Select(x => element.Document.GetElement(x)))
            {
              if (dependent.get_BoundingBox(null) is null)
                continue;

              using (var dependentOptions = new DB.Options() { DetailLevel = detailLevel == DB.ViewDetailLevel.Undefined ? DB.ViewDetailLevel.Medium : detailLevel })
              using (var dependentGeometry = dependent?.GetGeometry(dependentOptions))
              {
                if (dependentGeometry is object)
                {
                  subWires.AddRange(dependentGeometry.GetPreviewWires().Where(x => x is object));
                  subMeshes.AddRange(dependentGeometry.GetPreviewMeshes(element.Document, meshingParameters));
                  subMaterials.AddRange(dependentGeometry.GetPreviewMaterials(element.Document, elementMaterial));
                }
              }
            }

            meshes = subMeshes.ToArray();
            wires = subWires.ToArray();
            materials = subMaterials.ToArray();
          }

          foreach (var mesh in meshes)
            mesh.Normals.ComputeNormals();
        }
      }
    }

    class Preview : IDisposable
    {
      readonly GeometricElement geometricElement;
      readonly BoundingBox clippingBox;
      public readonly MeshingParameters MeshingParameters;
      public Rhino.Display.DisplayMaterial[] materials;
      public Mesh[] meshes;
      public Curve[] wires;

      static List<Preview> previewsQueue;

      void Build()
      {
        if (meshes is null && wires is null && materials is null)
        {
          var element = geometricElement.Document.GetElement(geometricElement.Id);
          if (element is null)
            return;

          BuildPreview(element, MeshingParameters, DB.ViewDetailLevel.Undefined, out var materialElements, out meshes, out wires);

          // Combine meshes of same material for display performance
          if (meshes is object && materialElements is object)
          {
            var outMesh = new Mesh();
            var dictionary = PreviewConverter.ZipByMaterial(materialElements, meshes, outMesh);
            if (outMesh.Faces.Count > 0)
            {
              materials = dictionary.Keys.Select(x => x.ToDisplayMaterial(null)).Concat(Enumerable.Repeat(new Rhino.Display.DisplayMaterial(), 1)).ToArray();
              meshes = dictionary.Values.Concat(Enumerable.Repeat(outMesh, 1)).ToArray();
            }
            else
            {
              materials = dictionary.Keys.Select(x => x.ToDisplayMaterial(null)).ToArray();
              meshes = dictionary.Values.ToArray();
            }
          }
        }
      }

      static void BuildPreviews(DB.Document _, bool cancelled)
      {
        var previews = previewsQueue;
        previewsQueue = null;

        if (cancelled)
          return;

        // Sort in reverse order depending on how 'big' is the element on screen.
        // The bigger the more at the end on the list.
        previews.Sort((x, y) => (x.clippingBox.Diagonal.Length < y.clippingBox.Diagonal.Length) ? -1 : +1);
        BuildPreviews(cancelled, previews);
      }

      static void BuildPreviews(bool cancelled, List<Preview> previews)
      {
        if (cancelled)
          return;

        var stopWatch = new Stopwatch();

        int count = 0;
        while ((count = previews.Count) > 0)
        {
          // Draw the biggest elements first.
          // The biggest element is at the end of previews List, this way no realloc occurs when removing it

          int last = count - 1;
          var preview = previews[last];
          previews.RemoveAt(last);

          stopWatch.Start();
          preview.Build();
          stopWatch.Stop();

          // If building those previews take use more than 200 ms we return to Revit, to keep it 'interactive'.
          if (stopWatch.ElapsedMilliseconds > 200)
            break;
        }

        // RhinoDoc.ActiveDoc.Views.Redraw is synchronous :(
        // better use RhinoView.Redraw that just invalidate the view, the OS will update it when possible
        foreach (var view in Rhino.RhinoDoc.ActiveDoc.Views)
          view.Redraw();

        // If there are pending previews to generate enqueue BuildPreviews again
        if (previews.Count > 0)
          Revit.EnqueueReadAction((_, cancel) => BuildPreviews(cancel, previews));
      }

      Preview(GeometricElement element)
      {
        geometricElement = element;
        clippingBox = element.ClippingBox;
        MeshingParameters = element.meshingParameters;
      }

      public static Preview OrderNew(GeometricElement element)
      {
        if (!element.IsValid)
          return null;

        if (previewsQueue is null)
        {
          previewsQueue = new List<Preview>();
          Revit.EnqueueReadAction((doc, cancel) => BuildPreviews(doc, cancel));
        }

        var preview = new Preview(element);
        previewsQueue.Add(preview);
        return preview;
      }

      void IDisposable.Dispose()
      {
        if (meshes is object)
        {
          foreach (var mesh in meshes)
            mesh.Dispose();

          meshes = null;
        }

        if (wires is object)
        {
          foreach (var wire in wires)
            wire.Dispose();

          wires = null;
        }
      }
    }

    MeshingParameters meshingParameters;
    Preview geometryPreview;
    Preview GeometryPreview
    {
      get { return geometryPreview ?? (geometryPreview = Preview.OrderNew(this)); }
      set { if (geometryPreview != value) geometryPreview = value; }
    }

    public Rhino.Display.DisplayMaterial[] TryGetPreviewMaterials()
    {
      return GeometryPreview.materials;
    }

    public Mesh[] TryGetPreviewMeshes(MeshingParameters parameters)
    {
      if (!ReferenceEquals(meshingParameters, parameters))
      {
        meshingParameters = parameters;
        if (geometryPreview is object)
        {
          if (geometryPreview.MeshingParameters?.RelativeTolerance != meshingParameters?.RelativeTolerance)
            GeometryPreview = null;
        }
      }

      return GeometryPreview.meshes;
    }

    public Mesh[] TryGetPreviewMeshes() => GeometryPreview.meshes;

    public Curve[] TryGetPreviewWires() => GeometryPreview.wires;
    #endregion

    #region IGH_PreviewData
    public override void DrawViewportMeshes(GH_PreviewMeshArgs args)
    {
      if (!IsValid)
        return;

      var meshes = TryGetPreviewMeshes(args.MeshingParameters);
      if (meshes is null)
        return;

      var material = args.Material;
      var element = Value;
      if (element is null)
      {
        const int factor = 3;

        // Erased element
        material = new Rhino.Display.DisplayMaterial(material)
        {
          Diffuse = System.Drawing.Color.FromArgb(20, 20, 20),
          Emission = System.Drawing.Color.FromArgb(material.Emission.R / factor, material.Emission.G / factor, material.Emission.B / factor),
          Shine = 0.0,
        };
      }
      else if (!element.Pinned)
      {
        if (args.Pipeline.DisplayPipelineAttributes.ShadingEnabled)
        {
          // Unpinned element
          if (args.Pipeline.DisplayPipelineAttributes.UseAssignedObjectMaterial)
          {
            var materials = TryGetPreviewMaterials();

            for (int m = 0; m < meshes.Length; ++m)
              args.Pipeline.DrawMeshShaded(meshes[m], materials[m]);

            return;
          }
          else
          {
            material = new Rhino.Display.DisplayMaterial(material)
            {
              Diffuse = element.Category?.LineColor.ToColor() ?? System.Drawing.Color.White,
              Transparency = 0.0
            };

            if (material.Diffuse == System.Drawing.Color.Black)
              material.Diffuse = System.Drawing.Color.White;
          }
        }
      }

      foreach (var mesh in meshes)
        args.Pipeline.DrawMeshShaded(mesh, material);
    }

    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (!IsValid)
        return;

      if (!args.Pipeline.DisplayPipelineAttributes.ShowSurfaceEdges)
        return;

      int thickness = 1; //args.Thickness;
      const int factor = 3;

      var color = args.Color;
      var element = Value;
      if (element is null)
      {
        // Erased element
        color = System.Drawing.Color.FromArgb(args.Color.R / factor, args.Color.G / factor, args.Color.B / factor);
      }
      else if (!element.Pinned)
      {
        // Unpinned element
        if (args.Thickness <= 1 && args.Pipeline.DisplayPipelineAttributes.UseAssignedObjectMaterial)
          color = System.Drawing.Color.Black;
      }

      var wires = TryGetPreviewWires();
      if (wires is object && wires.Length > 0)
      {
        foreach (var wire in wires)
          args.Pipeline.DrawCurve(wire, color, thickness);
      }
      else
      {
        var meshes = TryGetPreviewMeshes();
        if (meshes is object)
        {
          if (Grasshopper.CentralSettings.PreviewMeshEdges)
          {
            foreach (var mesh in meshes)
              args.Pipeline.DrawMeshWires(mesh, color, thickness);
          }
        }
        else
        {
          foreach (var edge in ClippingBox.GetEdges() ?? Enumerable.Empty<Line>())
            args.Pipeline.DrawPatternedLine(edge.From, edge.To, System.Drawing.Color.Black /*color*/, 0x00001111, thickness);
        }
      }
    }
    #endregion

    #region IGH_PreviewMeshData
    void IGH_PreviewMeshData.DestroyPreviewMeshes() => SubInvalidateGraphics();

    Mesh[] IGH_PreviewMeshData.GetPreviewMeshes() => TryGetPreviewMeshes();
    #endregion

    #region IGH_BakeAwareElement
    static ObjectAttributes PeekAttributes(IDictionary<DB.ElementId, Guid> idMap, RhinoDoc doc, ObjectAttributes att, DB.Document document)
    {
      var context = GeometryDecoder.Context.Peek;
      var attributes = new ObjectAttributes();

      if (context.GraphicsStyleId.IsValid() && document.GetElement(context.GraphicsStyleId) is DB.GraphicsStyle graphicsStyle)
      {
        if (new Category(graphicsStyle.GraphicsStyleCategory).BakeElement(idMap, false, doc, att, out var layerGuid))
          attributes.LayerIndex = doc.Layers.FindId(layerGuid).Index;
      }

      if (context.MaterialId.IsValid() && document.GetElement(context.MaterialId) is DB.Material material)
      {
        var mat = new Material(material);
        if (mat.BakeElement(idMap, false, doc, att, out var materialGuid))
        {
          attributes.MaterialSource = ObjectMaterialSource.MaterialFromObject;
          attributes.MaterialIndex = doc.Materials.FindId(materialGuid).Index;
        }
      }

      return attributes;
    }

    protected internal static bool BakeGeometryElement
    (
      IDictionary<DB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      Transform transform,
      DB.Element element,
      DB.GeometryElement geometryElement,
      out int index
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(element.Id, out var guid))
      {
        index = doc.InstanceDefinitions.FindId(guid).Index;
        return true;
      }

      var geometryElementContent = geometryElement.ToArray();
      if (geometryElementContent.Length < 1)
      {
        index = -1;
        return false;
      }
      else if
      (
        geometryElementContent.Length == 1 &&
        geometryElementContent[0] is DB.GeometryInstance geometryInstance &&
        geometryInstance.Symbol is DB.ElementType
      )
      {
        // Special case to simplify DB.FamilyInstance elements.
        var instanceTransform = geometryInstance.Transform.ToTransform();
        return BakeGeometryElement(idMap, false, doc, att, instanceTransform * transform, geometryInstance.Symbol, geometryInstance.SymbolGeometry, out index);
      }

      var idef_name = FullUniqueId.Format(element.Document.GetFingerprintGUID(), element.UniqueId);
      var idef_description = string.Empty;

      // Decorate idef_name using "Category:FamilyName:TypeName" when posible
      if (element is DB.ElementType type)
      {
        idef_name = $"Revit:{type.Category?.FullName()}:{type.FamilyName}:{type.Name} {{{idef_name}}}";
        idef_description = element.get_Parameter(DB.BuiltInParameter.ALL_MODEL_DESCRIPTION)?.AsString() ?? string.Empty;
      }
      else if (element.Document.GetElement(element.GetTypeId()) is DB.ElementType elementType)
      {
        idef_name = $"Revit:{elementType.Category?.FullName()}:{elementType.FamilyName}:{elementType.Name} {{{idef_name}}}";
      }
      else
      {
        idef_name = $"*Revit:{element.Category?.FullName()}:: {{{idef_name}}}";
      }

      // 2. Check if already exist
      index = doc.InstanceDefinitions.Find(idef_name)?.Index ?? -1;

      // 3. Update if necessary
      if (index < 0 || overwrite)
      {
        bool identity = transform.IsIdentity;

        GeometryDecoder.UpdateGraphicAttributes(geometryElement);

        var geometry = new List<GeometryBase>(geometryElementContent.Length);
        var attributes = new List<ObjectAttributes>(geometryElementContent.Length);

        foreach (var g in geometryElementContent)
        {
          using (GeometryDecoder.Context.Push())
          {
            GeometryDecoder.UpdateGraphicAttributes(g);

            var geo = default(GeometryBase);
            switch (g)
            {
              case DB.Mesh mesh: if(mesh.NumTriangles > 0) geo = mesh.ToMesh(); break;
              case DB.Solid solid: if(!solid.Faces.IsEmpty) geo = solid.ToBrep(); break;
              case DB.Curve curve: geo = curve.ToCurve(); break;
              case DB.PolyLine pline: if (pline.NumberOfCoordinates > 0) geo = pline.ToPolylineCurve(); break;
              case DB.GeometryInstance instance:
                using (GeometryDecoder.Context.Push())
                {
                  if (BakeGeometryElement(idMap, false, doc, att, Transform.Identity, instance.Symbol, instance.SymbolGeometry, out var idefIndex))
                    geo = new InstanceReferenceGeometry(doc.InstanceDefinitions[idefIndex].Id, instance.Transform.ToTransform());
                }
                break;
            }

            if (!(geo is null))
            {
              if (!identity)
                geo.Transform(transform);

              geometry.Add(geo);
              var geoAtt = PeekAttributes(idMap, doc, att, element.Document);

              // In case geo is a Brep and has different materials per face.
              var context = GeometryDecoder.Context.Peek;
              if (context.FaceMaterialId?.Length > 0)
              {
                bool hasPerFaceMaterials = false;
                {
                  for (int f = 1; f < context.FaceMaterialId.Length && !hasPerFaceMaterials; ++f)
                    hasPerFaceMaterials |= context.FaceMaterialId[f] != context.FaceMaterialId[f - 1];
                }

                if (hasPerFaceMaterials && geo is Brep brep)
                {
                  // Solve baseMaterial
                  var baseMaterial = Rhino.DocObjects.Material.DefaultMaterial;
                  if (geoAtt.MaterialSource == ObjectMaterialSource.MaterialFromLayer)
                    baseMaterial = doc.Materials[doc.Layers[geoAtt.LayerIndex].RenderMaterialIndex];
                  else if (geoAtt.MaterialSource == ObjectMaterialSource.MaterialFromObject)
                    baseMaterial = doc.Materials[geoAtt.MaterialIndex];

                  // Create a new material for this brep
                  var brepMaterial = new Rhino.DocObjects.Material(baseMaterial);

                  foreach (var face in brep.Faces)
                  {
                    var faceMaterialId = context.FaceMaterialId[face.SurfaceIndex];
                    if (faceMaterialId != context.MaterialId)
                    {
                      var faceMaterial = new Material(element.Document, faceMaterialId);
                      if (faceMaterial.BakeElement(idMap, false, doc, att, out var materialGuid))
                      {
                        face.MaterialChannelIndex = brepMaterial.MaterialChannelIndexFromId(materialGuid, true);
                        face.PerFaceColor = faceMaterial.ObjectColor;
                      }
                    }
                    else
                    {
                      face.ClearMaterialChannelIndex();
                      face.PerFaceColor = System.Drawing.Color.Empty;
                    }
                  }

                  geoAtt.MaterialIndex = doc.Materials.Add(brepMaterial);
                  geoAtt.MaterialSource = ObjectMaterialSource.MaterialFromObject;
                }
                else if (context.FaceMaterialId[0].IsValid())
                {
                  var faceMaterial = new Material(element.Document, context.FaceMaterialId[0]);

                  if (faceMaterial.BakeElement(idMap, false, doc, att, out var materialGuid))
                  {
                    geoAtt.MaterialIndex = doc.Materials.FindId(materialGuid).Index;
                    geoAtt.MaterialSource = ObjectMaterialSource.MaterialFromObject;

                    if (geo is Brep b)
                    {
                      foreach (var face in b.Faces)
                        face.PerFaceColor = faceMaterial.ObjectColor;
                    }
                    else if (geo is Mesh m)
                    {
                      m.VertexColors.SetColors(Enumerable.Repeat(faceMaterial.ObjectColor, m.Vertices.Count).ToArray());
                    }
                  }
                }
              }

              attributes.Add(geoAtt);
            }
          }
        }

        if (index < 0) index = doc.InstanceDefinitions.Add(idef_name, idef_description, Point3d.Origin, geometry, attributes);
        else if (!doc.InstanceDefinitions.ModifyGeometry(index, geometry, attributes)) index = -1;
      }

      return index >= 0;
    }

    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<DB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<DB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      // 3. Update if necessary
      if (Value is DB.Element element)
      {
        using (var options = new DB.Options() { DetailLevel = DB.ViewDetailLevel.Fine })
        {
          using (var geometry = element.GetGeometry(options))
          {
            if (geometry is object)
            {
              using (var context = GeometryDecoder.Context.Push())
              {
                context.Element = element;
                context.GraphicsStyleId = element.Category?.GetGraphicsStyle(DB.GraphicsStyleType.Projection)?.Id ?? DB.ElementId.InvalidElementId;
                context.MaterialId = element.Category?.Material?.Id ?? DB.ElementId.InvalidElementId;

                var location = element.Category is null || element.Category.Parent is object ?
                  Plane.WorldXY :
                  Location;

                var worldToElement = Transform.PlaneToPlane(location, Plane.WorldXY);
                if (BakeGeometryElement(idMap, overwrite, doc, att, worldToElement, element, geometry, out var idefIndex))
                {
                  att = att.Duplicate();
                  att.Name = element.get_Parameter(DB.BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;
                  att.Url = element.get_Parameter(DB.BuiltInParameter.ALL_MODEL_URL)?.AsString() ?? string.Empty;

                  if (Category.BakeElement(idMap, false, doc, att, out var layerGuid))
                    att.LayerIndex = doc.Layers.FindId(layerGuid).Index;

                  guid = doc.Objects.AddInstanceObject(idefIndex, Transform.PlaneToPlane(Plane.WorldXY, location), att);
                }
              }

              if (guid != Guid.Empty)
              {
                idMap.Add(Id, guid);
                return true;
              }
            }
          }
        }
      }

      return false;
    }
    #endregion
  }
}

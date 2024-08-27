using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
#if RHINO_8
using Grasshopper.Rhinoceros;
using Grasshopper.Rhinoceros.Model;
using Grasshopper.Rhinoceros.Display;
using Grasshopper.Rhinoceros.Render;
#endif

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Display;
  using Convert.DocObjects;
  using Convert.Geometry;
  using Convert.System.Drawing;
  using External.DB;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Geometric Element")]
  public interface IGH_GeometricElement : IGH_GraphicalElement { }

  [Kernel.Attributes.Name("Geometric Element")]
  public class GeometricElement : GraphicalElement,
    IGH_GeometricElement,
    IHostElementAccess,
    IGH_PreviewMeshData,
    Bake.IGH_BakeAwareElement
  {
    public GeometricElement() { }
    public GeometricElement(ARDB.Element element) : base(element) { }

    public static new bool IsValidElement(ARDB.Element element)
    {
      if (element.Category is null)
        return false;

      if (!GraphicalElement.IsValidElement(element))
        return false;

      return element.HasGeometry();
    }

    protected override void SubInvalidateGraphics()
    {
      using (_GeometryPreview) _GeometryPreview = null;

      base.SubInvalidateGraphics();
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is ARDB.Element element)
      {
        if (!xform.IsIdentity)
        {
          var meshes = TryGetPreviewMeshes();
          var wires = TryGetPreviewWires();
          if (meshes is null && wires is null)
            BuildPreview(element, default, ARDB.ViewDetailLevel.Medium, out var _, out meshes, out wires);

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
      ARDB.Element element, MeshingParameters meshingParameters, ARDB.ViewDetailLevel detailLevel,
      out ARDB.Material[] materials, out Mesh[] meshes, out Curve[] wires
    )
    {
      bool voidGeometry = element is ARDB.GenericForm form && !form.IsSolid;

      using
      (
        var options = element.ViewSpecific ?
        new ARDB.Options() { View = element.Document.GetElement(element.OwnerViewId) as ARDB.View, IncludeNonVisibleObjects = voidGeometry } :
        new ARDB.Options() { DetailLevel = detailLevel == ARDB.ViewDetailLevel.Undefined ? ARDB.ViewDetailLevel.Medium : detailLevel, IncludeNonVisibleObjects = voidGeometry }
      )
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
          meshes = geometry.Visibility == ARDB.Visibility.Visible ?
                   geometry.GetPreviewMeshes(element.Document, meshingParameters).ToArray() :
                   Array.Empty<Mesh>();
          materials = geometry.Visibility == ARDB.Visibility.Visible ?
                      geometry.GetPreviewMaterials(element.Document, elementMaterial).ToArray() :
                      Array.Empty<ARDB.Material>();

          if (wires.Length == 0 && meshes.Length == 0 && element.get_BoundingBox(options.View) is ARDB.BoundingBoxXYZ)
          {
            var subMeshes = new List<Mesh>();
            var subWires = new List<Curve>();
            var subMaterials = new List<ARDB.Material>();

            foreach (var dependent in element.GetDependentElements(CompoundElementFilter.ElementHasBoundingBoxFilter).Select(element.Document.GetElement))
            {
              if (dependent.GetBoundingBoxXYZ(out var view) is null)
                continue;

              using
              (
                var dependentOptions = view is object ?
                new ARDB.Options() { View = view, IncludeNonVisibleObjects = voidGeometry } :
                new ARDB.Options() { DetailLevel = detailLevel == ARDB.ViewDetailLevel.Undefined ? ARDB.ViewDetailLevel.Medium : detailLevel, IncludeNonVisibleObjects = voidGeometry }
              )
              using (var dependentGeometry = dependent?.GetGeometry(dependentOptions))
              {
                if (dependentGeometry is object)
                {
                  subWires.AddRange(dependentGeometry.GetPreviewWires().Where(x => x is object));
                  if (!voidGeometry)
                  {
                    subMeshes.AddRange(dependentGeometry.GetPreviewMeshes(element.Document, meshingParameters));
                    subMaterials.AddRange(dependentGeometry.GetPreviewMaterials(element.Document, elementMaterial));
                  }
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
      static readonly Rhino.Display.DisplayMaterial[] empty_materials = Array.Empty<Rhino.Display.DisplayMaterial>();
      public Mesh[] meshes;
      static readonly Mesh[] empty_meshes = Array.Empty<Mesh>();
      public Curve[] wires;
      static readonly Curve[] empty_wires = Array.Empty<Curve>();

      static List<Preview> previewsQueue;

      void Build()
      {
        if (!geometricElement.IsValid || !clippingBox.IsValid)
        {
          materials = empty_materials;
          meshes = empty_meshes;
          wires = empty_wires;
        }
        else if (meshes is null && wires is null && materials is null)
        {
          var element = geometricElement.Document.GetElement(geometricElement.Id);
          if (element is null)
            return;

          BuildPreview(element, MeshingParameters, ARDB.ViewDetailLevel.Undefined, out var materialElements, out meshes, out wires);

          // Combine meshes of same material for display performance
          if (meshes is object && materialElements is object)
          {
            var outMesh = new Mesh();
            var dictionary = PreviewConverter.ZipByMaterial(materialElements, meshes, outMesh);
            if (outMesh.Faces.Count > 0)
            {
              materials = dictionary.Keys.Select(DisplayMaterialConverter.ToDisplayMaterial).Concat(Enumerable.Repeat(new Rhino.Display.DisplayMaterial(), 1)).ToArray();
              meshes = dictionary.Values.Concat(Enumerable.Repeat(outMesh, 1)).ToArray();
            }
            else
            {
              materials = dictionary.Keys.Select(DisplayMaterialConverter.ToDisplayMaterial).ToArray();
              meshes = dictionary.Values.ToArray();
            }
          }
        }
      }

      static void BuildPreviews(ARDB.Document _, bool cancelled)
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
        foreach (var view in RhinoDoc.ActiveDoc.Views)
          view.Redraw();

        // If there are pending previews to generate enqueue BuildPreviews again
        if (previews.Count > 0)
          Revit.EnqueueReadAction((_, cancel) => BuildPreviews(cancel, previews));
        else
          RhinoDoc.ActiveDoc.Views.Redraw();
      }

      Preview(GeometricElement element)
      {
        geometricElement = element;
        clippingBox = element.ClippingBox;
        MeshingParameters = element._MeshingParameters;
      }

      public static Preview OrderNew(GeometricElement element)
      {
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

    MeshingParameters _MeshingParameters;
    Preview _GeometryPreview;
    Preview GeometryPreview
    {
      get { return _GeometryPreview ?? (_GeometryPreview = Preview.OrderNew(this)); }
      set { if (_GeometryPreview != value) _GeometryPreview = value; }
    }

    public Rhino.Display.DisplayMaterial[] TryGetPreviewMaterials()
    {
      return GeometryPreview.materials;
    }

    public Mesh[] TryGetPreviewMeshes(MeshingParameters parameters)
    {
      if (!ReferenceEquals(_MeshingParameters, parameters))
      {
        _MeshingParameters = parameters;
        if (_GeometryPreview is object)
        {
          if (_GeometryPreview.MeshingParameters?.RelativeTolerance != _MeshingParameters?.RelativeTolerance)
            GeometryPreview = null;
        }
      }

      return GeometryPreview.meshes;
    }

    public Mesh[] TryGetPreviewMeshes() => GeometryPreview.meshes;

    public Curve[] TryGetPreviewWires() => GeometryPreview.wires;
    #endregion

    #region IGH_PreviewData
    protected override void DrawViewportMeshes(GH_PreviewMeshArgs args)
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

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (!IsValid)
        return;

      if (!args.Pipeline.DisplayPipelineAttributes.ShowSurfaceEdges && args.Thickness <= 1)
        return;

      int thickness = 1; //args.Thickness;

      var color = args.Color;
      var element = Value;
      if (element is null)
      {
        // Erased element
        const int factor = 3;
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
          if (meshes.Length == 0)
          {
            base.DrawViewportWires(args);
          }
          else if (Grasshopper.CentralSettings.PreviewMeshEdges)
          {
            foreach (var mesh in meshes)
              args.Pipeline.DrawMeshWires(mesh, color, thickness);
          }
        }
        else base.DrawViewportWires(args);
      }
    }
    #endregion

    #region IGH_PreviewMeshData
    void IGH_PreviewMeshData.DestroyPreviewMeshes() => SubInvalidateGraphics();

    Mesh[] IGH_PreviewMeshData.GetPreviewMeshes() => TryGetPreviewMeshes();
    #endregion

    #region IGH_BakeAwareElement
    static ObjectAttributes PeekAttributes(IDictionary<ARDB.ElementId, Guid> idMap, RhinoDoc doc, ObjectAttributes att, ARDB.Document document)
    {
      var context = GeometryDecoder.Context.Peek;
      var attributes = new ObjectAttributes();

      if (context.Category is ARDB.Category category)
      {
        if (new Category(category).BakeElement(idMap, false, doc, att, out var layerGuid))
          attributes.LayerIndex = doc.Layers.FindId(layerGuid).Index;
      }

      if (context.Material is ARDB.Material material)
      {
        if (new Material(material).BakeElement(idMap, false, doc, att, out var materialGuid))
        {
          attributes.MaterialSource = ObjectMaterialSource.MaterialFromObject;
          attributes.MaterialIndex = doc.Materials.FindId(materialGuid).Index;
        }
      }

      return attributes;
    }

    static System.Drawing.Color NoBlack(System.Drawing.Color value)
    {
      return (value.R == 0 && value.G == 0 && value.B == 0) ? System.Drawing.Color.FromArgb(value.A, 1, 1, 1) : value;
    }

    protected internal static bool BakeGeometryElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      Transform transform,
      ARDB.Element element,
      ARDB.GeometryElement geometryElement,
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
        geometryElementContent[0] is ARDB.GeometryInstance geometryInstance &&
        geometryInstance.GetSymbol() is ARDB.ElementType symbol
      )
      {
        // Special case to simplify ARDB.FamilyInstance elements.
        var instanceTransform = geometryInstance.Transform.ToTransform();
        return BakeGeometryElement(idMap, false, doc, att, instanceTransform * transform, symbol, geometryInstance.SymbolGeometry, out index);
      }

      // Get a Unique Instance Definition name.
      var idef_name = NameConverter.EscapeName(element, out var idef_description);

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
              case ARDB.Mesh mesh: if(mesh.NumTriangles > 0) geo = mesh.ToMesh(); break;
              case ARDB.Solid solid: if(!solid.Faces.IsEmpty) geo = solid.ToBrep(); break;
              case ARDB.Curve curve: geo = curve.ToCurve(); break;
              case ARDB.PolyLine pline: if (pline.NumberOfCoordinates > 0) geo = pline.ToPolylineCurve(); break;
              case ARDB.GeometryInstance instance:
                using (GeometryDecoder.Context.Push())
                {
                  if (BakeGeometryElement(idMap, false, doc, att, Transform.Identity, instance.GetSymbol(), instance.SymbolGeometry, out var idefIndex))
                    geo = new InstanceReferenceGeometry(doc.InstanceDefinitions[idefIndex].Id, instance.Transform.ToTransform());
                }
                break;
            }

            if (geo is null) continue;
            if (!identity) geo.Transform(transform);

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
                {
                  baseMaterial = doc.Materials[doc.Layers[geoAtt.LayerIndex].RenderMaterialIndex];
                  var objectColor = new Material(context.Category.Material).ObjectColor;
                  geoAtt.ColorSource = ObjectColorSource.ColorFromObject;
                  geoAtt.ObjectColor = NoBlack(baseMaterial.DiffuseColor);
                }
                else if (geoAtt.MaterialSource == ObjectMaterialSource.MaterialFromObject)
                {
                  baseMaterial = doc.Materials[geoAtt.MaterialIndex];
                  var objectColor = new Material(context.Material).ObjectColor;
#if RHINO_8
                  geoAtt.ColorSource = ObjectColorSource.ColorFromMaterial;
#else
                  geoAtt.ColorSource = ObjectColorSource.ColorFromObject;
                  geoAtt.ObjectColor = NoBlack(baseMaterial.DiffuseColor);
#endif
                }

                // Create a new material for this brep
                var brepMaterial = new Rhino.DocObjects.Material(baseMaterial);

                foreach (var face in brep.Faces)
                {
                  var faceMaterialId = context.FaceMaterialId[face.SurfaceIndex];
                  if (faceMaterialId != (context.Material?.Id ?? ARDB.ElementId.InvalidElementId))
                  {
                    var faceMaterial = new Material(element.Document, faceMaterialId);
                    if (faceMaterial.BakeElement(idMap, false, doc, att, out var materialGuid))
                    {
                      face.MaterialChannelIndex = brepMaterial.MaterialChannelIndexFromId(materialGuid, true);
                      face.PerFaceColor = NoBlack(faceMaterial.ObjectColor);
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
              else
              {
                if (context.FaceMaterialId[0].IsValid())
                {
                  var faceMaterial = new Material(element.Document, context.FaceMaterialId[0]);
                  if (faceMaterial.BakeElement(idMap, false, doc, att, out var materialGuid))
                  {
                    geoAtt.MaterialIndex = doc.Materials.FindId(materialGuid).Index;
                    geoAtt.MaterialSource = ObjectMaterialSource.MaterialFromObject;
#if RHINO_8
                    geoAtt.ColorSource = ObjectColorSource.ColorFromMaterial;
#else
                    geoAtt.ColorSource = ObjectColorSource.ColorFromObject;
                    geoAtt.ObjectColor = NoBlack(faceMaterial.ObjectColor);
#endif
                    if ((geo as Brep)?.TryGetExtrusion(out var extrusion) is true) geo = extrusion;
                  }
                }
                else
                {
                  if (geo is Brep brepFrom && brepFrom.TryGetExtrusion(out var extrusion))
                    geo = extrusion;
                }
              }
            }

            geometry.Add(geo);
            attributes.Add(geoAtt);
          }
        }

        if (index < 0) index = doc.InstanceDefinitions.Add(idef_name, idef_description, Point3d.Origin, geometry, attributes);
        else if (!doc.InstanceDefinitions.ModifyGeometry(index, geometry, attributes)) index = -1;
      }

      return index >= 0;
    }

    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    public virtual bool BakeElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
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
      if (Value is ARDB.Element element)
      {
        using (var options = new ARDB.Options() { DetailLevel = ARDB.ViewDetailLevel.Fine })
        {
          using (var geometry = element.GetGeometry(options))
          {
            if (geometry is object)
            {
              using (var context = GeometryDecoder.Context.Push())
              {
                context.Element = element;
                context.Category = element.Category;
                context.Material = element.Category?.Material;

                var location = element.Category is null || element.Category.Parent is object ?
                  Plane.WorldXY :
                  Location;

                var worldToElement = Transform.PlaneToPlane(location, Plane.WorldXY);
                if (BakeGeometryElement(idMap, overwrite, doc, att, worldToElement, element, geometry, out var idefIndex))
                {
                  att = att?.Duplicate() ?? doc.CreateDefaultAttributes();
                  att.Space = ActiveSpace.ModelSpace;
                  att.ViewportId = Guid.Empty;
                  att.Name = element.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;
                  att.Url = element.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_URL)?.AsString() ?? string.Empty;

                  if (Category.BakeElement(idMap, false, doc, att, out var layerGuid))
                    att.LayerIndex = doc.Layers.FindId(layerGuid).Index;

                  guid = doc.Objects.AddInstanceObject(idefIndex, Transform.PlaneToPlane(Plane.WorldXY, location), att);

                  // We don't want geometry on the active viewport but on its own.
                  doc.Objects.ModifyAttributes(guid, att, quiet: true);
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

    #region ModelContent
#if RHINO_8

    static void PeekModelAttributes(IDictionary<ARDB.ElementId, ModelContent> idMap, ModelObject.Attributes attributes, ARDB.Document document)
    {
      var context = GeometryDecoder.Context.Peek;

      if (context.Category is ARDB.Category category)
      {
        attributes.Layer = new Category(category).ToModelContent(idMap) as ModelLayer;
      }

      if (context.Material is ARDB.Material material)
      {
        if (new Material(material).ToModelContent(idMap) is ModelRenderMaterial renderMaterial)
          attributes.Render = new ObjectRender.Attributes() { Material = renderMaterial };
      }
    }

    internal static ModelInstanceDefinition ToModelInstanceDefinition
    (
      IDictionary<ARDB.ElementId, ModelContent> idMap,
      Transform transform,
      ARDB.Element element,
      ARDB.GeometryElement geometryElement
    )
    {
      if (idMap.TryGetValue(element.Id, out var modelContent))
        return modelContent as ModelInstanceDefinition;

      var geometryElementContent = geometryElement?.ToArray() ?? Array.Empty<ARDB.GeometryObject>();

      if
      (
        geometryElementContent.Length == 1 &&
        geometryElementContent[0] is ARDB.GeometryInstance geometryInstance &&
        geometryInstance.GetSymbol() is ARDB.ElementType symbol
      )
      {
        // Special case to simplify ARDB.FamilyInstance elements.
        var instanceTransform = geometryInstance.Transform.ToTransform();
        return ToModelInstanceDefinition(idMap, instanceTransform * transform, symbol, geometryInstance.SymbolGeometry);
      }

      var attributes = new ModelInstanceDefinition.Attributes()
      {
        Path = NameConverter.EscapeName(element, out var description),
        Notes = description
      };

      GeometryDecoder.UpdateGraphicAttributes(geometryElement);

      bool identity = transform.IsIdentity;
      var objects = new List<ModelObject.Attributes>(geometryElementContent.Length);
      foreach (var g in geometryElementContent)
      {
        using (GeometryDecoder.Context.Push())
        {
          GeometryDecoder.UpdateGraphicAttributes(g);

          var shaded = false;
          var geo = default(IGH_GeometricGoo);
          switch (g)
          {
            case ARDB.Point point:
              var pointGeometry = point.Coord.ToPoint3d();
              if (!identity) pointGeometry.Transform(transform);
              geo = new GH_Point(pointGeometry);
              break;

            case ARDB.Mesh mesh:
              if (mesh.NumTriangles == 0) continue;
              var meshGeometry = mesh.ToMesh();
              if (!identity) meshGeometry.Transform(transform);
              geo = new GH_Mesh(meshGeometry);
              shaded = true;
              break;

            case ARDB.Solid solid:
              if (solid.Faces.IsEmpty) continue;
              var solidGeometry = solid.ToBrep();
              if (!identity) solidGeometry.Transform(transform);
              if (solidGeometry.TryGetExtrusion(out var extrusion)) geo = new GH_Extrusion(extrusion);
              else if (solidGeometry.Faces.Count == 1)              geo = new GH_Surface(solidGeometry);
              else                                                  geo = new GH_Brep(solidGeometry);
              shaded = true;
              break;

            case ARDB.Curve curve:
              var curveGeometry = curve.ToCurve();
              if (!identity) curveGeometry.Transform(transform);
              geo = new GH_Curve(curveGeometry);
              break;

            case ARDB.PolyLine pline:
              if (pline.NumberOfCoordinates == 0) continue;
              var plineGeometry = pline.ToPolylineCurve();
              if (!identity) plineGeometry.Transform(transform);
              geo = new GH_Curve(plineGeometry);
              break;

            case ARDB.GeometryInstance instance:
              using (GeometryDecoder.Context.Push())
              {
                if (ToModelInstanceDefinition(idMap, Transform.Identity, instance.GetSymbol(), instance.SymbolGeometry) is ModelInstanceDefinition definition)
                  geo = new GH_InstanceReference(new InstanceReferenceGeometry(Guid.Empty, transform * instance.Transform.ToTransform()), definition);
              }
              break;
          }

          if (geo is null) continue;

          var objectAttributes = ModelObject.Cast(geo).ToAttributes();
          PeekModelAttributes(idMap, objectAttributes, element.Document);

          if (shaded)
          {
            objectAttributes.Display = new ObjectDisplay.Attributes() { Color = ObjectDisplayColor.Value.ByMaterial };

            var context = GeometryDecoder.Context.Peek;
            if (context.FaceMaterialId is object)
            {
              bool hasPerFaceMaterials = false;
              for (int f = 1; f < context.FaceMaterialId.Length && !hasPerFaceMaterials; ++f)
                hasPerFaceMaterials |= context.FaceMaterialId[f] != context.FaceMaterialId[f - 1];

              if (!hasPerFaceMaterials)
              {
                var faceMaterial = new Material(element.Document, context.FaceMaterialId[0]);
                var faceModelMaterial = faceMaterial.ToModelContent(idMap) as ModelRenderMaterial;
                objectAttributes.Render = new ObjectRender.Attributes() { Material = faceModelMaterial };
              }
            }
          }

          objects.Add(objectAttributes);
        }
      }

      attributes.Objects = objects.Select(x => x.ToModelData() as ModelObject).ToArray();

      var modelInstanceDefinition = attributes.ToModelData() as ModelInstanceDefinition;
      idMap.Add(element.Id, modelInstanceDefinition);
      return modelInstanceDefinition;
    }

    internal override ModelContent ToModelContent(IDictionary<ARDB.ElementId, ModelContent> idMap)
    {
      if (idMap.TryGetValue(Id, out var modelContent))
        return modelContent;

      if (Value is ARDB.Element element)
      {
        using (var options = new ARDB.Options() { DetailLevel = ARDB.ViewDetailLevel.Fine })
        {
          using (var geometry = element.GetGeometry(options))
          {
            using (var context = GeometryDecoder.Context.Push())
            {
              context.Element = element;
              context.Category = element.Category;
              context.Material = element.Category?.Material;

              var location = element.Category is null || element.Category.Parent is object ?
                Plane.WorldXY :
                Location;

              var worldToElement = Transform.PlaneToPlane(location, Plane.WorldXY);
              if (ToModelInstanceDefinition(idMap, worldToElement, element, geometry) is ModelInstanceDefinition definition)
              {
                var elementToWorld = Transform.PlaneToPlane(Plane.WorldXY, location);
                var attributes = ModelObject.Cast(new GH_InstanceReference(new InstanceReferenceGeometry(Guid.Empty, elementToWorld), definition)).ToAttributes();
                attributes.Name = element.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;
                attributes.Url = element.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_URL)?.AsString() ?? string.Empty;
                attributes.Layer = Category.ToModelContent(idMap) as ModelLayer;

                modelContent = attributes.ToModelData() as ModelContent;
                //idMap.Add(Id, modelContent);
                return modelContent;
              }
            }
          }
        }
      }

      return null;
    }
#endif
#endregion

    #region IHostElementAccess
    GraphicalElement IHostElementAccess.HostElement => Value is ARDB.Element element ?
      element.ViewSpecific ? OwnerView?.Viewer :
      HostElement :
      default;

    public virtual GraphicalElement HostElement => Value is ARDB.Element element ?
      GetElement<GraphicalElement>(element.LevelId) :
      default;
    #endregion
  }
}

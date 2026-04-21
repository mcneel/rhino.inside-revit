using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Display;
  using Convert.Geometry;
  using External.DB;
  using External.DB.Extensions;
  using Grasshopper.Kernel;
  using Rhino.DocObjects;

  partial class GeometricElement
  {
    #region Preview
    internal static void BuildPreview
    (
      ARDB.Element element, MeshingParameters meshingParameters, ARDB.ViewDetailLevel detailLevel,
      out ARDB.View view, List<ARDB.Material> materials, List<Mesh> meshes, List<Curve> wires
    )
    {
      bool voidGeometry = element is ARDB.GenericForm form && !form.IsSolid;

      using
      (
        var options = element.ViewSpecific ?
        new ARDB.Options() { View = element.Document.GetElement(element.OwnerViewId) as ARDB.View, IncludeNonVisibleObjects = voidGeometry } :
        new ARDB.Options() { DetailLevel = detailLevel == ARDB.ViewDetailLevel.Undefined ? ARDB.ViewDetailLevel.Medium : detailLevel, IncludeNonVisibleObjects = voidGeometry }
      )
      {
        view = options.View;
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
            wires.AddRange(geometry.GetPreviewWires().Where(x => x is object));
            if (!voidGeometry)
            {
              var categoryMaterial = element.Category?.Material;
              var elementMaterial = geometry.MaterialElement ?? categoryMaterial;

              meshes?.AddRange(geometry.GetPreviewMeshes(element.Document, meshingParameters));
              materials?.AddRange(geometry.GetPreviewMaterials(element.Document, elementMaterial));
            }
          }
        }
      }
    }

    internal static void BuildPreview
    (
      ARDB.Element element, MeshingParameters meshingParameters, ARDB.ViewDetailLevel detailLevel,
      out ARDB.Material[] materials, out Mesh[] meshes, out Curve[] wires
    )
    {
      if (element is null) { materials = default; meshes = default; wires = default; return; }

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

            foreach (var dependent in element.GetDependentElements(ElementFilters.ElementHasBoundingBoxFilter).Select(element.Document.GetElement))
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
      public Mesh[] meshes;
      public Curve[] wires;
      public Curve[] highlights;
      static List<Preview> previewsQueue;

      void Build()
      {
        if (!geometricElement.IsValid || !clippingBox.IsValid)
        {
          materials = Array.Empty<DisplayMaterial>();
          meshes = Array.Empty<Mesh>();
          wires = Array.Empty<Curve>();
          highlights = Array.Empty<Curve>();
        }
        else if (meshes is null && wires is null && materials is null && highlights is null)
        {
          var element = geometricElement.Document.GetElement(geometricElement.Id);
          if (element is null)
            return;

          var elementMaterials = new List<ARDB.Material>();
          var elementMeshes = new List<Mesh>();
          var elementWires = new List<Curve>();
          var elementHighlight = new List<Curve>();
          BuildPreview(element, MeshingParameters, ARDB.ViewDetailLevel.Undefined, out var elementView, default, elementMeshes, elementWires);

          //if (element.Location is ARDB.LocationCurve elementCurve)
          //  elementHighlight.Add(elementCurve.Curve.ToCurve());

          // Extract dependents preview
          {
            var dependents = new List<ARDB.ElementId>();
            switch (element)
            {
              case ARDB.FamilyInstance instance:
                dependents.AddRange(instance.GetSubComponentIds());
                break;

              case ARDB.Wall wall:
                if (wall.IsStackedWall) dependents.AddRange(wall.GetStackedWallMemberIds());
                if (wall.CurtainGrid is ARDB.CurtainGrid grid) dependents.AddRange(grid.GetMullionIds().Concat(grid.GetPanelIds()));
                break;

              case ARDB.BeamSystem beamSystem:
                dependents.AddRange(beamSystem.GetBeamIds());
                break;

              case ARDB.Architecture.Railing railing:
                if (railing.TopRail.IsValid()) dependents.Add(railing.TopRail);
                dependents.AddRange(railing.GetHandRails());
                break;

              default:
                if (elementWires.Count == 0 && elementMeshes.Count == 0 && element.get_BoundingBox(elementView) is ARDB.BoundingBoxXYZ)
                  dependents.AddRange(element.GetDependentElements(ElementFilters.ElementHasBoundingBoxFilter));
                break;
            }

            //if (element is ARDB.HostObject hostObject)
            //  dependents.AddRange(hostObject.FindInserts(false, false, false, false));

            foreach (var dependent in dependents.Select(element.Document.GetElement))
              BuildPreview(dependent, MeshingParameters, ARDB.ViewDetailLevel.Undefined, out var _, null, null, elementHighlight);
          }

          // Optimize for display
          {
            wires = elementWires.ToArray();
            highlights = elementHighlight.ToArray();

            foreach (var elementMesh in elementMeshes)
              elementMesh.Normals.ComputeNormals();

            // Combine meshes of same material for display performance
            if (elementMeshes.Count > 0 && elementMeshes.Count == elementMaterials.Count)
            {
              var outMesh = new Mesh();
              var dictionary = PreviewConverter.ZipByMaterial(elementMaterials, elementMeshes, outMesh);
              if (outMesh.Faces.Count > 0)
              {
                var pairs = dictionary;//.OrderBy(x => x.Key.Transparency).ToList();
                materials = pairs.Select(x => DisplayMaterialConverter.ToDisplayMaterial(x.Key)).Concat(Enumerable.Repeat(new DisplayMaterial(), 1)).ToArray();
                meshes = pairs.Select(x => x.Value).Concat(Enumerable.Repeat(outMesh, 1)).ToArray();
              }
              else
              {
                var pairs = dictionary;//.OrderBy(x => x.Key.Transparency).ToList();
                materials = pairs.Select(x => DisplayMaterialConverter.ToDisplayMaterial(x.Key)).ToArray();
                meshes = pairs.Select(x => x.Value).ToArray();
              }
            }
            else
            {
              materials = Array.Empty<DisplayMaterial>();
              if (elementMeshes.Count > 1)
              {
                var combined = new Mesh();
                foreach (var mesh in elementMeshes)
                  combined.Append(mesh);

                meshes = new Mesh[] { combined };
              }
              else meshes = elementMeshes.ToArray();
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
        if (materials is object)
        {
          foreach (var material in materials)
            material.Dispose();

          materials = null;
        }

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

        if (highlights is object)
        {
          foreach (var highlight in highlights)
            highlight.Dispose();

          highlights = null;
        }
      }
    }

    MeshingParameters _MeshingParameters;
    Preview _GeometryPreview;
    Preview GeometryPreview
    {
      get => _GeometryPreview ??= Preview.OrderNew(this);
      set => _GeometryPreview = value;
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

    public Curve[] TryGetPreviewHighlights() => GeometryPreview.highlights;
    #endregion

    protected override void SubInvalidateGraphics()
    {
      /*using (_GeometryPreview) */_GeometryPreview = null;
      _MeshingParameters = null;

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

    #region IGH_PreviewData
    protected override void DrawViewportMeshes(GH_PreviewMeshArgs args)
    {
      if (!IsValid)
        return;

      var meshes = TryGetPreviewMeshes(args.MeshingParameters);
      if (meshes is null)
        return;

      var material = args.Material;
      //var element = Value;
      //if (element is null)
      //{
      //  const int factor = 3;

      //  // Erased element
      //  material = new Rhino.Display.DisplayMaterial(material)
      //  {
      //    Diffuse = System.Drawing.Color.FromArgb(20, 20, 20),
      //    Emission = System.Drawing.Color.FromArgb(material.Emission.R / factor, material.Emission.G / factor, material.Emission.B / factor),
      //    Shine = 0.0,
      //  };
      //}
      //else if (!element.Pinned)
      //{
      //  if (args.Pipeline.DisplayPipelineAttributes.ShadingEnabled)
      //  {
      //    // Unpinned element
      //    if (args.Pipeline.DisplayPipelineAttributes.UseAssignedObjectMaterial)
      //    {
      //      var materials = TryGetPreviewMaterials();

      //      for (int m = 0; m < meshes.Length; ++m)
      //        args.Pipeline.DrawMeshShaded(meshes[m], materials[m]);

      //      return;
      //    }
      //    else
      //    {
      //      material = new Rhino.Display.DisplayMaterial(material)
      //      {
      //        Diffuse = element.Category?.LineColor.ToColor() ?? System.Drawing.Color.White,
      //        Transparency = 0.0
      //      };

      //      if (material.Diffuse == System.Drawing.Color.Black)
      //        material.Diffuse = System.Drawing.Color.White;

      //      var materials = TryGetPreviewMaterials();
      //      for (int m = 0; m < meshes.Length; ++m)
      //      {
      //        material.Transparency = materials[m].Transparency;
      //        args.Pipeline.DrawMeshShaded(meshes[m], material);
      //      }

      //      return;
      //    }
      //  }
      //}

      foreach (var mesh in meshes)
        args.Pipeline.DrawMeshShaded(mesh, material);
    }

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (!IsValid)
        return;

      var thickness = args.Thickness * args.Pipeline.DpiScale;
      var color = args.Color;

      var drawBox = true;
      var higlights = TryGetPreviewHighlights();
      if (higlights is object && higlights.Length > 0)
      {
        drawBox = false;
        DrawWires(args.Pipeline, higlights, System.Drawing.Color.FromArgb(100, color), thickness/*, 5.0f*/);
      }

      var wires = TryGetPreviewWires();
      if (wires is object && wires.Length > 0)
      {
        drawBox = false;
        DrawWires(args.Pipeline, wires, color, thickness);
      }

      if (drawBox) base.DrawViewportWires(args);
    }

    private static void DrawWires(DisplayPipeline pipeline, IEnumerable<Curve> wires, System.Drawing.Color color, float thickness, float? pattern = default)
    {
#if RHINO_8
      var pen = new DisplayPen()
      {
        Color = color,
        Thickness = thickness,
        ThicknessSpace = CoordinateSystem.Screen,
        PatternLengthInWorldUnits = false,
      };

      if (pattern.HasValue) pen.SetPattern(new float[] { pattern.Value, pattern.Value });

      foreach (var wire in wires)
        pipeline.DrawCurve(wire, pen);
#else
      foreach (var wire in wires)
        pipeline.DrawCurve(wire, color, (int) Math.Round(thickness));
#endif
    }
    #endregion

    #region IGH_PreviewMeshData
    void IGH_PreviewMeshData.DestroyPreviewMeshes() => SubInvalidateGraphics();

    Mesh[] IGH_PreviewMeshData.GetPreviewMeshes() => TryGetPreviewMeshes();
    #endregion
  }
}

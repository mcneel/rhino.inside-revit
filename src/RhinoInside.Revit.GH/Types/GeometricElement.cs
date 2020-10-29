using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Display;
using RhinoInside.Revit.Convert.System.Drawing;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Geometric Element")]
  public interface IGH_GeometricElement : IGH_GraphicalElement { }

  [Kernel.Attributes.Name("Geometric Element")]
  public class GeometricElement : GraphicalElement, IGH_GeometricElement, IGH_PreviewMeshData
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
    public static void BuildPreview
    (
      DB.Element element, MeshingParameters meshingParameters, DB.ViewDetailLevel detailLevel,
      out Rhino.Display.DisplayMaterial[] materials, out Mesh[] meshes, out Curve[] wires
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
          var categoryMaterial = element.Category?.Material.ToDisplayMaterial(null);
          var elementMaterial = geometry.MaterialElement.ToDisplayMaterial(categoryMaterial);

          meshes = geometry.GetPreviewMeshes(meshingParameters).Where(x => x is object).ToArray();
          wires = geometry.GetPreviewWires().Where(x => x is object).ToArray();
          materials = geometry.GetPreviewMaterials(element.Document, elementMaterial).Where(x => x is object).ToArray();

          if (meshes.Length == 0 && wires.Length == 0 && element.get_BoundingBox(null) is DB.BoundingBoxXYZ)
          {
            var subMeshes = new List<Mesh>();
            var subWires = new List<Curve>();
            var subMaterials = new List<Rhino.Display.DisplayMaterial>();

            foreach (var dependent in element.GetDependentElements(null).Select(x => element.Document.GetElement(x)))
            {
              if (dependent.get_BoundingBox(null) is null)
                continue;

              using (var dependentOptions = new DB.Options() { DetailLevel = detailLevel == DB.ViewDetailLevel.Undefined ? DB.ViewDetailLevel.Medium : detailLevel })
              using (var dependentGeometry = dependent?.GetGeometry(dependentOptions))
              {
                if (dependentGeometry is object)
                {
                  subMeshes.AddRange(dependentGeometry.GetPreviewMeshes(meshingParameters).Where(x => x is object));
                  subWires.AddRange(dependentGeometry.GetPreviewWires().Where(x => x is object));
                  subMaterials.AddRange(dependentGeometry.GetPreviewMaterials(element.Document, elementMaterial).Where(x => x is object));
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
        if ((meshes is null && wires is null && materials is null))
        {
          var element = geometricElement.Document.GetElement(geometricElement.Id);
          if (element is null)
            return;

          BuildPreview(element, MeshingParameters, DB.ViewDetailLevel.Undefined, out materials, out meshes, out wires);
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
        foreach (var mesh in meshes ?? Enumerable.Empty<Mesh>())
          mesh.Dispose();
        meshes = null;

        foreach (var wire in wires ?? Enumerable.Empty<Curve>())
          wire.Dispose();
        wires = null;
      }
    }

    MeshingParameters meshingParameters;
    Preview geometryPreview;
    Preview GeometryPreview
    {
      get { return geometryPreview ?? (geometryPreview = Preview.OrderNew(this)); }
      set { if (geometryPreview != value) { ((IDisposable) geometryPreview)?.Dispose(); geometryPreview = value; } }
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
      var element = Document?.GetElement(Id);
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
      var element = Document?.GetElement(Id);
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
          // Grasshopper does not show mesh wires.
          //foreach (var mesh in meshes)
          //  args.Pipeline.DrawMeshWires(mesh, color, thickness);
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

    Mesh[] IGH_PreviewMeshData.GetPreviewMeshes()
    {
      return TryGetPreviewMeshes();
    }
    #endregion
  }
}

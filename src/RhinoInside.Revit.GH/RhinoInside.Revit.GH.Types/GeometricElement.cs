using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class GeometricElement :
    Element,
    IGH_GeometricGoo,
    IGH_PreviewData,
    IGH_PreviewMeshData
  {
    public override string TypeName => "Revit Geometric element";
    public override string TypeDescription => "Represents a Revit geometric element";

    public override string DisplayName
    {
      get
      {
        var element = (DB.Element) this;
        if (element is object)
        {
          if (element.get_Parameter(DB.BuiltInParameter.ALL_MODEL_MARK) is DB.Parameter parameter && parameter.HasValue)
          {
            var mark = parameter.AsString();
            if(!string.IsNullOrEmpty(mark))
              return $"{base.DisplayName} [{mark}]";
          }
        }

        return base.DisplayName;
      }
    }

    public GeometricElement() { }
    public GeometricElement(DB.Element element) : base(element) { }
    protected override bool SetValue(DB.Element element) => IsValidElement(element) ? base.SetValue(element) : false;
    public static bool IsValidElement(DB.Element element)
    {
      return
      (
        element is DB.DirectShape ||
        element is DB.CurveElement ||
        element is DB.CombinableElement ||
        element is DB.Architecture.TopographySurface ||
        (element.Category is object && element.CanHaveTypeAssigned())
      );
    }
    #region Preview
    public static void BuildPreview
    (
      DB.Element element, MeshingParameters meshingParameters, DB.ViewDetailLevel DetailLevel,
      out Rhino.Display.DisplayMaterial[] materials, out Mesh[] meshes, out Curve[] wires
    )
    {
      DB.Options options = null;
      using (var geometry = element?.GetGeometry(DetailLevel == DB.ViewDetailLevel.Undefined ? DB.ViewDetailLevel.Medium : DetailLevel, out options)) using (options)
      {
        if (geometry is null)
        {
          materials = null;
          meshes = null;
          wires = null;
        }
        else
        {
          var categoryMaterial = element.Category?.Material.ToRhino(null);
          var elementMaterial = geometry.MaterialElement.ToRhino(categoryMaterial);

          meshes = geometry.GetPreviewMeshes(meshingParameters).Where(x => x is object).ToArray();
          wires = geometry.GetPreviewWires().Where(x => x is object).ToArray();
          materials = geometry.GetPreviewMaterials(element.Document, elementMaterial).Where(x => x is object).ToArray();

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

    public Mesh[] TryGetPreviewMeshes(MeshingParameters parameters = default)
    {
      if (parameters is object && !ReferenceEquals(meshingParameters, parameters))
      {
        meshingParameters = parameters;
        if (geometryPreview is object)
        {
          if (geometryPreview.MeshingParameters?.RelativeTolerance != meshingParameters.RelativeTolerance)
            GeometryPreview = null;
        }
      }

      return GeometryPreview.meshes;
    }

    public Curve[] TryGetPreviewWires()
    {
      return GeometryPreview.wires;
    }
    #endregion

    #region IGH_GeometricGoo
    public BoundingBox Boundingbox => ClippingBox;
    Guid IGH_GeometricGoo.ReferenceID
    {
      get => Guid.Empty;
      set { if (value != Guid.Empty) throw new InvalidOperationException(); }
    }
    bool IGH_GeometricGoo.IsReferencedGeometry => IsReferencedElement;
    bool IGH_GeometricGoo.IsGeometryLoaded => IsElementLoaded;

    void IGH_GeometricGoo.ClearCaches() => UnloadElement();
    IGH_GeometricGoo IGH_GeometricGoo.DuplicateGeometry() => (IGH_GeometricGoo) MemberwiseClone();
    public BoundingBox GetBoundingBox(Transform xform) => ClippingBox;
    bool IGH_GeometricGoo.LoadGeometry() => IsElementLoaded || LoadElement();
    bool IGH_GeometricGoo.LoadGeometry(Rhino.RhinoDoc doc) => IsElementLoaded || LoadElement();
    IGH_GeometricGoo IGH_GeometricGoo.Transform(Transform xform) => null;
    IGH_GeometricGoo IGH_GeometricGoo.Morph(SpaceMorph xmorph) => null;
    #endregion

    #region IGH_PreviewData
    void IGH_PreviewData.DrawViewportMeshes(GH_PreviewMeshArgs args)
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
              Diffuse = element.Category?.LineColor.ToRhino() ?? System.Drawing.Color.White,
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

    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
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
    void IGH_PreviewMeshData.DestroyPreviewMeshes()
    {
      GeometryPreview = null;
      clippingBox = BoundingBox.Empty;
    }

    Mesh[] IGH_PreviewMeshData.GetPreviewMeshes()
    {
      return TryGetPreviewMeshes();
    }
    #endregion
  }
}

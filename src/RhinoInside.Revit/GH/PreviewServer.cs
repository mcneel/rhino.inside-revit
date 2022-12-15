#if REVIT_2018
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using ARUI = Autodesk.Revit.UI;
using ARDB = Autodesk.Revit.DB;
using ARDBES = Autodesk.Revit.DB.ExternalService;
using ARDB3D = Autodesk.Revit.DB.DirectContext3D;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.GUI.Canvas;

namespace RhinoInside.Revit.GH
{
  using Convert.Geometry;
  
  internal class PreviewServer : DirectContext3DServer
  {
    static GH_Document ActiveDefinition => Instances.ActiveCanvas?.Document;

    class PreviewNode : IDisposable
    {
      public readonly IGH_ActiveObject ActiveObject;
      public PreviewNode(IGH_ActiveObject activeObject)
      {
        ActiveObject = activeObject?.Attributes.GetTopLevel?.DocObject as IGH_ActiveObject ?? activeObject;
        ActiveObject.ObjectChanged += ObjectChanged;
      }

      public List<ParamPrimitive> Primitives = new List<ParamPrimitive>();
      public void Dispose()
      {
        ActiveObject.ObjectChanged -= ObjectChanged;

        foreach (var primitive in Primitives)
          ((IDisposable) primitive).Dispose();
      }

      void ObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
      {
        if (e.Type == GH_ObjectEventType.Preview)
          Revit.RefreshActiveView();
      }
    }

    Dictionary<Guid, PreviewNode> PreviewNodes = new Dictionary<Guid, PreviewNode>();

    readonly object PreviewNodesToken = new object();
    bool PreviewNodesWasTaken = false;

    static GH_PreviewMode _PreviewMode = GH_PreviewMode.Disabled;
    public static GH_PreviewMode PreviewMode
    {
      get => _PreviewMode;
      set
      {
        if (value == _PreviewMode) return;

        var previous = _PreviewMode;
        _PreviewMode = value;
        PreviewModeChanged?.Invoke(default, previous);
      }
    }

    public static event EventHandler<GH_PreviewMode> PreviewModeChanged;

    static readonly Rhino.Geometry.MeshingParameters previewCurrentMeshParameters = Rhino.Geometry.MeshingParameters.Default;
    static Rhino.Geometry.MeshingParameters PreviewCurrentMeshParameters
    {
      get
      {
        previewCurrentMeshParameters.RelativeTolerance = 0.15;
        previewCurrentMeshParameters.MinimumEdgeLength = GeometryTolerance.Model.ShortCurveTolerance;

        if (ActiveDefinition?.PreviewCurrentMeshParameters() is Rhino.Geometry.MeshingParameters parameters)
        {
          previewCurrentMeshParameters.MinimumTolerance = parameters.MinimumTolerance;
          previewCurrentMeshParameters.RelativeTolerance = parameters.RelativeTolerance;
          previewCurrentMeshParameters.MinimumEdgeLength = Math.Max(previewCurrentMeshParameters.MinimumEdgeLength, parameters.MinimumEdgeLength);
          previewCurrentMeshParameters.Tolerance = parameters.Tolerance;
          previewCurrentMeshParameters.GridAmplification = parameters.GridAmplification;
          previewCurrentMeshParameters.GridAspectRatio = parameters.GridAspectRatio;
          previewCurrentMeshParameters.GridAngle = parameters.GridAngle;
          previewCurrentMeshParameters.GridMinCount = parameters.GridMinCount;
          previewCurrentMeshParameters.JaggedSeams = parameters.JaggedSeams;
          previewCurrentMeshParameters.SimplePlanes = parameters.SimplePlanes;
          previewCurrentMeshParameters.RefineGrid = parameters.RefineGrid;
          previewCurrentMeshParameters.MaximumEdgeLength = parameters.MaximumEdgeLength;
          previewCurrentMeshParameters.RefineAngle = parameters.RefineAngle;
          previewCurrentMeshParameters.TextureRange = parameters.TextureRange;
        }

        previewCurrentMeshParameters.GridMaxCount = 512;
        previewCurrentMeshParameters.ComputeCurvature = false;
        previewCurrentMeshParameters.SimplePlanes = true;
        previewCurrentMeshParameters.ClosedObjectPostProcess = false;
        return previewCurrentMeshParameters;
      }
    }

    public PreviewServer()
    {
      Instances.CanvasCreatedEventHandler CanvasCreated = default;
      Instances.CanvasCreated += CanvasCreated = (canvas) =>
      {
        Instances.CanvasCreated -= CanvasCreated;
        canvas.DocumentChanged += ActiveCanvas_DocumentChanged;
      };

      Instances.CanvasDestroyedEventHandler Canvas_Destroyed = default;
      Instances.CanvasDestroyed += Canvas_Destroyed = (canvas) =>
      {
        Instances.CanvasDestroyed -= Canvas_Destroyed;
        canvas.DocumentChanged -= ActiveCanvas_DocumentChanged;
      };
    }

    #region IExternalServer
    public override string GetName() => "Grasshopper";
    public override string GetDescription() => "Grasshopper previews server";
    public override Guid GetServerId() => Instances.GrasshopperPluginId;
    #endregion

    #region IDirectContext3DServer
    public override bool UseInTransparentPass(ARDB.View dBView) =>
      ((ActiveDefinition is null ? GH_PreviewMode.Disabled : PreviewMode) == GH_PreviewMode.Shaded);

    public override bool CanExecute(ARDB.View dBView) =>
      GH_Document.EnableSolutions &&
      !IsBusy &&
      PreviewMode != GH_PreviewMode.Disabled &&
      ActiveDefinition is object &&
      IsModelView(dBView);

    List<IGH_DocumentObject> lastSelection;

    private void SelectionChanged(object sender, EventArgs e)
    {
      var newSelection = ActiveDefinition.SelectedObjects();
      if (PreviewMode != GH_PreviewMode.Disabled && GH_Document.EnableSolutions)
      {
        if (lastSelection.Count != newSelection.Count || lastSelection.Except(newSelection).Any())
          Revit.RefreshActiveView();
      }

      lastSelection = newSelection;
    }

    static void Document_DefaultPreviewColourChanged(System.Drawing.Color colour) => Revit.RefreshActiveView();

    private void ActiveCanvas_DocumentChanged(GH_Canvas sender, GH_CanvasDocumentChangedEventArgs e)
    {
      if (e.OldDocument is object)
      {
        Rhino.RhinoApp.Idle -= SelectionChanged;
        e.OldDocument.SolutionStart -= ActiveDefinition_SolutionStart;
        e.OldDocument.SolutionEnd -= ActiveDefinition_SolutionEnd;
        e.OldDocument.SettingsChanged -= ActiveDefinition_SettingsChanged;
        GH_Document.DefaultSelectedPreviewColourChanged -= Document_DefaultPreviewColourChanged;
        GH_Document.DefaultPreviewColourChanged -= Document_DefaultPreviewColourChanged;
      }

      RebuildPrimitives();
      lastSelection = new List<IGH_DocumentObject>();

      if (e.NewDocument is object)
      {
        GH_Document.DefaultPreviewColourChanged += Document_DefaultPreviewColourChanged;
        GH_Document.DefaultSelectedPreviewColourChanged += Document_DefaultPreviewColourChanged;
        e.NewDocument.SettingsChanged += ActiveDefinition_SettingsChanged;
        e.NewDocument.SolutionEnd += ActiveDefinition_SolutionEnd;
        e.NewDocument.SolutionStart += ActiveDefinition_SolutionStart;
        Rhino.RhinoApp.Idle += SelectionChanged;
      }
    }

    void ActiveDefinition_SettingsChanged(object sender, GH_DocSettingsEventArgs e)
    {
      if (e.Kind == GH_DocumentSettings.Properties)
        RebuildPrimitives();

      if (PreviewMode != GH_PreviewMode.Disabled)
        Revit.RefreshActiveView();
    }

    void ActiveDefinition_SolutionStart(object sender, GH_SolutionEventArgs e)
    {
      var primitivesCache = new Dictionary<Guid, PreviewNode>();

      IsBusy = true;
      Monitor.Enter(PreviewNodesToken, ref PreviewNodesWasTaken);

      // Magic trick to force Revit update the Outline.
      {
        using (var uiDocument = new ARUI.UIDocument(Revit.ActiveDBDocument))
          uiDocument.Selection.SetElementIds(uiDocument.Selection.GetElementIds());
      }

      var expireAllObjects = !e.Document.Objects.Cast<IGH_ActiveObject>().Any(x => x.Phase != GH_SolutionPhase.Computed);
      foreach (var node in PreviewNodes)
      {
        if
        (
          !expireAllObjects &&
          e.Document.FindObject(node.Key, topLevelOnly: false) is IGH_ActiveObject activeObject &&
          activeObject.Phase == GH_SolutionPhase.Computed
        )
        {
          primitivesCache.Add(node.Key, node.Value);
        }
        else node.Value.Dispose();
      }

      PreviewNodes = primitivesCache;
    }

    void ActiveDefinition_SolutionEnd(object sender, GH_SolutionEventArgs e)
    {
      if (PreviewNodesWasTaken) { Monitor.Exit(PreviewNodesToken); PreviewNodesWasTaken = false; }
      IsBusy = false;

      if (PreviewMode != GH_PreviewMode.Disabled)
      {
        foreach (var param in DrawableParams)
        {
          if (PreviewNodes.ContainsKey(param.InstanceGuid))
            continue;

          var node = new PreviewNode(param);
          DrawData(param.VolatileData, node);
          PreviewNodes.Add(param.InstanceGuid, node);
        }

        Revit.RefreshActiveView();
        //Revit.ActiveUIApplication.ActiveUIDocument.UpdateAllOpenViews();
      }
    }

    private class ParamPrimitive : Primitive
    {
      readonly PreviewNode Node;

      public ParamPrimitive(PreviewNode node, Rhino.Geometry.Point p) : base(p) => Node = node;
      public ParamPrimitive(PreviewNode node, Rhino.Geometry.PointCloud c) : base(c) => Node = node;
      public ParamPrimitive(PreviewNode node, Rhino.Geometry.PointCloud c, Part p) : base(c, p) => Node = node;
      public ParamPrimitive(PreviewNode node, Rhino.Geometry.Curve c) : base(c) => Node = node;
      public ParamPrimitive(PreviewNode node, Rhino.Geometry.Mesh m) : base(m) => Node = node;
      public ParamPrimitive(PreviewNode node, Rhino.Geometry.Mesh m, Part p) : base(m, p) => Node = node;

      // Since ScriptVariable returns a copy, we can Dispose it.
      protected override bool IsGeometryDisposable => true;

      public override ARDB3D.EffectInstance EffectInstance(ARDB.DisplayStyle displayStyle, bool IsShadingPass)
      {
        var ei = base.EffectInstance(displayStyle, IsShadingPass);

        var topAttributes = Node.ActiveObject.Attributes?.GetTopLevel ?? Node.ActiveObject.Attributes;
        var color = topAttributes.Selected ? ActiveDefinition.PreviewColourSelected : ActiveDefinition.PreviewColour;

        if (IsShadingPass)
        {
          if (HasVertexColors(vertexFormatBits) && ShowsVertexColors(displayStyle))
          {
            ei.SetTransparency(0.0);
            ei.SetColor(ARDB.Color.InvalidColorValue);
            ei.SetEmissiveColor(ARDB.Color.InvalidColorValue);
          }
          else
          {
            ei.SetTransparency((255 - color.A) / 255.0);
            ei.SetEmissiveColor(new ARDB.Color(color.R, color.G, color.B));
          }
        }
        else
        {
          ei.SetTransparency(0.0);
          ei.SetColor(new ARDB.Color(color.R, color.G, color.B));
          ei.SetEmissiveColor(ARDB.Color.InvalidColorValue);
        }

        return ei;
      }

      public override void Draw(ARDB.DisplayStyle displayStyle)
      {
        if (Node.ActiveObject is IGH_PreviewObject preview)
        {
          if (preview.Hidden || !preview.IsPreviewCapable)
            return;
        }

        var topObject = Node.ActiveObject.Attributes?.GetTopLevel?.DocObject ?? Node.ActiveObject;
        if (topObject is IGH_PreviewObject topPreview)
        {
          if (topPreview.Hidden || !topPreview.IsPreviewCapable)
            return;
        }

        if (ActiveDefinition.PreviewFilter == GH_PreviewFilter.Selected && !topObject.Attributes.Selected)
          return;

        base.Draw(displayStyle);
      }
    }

    void RebuildPrimitives()
    {
      IsBusy = true;
      lock (PreviewNodesToken) PreviewNodes.Clear();
      IsBusy = false;
    }

    int _IsBusy = 0;
    bool IsBusy
    {
      get
      {
        Interlocked.MemoryBarrier();
        return _IsBusy != 0;
      }

      set => _IsBusy = value ? 1 : 0;
    }

    bool IsInterrupted => IsBusy || ARDB3D.DrawContext.IsInterrupted();

    IEnumerable<IGH_Param> DrawableParams
    {
      get
      {
        foreach (var obj in ActiveDefinition.Objects.OfType<IGH_ActiveObject>())
        {
          if (obj.Locked)
            continue;

          if (obj is IGH_PreviewObject previewObject && previewObject.IsPreviewCapable)
          {
            if (obj is IGH_Component component)
            {
              foreach (var param in component.Params.Output)
              {
                if (param is IGH_PreviewObject preview)
                {
                  if (preview.Hidden) continue;
                  yield return param;
                }
              }
            }
            else if (obj is IGH_Param param)
            {
              yield return param;
            }
          }
        }
      }
    }

    void DrawData(Grasshopper.Kernel.Data.IGH_Structure volatileData, PreviewNode node)
    {
      if (!volatileData.IsEmpty)
      {
        var primitives = node.Primitives;
        primitives.Capacity = volatileData.DataCount;

        foreach (var value in volatileData.AllData(true))
        {
          // First check for IGH_PreviewData to discard no graphic elements like strings, doubles, vectors...
          if (value is IGH_PreviewData)
          {
            switch (value.ScriptVariable())
            {
              case Rhino.Geometry.Point3d point:    primitives.Add(new ParamPrimitive(node, new Rhino.Geometry.Point(point))); break;
              case Rhino.Geometry.Line line:        primitives.Add(new ParamPrimitive(node, new Rhino.Geometry.LineCurve(line))); break;
              case Rhino.Geometry.Rectangle3d rect: primitives.Add(new ParamPrimitive(node, rect.ToNurbsCurve())); break;
              case Rhino.Geometry.Arc arc:          primitives.Add(new ParamPrimitive(node, new Rhino.Geometry.ArcCurve(arc))); break;
              case Rhino.Geometry.Circle circle:    primitives.Add(new ParamPrimitive(node, new Rhino.Geometry.ArcCurve(circle))); break;
              case Rhino.Geometry.Ellipse ellipse:  primitives.Add(new ParamPrimitive(node, ellipse.ToNurbsCurve())); break;
              case Rhino.Geometry.Curve curve:      primitives.Add(new ParamPrimitive(node, curve)); break;
              case Rhino.Geometry.PointCloud cloud:
              {
                var verticesCount = cloud.Count;
                if (verticesCount <= VertexThreshold)
                {
                  primitives.Add(new ParamPrimitive(node, cloud));
                }
                else
                {
                  int c = 0;
                  for (; c < verticesCount / VertexThreshold; ++c)
                  {
                    var part = new Primitive.Part(c * VertexThreshold, (c + 1) * VertexThreshold);
                    primitives.Add(new ParamPrimitive(node, cloud, part));
                  }

                  if ((verticesCount % VertexThreshold) > 0)
                  {
                    var part = new Primitive.Part(c * VertexThreshold, verticesCount);
                    primitives.Add(new ParamPrimitive(node, cloud, part));
                  }
                }
              }
              break;
              case Rhino.Geometry.Mesh mesh:
              {
                if (mesh.Vertices.Count <= VertexThreshold && mesh.Faces.Count <= VertexThreshold)
                {
                  primitives.Add(new ParamPrimitive(node, mesh));
                }
                else if (mesh.CreatePartitions(VertexThreshold, VertexThreshold))
                {
                  if (!mesh.IsValid)
                    mesh.Vertices.UseDoublePrecisionVertices = true;

                  var count = mesh.PartitionCount;
                  for (int p = 0; p < count; ++p)
                    primitives.Add(new ParamPrimitive(node, mesh, mesh.GetPartition(p)));
                }
              }
              break;
              case Rhino.Geometry.Box box:
              {
                if(Rhino.Geometry.Mesh.CreateFromBox(box, 1, 1, 1) is Rhino.Geometry.Mesh previewMesh)
                  primitives.Add(new ParamPrimitive(node, previewMesh));
              }
              break;
              case Rhino.Geometry.SubD subd:
              {
                if (Rhino.Geometry.Mesh.CreateFromSubD(subd, 3) is Rhino.Geometry.Mesh previewMesh)
                  primitives.Add(new ParamPrimitive(node, previewMesh));
              }
              break;
              case Rhino.Geometry.Brep brep:
              {
                if (Rhino.Geometry.Mesh.CreateFromBrep(brep, PreviewCurrentMeshParameters) is Rhino.Geometry.Mesh[] brepMeshes)
                {
                  var previewMesh = new Rhino.Geometry.Mesh();
                  previewMesh.Append(brepMeshes);

                  primitives.Add(new ParamPrimitive(node, previewMesh));
                }
              }
              break;
            }
          }
        }
      }
    }

    public override ARDB.Outline GetBoundingBox(ARDB.View dBView)
    {
      var outline = Rhino.Geometry.BoundingBox.Empty;

      if (!IsBusy && PreviewMode != GH_PreviewMode.Disabled)
      {
        lock (PreviewNodesToken)
        {
          foreach (var node in PreviewNodes.Values)
          {
            if (node.ActiveObject.Locked)
              continue;

            if (node.ActiveObject is IGH_PreviewObject previewObject)
            {
              if (previewObject.Hidden)
                continue;

              if (!previewObject.IsPreviewCapable)
                continue;

              outline = Rhino.Geometry.BoundingBox.Union(outline, previewObject.ClippingBox);
            }
          }
        }
      }

      return outline.ToOutline();
    }

    public override void RenderScene(ARDB.View dBView, ARDB.DisplayStyle displayStyle)
    {
      if (IsBusy) return;

      try
      {
        lock (PreviewNodesToken)
        {
          ARDB3D.DrawContext.SetWorldTransform
          (
            ARDB.Transform.Identity.ScaleBasis(GeometryEncoder.ModelScaleFactor)
          );

          var CropBox = dBView.CropBox.ToBoundingBox();
          var CropBoxActive = dBView.CropBoxActive;

          foreach (var node in PreviewNodes.Values)
          {
            foreach (var primitive in node.Primitives)
            {
              if (IsInterrupted)
                break;

              if (CropBoxActive && !Rhino.Geometry.BoundingBox.Intersection(CropBox, primitive.ClippingBox).IsValid)
                continue;

              primitive.Draw(displayStyle);
            }
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
#else
namespace RhinoInside.Revit.GH
{
  internal class PreviewServer
  {
    public void Register() { }

    public void Unregister() { }

    public static readonly Grasshopper.Kernel.GH_PreviewMode PreviewMode = Grasshopper.Kernel.GH_PreviewMode.Disabled;
  }
}
#endif

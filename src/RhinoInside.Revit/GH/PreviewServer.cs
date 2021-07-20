#if REVIT_2018
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using DB = Autodesk.Revit.DB;
using DBES = Autodesk.Revit.DB.ExternalService;
using DB3D = Autodesk.Revit.DB.DirectContext3D;
using RhinoInside.Revit.Convert.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.GUI.Canvas;

namespace RhinoInside.Revit.GH
{
  public class PreviewServer : DirectContext3DServer
  {
    static GH_Document ActiveDefinition => Instances.ActiveCanvas?.Document;

    List<ParamPrimitive> primitives = new List<ParamPrimitive>();
    Rhino.Geometry.BoundingBox primitivesBoundingBox = Rhino.Geometry.BoundingBox.Empty;
    int RebuildPrimitives = 1;

    public static GH_PreviewMode PreviewMode = GH_PreviewMode.Shaded;

    static Rhino.Geometry.MeshingParameters previewCurrentMeshParameters = new Rhino.Geometry.MeshingParameters(0.15, Revit.ShortCurveTolerance);
    static Rhino.Geometry.MeshingParameters PreviewCurrentMeshParameters
    {
      get
      {
        previewCurrentMeshParameters.RelativeTolerance = 0.15;
        previewCurrentMeshParameters.MinimumEdgeLength = Revit.ShortCurveTolerance * Revit.ModelUnits;

        if (ActiveDefinition?.PreviewCurrentMeshParameters() is Rhino.Geometry.MeshingParameters parameters)
        {
          previewCurrentMeshParameters.MinimumTolerance = parameters.MinimumTolerance;
          previewCurrentMeshParameters.RelativeTolerance = parameters.RelativeTolerance;
          previewCurrentMeshParameters.MinimumEdgeLength = Math.Max(Revit.ShortCurveTolerance * Revit.ModelUnits, parameters.MinimumEdgeLength);
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
    public override bool UseInTransparentPass(DB.View dBView) =>
      ((ActiveDefinition is null ? GH_PreviewMode.Disabled : PreviewMode) == GH_PreviewMode.Shaded);

    public override bool CanExecute(DB.View dBView) =>
      GH_Document.EnableSolutions &&
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
        e.OldDocument.SolutionEnd -= ActiveDefinition_SolutionEnd;
        e.OldDocument.SettingsChanged -= ActiveDefinition_SettingsChanged;
        GH_Document.DefaultSelectedPreviewColourChanged -= Document_DefaultPreviewColourChanged;
        GH_Document.DefaultPreviewColourChanged -= Document_DefaultPreviewColourChanged;
      }

      RebuildPrimitives = 1;
      lastSelection = new List<IGH_DocumentObject>();

      if (e.NewDocument is object)
      {
        GH_Document.DefaultPreviewColourChanged += Document_DefaultPreviewColourChanged;
        GH_Document.DefaultSelectedPreviewColourChanged += Document_DefaultPreviewColourChanged;
        e.NewDocument.SettingsChanged += ActiveDefinition_SettingsChanged;
        e.NewDocument.SolutionEnd += ActiveDefinition_SolutionEnd;
        Rhino.RhinoApp.Idle += SelectionChanged;
      }
    }

    void ActiveDefinition_SettingsChanged(object sender, GH_DocSettingsEventArgs e)
    {
      if (e.Kind == GH_DocumentSettings.Properties)
        RebuildPrimitives = 1;

      if (PreviewMode != GH_PreviewMode.Disabled)
        Revit.RefreshActiveView();
    }

    void ActiveDefinition_SolutionEnd(object sender, GH_SolutionEventArgs e)
    {
      RebuildPrimitives = 1;

      if (PreviewMode != GH_PreviewMode.Disabled)
        Revit.RefreshActiveView();
    }

    protected class ParamPrimitive : Primitive
    {
      readonly IGH_DocumentObject docObject;
      public ParamPrimitive(IGH_DocumentObject o, Rhino.Geometry.Point p) : base(p) { docObject = o; o.ObjectChanged += ObjectChanged; }
      public ParamPrimitive(IGH_DocumentObject o, Rhino.Geometry.Curve c) : base(c) { docObject = o; o.ObjectChanged += ObjectChanged; }
      public ParamPrimitive(IGH_DocumentObject o, Rhino.Geometry.Mesh m)  : base(m) { docObject = o; o.ObjectChanged += ObjectChanged; }

      void ObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
      {
        if (e.Type == GH_ObjectEventType.Preview)
          Revit.RefreshActiveView();
      }

      public override DB3D.EffectInstance EffectInstance(DB.DisplayStyle displayStyle, bool IsShadingPass)
      {
        var ei = base.EffectInstance(displayStyle, IsShadingPass);

        var topAttributes = docObject.Attributes?.GetTopLevel ?? docObject.Attributes;
        var color = topAttributes.Selected ? ActiveDefinition.PreviewColourSelected : ActiveDefinition.PreviewColour;

        if (IsShadingPass)
        {
          var vc = HasVertexColors(vertexFormatBits) && ShowsVertexColors(displayStyle);
          if (!vc)
          {
            ei.SetTransparency(Math.Max(1.0 / 255.0, (255 - color.A) / 255.0));
            ei.SetEmissiveColor(new DB.Color(color.R, color.G, color.B));
          }
        }
        else ei.SetColor(new DB.Color(color.R, color.G, color.B));

        return ei;
      }

      public override void Draw(DB.DisplayStyle displayStyle)
      {
        if (docObject is IGH_PreviewObject preview)
        {
          if (preview.Hidden || !preview.IsPreviewCapable)
            return;
        }

        var topObject = docObject.Attributes?.GetTopLevel?.DocObject ?? docObject;
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

    void DrawData(Grasshopper.Kernel.Data.IGH_Structure volatileData, IGH_DocumentObject docObject)
    {
      if (!volatileData.IsEmpty)
      {
        foreach (var value in volatileData.AllData(true))
        {
          // First check for IGH_PreviewData to discard no graphic elements like strings, doubles, vectors...
          if (value is IGH_PreviewData)
          {
            switch (value.ScriptVariable())
            {
              case Rhino.Geometry.Point3d point:    primitives.Add(new ParamPrimitive(docObject, new Rhino.Geometry.Point(point))); break;
              case Rhino.Geometry.Line line:        primitives.Add(new ParamPrimitive(docObject, new Rhino.Geometry.LineCurve(line))); break;
              case Rhino.Geometry.Rectangle3d rect: primitives.Add(new ParamPrimitive(docObject, rect.ToNurbsCurve())); break;
              case Rhino.Geometry.Arc arc:          primitives.Add(new ParamPrimitive(docObject, new Rhino.Geometry.ArcCurve(arc))); break;
              case Rhino.Geometry.Circle circle:    primitives.Add(new ParamPrimitive(docObject, new Rhino.Geometry.ArcCurve(circle))); break;
              case Rhino.Geometry.Ellipse ellipse:  primitives.Add(new ParamPrimitive(docObject, ellipse.ToNurbsCurve())); break;
              case Rhino.Geometry.Curve curve:      primitives.Add(new ParamPrimitive(docObject, curve)); break;
              case Rhino.Geometry.Mesh mesh:        primitives.Add(new ParamPrimitive(docObject, mesh)); break;
              case Rhino.Geometry.Box box:
              {
                if(Rhino.Geometry.Mesh.CreateFromBox(box, 1, 1, 1) is Rhino.Geometry.Mesh previewMesh)
                  primitives.Add(new ParamPrimitive(docObject, previewMesh));
              }
              break;
              case Rhino.Geometry.SubD subd:
              {
                if (Rhino.Geometry.Mesh.CreateFromSubD(subd, 3) is Rhino.Geometry.Mesh previewMesh)
                  primitives.Add(new ParamPrimitive(docObject, previewMesh));
              }
              break;
              case Rhino.Geometry.Brep brep:
              {
                if (Rhino.Geometry.Mesh.CreateFromBrep(brep, PreviewCurrentMeshParameters) is Rhino.Geometry.Mesh[] brepMeshes)
                {
                  var previewMesh = new Rhino.Geometry.Mesh();
                  previewMesh.Append(brepMeshes);

                  primitives.Add(new ParamPrimitive(docObject, previewMesh));
                }
              }
              break;
            }
          }
        }
      }
    }

    Rhino.Geometry.BoundingBox BuildScene(DB.View dBView)
    {
      if (Interlocked.Exchange(ref RebuildPrimitives, 0) != 0)
      {
        primitivesBoundingBox = Rhino.Geometry.BoundingBox.Empty;

        // Dispose previous primitives
        {
          foreach (var primitive in primitives)
            ((IDisposable) primitive).Dispose();

          primitives.Clear();
        }

        var previewColour = ActiveDefinition.PreviewColour;
        var previewColourSelected = ActiveDefinition.PreviewColourSelected;

        foreach (var obj in ActiveDefinition.Objects.OfType<IGH_ActiveObject>())
        {
          if (obj.Locked)
            continue;

          if (obj is IGH_PreviewObject previewObject)
          {
            if (previewObject.IsPreviewCapable)
            {
              primitivesBoundingBox = Rhino.Geometry.BoundingBox.Union(primitivesBoundingBox, previewObject.ClippingBox);

              if (obj is IGH_Component component)
              {
                foreach (var param in component.Params.Output)
                {
                  if(param is IGH_PreviewObject preview)
                    DrawData(param.VolatileData, param);
                }
              }
              else if (obj is IGH_Param param)
              {
                DrawData(param.VolatileData, param);
              }
            }
          }
        }
      }

      return primitivesBoundingBox;
    }

    public override DB.Outline GetBoundingBox(DB.View dBView) => primitivesBoundingBox.ToOutline();

    public override void RenderScene(DB.View dBView, DB.DisplayStyle displayStyle)
    {
      try
      {
        if (!BuildScene(dBView).IsValid)
          return;

        DB3D.DrawContext.SetWorldTransform(DB.Transform.Identity.ScaleBasis(UnitConverter.ToHostUnits));

        var CropBox = dBView.CropBox.ToBoundingBox();

        foreach (var primitive in primitives)
        {
          if (DB3D.DrawContext.IsInterrupted())
            break;

          if (dBView.CropBoxActive && !Rhino.Geometry.BoundingBox.Intersection(CropBox, primitive.ClippingBox).IsValid)
            continue;

          primitive.Draw(displayStyle);
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
  public class PreviewServer
  {
    public void Register() { }

    public void Unregister() { }
  }
}
#endif

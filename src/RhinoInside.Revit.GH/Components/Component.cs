using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class GH_Component : Grasshopper.Kernel.GH_Component
  {
    protected GH_Component(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    // Grasshopper default implementation has a bug, it checks inputs instead of outputs
    public override bool IsBakeCapable => Params?.Output.OfType<IGH_BakeAwareObject>().Where(x => x.IsBakeCapable).Any() ?? false;

    protected override Bitmap Icon => ((Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
                                      ImageBuilder.BuildIcon(IconTag, Properties.Resources.UnknownIcon);

    protected virtual string IconTag => GetType().Name.Substring(0, 1);
  }

  public abstract class Component : GH_Component, Kernel.IGH_ElementIdComponent
  {
    protected Component(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory)
    {
      if (Obsolete)
      {
        foreach (var obsolete in GetType().GetCustomAttributes(typeof(ObsoleteAttribute), false).Cast<ObsoleteAttribute>())
        {
          if(!string.IsNullOrEmpty(obsolete.Message))
            Description = obsolete.Message + Environment.NewLine + Description;
        }
      }
    }

    static string[] keywords = new string[] { "Revit" };
    public override IEnumerable<string> Keywords => base.Keywords is null ? keywords : Enumerable.Concat(base.Keywords, keywords);

    #region IGH_ActiveObject
    Exception unhandledException;
    protected bool IsAborted => unhandledException is object;
    protected virtual bool AbortOnUnhandledException => false;
    public override sealed void ComputeData()
    {
      Rhinoceros.InvokeInHostContext(() => base.ComputeData());

      if (unhandledException is object)
      {
        unhandledException = default;
        ResetData();
      }
    }

    protected virtual void ResetData()
    {
      Phase = GH_SolutionPhase.Failed;

      foreach (var param in Params.Output)
      {
        param.ClearData();
        param.Phase = GH_SolutionPhase.Failed;
      }
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      try
      {
        TrySolveInstance(DA);
      }
      catch (Exceptions.CancelException e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
      }
      catch (Exceptions.FailException e)
      {
        OnPingDocument()?.RequestAbortSolution();

        unhandledException = e;
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
      }
      catch (Autodesk.Revit.Exceptions.ArgumentOutOfRangeException e)
      {
        if (AbortOnUnhandledException)
          unhandledException = e;

        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.ParamName} value is out of range");
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException e)
      {
        if (AbortOnUnhandledException)
          unhandledException = e;

        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
      }
      catch (System.MissingMemberException e)
      {
        unhandledException = e;

        if (e.Message.Contains("Autodesk.Revit.DB."))
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: Please consider update Revit to the latest revision.{Environment.NewLine}{e.Message.TripleDot(128)}");
        else
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
      }
      catch (System.Exception e)
      {
        if (AbortOnUnhandledException)
          unhandledException = e;

        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
      }

      if (unhandledException is object)
      {
        DA.AbortComponentSolution();
        Phase = GH_SolutionPhase.Failed;
      }
    }
    protected abstract void TrySolveInstance(IGH_DataAccess DA);
    #endregion

    #region IGH_PreviewObject
    public override Rhino.Geometry.BoundingBox ClippingBox
    {
      get
      {
        var clippingBox = Rhino.Geometry.BoundingBox.Empty;

        foreach (var param in Params)
        {
          if (param.SourceCount > 0)
            continue;

          if (param is IGH_PreviewObject previewObject)
          {
            if (!previewObject.Hidden && previewObject.IsPreviewCapable)
              clippingBox.Union(previewObject.ClippingBox);
          }
        }

        return clippingBox;
      }
    }
    #endregion

    #region AddGeometryConversionError
    readonly List<Rhino.Geometry.GeometryBase> GeometryConversionErrors = new List<Rhino.Geometry.GeometryBase>();
    public override void ClearData()
    {
      base.ClearData();
      GeometryConversionErrors.Clear();
    }

    public void AddGeometryConversionError(GH_RuntimeMessageLevel level, string text, Rhino.Geometry.GeometryBase geometry)
    {
      AddRuntimeMessage(level, text);
      if (geometry is object) GeometryConversionErrors.Add(geometry.InRhinoUnits());
    }

    public override void DrawViewportWires(IGH_PreviewArgs args)
    {
      base.DrawViewportWires(args);

      foreach (var geometry in GeometryConversionErrors)
      {
        switch (geometry)
        {
          case Rhino.Geometry.Point point:
            args.Display.DrawPoint(point.Location, System.Drawing.Color.Orange);
            break;
          case Rhino.Geometry.Curve curve:
            args.Display.DrawCurve(curve, System.Drawing.Color.Orange, args.DefaultCurveThickness * 8);
            args.Display.DrawPoint(curve.PointAtStart, System.Drawing.Color.Orange);
            args.Display.DrawPoint(curve.PointAtEnd, System.Drawing.Color.Orange);
            break;
        }
      }
    }
    #endregion

    #region IGH_ElementIdComponent
    protected virtual DB.ElementFilter ElementFilter { get; }
    public virtual bool NeedsToBeExpired(DB.Events.DocumentChangedEventArgs e)
    {
      var persistentInputs = Params.Input.
        Where(x => x.DataType == GH_ParamData.local).
        OfType<Kernel.IGH_ElementIdParam>();

      if (persistentInputs.Any())
      {
        var filter = ElementFilter;

        var modified = filter is null ? e.GetModifiedElementIds() : e.GetModifiedElementIds(filter);
        var deleted = e.GetDeletedElementIds();

        if (modified.Count > 0 || deleted.Count > 0)
        {
          var document = e.GetDocument();
          var empty = new DB.ElementId[0];

          foreach (var param in persistentInputs)
          {
            if (param.NeedsToBeExpired(document, empty, deleted, modified))
              return true;
          }
        }
      }

      return false;
    }
    #endregion
  }
}

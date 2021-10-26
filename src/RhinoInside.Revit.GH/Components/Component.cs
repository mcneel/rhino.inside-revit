using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Exceptions
{
  /// <summary>
  /// The exception that is thrown when a non-fatal remark occurs.
  /// The current operation is canceled but what is already committed remains valid.
  /// </summary>
  /// <remarks>
  /// If it is catched inside a loop is safe to continue looping over the rest of elements.
  /// </remarks>
  class RuntimeArgumentException : Exception
  {
    readonly string paramName = "";
    public virtual string ParamName => paramName;

    public RuntimeArgumentException() : this(string.Empty, string.Empty) { }
    public RuntimeArgumentException(string paramName) : this(string.Empty, paramName) { }
    public RuntimeArgumentException(string paramName, string message) : base(message)
    {
      this.paramName = paramName;
    }
  }
  class RuntimeArgumentNullException : RuntimeArgumentException
  {
    public RuntimeArgumentNullException() : base(string.Empty, string.Empty) { }
    public RuntimeArgumentNullException(string paramName) : base(paramName, string.Empty) { }
    public RuntimeArgumentNullException(string paramName, string message) : base(paramName, message) { }
  }

  /// <summary>
  /// The exception that is thrown when a non-fatal warning occurs.
  /// The current operation is canceled but what is already committed remains valid.
  /// </summary>
  /// <remarks>
  /// If it is catched inside a loop is safe to continue looping over the rest of elements.
  /// </remarks>
  public class RuntimeWarningException : Exception
  {
    public RuntimeWarningException() : base(string.Empty) { }
    public RuntimeWarningException(string message) : base(message) { }
    public RuntimeWarningException(string message, Exception inner) : base(message, inner)
    {
    }
  }

  /// <summary>
  /// The exception that is thrown when a non-fatal error occurs.
  /// The current operation is canceled but what is already committed remains valid.
  /// </summary>
  /// <remarks>
  /// If it is catched inside a loop is safe to continue looping over the rest of elements.
  /// </remarks>
  public class RuntimeErrorException : Exception
  {
    public RuntimeErrorException() : base(string.Empty) { }
    public RuntimeErrorException(string message) : base(message) { }
    public RuntimeErrorException(string message, Exception inner) : base(message, inner)
    {
    }
  }
}

namespace RhinoInside.Revit.GH.Components
{
  using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;
  using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;

  [EditorBrowsable(EditorBrowsableState.Never)]
  public abstract class GH_Component : Grasshopper.Kernel.GH_Component
  {
    protected GH_Component(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    // Grasshopper default implementation has a bug, it checks inputs instead of outputs
    public override bool IsBakeCapable => Params?.Output.OfType<IGH_BakeAwareObject>().Where(x => x.IsBakeCapable).Any() ?? false;

    protected override Bitmap Icon => ((Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
                                      ImageBuilder.BuildIcon(IconTag, Properties.Resources.UnknownIcon);

    protected virtual string IconTag => GetType().Name.Substring(0, 1);

#if DEBUG
    // Placeholder for breakpoints in DEBUG
    public override void AddRuntimeMessage(GH_RuntimeMessageLevel level, string text) =>
      base.AddRuntimeMessage(level, text);
#endif
  }

  [ComponentVersion(since: "1.0", updated: "1.3")]
  public abstract class Component : GH_Component, Kernel.IGH_ElementIdComponent
  {
    protected Component(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory)
    {
      ComponentVersion = CurrentVersion;

      if (Obsolete)
      {
        foreach (var obsolete in GetType().GetCustomAttributes(typeof(ObsoleteAttribute), false).Cast<ObsoleteAttribute>())
        {
          if (!string.IsNullOrEmpty(obsolete.Message))
            Description = obsolete.Message + Environment.NewLine + Description;
        }
      }
    }

    static readonly string[] keywords = new string[] { "Revit" };
    public override IEnumerable<string> Keywords => base.Keywords is null ? keywords : Enumerable.Concat(base.Keywords, keywords);

    #region IO
    private Version CurrentVersion
    {
      get
      {
        var maxVersion = ComponentVersionAttribute.GetTypeVersionCurrentVersion(GetType());

        // If an input parameter is been modified this updates the component version
        foreach (var input in Params.Input)
        {
          var version = ComponentVersionAttribute.GetTypeVersionCurrentVersion(input.GetType());
          if (version > maxVersion) maxVersion = version;
        }

        // If an output parameter is been modified this updates the component version
        foreach (var output in Params.Output)
        {
          var version = ComponentVersionAttribute.GetTypeVersionCurrentVersion(output.GetType());
          if (version > maxVersion) maxVersion = version;
        }

        return maxVersion;
      }
    }

    protected internal Version ComponentVersion { get; private set; }

    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      string version = "0.0.0.0";
      reader.TryGetString("ComponentVersion", ref version);
      ComponentVersion = Version.TryParse(version, out var componentVersion) ?
        componentVersion : new Version(0, 0, 0, 0);

      if (ComponentVersion > CurrentVersion)
      {
        var assemblyName = new AssemblyInfo().Name;
        reader.AddMessage
        (
          $"Component '{Name}' was saved with a newer version." + Environment.NewLine +
          "Some information may be lost" + Environment.NewLine +
          $"Please update '{assemblyName}' to version {ComponentVersion} or above.",
          GH_Message_Type.warning
        );
      }

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      writer.SetString("ComponentVersion", CurrentVersion.ToString());

      return true;
    }
    #endregion

    #region IGH_ActiveObject
    Exception unhandledException;
    protected bool IsAborted => unhandledException is object;
    protected virtual bool AbortOnUnhandledException => false;

    static Component ComputingComponent;
    public sealed override void ComputeData()
    {
      var current = ComputingComponent;
      ComputingComponent = this;
      try
      {
        Rhinoceros.InvokeInHostContext(() => base.ComputeData());

        if (unhandledException is object)
        {
          unhandledException = default;
          ResetData();
        }
      }
      finally { ComputingComponent = current; }
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
      catch (Exceptions.RuntimeArgumentNullException e)
      {
        // Grasshopper components use to send a Null when
        // they receive a Null without throwing any error
      }
      catch (Exceptions.RuntimeArgumentException e)
      {
        if (AbortOnUnhandledException)
          unhandledException = e;

        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
      }
      catch (Exceptions.RuntimeWarningException e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, e.Message);
      }
      catch (Exceptions.RuntimeErrorException e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
      }
      catch (RhinoInside.Revit.Exceptions.CancelException e)
      {
        // This will abort component solution
        unhandledException = e;
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
      }
      catch (RhinoInside.Revit.Exceptions.FailException e)
      {
        // This will abort entire solution
        OnPingDocument()?.RequestAbortSolution();

        unhandledException = e;
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
      }
      catch (Autodesk.Revit.Exceptions.ArgumentOutOfRangeException e)
      {
        if (AbortOnUnhandledException)
          unhandledException = e;

        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: Value is out of range");
      }
      catch (Autodesk.Revit.Exceptions.ArgumentException e)
      {
        if (AbortOnUnhandledException)
          unhandledException = e;

        var message = e.Message.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)[0];
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {message}");
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

    #region AddGeometryRuntimeError
    readonly List<(Rhino.Geometry.GeometryBase geometry, Rhino.Geometry.BoundingBox bbox)> RuntimeErrorGeometry = new List<(Rhino.Geometry.GeometryBase, Rhino.Geometry.BoundingBox)>();

    public override void ClearData()
    {
      base.ClearData();
      RuntimeErrorGeometry.Clear();
    }

    public void AddGeometryConversionError(GH_RuntimeMessageLevel level, string text, Rhino.Geometry.GeometryBase geometry)
    {
#if DEBUG
      switch (geometry)
      {
        case Rhino.Geometry.BrepVertex vertex:
          text = $"* {text}{Environment.NewLine}Vertex Index = {vertex.VertexIndex} Location = {vertex.Location * Revit.ModelUnits}.";
          break;
        case Rhino.Geometry.BrepEdge edge:
          text = $"◉ {text}{Environment.NewLine}Edge Index = {edge.EdgeIndex} Tolerance {edge.Tolerance * Revit.ModelUnits} Length = {edge.GetLength(edge.Domain) * Revit.ModelUnits}.";
          break;
        case Rhino.Geometry.BrepFace face:
          var mass = Rhino.Geometry.AreaMassProperties.Compute(face);
          text = $"◼ {text}{Environment.NewLine}Face Index = {face.FaceIndex} Surface Area = {mass?.Area * Revit.ModelUnits * Revit.ModelUnits}.";
          break;
      }
#endif
      AddGeometryRuntimeError(level, text, geometry?.InRhinoUnits());
    }

    public void AddGeometryRuntimeError(GH_RuntimeMessageLevel level, string text, Rhino.Geometry.GeometryBase geometry)
    {
      if (text is object) AddRuntimeMessage(level, text);
      if (geometry is object) RuntimeErrorGeometry.Add((geometry, geometry.GetBoundingBox(false)));
    }

    public override void DrawViewportWires(IGH_PreviewArgs args)
    {
      base.DrawViewportWires(args);

      var viewport = args.Display.Viewport;

      /// To speed up display in case there are a lot of errors,
      /// something that may happen with dense meshes,
      /// we avoid drawing while in dynamic display.
      if (!Attributes.Selected && args.Display.IsDynamicDisplay)
        return;

      var dpi = args.Display.DpiScale;
      var pointRadius = 2.0f * args.Display.DisplayPipelineAttributes.PointRadius * dpi;
      var curveThickness = (int) Math.Round(1.5f * args.DefaultCurveThickness * dpi, MidpointRounding.AwayFromZero);

      var index = 0;
      foreach (var error in RuntimeErrorGeometry)
      {
        index++;
        // Skip geometry outside the viewport
        if (!viewport.IsVisible(error.bbox))
          continue;

        var center = error.bbox.Center;
        if (viewport.GetWorldToScreenScale(center, out var pixelsPerUnits))
        {
          // If geometry is smaller than a point diameter we show it as a point
          if (error.bbox.Diagonal.Length * pixelsPerUnits < pointRadius * 2.0)
          {
            args.Display.DrawPoint(center, Rhino.Display.PointStyle.RoundControlPoint, Color.Orange, Color.White, pointRadius, dpi, pointRadius * 0.5f, 0.0f, true, false);
          }
          else
          {
            switch (error.geometry)
            {
              case Rhino.Geometry.Point point:
                args.Display.DrawPoint(point.Location, Rhino.Display.PointStyle.X, Color.Orange, Color.White, pointRadius, dpi, pointRadius * 0.5f, 0.0f, true, false);
                break;
              case Rhino.Geometry.Curve curve:
                args.Display.DrawCurve(curve, Color.Orange, curveThickness);
                break;
              case Rhino.Geometry.Surface surface:
                args.Display.DrawSurface(surface, Color.Orange, curveThickness);
                break;
              case Rhino.Geometry.Brep brep:
                args.Display.DrawBrepWires(brep, Color.Orange, curveThickness);
                break;
            }
          }
        }
      }
    }
    #endregion

    #region IGH_ElementIdComponent
    static readonly DB.ElementId[] EmptyElementIds = new DB.ElementId[0];
    public virtual bool NeedsToBeExpired
    (
      DB.Document document,
      ICollection<DB.ElementId> added,
      ICollection<DB.ElementId> deleted,
      ICollection<DB.ElementId> modified
    )
    {
      // Changes made by this should not expire this.
      if (ReferenceEquals(ComputingComponent, this)) return false;

      // Only Query-Collector components need to be expired when something is added.
      if (modified.Count > 0 || deleted.Count > 0)
      {
        // Only inputs with persitent data or outputs are considered source of data.
        var persistentInputs = Params.Input.
          Where(x => x.DataType == GH_ParamData.local).
          OfType<Kernel.IGH_ElementIdParam>();

        // Check inputs
        foreach (var param in persistentInputs)
        {
          if (param.NeedsToBeExpired(document, EmptyElementIds, deleted, modified))
            return true;
        }

        // Check outputs
        foreach (var output in Params.Output.OfType<Kernel.IGH_ElementIdParam>())
        {
          if (output.NeedsToBeExpired(document, EmptyElementIds, deleted, modified))
            return true;
        }
      }

      return false;
    }
    #endregion
  }

  [EditorBrowsable(EditorBrowsableState.Never)]
  public abstract class ComponentUpgrader : IGH_UpgradeObject
  {
    public abstract DateTime Version { get; }
    public abstract Guid UpgradeFrom { get; }
    public abstract Guid UpgradeTo { get; }

    public virtual IReadOnlyDictionary<string, string> GetInputAliases(IGH_Component source) => default;
    public virtual IReadOnlyDictionary<string, string> GetOutputAliases(IGH_Component source) => default;

    public virtual IGH_DocumentObject Upgrade(IGH_DocumentObject docObject, GH_Document document)
    {
      if (docObject is IGH_Component source && Grasshopper.Instances.ComponentServer.EmitObject(UpgradeTo) is IGH_Component target)
      {
        int master = -1;

        var inputAliases = GetInputAliases(source);
        var input_index = -1;
        foreach (var input in source.Params.Input)
        {
          input_index++;

          var inputName = input.Name;
          if (inputAliases?.TryGetValue(input.Name, out inputName) == true && string.IsNullOrEmpty(inputName))
            continue;

          var index = target.Params.IndexOfInputParam(inputName ?? input.Name);
          if (index >= 0)
          {
            if (input_index == source.MasterParameterIndex)
              master = index;

            var target_parameter = target.Params.Input[index];

            target_parameter.WireDisplay = input.WireDisplay;
            target_parameter.DataMapping = input.DataMapping;
            target_parameter.Reverse = input.Reverse;
            target_parameter.Simplify = input.Simplify;

            // Implementation relay on Optional & Access
            //target_parameter.Optional = input.Optional;
            //target_parameter.Access = input.Access;

            GH_UpgradeUtil.MigrateSources(input, target_parameter);
            target_parameter.NewInstanceGuid(input.InstanceGuid);
          }
        }

        var outputAliases = GetOutputAliases(source);
        foreach (var output in source.Params.Output)
        {
          var outputName = output.Name;
          if (outputAliases?.TryGetValue(output.Name, out outputName) == true && string.IsNullOrEmpty(outputName))
            continue;

          var index = target.Params.IndexOfOutputParam(outputName ?? output.Name);
          if (index >= 0)
          {
            var target_parameter = target.Params.Output[index];

            target_parameter.WireDisplay = output.WireDisplay;
            target_parameter.DataMapping = output.DataMapping;
            target_parameter.Reverse = output.Reverse;
            target_parameter.Simplify = output.Simplify;

            // Implementation relay on Optional & Access
            //target_parameter.Optional = input.Optional;
            //target_parameter.Access = input.Access;

            GH_UpgradeUtil.MigrateRecipients(output, target_parameter);
            target_parameter.NewInstanceGuid(output.InstanceGuid);
          }
        }

        target.IconDisplayMode = source.IconDisplayMode;
        target.MasterParameterIndex = master;
        target.Locked = source.Locked;
        target.Hidden = source.Hidden;

        if (GH_UpgradeUtil.SwapComponents(source, target, false))
          return target;
      }

      return default;
    }

    public static void SolveInstance(IGH_Component component, IGH_DataAccess DA)
    {
      component.AddRuntimeMessage
      (
        GH_RuntimeMessageLevel.Error,
        "This component is Obsolete." + Environment.NewLine +
        "Please use 'Solution > Upgrade Components…' from Grasshopper menu."
      );

      DA.AbortComponentSolution();
    }
  }
}

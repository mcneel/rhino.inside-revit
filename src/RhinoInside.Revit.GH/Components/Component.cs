using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
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

  [ComponentVersion(introduced: "0.0", updated: "1.3")]
  public abstract class Component : GH_Component, Kernel.IGH_ElementIdComponent, IGH_RuntimeContract
  {
    protected Component(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory)
    {
      ComponentVersion = CurrentVersion;

      ComponentVersionAttribute.GetVersionHistory(GetType(), out var _, out var _, out var deprecated);
      obsolete = Obsolete || deprecated is object;
    }

    #if DEBUG
    public override string InstanceDescription =>
      $"{base.InstanceDescription}{Environment.NewLine}{VersionDescription}";

    string VersionDescription
    {
      get
      {
        ComponentVersionAttribute.GetVersionHistory(GetType(), out var introduced, out var _, out var deprecated);

        var versionDescription = string.Empty;

        if (introduced is object)
          versionDescription += $"Introduced in v{introduced}" + Environment.NewLine;

        if (Obsolete)
        {
          if (deprecated is object)
            versionDescription += $"Obsolete since v{deprecated}" + Environment.NewLine;

          foreach (var attribute in GetType().GetCustomAttributes(typeof(ObsoleteAttribute), false).Cast<ObsoleteAttribute>())
          {
            if (!string.IsNullOrWhiteSpace(attribute.Message))
              versionDescription += attribute.Message + Environment.NewLine;
          }
        }

        return versionDescription;
      }
    }
    #endif

    #region Obsolete
    readonly bool? obsolete;
    public override bool Obsolete => obsolete.GetValueOrDefault(base.Obsolete);
    #endregion

    static readonly string[] keywords = new string[] { "Revit" };
    public override IEnumerable<string> Keywords => base.Keywords is null ? keywords : Enumerable.Concat(base.Keywords, keywords);

    #region IO
    private Version CurrentVersion
    {
      get
      {
        var current = ComponentVersionAttribute.GetCurrentVersion(GetType());

        // If an input parameter is been modified this updates the component version
        foreach (var input in Params.Input)
        {
          var version = ComponentVersionAttribute.GetCurrentVersion(input.GetType());
          if (version > current) current = version;
        }

        // If an output parameter is been modified this updates the component version
        foreach (var output in Params.Output)
        {
          var version = ComponentVersionAttribute.GetCurrentVersion(output.GetType());
          if (version > current) current = version;
        }

        return current;
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
    protected virtual bool AbortOnContinuableException => false;

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
      catch (System.Exception e)
      {
        if (!TryCatchException(DA, e))
        {
          ResetData();
          DA.AbortComponentSolution();
          Phase = GH_SolutionPhase.Failed;
        }
      }
    }

    protected abstract void TrySolveInstance(IGH_DataAccess DA);
    protected virtual bool TryCatchException(IGH_DataAccess DA, Exception e)
    {
      switch(e)
      {
        case Exceptions.RuntimeArgumentNullException _:
          // Grasshopper components use to send a Null when
          // they receive a Null without throwing any error
          return true;

        case Exceptions.RuntimeArgumentException argument:
          if (!AbortOnContinuableException)
          {
            AddGeometryRuntimeError(GH_RuntimeMessageLevel.Warning, argument.Message, argument.Value as Rhino.Geometry.GeometryBase);
            return true;
          }

          AddGeometryRuntimeError(GH_RuntimeMessageLevel.Error, argument.Message, argument.Value as Rhino.Geometry.GeometryBase);
          break;

        case Exceptions.RuntimeException _:
          if (!AbortOnContinuableException)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, e.Message);
            return true;
          }

          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
          break;

        case Exceptions.RuntimeWarningException _:
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, e.Message);
          break;

        case Exceptions.RuntimeErrorException _:
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
          break;

        case Autodesk.Revit.Exceptions.ArgumentOutOfRangeException _:
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: Value is out of range");
          break;

        case Autodesk.Revit.Exceptions.ArgumentException _:
          var message = e.Message.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)[0];
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {message}");
          break;

        case Autodesk.Revit.Exceptions.ApplicationException _:
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
          break;

        case System.MissingMemberException _:
          if (e.Message.Contains("Autodesk.Revit.DB."))
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: Please consider update Revit to the latest revision.{Environment.NewLine}{e.Message.TripleDot(128)}");
          else
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
          break;

        case System.Exception _:
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
          break;
      }

      return false;
    }
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
    static readonly ARDB.ElementId[] EmptyElementIds = new ARDB.ElementId[0];
    public virtual bool NeedsToBeExpired
    (
      ARDB.Document document,
      ICollection<ARDB.ElementId> added,
      ICollection<ARDB.ElementId> deleted,
      ICollection<ARDB.ElementId> modified
    )
    {
      // Only Query-Collector components need to be expired when something is added.
      if (modified.Count > 0 || deleted.Count > 0)
      {
        // Only inputs with persitent data are considered source of data.
        var persistentInputs = Params.Input.
          Where(x => x.DataType == GH_ParamData.local).
          OfType<Kernel.IGH_ElementIdParam>();

        // Check inputs
        foreach (var param in persistentInputs)
        {
          if (param.NeedsToBeExpired(document, EmptyElementIds, deleted, modified))
            return true;
        }
      }

      return false;
    }
    #endregion

    #region IGH_RuntimeContract
    public virtual bool RequiresFailed
    (
      IGH_DataAccess access, int index, object value,
      string message
    )
    {
      var failureMessage = $"Input parameter '{Params.Input[index].NickName}' collected invalid data.";

      if (value is Rhino.Geometry.GeometryBase geometry)
      {
        if (string.IsNullOrWhiteSpace(message))
        {
          // If we have no message try to get a more accurate reason
          if (!geometry.IsValidWithLog(out message))
            failureMessage += Environment.NewLine + message;
        }
        else failureMessage += Environment.NewLine + message;
      }
      else
      {
        if (!string.IsNullOrWhiteSpace(message))
          failureMessage += Environment.NewLine + message;
      }

      throw new Exceptions.RuntimeArgumentException(Params.Input[index].Name, failureMessage, value);
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

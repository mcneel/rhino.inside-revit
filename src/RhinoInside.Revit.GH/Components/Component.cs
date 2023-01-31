using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;
using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;
using SD = System.Drawing;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [EditorBrowsable(EditorBrowsableState.Never)]
  public abstract class GH_Component : Grasshopper.Kernel.GH_Component
  {
    protected GH_Component(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected override SD.Bitmap Icon =>
      ((SD.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
      ImageBuilder.BuildIcon(IconTag, Properties.Resources.UnknownIcon);

    protected virtual string IconTag => GetType().Name.Substring(0, 1);

    #region IGH_PreviewObject
    // Grasshopper default implementation does not take into Hidden property and produce a bigger than necessary ClippingBox.
    public override BoundingBox ClippingBox => Hidden ? BoundingBox.Empty : base.ClippingBox;
    #endregion

    #region IGH_BakeAwareObject
    // Grasshopper default implementation has a bug, it checks inputs instead of outputs
    public override bool IsBakeCapable => Params?.Output.OfType<IGH_BakeAwareObject>().Any(x => x.IsBakeCapable) ?? false;

    public override bool AppendMenuItems(ToolStripDropDown menu)
    {
      if (!base.AppendMenuItems(menu)) return false;

      // Grasshopper default implementation has a bug, and does not check IsBakeCapable.
      if (this is IGH_BakeAwareObject bake && !bake.IsBakeCapable)
      {
        for (int i = 0; i < menu.Items.Count; ++i)
        {
          if (menu.Items[i].Text == "Bake…")
          {
            menu.Items[i].Enabled = false;
            break;
          }
        }
      }

      return true;
    }
    #endregion

#if DEBUG
    // Placeholder for breakpoints in DEBUG
    public override void AddRuntimeMessage(GH_RuntimeMessageLevel level, string text) =>
      base.AddRuntimeMessage(level, text);
#endif
  }

  [ComponentVersion(introduced: "0.0", updated: "1.3")]
  public abstract class Component : GH_Component, Kernel.IGH_ReferenceComponent, IGH_RuntimeContract
  {
    protected Component(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory)
    {
      ComponentVersion = CurrentVersion;

      ComponentVersionAttribute.GetVersionHistory(GetType(), out var _, out var _, out var deprecated);
      _Obsolete = Obsolete || deprecated is object;
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
    readonly bool? _Obsolete;
    public override bool Obsolete => _Obsolete.GetValueOrDefault(base.Obsolete);
    #endregion

    static readonly string[] _Keywords = new string[] { "Revit" };
    public override IEnumerable<string> Keywords => base.Keywords is null ? _Keywords : Enumerable.Concat(base.Keywords, _Keywords);

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
    Exception UnhandledException;
    protected bool IsAborted => UnhandledException is object;
    protected virtual bool AbortOnContinuableException => false;

    static Component ComputingComponent;
    public sealed override void ComputeData()
    {
      var current = ComputingComponent;
      ComputingComponent = this;
      try
      {
        Rhinoceros.InvokeInHostContext(() => base.ComputeData());

        if (UnhandledException is object)
        {
          UnhandledException = default;
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
          UnhandledException = e;
        }
      }
    }

    protected abstract void TrySolveInstance(IGH_DataAccess DA);
    protected virtual bool TryCatchException(IGH_DataAccess DA, Exception e)
    {
      for (int o = 0; o < Params.Output.Count; ++o)
      {
        switch (Params.Output[o].Access)
        {
          case GH_ParamAccess.item: DA.SetData(o, default); break;
          case GH_ParamAccess.list: DA.SetDataList(o, default); break;
          case GH_ParamAccess.tree: Params.Output[o].AddVolatileDataList(DA.ParameterTargetPath(o).AppendElement(DA.ParameterTargetIndex(o)).AppendElement(0), default); break;
        }
      }

      switch (e)
      {
        case Exceptions.RuntimeArgumentNullException _:
          // Grasshopper components use to send a Null when
          // they receive a Null without throwing any error
          return true;

        case Exceptions.RuntimeArgumentException argument:
          if (!AbortOnContinuableException)
          {
            switch (argument.Value)
            {
              case ARDB.Element element:
              {
                if (Types.GraphicalElement.FromElement(element) is Types.GraphicalElement ge)
                {
                  var inch = Revit.ModelUnits / 12.0;
                  var box = ge.Box; box.Inflate(inch, inch, inch);
                  var mesh = Rhino.Geometry.Mesh.CreateFromBox(box, 1, 1, 1);
                  AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, argument.Message, mesh);
                }
                else
                {
                  AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, argument.Message);
                }
                break;
              }
              case Types.GraphicalElement graphicalElement:
              {
                var inch = Revit.ModelUnits / 12.0;
                var box = graphicalElement.Box; box.Inflate(inch, inch, inch);
                var mesh = Rhino.Geometry.Mesh.CreateFromBox(box, 1, 1, 1);
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, argument.Message, mesh);
                break;
              }
              case Rhino.Geometry.GeometryBase geometry:
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, argument.Message, geometry);
                break;

              case BoundingBox bbox:
              {
                var inch = Revit.ModelUnits / 12.0;
                var box = new Box(bbox); box.Inflate(inch, inch, inch);
                var mesh = Rhino.Geometry.Mesh.CreateFromBox(box, 1, 1, 1);
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, argument.Message, mesh);
                break;
              }

              default:
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, argument.Message, default);
                break;
            }

            return true;
          }

          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, argument.Message, argument.Value as Rhino.Geometry.GeometryBase);
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
          if (e.Source == System.Reflection.Assembly.GetExecutingAssembly().GetName().Name)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)[0]);
          else
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message.Split(new string[] { Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)[0]}");
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
          var assemblyName = GetType().Assembly.GetName().Name;
          if (e.Source == assemblyName)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
          else
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
          break;
      }

      return false;
    }

    public override void CollectData()
    {
      base.CollectData();

      const double DesignLimits = 52_800.0; // feet.
      var LengthLimit = GeometryDecoder.ToModelLength(DesignLimits); 

      foreach (var input in Params.Input.Where(x => x is IGH_BakeAwareObject))
      {
        var reported = false;
        var data = input.VolatileData;
        foreach (var path in data.Paths)
        {
          var branch = data.get_Branch(path);
          for (int i = 0; i < branch.Count; i++)
          {
            if (branch[i] is IGH_GeometricGoo goo)
            {
              var bbox = goo.Boundingbox;
              if (bbox.Min.DistanceTo(Point3d.Origin) > LengthLimit || bbox.Max.DistanceTo(Point3d.Origin) > LengthLimit)
              {
                if (!reported) AddRuntimeMessage
                (
                  GH_RuntimeMessageLevel.Warning,
                  $"The input {input.NickName} lies outside of Revit design limits." +
                  $" Design limits are ±{GeometryDecoder.ToModelLength(DesignLimits):N0} {GH_Format.RhinoUnitSymbol()} around the origin.",
                  GH_Convert.ToGeometryBase(goo)
                );
              }
            }
          }
        }
      }
    }
    #endregion

    #region AddGeometryRuntimeError
    readonly List<(GH_RuntimeMessageLevel Level, GeometryBase Geometry, BoundingBox BoundingBox)> RuntimeGeometry = new List<(GH_RuntimeMessageLevel, GeometryBase, BoundingBox)>();

    public override void ClearData()
    {
      base.ClearData();
      RuntimeGeometry.Clear();
    }

    public void AddGeometryConversionError(GH_RuntimeMessageLevel level, string text, GeometryBase geometry)
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
      AddRuntimeMessage(level, text, geometry?.InRhinoUnits());
    }

    public void AddRuntimeMessage(GH_RuntimeMessageLevel level, string text, GeometryBase geometry)
    {
      if (text is object) AddRuntimeMessage(level, text);
      if (geometry is object) RuntimeGeometry.Add((level, geometry, geometry.GetBoundingBox(false)));
    }

    public override void DrawViewportWires(IGH_PreviewArgs args)
    {
      base.DrawViewportWires(args);

      /// To speed up display in case there are a lot of errors,
      /// something that may happen with dense meshes,
      /// we avoid drawing while in dynamic display.
      if (!Attributes.Selected && args.Display.IsDynamicDisplay)
        return;

      var viewport = args.Display.Viewport;
      var dpi = args.Display.DpiScale;
      var pointRadius = 2.0f * args.Display.DisplayPipelineAttributes.PointRadius * dpi;
      var curveThickness = (int) Math.Round(1.5f * args.DefaultCurveThickness * dpi, MidpointRounding.AwayFromZero);

      foreach (var error in RuntimeGeometry)
      {
        // Skip geometry outside the viewport
        if (!viewport.IsVisible(error.BoundingBox))
          continue;

        var center = error.BoundingBox.Center;
        if (viewport.GetWorldToScreenScale(center, out var pixelsPerUnits))
        {
          var color = Attributes.Selected ? args.WireColour_Selected : args.WireColour;
          switch (error.Level)
          {
            case GH_RuntimeMessageLevel.Blank:    color = SD.Color.Black; break;
            case GH_RuntimeMessageLevel.Remark:   color = Attributes.Selected ? args.WireColour_Selected : args.WireColour; break;
            case GH_RuntimeMessageLevel.Warning:  color = SD.Color.Orange; break;
            case GH_RuntimeMessageLevel.Error:    color = SD.Color.HotPink; break;
          }

          // If geometry is smaller than a point diameter we show it as a point
          if (error.BoundingBox.Diagonal.Length * pixelsPerUnits < pointRadius * 2.0)
          {
            args.Display.DrawPoint(center, Rhino.Display.PointStyle.RoundControlPoint, color, SD.Color.White, pointRadius, dpi, pointRadius * 0.5f, 0.0f, true, false);
          }
          else switch (error.Geometry)
          {
            case Point point:               args.Display.DrawPoint(point.Location, Rhino.Display.PointStyle.X, color, SD.Color.White, pointRadius, dpi, pointRadius * 0.5f, 0.0f, true, false); break;
            case Curve curve:               args.Display.DrawCurve(curve, color, curveThickness); break;
            case Surface surface:           args.Display.DrawSurface(surface, color, curveThickness); break;
            case Brep brep:                 args.Display.DrawBrepWires(brep, color, curveThickness); break;
            case Mesh mesh:                 args.Display.DrawMeshWires(mesh, color, curveThickness); break;
            case AnnotationBase annotation: args.Display.DrawAnnotation(annotation, color); break;
          }
        }
      }
    }

    public override bool IsPreviewCapable => RuntimeGeometry.Count > 0 || base.IsPreviewCapable;
    public override BoundingBox ClippingBox => base.ClippingBox;
    #endregion

    #region IGH_ReferenceComponent
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
          OfType<Kernel.IGH_ReferenceParam>();

        // Check inputs
        foreach (var param in persistentInputs)
        {
          if (param.NeedsToBeExpired(document, ElementIdExtension.EmptyCollection, deleted, modified))
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

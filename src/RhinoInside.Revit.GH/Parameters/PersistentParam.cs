using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace RhinoInside.Revit.GH.Types
{
  /// <summary>
  /// Implement this interface into your Goo type if you are referencing external resources.
  /// </summary>
  /// <remarks>
  /// <see cref="Parameters.PersistentParam{T}"/> uses it to load-unload external resources.
  /// </remarks>
  interface IGH_ReferenceData
  {
    bool IsReferencedData { get; }
    bool IsReferencedDataLoaded { get; }
    bool LoadReferencedData();
    void UnloadReferencedData();
  }
}

namespace RhinoInside.Revit.GH.Parameters
{
  interface IGH_PersistentStateAwareObject
  {
    bool LoadState(GH_IReader reader);
    bool SaveState(GH_IWriter writer);
    bool ResetState();
  }

  public abstract class PersistentParam<T> : GH_PersistentParam<T>, IGH_InitCodeAware, IGH_PersistentStateAwareObject
    where T : class, IGH_Goo
  {
    protected override /*sealed*/ Bitmap Icon => ((Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
                                              ImageBuilder.BuildIcon(IconTag, Properties.Resources.UnknownIcon);

    protected virtual string IconTag => typeof(T).Name.Substring(0, 1);
    public virtual void SetInitCode(string code) => SetPersistentData(code);

    protected PersistentParam(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory)
    {
      Debug.Assert(GetType().IsPublic, $"{GetType()} is not public, Grasshopper will fail deserializing this type.");

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

    #region IO
    protected virtual Version CurrentVersion => GetType().Assembly.GetName().Version;
    protected Version ComponentVersion { get; private set; }

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
        var assemblyName = Grasshopper.Instances.ComponentServer.FindAssemblyByObject(this)?.Name ?? GetType().Assembly.GetName().Name;
        reader.AddMessage($"Component '{Name}' was saved with a newer version.{Environment.NewLine}Please update '{assemblyName}' to version {ComponentVersion} or above.", GH_Message_Type.error);
      }

      int culling = (int) DataCulling.None;
      reader.TryGetInt32("Culling", ref culling);
      Culling = (DataCulling) culling;

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      RequiresPersistenDataConstructor();

      if (!base.Write(writer))
        return false;

      if (ComponentVersion > CurrentVersion)
        writer.SetString("ComponentVersion", ComponentVersion.ToString());
      else
        writer.SetString("ComponentVersion", CurrentVersion.ToString());

      if (Culling != DataCulling.None)
        writer.SetInt32("Culling", (int) Culling);

      return true;
    }

    [Conditional("DEBUG")]
    void RequiresPersistenDataConstructor()
    {
      var set = new HashSet<string>();
      var errors = new List<string>();
      foreach (var goo in PersistentData.NonNulls)
      {
        if (goo.GetType().GetConstructor(Type.EmptyTypes) is null)
        {
          var error = $"'{goo.GetType().FullName}' has no public empty constructor.{Environment.NewLine}Parameterless constructor is mandatory for serialization.";
          if (set.Add(error)) errors.Add(error);
        }
      }

      var builder = new System.Text.StringBuilder();
      foreach (var error in errors) builder.AppendLine(error);
      Debug.Assert(errors.Count == 0, builder.ToString());
    }
    #endregion

    #region VolatileData
    [Flags]
    public enum DataCulling
    {
      None = 0,
      Nulls = 1 << 0,
      Invalids = 1 << 1,
      Duplicates = 1 << 2,
      Empty = 1 << 31
    };

    DataCulling culling = DataCulling.None;
    public DataCulling Culling
    {
      get => culling;
      set => culling = value & CullingMask;
    }

    /// <summary>
    /// Culling options depend on capabilities of <see cref="T"/>
    /// </summary>
    static DataCulling CullingMask =>
      (!typeof(T).IsValueType ? DataCulling.Nulls : DataCulling.None) |
      (typeof(IGH_Goo).IsAssignableFrom(typeof(T)) ? DataCulling.Invalids : DataCulling.None) |
      (IsEquatable(typeof(T)) ? DataCulling.Duplicates : DataCulling.None) |
      DataCulling.Empty;

    static bool IsEquatable(Type value) => value?.GetInterfaces().Any
    (
      i =>
      i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>)
    ) == true;

    public override void ClearData()
    {
      base.ClearData();

      if (PersistentData.IsEmpty)
        return;

      foreach (var reference in PersistentData.OfType<Types.IGH_ReferenceData>())
        reference.UnloadReferencedData();
    }

    protected virtual void LoadVolatileData()
    {
      if (DataType != GH_ParamData.local)
        return;

      foreach (var branch in m_data.Branches)
      {
        for (int i = 0; i < branch.Count; i++)
        {
          if (branch[i] is Types.IGH_ReferenceData reference)
          {
            if (reference.IsReferencedData && !reference.LoadReferencedData())
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"A referenced {branch[i].TypeName} could not be found.");
              branch[i] = null;
            }
          }
        }
      }
    }

    protected virtual void PreProcessVolatileData()
    {
      if (Culling != DataCulling.None)
      {
        var data = new GH_Structure<T>();
        var pathCount = m_data.PathCount;
        for (int p = 0; p < pathCount; ++p)
        {
          var path = m_data.Paths[p];
          var branch = m_data.get_Branch(path);

          var items = branch.Cast<T>();
          if (Culling.HasFlag(DataCulling.Nulls))
            items = items.Where(x => x is object);

          if (Culling.HasFlag(DataCulling.Invalids))
            items = items.Where(x => x?.IsValid != false);

          if (Culling.HasFlag(DataCulling.Duplicates))
            items = items.Distinct();

          if (!Culling.HasFlag(DataCulling.Empty) || items.Any())
            data.AppendRange(items, path);
        }

        m_data = data;
      }
    }
    protected virtual void ProcessVolatileData() { }
    protected virtual void PostProcessVolatileData() => base.PostProcessData();

    public sealed override void PostProcessData()
    {
      if (ComponentVersion > CurrentVersion)
      {
        var assemblyName = Grasshopper.Instances.ComponentServer.FindAssemblyByObject(this)?.Name ?? GetType().Assembly.GetName().Name;
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"This component was saved with a newer version.{Environment.NewLine}Please update '{assemblyName}' to version {ComponentVersion} or above.");
        VolatileData.Clear();
        return;
      }

      LoadVolatileData();

      PreProcessVolatileData();

      ProcessVolatileData();

      PostProcessVolatileData();
    }
    #endregion

    #region PersistentData
    protected T PersistentValue
    {
      get
      {
        var value = PersistentData.PathCount == 1 &&
          PersistentData.DataCount == 1 ?
          PersistentData.get_FirstItem(false) :
          default;

        value = (T) value?.Duplicate();

        if (value is Types.IGH_ReferenceData data)
          data.LoadReferencedData();

        return value?.IsValid == true ? value : default;
      }
    }
    #endregion

    #region UI
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendWireDisplay(menu);
      this.Menu_AppendConnect(menu);
      Menu_AppendDisconnectWires(menu);

      Menu_AppendPreProcessParameter(menu);
      Menu_AppendPrincipalParameter(menu);
      Menu_AppendReverseParameter(menu);
      Menu_AppendFlattenParameter(menu);
      Menu_AppendGraftParameter(menu);
      Menu_AppendSimplifyParameter(menu);
      Menu_AppendPostProcessParameter(menu);

      if (Kind == GH_ParamKind.floating || Kind == GH_ParamKind.input)
      {
        Menu_AppendSeparator(menu);
        if (Menu_CustomSingleValueItem() is ToolStripMenuItem single)
        {
          single.Enabled &= SourceCount == 0;
          menu.Items.Add(single);
        }
        else Menu_AppendPromptOne(menu);

        if (Menu_CustomMultiValueItem() is ToolStripMenuItem more)
        {
          more.Enabled &= SourceCount == 0;
          menu.Items.Add(more);
        }
        else Menu_AppendPromptMore(menu);
        Menu_AppendManageCollection(menu);

        Menu_AppendSeparator(menu);
        Menu_AppendDestroyPersistent(menu);
        Menu_AppendInternaliseData(menu);

        if (Exposure != GH_Exposure.hidden)
          Menu_AppendExtractParameter(menu);
      }
    }

    protected virtual void Menu_AppendPreProcessParameter(ToolStripDropDown menu)
    {
      if (Kind != GH_ParamKind.output)
      {
        var Cull = Menu_AppendItem(menu, "Cull");

        Cull.Checked = Culling != DataCulling.None;
        if (CullingMask.HasFlag(DataCulling.Nulls))
          Menu_AppendItem(Cull.DropDown, "Nulls", (s, a) => Menu_Culling(DataCulling.Nulls), true, Culling.HasFlag(DataCulling.Nulls));

        if (CullingMask.HasFlag(DataCulling.Invalids))
          Menu_AppendItem(Cull.DropDown, "Invalids", (s, a) => Menu_Culling(DataCulling.Invalids), true, Culling.HasFlag(DataCulling.Invalids));

        if (CullingMask.HasFlag(DataCulling.Duplicates))
          Menu_AppendItem(Cull.DropDown, "Duplicates", (s, a) => Menu_Culling(DataCulling.Duplicates), true, Culling.HasFlag(DataCulling.Duplicates));

        if (CullingMask.HasFlag(DataCulling.Empty))
          Menu_AppendItem(Cull.DropDown, "Empty", (s, a) => Menu_Culling(DataCulling.Empty), true, Culling.HasFlag(DataCulling.Empty));
      }
    }

    private void Menu_Culling(DataCulling value)
    {
      RecordUndoEvent("Set: Culling");

      if ((Culling & value) != 0)
        Culling &= ~value;
      else
        Culling |= value;

      OnObjectChanged(GH_ObjectEventType.Options);

      if (Kind == GH_ParamKind.output)
        ExpireOwner();

      ExpireSolution(true);
    }

    protected virtual void Menu_AppendPostProcessParameter(ToolStripDropDown menu) { }

    protected override bool Prompt_ManageCollection(GH_Structure<T> values)
    {
      foreach (var item in values.AllData(true))
      {
        if (item.IsValid)
          continue;

        if (item is Types.IGH_ReferenceData reference)
        {
          if (reference.IsReferencedData)
            reference.LoadReferencedData();
        }
      }

      return base.Prompt_ManageCollection(values);
    }
    #endregion

    #region IGH_PersistentStateAwareObject
    bool IGH_PersistentStateAwareObject.SaveState(GH_IWriter writer)
    {
      if (NickName != Name)
        writer.SetString(nameof(NickName), NickName);

      if (MutableNickName != true)
        writer.SetBoolean(nameof(MutableNickName), MutableNickName);

      if (IconDisplayMode != GH_IconDisplayMode.application)
        writer.SetInt32(nameof(IconDisplayMode), (int) IconDisplayMode);

      if (PersistentData.IsEmpty != true)
        PersistentData.Write(writer.CreateChunk(nameof(PersistentData)));

      return true;
    }

    bool IGH_PersistentStateAwareObject.LoadState(GH_IReader reader)
    {
      var nickName = Name;
      reader.TryGetString(nameof(NickName), ref nickName);
      NickName = nickName;

      var mutableNickName = true;
      reader.TryGetBoolean(nameof(MutableNickName), ref mutableNickName);
      MutableNickName = mutableNickName;

      var iconDisplayMode = (int) GH_IconDisplayMode.application;
      reader.TryGetInt32(nameof(IconDisplayMode), ref iconDisplayMode);
      IconDisplayMode = (GH_IconDisplayMode) iconDisplayMode;

      PersistentData.Clear();
      if (reader.FindChunk(nameof(PersistentData)) is GH_Chunk persistentData)
        PersistentData.Read(persistentData);

      ExpireSolution(false);
      return true;
    }

    bool IGH_PersistentStateAwareObject.ResetState()
    {
      PersistentData.Clear();

      ExpireSolution(false);
      return true;
    }
    #endregion
  }
}

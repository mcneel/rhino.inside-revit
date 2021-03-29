using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace RhinoInside.Revit.GH.Parameters
{
  public abstract class PersistentParam<T> : GH_PersistentParam<T>
    where T : class, IGH_Goo
  {
    protected override /*sealed*/ Bitmap Icon => ((Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
                                              ImageBuilder.BuildIcon(IconTag, Properties.Resources.UnknownIcon);

    protected virtual string IconTag => typeof(T).Name.Substring(0, 1);
    public virtual void SetInitCode(string code) => NickName = code;

    protected PersistentParam(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory)
    { }

    [Flags]
    public enum DataCulling
    {
      None = 0,
      Nulls = 1 << 0,
      Invalids = 1 << 1,
      Duplicates = 1 << 2,
    };

    DataCulling culling = DataCulling.None;
    public DataCulling Culling
    {
      get => culling;
      set => culling = value & CullingMask;
    }

    public virtual DataCulling CullingMask =>
      DataCulling.Nulls | DataCulling.Invalids |
      (
        IsEquatable(typeof(T)) ?
        DataCulling.Duplicates :
        DataCulling.None
      );

    static bool IsEquatable(Type value)
    {
      for (var type = value; type is object; type = type.BaseType)
      {
        var IEquatableT = typeof(IEquatable<>).MakeGenericType(type);
        if (IEquatableT.IsAssignableFrom(value))
          return true;
      }

      return false;
    }

    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      int grouping = (int) DataCulling.None;
      reader.TryGetInt32("Culling", ref grouping);
      Culling = (DataCulling) grouping;

      return true;
    }
    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (Culling != DataCulling.None)
        writer.SetInt32("Culling", (int) Culling);

      return true;
    }

    protected virtual void LoadVolatileData() { }
    protected virtual void PreProcessVolatileData()
    {
      if (Culling != DataCulling.None)
      {
        if (Kind == GH_ParamKind.floating)
        {
          if ((Culling & DataCulling.Nulls) != 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Nulls culled");

          if ((Culling & DataCulling.Invalids) != 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Invalids culled");

          if ((Culling & DataCulling.Duplicates) != 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Duplicates culled");
        }

        var data = new GH_Structure<T>();
        var pathCount = m_data.PathCount;
        for (int p = 0; p < pathCount; ++p)
        {
          var path = m_data.Paths[p];
          var branch = m_data.get_Branch(path);

          var items = branch.Cast<object>();
          if ((Culling & DataCulling.Nulls) != 0)
            items = items.Where(x => x != null);

          if ((Culling & DataCulling.Invalids) != 0)
            items = items.Where(x => (x as IGH_Goo)?.IsValid != false);

          if ((Culling & DataCulling.Duplicates) != 0)
            items = items.GroupBy(x => x).Select(x => x.Key);

          foreach (var item in items)
            data.Append((T) item, path);
        }

        m_data = data;
      }
    }
    protected virtual void ProcessVolatileData() { }
    protected virtual void PostProcessVolatileData() => base.PostProcessData();

    public sealed override void PostProcessData()
    {
      LoadVolatileData();

      PreProcessVolatileData();

      ProcessVolatileData();

      PostProcessVolatileData();
    }

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
      var Cull = Menu_AppendItem(menu, "Cull") as ToolStripMenuItem;

      Cull.Checked = Culling != DataCulling.None;
      if ((CullingMask & DataCulling.Nulls) != 0)
        Menu_AppendItem(Cull.DropDown, "Nulls", (s, a) => Menu_Culling(DataCulling.Nulls), true, (Culling & DataCulling.Nulls) != 0);

      if ((CullingMask & DataCulling.Nulls) != 0)
        Menu_AppendItem(Cull.DropDown, "Invalids", (s, a) => Menu_Culling(DataCulling.Invalids), true, (Culling & DataCulling.Invalids) != 0);

      if ((CullingMask & DataCulling.Nulls) != 0)
        Menu_AppendItem(Cull.DropDown, "Duplicates", (s, a) => Menu_Culling(DataCulling.Duplicates), true, (Culling & DataCulling.Duplicates) != 0);
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
    #endregion
  }
}

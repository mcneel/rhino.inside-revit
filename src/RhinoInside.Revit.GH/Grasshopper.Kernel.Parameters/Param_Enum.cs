using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Types;

namespace Grasshopper.Kernel.Parameters
{
  public class Param_Enum<T> : GH_PersistentParam<T>, IGH_ObjectProxy
    where T : Types.GH_Enumerate
  {
    protected Param_Enum(string name, string abbreviation, string description, string category, string subcategory) :
      base(name, abbreviation, description, category, subcategory)
    { }

    public Param_Enum() :
    base
    (
      typeof(T).Name,
      typeof(T).Name.Substring(0, 1),
      string.Empty,
      string.Empty,
      string.Empty
    )
    {
      ProxyExposure = Exposure;

      if (GetType().GetTypeInfo().GetCustomAttribute(typeof(DisplayNameAttribute)) is DisplayNameAttribute name)
      {
        Name = name.DisplayName;
        NickName = name.DisplayName.Substring(0, 1);
      }

      if (GetType().GetTypeInfo().GetCustomAttribute(typeof(NickNameAttribute)) is NickNameAttribute nick)
        NickName = nick.NickName;

      if (GetType().GetTypeInfo().GetCustomAttribute(typeof(DescriptionAttribute)) is DescriptionAttribute description)
        Description = description.Description;
    }

    public override Guid ComponentGuid => typeof(T).GUID;
    public override GH_Exposure Exposure
    {
      get
      {
        if (GetType().GetTypeInfo().GetCustomAttribute(typeof(ExposureAttribute)) is ExposureAttribute exposure)
          return exposure.Exposure;

        return GH_Exposure.hidden;
      }
    }
    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }

    protected override GH_GetterResult Prompt_Plural(ref List<T> values) => GH_GetterResult.cancel;
    protected override GH_GetterResult Prompt_Singular(ref T value) => GH_GetterResult.cancel;

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      if (Kind > GH_ParamKind.input || DataType == GH_ParamData.remote)
      {
        base.AppendAdditionalMenuItems(menu);
        return;
      }

      Menu_AppendWireDisplay(menu);
      Menu_AppendDisconnectWires(menu);

      Menu_AppendPrincipalParameter(menu);
      Menu_AppendReverseParameter(menu);
      Menu_AppendFlattenParameter(menu);
      Menu_AppendGraftParameter(menu);
      Menu_AppendSimplifyParameter(menu);

      var current = InstantiateT();
      if (SourceCount == 0 && PersistentDataCount == 1)
      {
        if (PersistentData.get_FirstItem(true) is T firstValue)
          current.Value = firstValue.Value;
      }

      var values = current.GetEnumValues();
      if (values.Length < 7)
      {
        Menu_AppendSeparator(menu);
        foreach (var e in values)
        {
          var tag = InstantiateT(); tag.Value = (int) e;
          var item = Menu_AppendItem(menu, tag.ToString(), Menu_NamedValueClicked, SourceCount == 0, (int) e == current.Value);
          item.Tag = tag;
        }
        Menu_AppendSeparator(menu);
      }
      else
      {
        var listBox = new ListBox();
        foreach (var e in values)
        {
          var tag = InstantiateT(); tag.Value = (int) e;
          int index = listBox.Items.Add(tag);
          if ((int) e == current.Value)
            listBox.SelectedIndex = index;
        }

        listBox.BorderStyle = BorderStyle.FixedSingle;

        listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

        listBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
        listBox.Height = (int) (100 * GH_GraphicsUtil.UiScale);
        Menu_AppendCustomItem(menu, listBox);
      }

      Menu_AppendDestroyPersistent(menu);
      Menu_AppendInternaliseData(menu);

      if (Exposure != GH_Exposure.hidden)
        Menu_AppendExtractParameter(menu);
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is T value)
          {
            RecordUndoEvent($"Set: {value}");
            PersistentData.Clear();
            PersistentData.Append(value.Duplicate() as T);
          }
        }

        ExpireSolution(true);
      }
    }

    private void Menu_NamedValueClicked(object sender, EventArgs e)
    {
      if (sender is ToolStripMenuItem item)
      {
        if (item.Tag is T value)
        {
          RecordUndoEvent($"Set: {value}");
          PersistentData.Clear();
          PersistentData.Append(value.Duplicate() as T);

          ExpireSolution(true);
        }
      }
    }

    #region IGH_ObjectProxy
    string IGH_ObjectProxy.Location => GetType().Assembly.Location;
    Guid IGH_ObjectProxy.LibraryGuid => Guid.Empty;
    bool IGH_ObjectProxy.SDKCompliant => SDKCompliancy(Rhino.RhinoApp.ExeVersion, Rhino.RhinoApp.ExeServiceRelease);
    bool IGH_ObjectProxy.Obsolete => Obsolete;
    Type IGH_ObjectProxy.Type => GetType();
    GH_ObjectType IGH_ObjectProxy.Kind => GH_ObjectType.CompiledObject;
    Guid IGH_ObjectProxy.Guid => ComponentGuid;
    Bitmap IGH_ObjectProxy.Icon => Icon;
    IGH_InstanceDescription IGH_ObjectProxy.Desc => this;

    GH_Exposure ProxyExposure;
    GH_Exposure IGH_ObjectProxy.Exposure { get => ProxyExposure; set => ProxyExposure = value; }

    IGH_DocumentObject IGH_ObjectProxy.CreateInstance() => new Param_Enum<T>();
    IGH_ObjectProxy IGH_ObjectProxy.DuplicateProxy() => (IGH_ObjectProxy) MemberwiseClone();
    #endregion
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Parameters
{
  using External.DB.Extensions;

  public abstract class View<T, R> : Element<T, R>
    where T : class, Types.IGH_View
    where R : ARDB.View
  {
    protected View(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory)
    { }

    protected override T InstantiateT() => Activator.CreateInstance<T>();

    protected virtual ARDB.ViewFamily ViewFamily => ARDB.ViewFamily.Invalid;

    #region UI
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Transform", "Plane", "Box", "Surface", "Shader" }
    );

    public override void Menu_AppendActions(ToolStripDropDown menu)
    {
      base.Menu_AppendActions(menu);

      var activeApp = Revit.ActiveUIApplication;
#if REVIT_2019
      {
        var commandId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.CloseInactiveViews);
        Menu_AppendItem
        (
          menu, "Close Inactive Views…",
          (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
          activeApp.CanPostCommand(commandId), false
        );
      }
#endif
    }

    protected virtual void Menu_AppendPromptNew(ToolStripDropDown menu) { }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0) return;
      if (Revit.ActiveUIDocument?.Document is null) return;

      Menu_AppendPromptNew(menu);

      var listBox = new ListBox
      {
        Sorted = true,
        BorderStyle = BorderStyle.FixedSingle,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Height = (int) (100 * GH_GraphicsUtil.UiScale)
      };
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

      RefreshViewsList(listBox, ViewFamily);

      Menu_AppendCustomItem(menu, listBox);
    }

    protected void RefreshViewsList(ListBox listBox, ARDB.ViewFamily viewFamily, string displayMember = nameof(Types.View.Nomen))
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.Items.Clear();

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var views = collector.
                    OfClass(typeof(ARDB.View)).
                    Cast<ARDB.View>().
                    Where(x => !x.IsTemplate).
                    Where(x => viewFamily == ARDB.ViewFamily.Invalid || x.Document.GetElement<ARDB.ViewFamilyType>(x.GetTypeId())?.ViewFamily == viewFamily);

        listBox.DisplayMember = displayMember;
        foreach (var view in views)
          listBox.Items.Add(Types.View.FromElement(view));
      }

      listBox.SelectedIndex = listBox.Items.OfType<Types.IGH_View>().IndexOf(PersistentValue, 0).FirstOr(-1);
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    protected void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is Types.IGH_View value)
          {
            RecordPersistentDataEvent($"Set: {value}");
            PersistentData.Clear();
            PersistentData.Append(value as T);
            OnObjectChanged(GH_ObjectEventType.PersistentData);
          }
        }

        ExpireSolution(true);
      }
    }
    #endregion
  }

  public class View : View<Types.IGH_View, ARDB.View>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary;
    public override Guid ComponentGuid => new Guid("2DC4B866-54DB-4CE6-94C0-C51B33D35B49");

    public View() : base("View", "View", "Contains a collection of Revit view elements", "Params", "Revit") { }

    protected override Types.IGH_View InstantiateT() => new Types.View();

    #region UI
    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0) return;
      if (Revit.ActiveUIDocument?.Document is null) return;

      var listBox = new ListBox
      {
        Sorted = true,
        BorderStyle = BorderStyle.FixedSingle,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Height = (int) (100 * GH_GraphicsUtil.UiScale)
      };
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

      var viewTypeBox = new ComboBox
      {
        Sorted = true,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Tag = listBox
      };
      viewTypeBox.SelectedIndexChanged += ViewTypeBox_SelectedIndexChanged;
      viewTypeBox.SetCueBanner("View type filter…");

      using (var collector = new ARDB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
      {
        listBox.Items.Clear();

        var views = collector.
                    OfClass(typeof(ARDB.View)).
                    Cast<ARDB.View>().
                    Where(x => !x.IsTemplate).
                    GroupBy(x => x.Document.GetElement<ARDB.ViewFamilyType>(x.GetTypeId())?.ViewFamily ?? ARDB.ViewFamily.Invalid);

        viewTypeBox.DisplayMember = "Text";
        foreach (var view in views)
        {
          if(view.Key != ARDB.ViewFamily.Invalid)
            viewTypeBox.Items.Add(new Types.ViewFamily(view.Key));
        }

        var viewFamily = (PersistentValue?.Type?.Value as ARDB.ViewFamilyType)?.ViewFamily ??
          ARDB.ViewFamily.Invalid;

        if (viewFamily != ARDB.ViewFamily.Invalid)
        {
          var familyIndex = 0;
          foreach (var family in viewTypeBox.Items.Cast<Types.ViewFamily>())
          {
            if (family.Value == viewFamily)
            {
              viewTypeBox.SelectedIndex = familyIndex;
              break;
            }
            familyIndex++;
          }
        }
        else RefreshViewsList(listBox, ARDB.ViewFamily.Invalid, nameof(Types.View.FullName));
      }

      Menu_AppendCustomItem(menu, viewTypeBox);
      Menu_AppendCustomItem(menu, listBox);
    }

    private void ViewTypeBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox comboBox)
      {
        if (comboBox.Tag is ListBox listBox)
          RefreshViewsList(listBox, ((Types.ViewFamily) comboBox.SelectedItem)?.Value ?? ARDB.ViewFamily.Invalid, nameof(Types.View.FullName));
      }
    }
    #endregion
  }

  [ComponentVersion(introduced: "1.5")]
  public class ViewFamilyType : ElementType<Types.ViewFamilyType, ARDB.ViewFamilyType>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary;
    public override Guid ComponentGuid => new Guid("972B6FBE-B3E4-4576-B86D-D2BA380A4757");

    public ViewFamilyType() : base("View Type", "ViewType", "Contains a collection of Revit view types", "Params", "Revit") { }

    protected override Types.ViewFamilyType InstantiateT() => new Types.ViewFamilyType();

    #region UI
    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)  
    {
      if (SourceCount != 0) return;
      if (Revit.ActiveUIDocument?.Document is null) return;

      var listBox = new ListBox
      {
        Sorted = true,
        BorderStyle = BorderStyle.FixedSingle,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Height = (int) (130 * GH_GraphicsUtil.UiScale)
      };
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

      var viewTypeBox = new ComboBox
      {
        Sorted = true,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Tag = listBox
      };
      viewTypeBox.SelectedIndexChanged += ViewTypeBox_SelectedIndexChanged;
      viewTypeBox.SetCueBanner("View type filter…");

      using (var collector = new ARDB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
      {
        listBox.Items.Clear();

        var views = collector.
                    OfClass(typeof(ARDB.ViewFamilyType)).
                    Cast<ARDB.ViewFamilyType>().
                    GroupBy(x => x.ViewFamily);

        viewTypeBox.DisplayMember = "Text";
        foreach (var view in views)
        {
          if (view.Key != ARDB.ViewFamily.Invalid)
            viewTypeBox.Items.Add(new Types.ViewFamily(view.Key));
        }

        var viewFamily = PersistentValue?.Value?.ViewFamily ?? ARDB.ViewFamily.Invalid;

        if (viewFamily != ARDB.ViewFamily.Invalid)
        {
          var familyIndex = 0;
          foreach (var family in viewTypeBox.Items.Cast<Types.ViewFamily>())
          {
            if (family.Value == viewFamily)
            {
              viewTypeBox.SelectedIndex = familyIndex;
              break;
            }
            familyIndex++;
          }
        }
        else RefreshViewTypesList(listBox, ARDB.ViewFamily.Invalid);
      }

      Menu_AppendCustomItem(menu, viewTypeBox);
      Menu_AppendCustomItem(menu, listBox);
    }

    private void ViewTypeBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox comboBox)
      {
        if (comboBox.Tag is ListBox listBox)
          RefreshViewTypesList(listBox, ((Types.ViewFamily) comboBox.SelectedItem)?.Value ?? ARDB.ViewFamily.Invalid);
      }
    }

    protected void RefreshViewTypesList(ListBox listBox, ARDB.ViewFamily viewFamily)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.Items.Clear();

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var views = collector.
                    OfClass(typeof(ARDB.ViewFamilyType)).
                    Cast<ARDB.ViewFamilyType>().
                    Where(x => viewFamily == ARDB.ViewFamily.Invalid || x.ViewFamily == viewFamily);

        listBox.DisplayMember = nameof(Types.View.Nomen);
        foreach (var view in views)
          listBox.Items.Add(Types.ViewFamilyType.FromElement(view));
      }

      listBox.SelectedIndex = listBox.Items.OfType<Types.ViewFamilyType>().IndexOf(PersistentValue, 0).FirstOr(-1);
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    protected void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is Types.ViewFamilyType value)
          {
            RecordPersistentDataEvent($"Set: {value}");
            PersistentData.Clear();
            PersistentData.Append(value);
            OnObjectChanged(GH_ObjectEventType.PersistentData);
          }
        }

        ExpireSolution(true);
      }
    }
    #endregion
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Category : Element<Types.Category, ARDB.Element>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    public override Guid ComponentGuid => new Guid("6722C7A5-EFD3-4119-A7FD-6C8BE892FD04");

    public Category() : base("Category", "Category", "Contains a collection of Revit categories", "Params", "Revit Primitives") { }

    #region UI
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.ObjectStyles);
      Menu_AppendItem
      (
        menu, $"Open Object Styles…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0)
        return;

      var listBox = new ListBox
      {
        Sorted = true,
        BorderStyle = BorderStyle.FixedSingle,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Height = (int) (100 * GH_GraphicsUtil.UiScale)
      };
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

      var categoriesTypeBox = new ComboBox
      {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Tag = listBox
      };
      categoriesTypeBox.SelectedIndexChanged += CategoriesTypeBox_SelectedIndexChanged;
      categoriesTypeBox.Items.Add("Model");
      categoriesTypeBox.Items.Add("Annotation");
      categoriesTypeBox.Items.Add("Tags");
      categoriesTypeBox.Items.Add("Internal");
      categoriesTypeBox.Items.Add("Analytical");

      if(PersistentValue?.APIObject is ARDB.Category current)
      {
        if (current.IsTagCategory)
          categoriesTypeBox.SelectedIndex = 2;
        else
          categoriesTypeBox.SelectedIndex = (int) current.CategoryType - 1;
      }
      else categoriesTypeBox.SelectedIndex = 0;

      Menu_AppendCustomItem(menu, categoriesTypeBox);
      Menu_AppendCustomItem(menu, listBox);
    }

    private void CategoriesTypeBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if(sender is ComboBox comboBox)
      {
        if(comboBox.Tag is ListBox listBox)
          RefreshCategoryList(listBox, (ARDB.CategoryType) comboBox.SelectedIndex + 1);
      }
    }

    private void RefreshCategoryList(ListBox listBox, ARDB.CategoryType categoryType)
    {
      var doc = Revit.ActiveUIDocument?.Document;
      if (doc is null)
        return;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.Items.Clear();

      using (var collector = doc.Settings.Categories)
      {
        var categories = collector.
          Cast<ARDB.Category>().
          Where(x => 3 == (int) categoryType ? x.IsTagCategory : x.CategoryType == categoryType && !x.IsTagCategory);

        listBox.DisplayMember = "DisplayName";
        foreach (var category in categories)
          listBox.Items.Add(Types.Category.FromCategory(category));
      }

      listBox.SelectedIndex = listBox.Items.Cast<Types.Category>().IndexOf(PersistentValue, 0).FirstOr(-1);
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is Types.Category value)
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

  public class GraphicsStyle : Element<Types.GraphicsStyle, ARDB.GraphicsStyle>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("833E6207-BA60-4C6B-AB8B-96FDA0F91822");

    public GraphicsStyle() : base("Graphics Style", "Graphics Style", "Contains a collection of Revit graphics styles", "Params", "Revit Primitives") { }
  }
}

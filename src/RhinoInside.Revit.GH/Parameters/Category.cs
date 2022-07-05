using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Category : Element<Types.Category, ARDB.Element>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary;
    public override Guid ComponentGuid => new Guid("6722C7A5-EFD3-4119-A7FD-6C8BE892FD04");

    public Category() : base("Category", "Category", "Contains a collection of Revit categories", "Params", "Revit") { }

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
      listBox.Items.Add(new Types.Category());

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
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    public override Guid ComponentGuid => new Guid("833E6207-BA60-4C6B-AB8B-96FDA0F91822");

    public GraphicsStyle() : base("Line Style", "Line Style", "Contains a collection of Revit line styles", "Params", "Revit Elements") { }

    #region UI
    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0) return;
      if (Revit.ActiveUIDocument?.Document is null) return;

      if (MutableNickName)
      {
        var listBox = new ListBox
        {
          BorderStyle = BorderStyle.FixedSingle,
          Width = (int) (250 * GH_GraphicsUtil.UiScale),
          Height = (int) (100 * GH_GraphicsUtil.UiScale),
          SelectionMode = SelectionMode.MultiExtended
        };
        listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

        Menu_AppendCustomItem(menu, listBox);
        RefreshList(listBox);
      }

      base.Menu_AppendPromptOne(menu);
    }

    private void RefreshList(ListBox listBox)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.BeginUpdate();
      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.DisplayMember = nameof(Types.Element.DisplayName);
      listBox.Items.Clear();

      {
        var items = default(IList<Types.GraphicsStyle>);

        if (doc.IsFamilyDocument && doc.OwnerFamily.FamilyCategory is ARDB.Category familyCategory)
        {
          var invisibleLines = doc.GetCategory(ARDB.BuiltInCategory.OST_InvisibleLines);
          listBox.Items.Add(new Types.GraphicsStyle(invisibleLines.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection)));
          listBox.Items.Add(new Types.GraphicsStyle(familyCategory.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection)));
          listBox.Items.Add(new Types.GraphicsStyle(familyCategory.GetGraphicsStyle(ARDB.GraphicsStyleType.Cut)));

          var categories = familyCategory.SubCategories.Cast<ARDB.Category>();
          var projection = categories.Select(x => x.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection));
          var cut = categories.Select(x => x.GetGraphicsStyle(ARDB.GraphicsStyleType.Cut));

          items = projection.Concat(cut).
            OfType<ARDB.GraphicsStyle>().
            Select(x => new Types.GraphicsStyle(x)).
            OrderBy(x => x.DisplayName, ElementNaming.NameComparer).
            ToList();
        }
        else
        {
          var categories = doc.Settings.Categories.
            get_Item(ARDB.BuiltInCategory.OST_Lines).SubCategories.Cast<ARDB.Category>();

          items = categories.
            Select(x => x.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection)).
            Select(x => new Types.GraphicsStyle(x)).
            OrderBy(x => x.DisplayName, ElementNaming.NameComparer).
            ToList();
        }

        foreach (var item in items)
          listBox.Items.Add(item);

        var selectedItems = items.Intersect(PersistentData.OfType<Types.GraphicsStyle>());

        foreach (var item in selectedItems)
          listBox.SelectedItems.Add(item);
      }

      listBox.EndUpdate();
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        RecordPersistentDataEvent($"Set: {NickName}");
        PersistentData.Clear();
        PersistentData.AppendRange(listBox.SelectedItems.OfType<Types.GraphicsStyle>());
        OnObjectChanged(GH_ObjectEventType.PersistentData);

        ExpireSolution(true);
      }
    }
    #endregion

  }
}

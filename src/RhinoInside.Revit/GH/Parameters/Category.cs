using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Category : ElementIdNonGeometryParam<Types.Category, DB.Category>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("6722C7A5-EFD3-4119-A7FD-6C8BE892FD04");

    public Category() : base("Category", "Category", "Represents a Revit document category.", "Params", "Revit") { }

    protected override Types.Category PreferredCast(object data) => Types.Category.FromValue(data);

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

      {
        var listBox = new ListBox();
        listBox.BorderStyle = BorderStyle.FixedSingle;
        listBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
        listBox.Height = (int) (100 * GH_GraphicsUtil.UiScale);
        listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
        listBox.Sorted = true;

        RefreshCategoryList(listBox, DB.CategoryType.Model);

        var categoriesTypeBox = new ComboBox();
        categoriesTypeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        categoriesTypeBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
        categoriesTypeBox.Tag = listBox;
        categoriesTypeBox.SelectedIndexChanged += CategoriesTypeBox_SelectedIndexChanged;
        categoriesTypeBox.Items.Add("Model");
        categoriesTypeBox.Items.Add("Annotation");
        categoriesTypeBox.Items.Add("Tags");
        categoriesTypeBox.Items.Add("Internal");
        categoriesTypeBox.Items.Add("Analytical");
        categoriesTypeBox.SelectedIndex = 0;

        if
        (
          SourceCount == 0 && PersistentDataCount == 1 &&
          PersistentData.get_FirstItem(true) is Types.Category firstValue &&
          firstValue.LoadElement() &&
          (DB.Category) firstValue is DB.Category current
        )
        {
          categoriesTypeBox.SelectedIndex = (int) current.CategoryType - 1;
          if (current.IsTagCategory)
            categoriesTypeBox.SelectedIndex = 2;
        }

        Menu_AppendCustomItem(menu, categoriesTypeBox);
        Menu_AppendCustomItem(menu, listBox);
      }

      Menu_AppendManageCollection(menu);
      Menu_AppendSeparator(menu);

      Menu_AppendDestroyPersistent(menu);
      Menu_AppendInternaliseData(menu);

      if (Exposure != GH_Exposure.hidden)
        Menu_AppendExtractParameter(menu);
    }

    private void CategoriesTypeBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if(sender is ComboBox comboBox)
      {
        if(comboBox.Tag is ListBox listBox)
          RefreshCategoryList(listBox, (DB.CategoryType) comboBox.SelectedIndex + 1);
      }
    }

    private void RefreshCategoryList(ListBox listBox, DB.CategoryType categoryType)
    {
      var doc = Revit.ActiveUIDocument.Document;
      var selectedIndex = -1;

      try
      {
        listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
        listBox.Items.Clear();

        var current = default(Types.Category);
        if (SourceCount == 0 && PersistentDataCount == 1)
        {
          if (PersistentData.get_FirstItem(true) is Types.Category firstValue)
            current = firstValue as Types.Category;
        }

        using (var collector = doc.Settings.Categories)
        {
          var categories = collector.
                           Cast<DB.Category>().
                           Where(x => 3 == (int) categoryType ? x.IsTagCategory : x.CategoryType == categoryType && !x.IsTagCategory);

          foreach (var category in categories)
          {
            var tag = Types.Category.FromCategory(category);
            int index = listBox.Items.Add(tag.EmitProxy());
            if (tag.UniqueID == current?.UniqueID)
              selectedIndex = index;
          }
        }
      }
      finally
      {
        listBox.SelectedIndex = selectedIndex;
        listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
      }
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is IGH_GooProxy value)
          {
            RecordUndoEvent($"Set: {value}");
            PersistentData.Clear();
            PersistentData.Append(value.ProxyOwner as Types.Category);
          }
        }

        ExpireSolution(true);
      }
    }
  }

  public class GraphicsStyle : ElementIdNonGeometryParam<Types.GraphicsStyle, DB.GraphicsStyle>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("833E6207-BA60-4C6B-AB8B-96FDA0F91822");

    public GraphicsStyle() : base("Graphics Style", "Graphics Style", "Represents a Revit graphics style.", "Params", "Revit") { }
  }
}

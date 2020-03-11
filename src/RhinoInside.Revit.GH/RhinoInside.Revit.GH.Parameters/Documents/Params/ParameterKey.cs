using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.GH.Extensions;
using RhinoInside.Revit.GH.Parameters.Elements;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Documents.Params
{
  public class ParameterKey : ElementIdNonGeometryParam<Types.Documents.Params.ParameterKey, DB.ElementId>
  {
    public override Guid ComponentGuid => new Guid("A550F532-8C68-460B-91F3-DA0A5A0D42B5");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public ParameterKey() : base("Parameter Key", "Parameter Key", "Represents a Revit parameter definition.", "Params", "Revit") { }

    protected override Types.Documents.Params.ParameterKey PreferredCast(object data) => null;
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

        var categoriesBox = new ComboBox();
        categoriesBox.DropDownStyle = ComboBoxStyle.DropDownList;
        categoriesBox.DropDownHeight = categoriesBox.ItemHeight * 15;
        categoriesBox.SetCueBanner("Category filter…");
        categoriesBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
        categoriesBox.Tag = listBox;
        categoriesBox.SelectedIndexChanged += CategoriesBox_SelectedIndexChanged;

        var categoriesTypeBox = new ComboBox();
        categoriesTypeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        categoriesTypeBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
        categoriesTypeBox.Tag = categoriesBox;
        categoriesTypeBox.SelectedIndexChanged += CategoryType_SelectedIndexChanged;
        categoriesTypeBox.Items.Add("All Categories");
        categoriesTypeBox.Items.Add("Model");
        categoriesTypeBox.Items.Add("Annotation");
        categoriesTypeBox.Items.Add("Tags");
        categoriesTypeBox.Items.Add("Internal");
        categoriesTypeBox.Items.Add("Analytical");
        categoriesTypeBox.SelectedIndex = 0;

        Menu_AppendCustomItem(menu, categoriesTypeBox);
        Menu_AppendCustomItem(menu, categoriesBox);
        Menu_AppendCustomItem(menu, listBox);
      }

      Menu_AppendManageCollection(menu);
      Menu_AppendSeparator(menu);

      Menu_AppendDestroyPersistent(menu);
      Menu_AppendInternaliseData(menu);

      if (Exposure != GH_Exposure.hidden)
        Menu_AppendExtractParameter(menu);
    }

    private void RefreshCategoryList(ComboBox categoriesBox, DB.CategoryType categoryType)
    {
      var categories = Revit.ActiveUIDocument.Document.Settings.Categories.Cast<DB.Category>().Where(x => x.AllowsBoundParameters);

      if (categoryType != DB.CategoryType.Invalid)
      {
        if (categoryType == (DB.CategoryType) 3)
          categories = categories.Where(x => x.IsTagCategory);
        else
          categories = categories.Where(x => x.CategoryType == categoryType && !x.IsTagCategory);
      }

      categoriesBox.SelectedIndex = -1;
      categoriesBox.Items.Clear();
      foreach (var category in categories.OrderBy(x => x.Name))
      {
        var tag = Types.Documents.Categories.Category.FromCategory(category);
        int index = categoriesBox.Items.Add(tag.EmitProxy());
      }
    }

    private void RefreshParametersList(ListBox listBox, ComboBox categoriesBox)
    {
      var doc = Revit.ActiveUIDocument.Document;
      var selectedIndex = -1;

      try
      {
        listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
        listBox.Items.Clear();

        var current = default(Types.Documents.Params.ParameterKey);
        if (SourceCount == 0 && PersistentDataCount == 1)
        {
          if (PersistentData.get_FirstItem(true) is Types.Documents.Params.ParameterKey firstValue)
            current = firstValue as Types.Documents.Params.ParameterKey;
        }

        {
          var parameters = default(IEnumerable<DB.ElementId>);
          if (categoriesBox.SelectedIndex == -1)
          {
            parameters = categoriesBox.Items.
                         Cast<IGH_GooProxy>().
                         Select(x => x.ProxyOwner).
                         Cast<Types.Documents.Categories.Category>().
                         SelectMany(x => DB.TableView.GetAvailableParameters(doc, x.Id)).
                         GroupBy(x => x.IntegerValue).
                         Select(x => x.First());
          }
          else
          {
            parameters = DB.TableView.GetAvailableParameters(doc, ((categoriesBox.Items[categoriesBox.SelectedIndex] as IGH_GooProxy).ProxyOwner as Types.Documents.Categories.Category).Id);
          }

          foreach (var parameter in parameters)
          {
            var tag = Types.Documents.Params.ParameterKey.FromElementId(doc, parameter);
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

    private void CategoryType_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox categoriesTypeBox && categoriesTypeBox.Tag is ComboBox categoriesBox)
      {
        RefreshCategoryList(categoriesBox, (DB.CategoryType) categoriesTypeBox.SelectedIndex);
        RefreshParametersList(categoriesBox.Tag as ListBox, categoriesBox);
      }
    }

    private void CategoriesBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox categoriesBox && categoriesBox.Tag is ListBox parametersListBox)
        RefreshParametersList(parametersListBox, categoriesBox);
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
            PersistentData.Append(value.ProxyOwner as Types.Documents.Params.ParameterKey);
          }
        }

        ExpireSolution(true);
      }
    }
  }
}

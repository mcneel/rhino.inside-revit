using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.GH.Extensions;
using RhinoInside.Revit.GH.Parameters.Elements;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Documents.ElementTypes
{
  public class ElementType : ElementIdNonGeometryParam<Types.Documents.ElementTypes.ElementType, DB.ElementType>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("97DD546D-65C3-4D00-A609-3F5FBDA67142");

    public ElementType() : base("Element Type", "Element Type", "Represents a Revit document element type.", "Params", "Revit") { }

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
        var elementTypesBox = new ListBox();
        elementTypesBox.BorderStyle = BorderStyle.FixedSingle;
        elementTypesBox.Width = (int) (300 * GH_GraphicsUtil.UiScale);
        elementTypesBox.Height = (int) (100 * GH_GraphicsUtil.UiScale);
        elementTypesBox.SelectedIndexChanged += ElementTypesBox_SelectedIndexChanged;
        elementTypesBox.Sorted = true;

        var familiesBox = new ComboBox();
        familiesBox.DropDownStyle = ComboBoxStyle.DropDownList;
        familiesBox.DropDownHeight = familiesBox.ItemHeight * 15;
        familiesBox.SetCueBanner("Family filter…");
        familiesBox.Width = (int) (300 * GH_GraphicsUtil.UiScale);

        var categoriesBox = new ComboBox();
        categoriesBox.DropDownStyle = ComboBoxStyle.DropDownList;
        categoriesBox.DropDownHeight = categoriesBox.ItemHeight * 15;
        categoriesBox.SetCueBanner("Category filter…");
        categoriesBox.Width = (int) (300 * GH_GraphicsUtil.UiScale);

        familiesBox.Tag = Tuple.Create(elementTypesBox, categoriesBox);
        familiesBox.SelectedIndexChanged += FamiliesBox_SelectedIndexChanged;
        categoriesBox.Tag = Tuple.Create(elementTypesBox, familiesBox);
        categoriesBox.SelectedIndexChanged += CategoriesBox_SelectedIndexChanged;

        var categoriesTypeBox = new ComboBox();
        categoriesTypeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        categoriesTypeBox.Width = (int) (300 * GH_GraphicsUtil.UiScale);
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
        Menu_AppendCustomItem(menu, familiesBox);
        Menu_AppendCustomItem(menu, elementTypesBox);
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

    private void RefreshFamiliesBox(ComboBox familiesBox, ComboBox categoriesBox)
    {
      familiesBox.SelectedIndex = -1;
      familiesBox.Items.Clear();

      using (var collector = new DB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
      {
        var categories = (
                  categoriesBox.SelectedItem is IGH_GooProxy proxy ?
                    Enumerable.Repeat(proxy, 1) :
                    categoriesBox.Items.OfType<IGH_GooProxy>()
                  ).
                  Select(x => x.ProxyOwner as Types.Documents.Categories.Category).
                  Select(x => x.Id).
                  ToArray();

        foreach (var familyName in collector.WhereElementIsElementType().
          WherePasses(new DB.ElementMulticategoryFilter(categories)).
          ToElements().Cast<DB.ElementType>().GroupBy(x => x.FamilyName).Select(x => x.Key))
        {
          familiesBox.Items.Add(familyName);
        }
      }
    }

    private void RefreshElementTypesList(ListBox listBox, ComboBox categoriesBox, ComboBox familiesBox)
    {
      var doc = Revit.ActiveUIDocument.Document;

      try
      {
        listBox.SelectedIndexChanged -= ElementTypesBox_SelectedIndexChanged;
        listBox.Items.Clear();

        var current = default(Types.Documents.ElementTypes.ElementType);
        if (SourceCount == 0 && PersistentDataCount == 1)
        {
          if (PersistentData.get_FirstItem(true) is Types.Documents.ElementTypes.ElementType firstValue)
            current = firstValue as Types.Documents.ElementTypes.ElementType;
        }

        {
          var categories = (
                            categoriesBox.SelectedItem is IGH_GooProxy proxy ?
                              Enumerable.Repeat(proxy, 1) :
                              categoriesBox.Items.OfType<IGH_GooProxy>()
                           ).
                           Select(x => x.ProxyOwner as Types.Documents.Categories.Category).
                           Select(x => x.Id).
                           ToArray();

          if (categories.Length > 0)
          {
            var elementTypes = default(IEnumerable<DB.ElementId>);
            using (var collector = new DB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
            {
              elementTypes = collector.WhereElementIsElementType().
                             WherePasses(new DB.ElementMulticategoryFilter(categories)).
                             ToElementIds();
            }

            var familyName = familiesBox.SelectedItem as string;

            foreach (var elementType in elementTypes)
            {
              if
              (
                !string.IsNullOrEmpty(familyName) &&
                doc.GetElement(elementType) is DB.ElementType type &&
                type.FamilyName != familyName
              )
                continue;

              var tag = Types.Documents.ElementTypes.ElementType.FromElementId(doc, elementType);
              int index = listBox.Items.Add(tag.EmitProxy());
              if (tag.UniqueID == current?.UniqueID)
                listBox.SetSelected(index, true);
            }
          }
        }
      }
      finally
      {
        listBox.SelectedIndexChanged += ElementTypesBox_SelectedIndexChanged;
      }
    }

    private void CategoryType_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox categoriesTypeBox && categoriesTypeBox.Tag is ComboBox categoriesBox)
      {
        RefreshCategoryList(categoriesBox, (DB.CategoryType) categoriesTypeBox.SelectedIndex);
        if (categoriesBox.Tag is Tuple<ListBox, ComboBox> tuple)
        {
          RefreshFamiliesBox(tuple.Item2, categoriesBox);
          RefreshElementTypesList(tuple.Item1, categoriesBox, tuple.Item2);
        }
      }
    }

    private void CategoriesBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox categoriesBox && categoriesBox.Tag is Tuple<ListBox, ComboBox> tuple)
      {
        RefreshFamiliesBox(tuple.Item2, categoriesBox);
        RefreshElementTypesList(tuple.Item1, categoriesBox, tuple.Item2);
      }
    }

    private void FamiliesBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox familiesBox && familiesBox.Tag is Tuple<ListBox, ComboBox> tuple)
        RefreshElementTypesList(tuple.Item1, tuple.Item2, familiesBox);
    }

    private void ElementTypesBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is IGH_GooProxy value)
          {
            RecordUndoEvent($"Set: {value}");
            PersistentData.Clear();
            PersistentData.Append(value.ProxyOwner as Types.Documents.ElementTypes.ElementType);
          }
        }

        ExpireSolution(true);
      }
    }
  }
}

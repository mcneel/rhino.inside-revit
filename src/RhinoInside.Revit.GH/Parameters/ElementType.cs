using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public abstract class ElementType<T, R> : Element<T, R>
    where T : class, Types.IGH_ElementType
    where R : DB.ElementType
  {
    protected ElementType(string name, string nickname, string description, string category, string subcategory) :
    base(name, nickname, description, category, subcategory)
    { }

    protected override System.Drawing.Bitmap Icon => Properties.Resources.ElementType;

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0)
        return;

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

      if (Current is Types.ElementType current)
      {
        if (current.Category.Value.IsTagCategory)
          categoriesTypeBox.SelectedIndex = 3;
        else
          categoriesTypeBox.SelectedIndex = (int) current.Category.Value.CategoryType;

        var categoryIndex = 0;
        var currentCategory = current.Category;
        foreach (var category in categoriesBox.Items.Cast<Types.Category>())
        {
          if (currentCategory.Equals(category))
          {
            categoriesBox.SelectedIndex = categoryIndex;
            break;
          }
          categoryIndex++;
        }

        var familyIndex = 0;
        foreach (var familyName in familiesBox.Items.Cast<string>())
        {
          if (current.FamilyName == familyName)
          {
            familiesBox.SelectedIndex = familyIndex;
            break;
          }
          familyIndex++;
        }
      }
      else categoriesTypeBox.SelectedIndex = 0;

      Menu_AppendCustomItem(menu, categoriesTypeBox);
      Menu_AppendCustomItem(menu, categoriesBox);
      Menu_AppendCustomItem(menu, familiesBox);
      Menu_AppendCustomItem(menu, elementTypesBox);
    }

    static bool HasElementTypes(DB.ElementId categoryId)
    {
      using (var collector = new DB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
      {
        var elementCollector = collector.OfClass(typeof(R)).OfCategoryId(categoryId);
        return elementCollector.GetElementCount() > 0;
      }
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
      categoriesBox.DisplayMember = "DisplayName";
      foreach (var category in categories.OrderBy(x => x.Name))
      {
        if (!HasElementTypes(category.Id))
          continue;

        categoriesBox.Items.Add(Types.Category.FromCategory(category));
      }
    }

    private DB.ElementId[] GetCategoryIds(ComboBox categoriesBox)
    {        
      return
      (
        categoriesBox.SelectedItem is Types.Category category ?
        Enumerable.Repeat(category, 1) :
        categoriesBox.Items.OfType<Types.Category>()
      ).
      Select(x => x.Id).
      ToArray();
    }

    private void RefreshFamiliesBox(ComboBox familiesBox, ComboBox categoriesBox)
    {
      familiesBox.SelectedIndex = -1;
      familiesBox.Items.Clear();

      using (var collector = new DB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
      {
        var categories = GetCategoryIds(categoriesBox);

        foreach (var familyName in collector.WhereElementIsElementType().OfClass(typeof(R)).
          WherePasses(new DB.ElementMulticategoryFilter(categories)).Cast<R>().
          GroupBy(x => x.GetFamilyName()).Select(x => x.Key))
        {
          familiesBox.Items.Add(familyName);
        }
      }
    }

    private void RefreshElementTypesList(ListBox listBox, ComboBox categoriesBox, ComboBox familiesBox)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ElementTypesBox_SelectedIndexChanged;
      listBox.Items.Clear();

      {
        var categories = GetCategoryIds(categoriesBox);
        if (categories.Length > 0)
        {
          var elementTypes = default(IEnumerable<R>);
          using (var collector = new DB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
          {
            elementTypes = collector.WhereElementIsElementType().OfClass(typeof(R)).
                            WherePasses(new DB.ElementMulticategoryFilter(categories)).Cast<R>();

            var familyName = familiesBox.SelectedItem as string;

            listBox.DisplayMember = "DisplayName";
            foreach (var elementType in elementTypes)
            {
              if
              (
                !string.IsNullOrEmpty(familyName) &&
                elementType.GetFamilyName() != familyName
              )
                continue;

              listBox.Items.Add(Types.ElementType.FromElement(elementType));
            }
          }
        }
      }

      listBox.SelectedIndex = listBox.Items.OfType<T>().IndexOf(Current, 0).FirstOr(-1);
      listBox.SelectedIndexChanged += ElementTypesBox_SelectedIndexChanged;
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
          if (listBox.Items[listBox.SelectedIndex] is T value)
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
  }

  public class ElementType : ElementType<Types.IGH_ElementType, DB.ElementType>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    public override Guid ComponentGuid => new Guid("97DD546D-65C3-4D00-A609-3F5FBDA67142");

    public ElementType() : base("Type", "Type", "Contains a collection of Revit element types", "Params", "Revit Primitives") { }

    protected override Types.IGH_ElementType InstantiateT() => new Types.ElementType();
  }
}

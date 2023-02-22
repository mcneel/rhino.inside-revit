using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  using External.DB.Extensions;

  public class Family : Element<Types.Family, ARDB.Family>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary;
    public override Guid ComponentGuid => new Guid("3966ADD8-07C0-43E7-874B-6EFF95598EB0");

    public Family() : base("Family", "Family", "Contains a collection of Revit family elements", "Params", "Revit") { }

    #region UI
    public ARDB.BuiltInCategory SelectedBuiltInCategory { get; set; } = ARDB.BuiltInCategory.INVALID;

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0) return;
      if (Revit.ActiveUIDocument?.Document is null) return;

      var familiesBox = new ListBox
      {
        Sorted = true,
        BorderStyle = BorderStyle.FixedSingle,
        Width = (int) (300 * GH_GraphicsUtil.UiScale),
        Height = (int) (100 * GH_GraphicsUtil.UiScale),
        DisplayMember = nameof(Types.ElementType.Nomen)
      };

      var categoriesBox = new ComboBox
      {
        Sorted = true,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = (int) (300 * GH_GraphicsUtil.UiScale),
        DisplayMember = nameof(Types.Element.DisplayName)
      };
      categoriesBox.DropDownHeight = categoriesBox.ItemHeight * 15;
      categoriesBox.SetCueBanner("Category filter…");

      familiesBox.SelectedIndexChanged += FamiliesBox_SelectedIndexChanged;
      categoriesBox.Tag = familiesBox;
      categoriesBox.SelectedIndexChanged += CategoriesBox_SelectedIndexChanged;

      var categoriesTypeBox = new ComboBox
      {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = (int) (300 * GH_GraphicsUtil.UiScale),
        Tag = categoriesBox
      };
      categoriesTypeBox.SelectedIndexChanged += CategoryType_SelectedIndexChanged;
      categoriesTypeBox.Items.Add("All Categories");
      categoriesTypeBox.Items.Add("Model");
      categoriesTypeBox.Items.Add("Annotation");
      categoriesTypeBox.Items.Add("Tags");
      categoriesTypeBox.Items.Add("Internal");
      categoriesTypeBox.Items.Add("Analytical");

      if (PersistentValue is Types.Family current && current.Value is object)
      {
        if (current.Value.FamilyCategory.IsTagCategory == true)
          categoriesTypeBox.SelectedIndex = 3;
        else
          categoriesTypeBox.SelectedIndex = (int) current.Value.FamilyCategory.CategoryType;

        var categoryIndex = 0;
        var currentCategory = current.Value.FamilyCategory;
        foreach (var category in categoriesBox.Items.Cast<Types.Category>())
        {
          if (currentCategory.Id == category.Id)
          {
            categoriesBox.SelectedIndex = categoryIndex;
            break;
          }
          categoryIndex++;
        }
      }
      else if (SelectedBuiltInCategory != ARDB.BuiltInCategory.INVALID)
      {
        var category = new Types.Category(Revit.ActiveDBDocument, new ARDB.ElementId(SelectedBuiltInCategory));
        if (category.IsTagCategory == true)
          categoriesTypeBox.SelectedIndex = 3;
        else
          categoriesTypeBox.SelectedIndex = (int) category.CategoryType;
      }
      else
      {
        categoriesTypeBox.SelectedIndex = 1;
      }

      Menu_AppendCustomItem(menu, categoriesTypeBox);
      Menu_AppendCustomItem(menu, categoriesBox);
      Menu_AppendCustomItem(menu, familiesBox);
    }

    ICollection<ARDB.Category> CategoriesWithTypes(ARDB.Document document)
    {
      using (var collector = new ARDB.FilteredElementCollector(document))
      {
        var elementCollector = collector.WhereElementIsElementType().OfClass(typeof(ARDB.FamilySymbol));
        return new HashSet<ARDB.Category>
        (
          collector.Select(x => x.Category),
          CategoryEqualityComparer.SameDocument
        );
      }
    }

    private void RefreshCategoryList(ComboBox categoriesBox, ARDB.CategoryType categoryType)
    {
      if (Revit.ActiveUIDocument is null) return;

      var doc = Revit.ActiveUIDocument.Document;
      var categories = (IEnumerable<ARDB.Category>) CategoriesWithTypes(doc);

      if (categoryType != ARDB.CategoryType.Invalid)
      {
        if (categoryType == (ARDB.CategoryType) 3)
          categories = categories.Where(x => x?.IsTagCategory is true);
        else
          categories = categories.Where
          (
            x => (x?.CategoryType ?? ARDB.CategoryType.Internal) == categoryType &&
            x?.IsTagCategory is false
          );
      }

      categoriesBox.SelectedIndex = ListBox.NoMatches;
      categoriesBox.BeginUpdate();
      categoriesBox.Items.Clear();

      foreach (var category in categories)
      {
        if (category is null)
          categoriesBox.Items.Add(new Types.Category());
        else
          categoriesBox.Items.Add(Types.Category.FromCategory(category));
      }
      categoriesBox.EndUpdate();

      if (SelectedBuiltInCategory != ARDB.BuiltInCategory.INVALID)
      {
        var currentCategory = new Types.Category(doc, new ARDB.ElementId(SelectedBuiltInCategory));
        categoriesBox.SelectedIndex = categoriesBox.Items.Cast<Types.Category>().IndexOf(currentCategory, 0).FirstOr(ListBox.NoMatches);
      }
    }

    private ARDB.ElementId[] GetCategoryIds(ComboBox categoriesBox)
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

    private void RefreshFamiliesBox(ListBox familiesBox, ComboBox categoriesBox) => Rhinoceros.InvokeInHostContext(() =>
    {
      try
      {
        familiesBox.SelectedIndexChanged -= FamiliesBox_SelectedIndexChanged;
        familiesBox.BeginUpdate();
        familiesBox.Items.Clear();

        using (var collector = new ARDB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
        {
          var categories = GetCategoryIds(categoriesBox);
          if (categories.Length == 1)
          {
            var families = collector.WhereElementIsElementType().OfClass(typeof(ARDB.FamilySymbol)).
            WherePasses(new ARDB.ElementMulticategoryFilter(categories)).Cast<ARDB.FamilySymbol>().
            Select(x => new Types.Family(x.Family)).Distinct();

            familiesBox.Items.AddRange(families.ToArray());
          }
        }

        familiesBox.SelectedIndex = familiesBox.Items.Cast<Types.Family>().IndexOf(PersistentValue, 0).FirstOr(-1);
      }
      finally { familiesBox.EndUpdate(); }
      familiesBox.SelectedIndexChanged += FamiliesBox_SelectedIndexChanged;
    });

    private void CategoryType_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox categoriesTypeBox && categoriesTypeBox.Tag is ComboBox categoriesBox)
        RefreshCategoryList(categoriesBox, (ARDB.CategoryType) categoriesTypeBox.SelectedIndex);
    }

    private void CategoriesBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox categoriesBox && categoriesBox.Tag is ListBox familiesBox)
      {
        SelectedBuiltInCategory = categoriesBox.SelectedItem is Types.Category category &&
          category.Id.TryGetBuiltInCategory(out var bic) ?
          bic : ARDB.BuiltInCategory.INVALID;

        RefreshFamiliesBox(familiesBox, categoriesBox);
      }
    }

    private void FamiliesBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != ListBox.NoMatches)
        {
          if (listBox.Items[listBox.SelectedIndex] is Types.Family value)
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

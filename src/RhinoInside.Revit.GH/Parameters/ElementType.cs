using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  using External.DB.Extensions;

  public abstract class ElementType<T, R> : Element<T, R>
    where T : class, Types.IGH_ElementType
    where R : ARDB.ElementType
  {
    protected ElementType(string name, string nickname, string description, string category, string subcategory) :
    base(name, nickname, description, category, subcategory)
    { }

    protected override System.Drawing.Bitmap Icon => Properties.Resources.ElementType;

    #region UI
    public ARDB.BuiltInCategory SelectedBuiltInCategory { get; set; } = ARDB.BuiltInCategory.INVALID;

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0) return;
      if (Revit.ActiveUIDocument?.Document is null) return;

      var elementTypesBox = new ListBox
      {
        Sorted = true,
        BorderStyle = BorderStyle.FixedSingle,
        Width = (int) (300 * GH_GraphicsUtil.UiScale),
        Height = (int) (100 * GH_GraphicsUtil.UiScale),
      };
      elementTypesBox.SelectedIndexChanged += ElementTypesBox_SelectedIndexChanged;

      var familiesBox = new ComboBox
      {
        Sorted = true,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = (int) (300 * GH_GraphicsUtil.UiScale),
      };
      familiesBox.DropDownHeight = familiesBox.ItemHeight * 15;
      familiesBox.SetCueBanner("Family filter…");

      var categoriesBox = new ComboBox
      {
        Sorted = true,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = (int) (300 * GH_GraphicsUtil.UiScale),
      };
      categoriesBox.DropDownHeight = categoriesBox.ItemHeight * 15;
      categoriesBox.SetCueBanner("Category filter…");

      familiesBox.Tag = Tuple.Create(elementTypesBox, categoriesBox);
      familiesBox.SelectedIndexChanged += FamiliesBox_SelectedIndexChanged;
      categoriesBox.Tag = Tuple.Create(elementTypesBox, familiesBox);
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

      if (PersistentValue is Types.ElementType current)
      {
        if (current.Category.IsTagCategory == true)
          categoriesTypeBox.SelectedIndex = 3;
        else
          categoriesTypeBox.SelectedIndex = (int) current.Category.CategoryType;

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
      Menu_AppendCustomItem(menu, elementTypesBox);
    }

    ICollection<ARDB.Category> CategoriesWithTypes(ARDB.Document document)
    {
      using (var collector = new ARDB.FilteredElementCollector(document))
      {
        var elementCollector = collector.WhereElementIsElementType().OfClass(typeof(R));
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
          categories = categories.Where(x => x?.IsTagCategory == true);
        else
          categories = categories.Where
          (
            x => (x?.CategoryType ?? ARDB.CategoryType.Internal) == categoryType &&
            x?.IsTagCategory == false
          );
      }

      categoriesBox.SelectedIndex = -1;
      categoriesBox.Items.Clear();
      categoriesBox.DisplayMember = "DisplayName";

      foreach (var category in categories)
      {
        if (category is null)
          categoriesBox.Items.Add(new Types.Category());
        else
          categoriesBox.Items.Add(Types.Category.FromCategory(category));
      }

      if (SelectedBuiltInCategory != ARDB.BuiltInCategory.INVALID)
      {
        var currentCategory = new Types.Category(doc, new ARDB.ElementId(SelectedBuiltInCategory));
        categoriesBox.SelectedIndex = categoriesBox.Items.Cast<Types.Category>().IndexOf(currentCategory, 0).FirstOr(-1);
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

    private void RefreshFamiliesBox(ComboBox familiesBox, ComboBox categoriesBox)
    {
      familiesBox.SelectedIndex = -1;
      familiesBox.Items.Clear();

      using (var collector = new ARDB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
      {
        var categories = GetCategoryIds(categoriesBox);

        foreach (var familyName in collector.WhereElementIsElementType().OfClass(typeof(R)).
          WherePasses(new ARDB.ElementMulticategoryFilter(categories)).Cast<R>().
          Select(x => x.GetFamilyName()).Distinct())
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

      if (categoriesBox.SelectedIndex != -1 || familiesBox.SelectedIndex != -1)
      {
        var categories = GetCategoryIds(categoriesBox);
        if (categories.Length > 0)
        {
          var elementTypes = default(IEnumerable<R>);
          using (var collector = new ARDB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
          {
            elementTypes = collector.WhereElementIsElementType().OfClass(typeof(R)).
                            WherePasses(new ARDB.ElementMulticategoryFilter(categories)).Cast<R>();

            var familyName = familiesBox.SelectedItem as string;

            listBox.DisplayMember = "Name";
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

      listBox.SelectedIndex = listBox.Items.Cast<T>().IndexOf(PersistentValue, 0).FirstOr(-1);
      listBox.SelectedIndexChanged += ElementTypesBox_SelectedIndexChanged;
    }

    private void CategoryType_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox categoriesTypeBox && categoriesTypeBox.Tag is ComboBox categoriesBox)
      {
        RefreshCategoryList(categoriesBox, (ARDB.CategoryType) categoriesTypeBox.SelectedIndex);
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
        SelectedBuiltInCategory = categoriesBox.SelectedItem is Types.Category category &&
          category.Id.TryGetBuiltInCategory(out var bic) ?
          bic : ARDB.BuiltInCategory.INVALID;

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
    #endregion

    #region IO
    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      var selectedBuiltInCategory = string.Empty;
      if (reader.TryGetString("SelectedBuiltInCategory", ref selectedBuiltInCategory))
        SelectedBuiltInCategory = new External.DB.Schemas.CategoryId(selectedBuiltInCategory);
      else
        SelectedBuiltInCategory = ARDB.BuiltInCategory.INVALID;

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (SelectedBuiltInCategory != ARDB.BuiltInCategory.INVALID)
        writer.SetString("SelectedBuiltInCategory", ((External.DB.Schemas.CategoryId) SelectedBuiltInCategory).FullName);

      return true;
    }
    #endregion

    public static bool GetDataOrDefault<TOutput>
    (
      IGH_Component component,
      IGH_DataAccess DA,
      string name,
      out TOutput type,
      Types.Document document,
      ARDB.ElementTypeGroup typeGroup
    )
      where TOutput : class
    {
      if (!component.Params.TryGetData(DA, name, out type)) return false;
      if (type is null)
      {
        var data = Types.ElementType.FromElementId(document.Value, document.Value.GetDefaultElementTypeId(typeGroup));
        if (data is null)
          throw new Exceptions.RuntimeArgumentException(name, $"No suitable {typeGroup} has been found.");

        type = data as TOutput;
        if (type is null)
          return data.CastTo(out type);
      }

      // Validate document
      switch (type)
      {
        case ARDB.Element element:
          if (!document.Value.IsEquivalent(element.Document))
            throw new Exceptions.RuntimeArgumentException(name, "Failed to assign a type from a diferent document.");
          break;
        case Types.IGH_ElementId id:
          if (!document.Value.IsEquivalent(id.Document))
            throw new Exceptions.RuntimeArgumentException(name, "Failed to assign a type from a diferent document.");
          break;
      }

      return true;
    }

    public static bool GetDataOrDefault<TOutput>
    (
      IGH_Component component,
      IGH_DataAccess DA,
      string name,
      out TOutput type,
      Types.Document document,
      ARDB.BuiltInCategory categoryId
    )
      where TOutput : class
    {
      if (!component.Params.TryGetData(DA, name, out type)) return false;
      if (type is null)
      {
        var data = Types.ElementType.FromElementId(document.Value, document.Value.GetDefaultFamilyTypeId(new ARDB.ElementId(categoryId)));
        if (data is null)
          throw new Exceptions.RuntimeArgumentException(name, $"No suitable {categoryId} type has been found.");

        if (data is Types.FamilySymbol symbol && !symbol.Value.IsActive)
          symbol.Value.Activate();

        type = data as TOutput;
        if (type is null)
          return data.CastTo(out type);
      }

      // Validate type
      switch (type)
      {
        case ARDB.Element element:
        {
          if (!document.Value.IsEquivalent(element.Document))
            throw new Exceptions.RuntimeArgumentException(name, "Failed to assign a type from a diferent document.");

          if (element.Category.Id.IntegerValue != (int)categoryId)
            throw new Exceptions.RuntimeArgumentException(name, $"Collected type is not on category '{((ERDB.Schemas.CategoryId) categoryId).Label}'.");

          if (element is ARDB.FamilySymbol symbol && !symbol.IsActive)
            symbol.Activate();

          return true;
        }

        case Types.IGH_Element goo:
        {
          if (!document.Value.IsEquivalent(goo.Document))
            throw new Exceptions.RuntimeArgumentException(name, "Failed to assign a type from a diferent document.");

          if (goo.Category.Id.IntegerValue != (int) categoryId)
            throw new Exceptions.RuntimeArgumentException(name, $"Collected type is not on category '{((ERDB.Schemas.CategoryId) categoryId).Label}'.");

          if (goo is Types.FamilySymbol symbol && !symbol.Value.IsActive)
            symbol.Value.Activate();

          return true;
        }
      }

      return false;
    }
  }

  public class ElementType : ElementType<Types.IGH_ElementType, ARDB.ElementType>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    public override Guid ComponentGuid => new Guid("97DD546D-65C3-4D00-A609-3F5FBDA67142");

    public ElementType() : base("Type", "Type", "Contains a collection of Revit element types", "Params", "Revit Primitives") { }

    protected override Types.IGH_ElementType InstantiateT() => new Types.ElementType();
  }
}

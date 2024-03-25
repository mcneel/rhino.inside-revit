using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class BuiltInCategories : Grasshopper.Special.ValueSet<Types.CategoryId>
  {
    public override Guid ComponentGuid => new Guid("AF9D949F-1692-45AA-9FE4-653CFF5ECA26");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override System.Drawing.Bitmap Icon =>
      ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
      base.Icon;

    bool ShowSubcategories = false;
    bool ShowObscure = false;
    ARDB.CategoryType CategoryType = ARDB.CategoryType.Invalid;
    const ARDB.CategoryType ARDB_CategoryType_Tags = (ARDB.CategoryType) 3;
    ERDB.CategoryDiscipline CategoryDiscipline = ~ERDB.CategoryDiscipline.None;

    public BuiltInCategories() : base
    (
      name: "Built-In Categories",
      nickname: "Categories",
      description: "Provides a picker for built-in categories",
      category: "Revit",
      subcategory: "Object Styles"
    )
    {
      IconDisplayMode = GH_IconDisplayMode.name;
    }

    protected override void LoadVolatileData()
    {
      if (SourceCount == 0)
      {
        m_data.Clear();
        m_data.AppendRange(Types.CategoryId.EnumValues);
      }

      if (SourceCount == 0)
      {
        MutableNickName = false;

        m_data.Clear();
        IEnumerable<Types.CategoryId> categories = Types.CategoryId.EnumValues;

        if (!ShowSubcategories)
          categories = categories.Where(c => !c.IsSubCategory);

        if (!ShowObscure)
          categories = categories.Where(c => c.IsVisibleInUI);

        if (CategoryType == ARDB.CategoryType.Invalid)
        {
          NickName = Name;
        }
        else
        {
          NickName = $"{Name} ({CategoryType_Label(CategoryType)})";
          categories = categories.
            Where(x => GetCategoryType(x) == CategoryType).
            OrderBy(x => x.Value.Label);
        }

        if (CategoryDiscipline != ~ERDB.CategoryDiscipline.None)
          categories = categories.Where(x => (x.CategoryDiscipline & CategoryDiscipline) != ERDB.CategoryDiscipline.None);

        m_data.AppendRange(categories);
      }
      else
      {
        MutableNickName = true;
      }
    }

    private ARDB.CategoryType GetCategoryType(Types.CategoryId categoryId)
    {
      return categoryId.IsTagCategory ? ARDB_CategoryType_Tags : categoryId.CategoryType;
    }

    private string CategoryType_Label(ARDB.CategoryType categoryType)
    {
      switch (categoryType)
      {
        case ARDB.CategoryType.Invalid: return "All";
        case ARDB.CategoryType.Model: return "Model";
        case ARDB.CategoryType.Annotation: return "Annotation";
        case ARDB_CategoryType_Tags: return "Tags";
        case ARDB.CategoryType.AnalyticalModel: return "Analytical";
        case ARDB.CategoryType.Internal: return "Internal";
      }

      return string.Empty;
    }

    protected override void Menu_AppendManageCollection(ToolStripDropDown menu) { }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount == 0 && Kind == GH_ParamKind.floating)
      {
        var types = new ARDB.CategoryType[]
        {
          ARDB.CategoryType.Invalid,
          ARDB.CategoryType.Model,
          ARDB.CategoryType.Annotation,
          ARDB_CategoryType_Tags,
          ARDB.CategoryType.AnalyticalModel,
          ARDB.CategoryType.Internal
        };

        var typeItem = Menu_AppendItem(menu, "Category Type");
        foreach (var type in types)
          Menu_AppendItem(typeItem.DropDown, CategoryType_Label(type), (s, e) => Menu_CategoryTypeClicked(type), true, type == CategoryType);

        var disciplineItem = Menu_AppendItem(menu, "Category Discipline");
        Menu_AppendItem(disciplineItem.DropDown, "All", (s, e) => Menu_CategoryDisciplineClicked(~ERDB.CategoryDiscipline.None, CategoryDiscipline != ~ERDB.CategoryDiscipline.None), true, CategoryDiscipline == ~ERDB.CategoryDiscipline.None);
        foreach (var discipline in Enum.GetValues(typeof(ERDB.CategoryDiscipline)).Cast<ERDB.CategoryDiscipline>().Skip(1))
          Menu_AppendItem(disciplineItem.DropDown, discipline.ToString(), (s, e) => Menu_CategoryDisciplineClicked(discipline, !CategoryDiscipline.HasFlag(discipline)), true, CategoryDiscipline.HasFlag(discipline));

        Menu_AppendItem(menu, "Show Subcategories", (s, e) => Menu_ShowSubcategoriesClicked(!ShowSubcategories), true, ShowSubcategories);
        Menu_AppendItem(menu, "Show Obscure", (s, e) => Menu_ShowObscureClicked(!ShowObscure), true, ShowObscure);
      }
    }

    private void Menu_ShowSubcategoriesClicked(bool showSubcategories)
    {
      RecordUndoEvent("Filter Subcategories");
      ShowSubcategories = showSubcategories;
      OnObjectChanged(GH_ObjectEventType.Custom);

      ExpireSolution(true);
    }

    private void Menu_ShowObscureClicked(bool showObscure)
    {
      RecordUndoEvent("Filter Obscure categories");
      ShowObscure = showObscure;
      OnObjectChanged(GH_ObjectEventType.Custom);

      ExpireSolution(true);
    }

    private void Menu_CategoryTypeClicked(ARDB.CategoryType type)
    {
      RecordUndoEvent("Filter By Category Type");
      CategoryType = type;
      OnObjectChanged(GH_ObjectEventType.Custom);

      ExpireSolution(true);
    }

    private void Menu_CategoryDisciplineClicked(ERDB.CategoryDiscipline discipline, bool value)
    {
      RecordUndoEvent("Filter by Category Discipline");

      CategoryDiscipline = discipline;
      //if(value)
      //  CategoryDiscipline |= discipline;
      //else
      //  CategoryDiscipline &= ~discipline;

      OnObjectChanged(GH_ObjectEventType.Custom);

      ExpireSolution(true);
    }

    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      int categoryType = (int) ARDB.CategoryType.Invalid;
      CategoryType = reader.TryGetInt32("CategoryType", ref categoryType) ?
      (ARDB.CategoryType) categoryType :
      ARDB.CategoryType.Invalid;

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (CategoryType != ARDB.CategoryType.Invalid)
        writer.SetInt32("CategoryType", (int) CategoryType);

      return true;
    }
  }
}

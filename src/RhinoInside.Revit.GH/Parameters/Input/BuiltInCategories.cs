using System;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class BuiltInCategories : Grasshopper.Special.ValueSet<Types.CategoryId>
  {
    public override Guid ComponentGuid => new Guid("AF9D949F-1692-45AA-9FE4-653CFF5ECA26");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override System.Drawing.Bitmap Icon =>
      ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
      base.Icon;

    ARDB.CategoryType CategoryType = ARDB.CategoryType.Invalid;
    const ARDB.CategoryType ARDB_CategoryType_Tags = (ARDB.CategoryType) 3;

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
        if (CategoryType == ARDB.CategoryType.Invalid)
        {
          NickName = Name;
          m_data.AppendRange(Types.CategoryId.EnumValues);
        }
        else
        {
          NickName = $"{Name} ({CategoryType_Label(CategoryType)})";
          m_data.AppendRange
          (
            Types.CategoryId.EnumValues.
            Where(x => GetCategoryType(x) == CategoryType).
            OrderBy(x => x.Value.Label)
          );
        }
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
        case ARDB.CategoryType.Invalid: return "All Categories";
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

        foreach (var type in types)
        {
          var item = Menu_AppendItem(menu, CategoryType_Label(type), Menu_CategoryTypeClicked, true, type.Equals(CategoryType));
          item.Tag = type;
        }
      }
    }

    private void Menu_CategoryTypeClicked(object sender, EventArgs e)
    {
      if (sender is ToolStripMenuItem item)
      {
        if (item.Tag is ARDB.CategoryType value)
        {
          RecordUndoEvent("Set Category Type");
          CategoryType = value;
          OnObjectChanged(GH_ObjectEventType.Custom);

          ExpireSolution(true);
        }
      }
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

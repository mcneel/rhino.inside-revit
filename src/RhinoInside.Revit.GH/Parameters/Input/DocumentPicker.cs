using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  #region DocumentValuePicker
  public abstract class DocumentValuePicker : GH_ValueList, Kernel.IGH_ElementIdParam
  {
    #region IGH_ElementIdParam
    protected virtual DB.ElementFilter ElementFilter => null;
    public virtual bool PassesFilter(DB.Document document, Autodesk.Revit.DB.ElementId id)
    {
      return ElementFilter?.PassesFilter(document, id) ?? true;
    }

    bool Kernel.IGH_ElementIdParam.NeedsToBeExpired
    (
      DB.Document doc,
      ICollection<DB.ElementId> added,
      ICollection<DB.ElementId> deleted,
      ICollection<DB.ElementId> modified
    )
    {
      // If anything of that type is added we need to update ListItems
      if (added.Where(id => PassesFilter(doc, id)).Any())
        return true;

      // If selected items are modified we need to expire dependant components
      foreach (var data in VolatileData.AllData(true).OfType<Types.IGH_ElementId>())
      {
        if (!data.IsValid)
          continue;

        if (modified.Contains(data.Id))
          return true;
      }

      // If an item in ListItems is deleted we need to update ListItems
      foreach (var item in ListItems.Select(x => x.Value).OfType<Grasshopper.Kernel.Types.GH_Integer>())
      {
        var id = new DB.ElementId(item.Value);

        if (deleted.Contains(id))
          return true;
      }

      return false;
    }
    #endregion
  }

  public abstract class DocumentCategoriesPicker : DocumentValuePicker
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override bool PassesFilter(DB.Document document, DB.ElementId id) => id.IsCategoryId(document);
    protected abstract bool CategoryIsInSet(DB.Category category);
    protected abstract DB.BuiltInCategory DefaultBuiltInCategory { get; }

    public DocumentCategoriesPicker()
    {
      Category = "Revit";
      SubCategory = "Input";
      NickName = "Document";
      MutableNickName = false;
      Name = $"{NickName} Categories Picker";
      Description = $"Provides a {NickName} Category picker";

      ListMode = GH_ValueListMode.DropDown;
    }

    protected override void CollectVolatileData_Custom()
    {
      var selectedItems = ListItems.Where(x => x.Selected).Select(x => x.Expression).ToList();
      ListItems.Clear();

      if (Revit.ActiveDBDocument is object)
      {
        foreach (var group in Revit.ActiveDBDocument.Settings.Categories.Cast<DB.Category>().GroupBy(x => x.CategoryType).OrderBy(x => x.Key))
        {
          foreach (var category in group.OrderBy(x => x.Name).Where(x => CategoryIsInSet(x)))
          {
            if (!category.Id.IsBuiltInId())
              continue;

            if (category.CategoryType == DB.CategoryType.Invalid)
              continue;

            var item = new GH_ValueListItem(category.Name, category.Id.IntegerValue.ToString());
            item.Selected = selectedItems.Contains(item.Expression);
            ListItems.Add(item);
          }
        }

        if (selectedItems.Count == 0 && ListMode != GH_ValueListMode.CheckList)
        {
          foreach (var item in ListItems)
            item.Selected = item.Expression == ((int) DefaultBuiltInCategory).ToString();
        }
      }

      base.CollectVolatileData_Custom();
    }
  }

  [Obsolete("Since 2021-06-10. Please use 'Built-In Categories'")]
  public class ModelCategoriesPicker : DocumentCategoriesPicker
  {
    public override Guid ComponentGuid => new Guid("EB266925-F1AA-4729-B5C0-B978937F51A3");
    public override string NickName => MutableNickName ? base.NickName : "Model";
    protected override DB.BuiltInCategory DefaultBuiltInCategory => DB.BuiltInCategory.OST_GenericModel;
    protected override bool CategoryIsInSet(DB.Category category) => !category.IsTagCategory && category.CategoryType == DB.CategoryType.Model;

    public ModelCategoriesPicker() { }
  }

  [Obsolete("Since 2021-06-10. Please use 'Built-In Categories'")]
  public class AnnotationCategoriesPicker : DocumentCategoriesPicker
  {
    public override Guid ComponentGuid => new Guid("B1D1CA45-3771-49CA-8540-9A916A743C1B");
    public override string NickName => MutableNickName ? base.NickName : "Annotation";
    protected override DB.BuiltInCategory DefaultBuiltInCategory => DB.BuiltInCategory.OST_GenericAnnotation;
    protected override bool CategoryIsInSet(DB.Category category) => !category.IsTagCategory && category.CategoryType == DB.CategoryType.Annotation;
    public AnnotationCategoriesPicker() { }
  }

  [Obsolete("Since 2021-06-10. Please use 'Built-In Categories'")]
  public class TagCategoriesPicker : DocumentCategoriesPicker
  {
    public override Guid ComponentGuid => new Guid("30F6DA06-35F9-4E83-AE9E-080AF26C8326");
    public override string NickName => MutableNickName ? base.NickName : "Tag";
    protected override DB.BuiltInCategory DefaultBuiltInCategory => DB.BuiltInCategory.OST_GenericModelTags;
    protected override bool CategoryIsInSet(DB.Category category) => category.IsTagCategory;
    public TagCategoriesPicker() { }
  }

  [Obsolete("Since 2021-06-10. Please use 'Built-In Categories'")]
  public class AnalyticalCategoriesPicker : DocumentCategoriesPicker
  {
    public override Guid ComponentGuid => new Guid("4120C5ED-4329-4F42-B8D3-FA518E6E6807");
    public override string NickName => MutableNickName ? base.NickName : "Analytical";
    protected override DB.BuiltInCategory DefaultBuiltInCategory => DB.BuiltInCategory.OST_AnalyticalNodes;
    protected override bool CategoryIsInSet(DB.Category category) => !category.IsTagCategory && category.CategoryType == DB.CategoryType.AnalyticalModel;

    public AnalyticalCategoriesPicker() { }
  }

  #endregion

  #region DocumentElementPicker

  public abstract class DocumentElementPicker<T> : Grasshopper.Special.ValueSet<T>,
  Kernel.IGH_ElementIdParam
  where T : class, IGH_Goo
  {
    protected DocumentElementPicker(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory)
    {
      IconDisplayMode = GH_IconDisplayMode.icon;
    }

    protected override System.Drawing.Bitmap Icon =>
      ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
      base.Icon;

    #region IGH_ElementIdParam
    protected virtual DB.ElementFilter ElementFilter => null;
    public virtual bool PassesFilter(DB.Document document, DB.ElementId id)
    {
      return ElementFilter?.PassesFilter(document, id) ?? true;
    }

    bool Kernel.IGH_ElementIdParam.NeedsToBeExpired
    (
      DB.Document doc,
      ICollection<DB.ElementId> added,
      ICollection<DB.ElementId> deleted,
      ICollection<DB.ElementId> modified
    )
    {
      // If selected items are modified we need to expire dependant components
      foreach (var data in VolatileData.AllData(true).OfType<Types.IGH_ElementId>())
      {
        if (!doc.IsEquivalent(data.Document))
          continue;

        if (modified.Contains(data.Id) || deleted.Contains(data.Id))
          return true;
      }

      if (SourceCount == 0)
      {
        // If an element that pass the filter is added we need to update ListItems
        var updateListItems = added.Where(id => PassesFilter(doc, id)).Any();

        if (!updateListItems)
        {
          // If an item in ListItems is deleted we need to update ListItems
          foreach (var item in ListItems.Select(x => x.Value).OfType<Types.IGH_ElementId>())
          {
            if (!doc.IsEquivalent(item.Document))
              continue;

            if (modified.Contains(item.Id) || deleted.Contains(item.Id))
            {
              updateListItems = true;
              break;
            }
          }
        }

        if (updateListItems)
        {
          ClearData();
          CollectData();
          ComputeData();
          OnDisplayExpired(false);
        }
      }

      return false;
    }
    #endregion
  }

  public class DocumentLevelsPicker : DocumentElementPicker<Types.Level>
  {
    public override Guid ComponentGuid => new Guid("BD6A74F3-8C46-4506-87D9-B34BD96747DA");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.Level));

    public DocumentLevelsPicker() : base
    (
      name: "Levels Picker",
      nickname: "Levels",
      description: "Provides a Level picker",
      category: "Revit",
      subcategory: "Input"
    )
    {}

    protected override void LoadVolatileData()
    {
      if (SourceCount == 0)
      {
        m_data.Clear();

        if (Document.TryGetCurrentDocument(this, out var doc))
        {
          using (var collector = new DB.FilteredElementCollector(doc.Value))
          {
            m_data.AppendRange(collector.OfClass(typeof(DB.Level)).Cast<DB.Level>().Select(x => new Types.Level(x)));
          }
        }
      }

      base.LoadVolatileData();
    }

    protected override void SortItems()
    {
      // Show elements sorted Alphabetically.
      ListItems.Sort((x, y) =>
      {
        var result = (int) (x.Value.Value.GetHeight() - y.Value.Value.GetHeight());
        return result == 0 ? string.CompareOrdinal(x.Name, y.Name) : result;
      });
    }
  }

  public class DocumentFamiliesPicker : DocumentElementPicker<Types.Family>
  {
    public override Guid ComponentGuid => new Guid("45CEE087-4194-4E55-AA20-9CC5D2193CE0");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.Family));

    public DocumentFamiliesPicker() : base
    (
      name: "Component Families Picker",
      nickname: "Component Families",
      description: "Provides a Family picker",
      category: "Revit",
      subcategory : "Input"
    )
    {}

    protected override void LoadVolatileData()
    {
      if (SourceCount == 0)
      {
        m_data.Clear();

        if (Document.TryGetCurrentDocument(this, out var doc))
        {
          using (var collector = new DB.FilteredElementCollector(doc.Value))
          {
            m_data.AppendRange(collector.OfClass(typeof(DB.Family)).Cast<DB.Family>().Select(x => new Types.Family(x)));
          }
        }
      }

      base.LoadVolatileData();
    }
  }

  public class DocumentTitleBlockSymbolPicker : DocumentElementPicker<Types.FamilySymbol>
  {
    public override Guid ComponentGuid => new Guid("f737745f-57ff-4699-a402-01a6db329313");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementCategoryFilter(DB.BuiltInCategory.OST_TitleBlocks);

    public DocumentTitleBlockSymbolPicker() : base
    (
      name: "Title Block Type Picker",
      nickname: "TBTP",
      description: "Provides a Title Block type picker",
      category: "Revit",
      subcategory: "Input"
    )
    { }

    protected override void LoadVolatileData()
    {
      if (SourceCount == 0)
      {
        m_data.Clear();

        if (Document.TryGetCurrentDocument(this, out var doc))
        {
          using (var collector = new DB.FilteredElementCollector(doc.Value).WherePasses(ElementFilter))
          {
            m_data.AppendRange(
              collector.WhereElementIsElementType()
                       .Cast<DB.FamilySymbol>()
                       .Select(x => new Types.FamilySymbol(x))
              );
          }
        }
      }

      base.LoadVolatileData();
    }
  }


  #endregion
}

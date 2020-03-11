using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Documents
{
  public abstract class DocumentPicker : GH_ValueList, IGH_ElementIdParam
  {
    #region IGH_ElementIdParam
    protected virtual DB.ElementFilter ElementFilter => null;
    public virtual bool PassesFilter(DB.Document document, Autodesk.Revit.DB.ElementId id)
    {
      return ElementFilter?.PassesFilter(document, id) ?? true;
    }

    bool IGH_ElementIdParam.NeedsToBeExpired
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
        if (!data.IsElementLoaded)
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

  public abstract class DocumentCategoriesPicker : DocumentPicker
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override bool PassesFilter(DB.Document document, DB.ElementId id) => id.IsCategoryId(document);
    protected abstract bool CategoryIsInSet(DB.Category category);
    protected abstract DB.BuiltInCategory DefaultBuiltInCategory { get; }

    public DocumentCategoriesPicker()
    {
      Category = "Revit";
      SubCategory = "Input";
      NickName = "Document";
      MutableNickName = false;
      Name = $"{NickName}.CategoriesPicker";
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

  public class ModelCategoriesPicker : DocumentCategoriesPicker
  {
    public override Guid ComponentGuid => new Guid("EB266925-F1AA-4729-B5C0-B978937F51A3");
    public override string NickName => MutableNickName ? base.NickName : "Model";
    protected override DB.BuiltInCategory DefaultBuiltInCategory => DB.BuiltInCategory.OST_GenericModel;
    protected override bool CategoryIsInSet(DB.Category category) => !category.IsTagCategory && category.CategoryType == DB.CategoryType.Model;

    public ModelCategoriesPicker() { }
  }
  public class AnnotationCategoriesPicker : DocumentCategoriesPicker
  {
    public override Guid ComponentGuid => new Guid("B1D1CA45-3771-49CA-8540-9A916A743C1B");
    public override string NickName => MutableNickName ? base.NickName : "Annotation";
    protected override DB.BuiltInCategory DefaultBuiltInCategory => DB.BuiltInCategory.OST_GenericAnnotation;
    protected override bool CategoryIsInSet(DB.Category category) => !category.IsTagCategory && category.CategoryType == DB.CategoryType.Annotation;
    public AnnotationCategoriesPicker() { }
  }
  public class TagCategoriesPicker : DocumentCategoriesPicker
  {
    public override Guid ComponentGuid => new Guid("30F6DA06-35F9-4E83-AE9E-080AF26C8326");
    public override string NickName => MutableNickName ? base.NickName : "Tag";
    protected override DB.BuiltInCategory DefaultBuiltInCategory => DB.BuiltInCategory.OST_GenericModelTags;
    protected override bool CategoryIsInSet(DB.Category category) => category.IsTagCategory;
    public TagCategoriesPicker() { }
  }
  public class AnalyticalCategoriesPicker : DocumentCategoriesPicker
  {
    public override Guid ComponentGuid => new Guid("4120C5ED-4329-4F42-B8D3-FA518E6E6807");
    public override string NickName => MutableNickName ? base.NickName : "Analytical";
    protected override DB.BuiltInCategory DefaultBuiltInCategory => DB.BuiltInCategory.OST_AnalyticalNodes;
    protected override bool CategoryIsInSet(DB.Category category) => !category.IsTagCategory && category.CategoryType == DB.CategoryType.AnalyticalModel;

    public AnalyticalCategoriesPicker() { }
  }


  public class DocumentLevelsPicker : DocumentPicker
  {
    public override Guid ComponentGuid => new Guid("BD6A74F3-8C46-4506-87D9-B34BD96747DA");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.Level));

    public DocumentLevelsPicker()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Document.LevelsPicker";
      MutableNickName = false;
      Description = "Provides a Level picker";
    }

    void RefreshList()
    {
      var selectedItems = ListItems.Where(x => x.Selected).Select(x => x.Expression).ToList();
      ListItems.Clear();

      if (Revit.ActiveDBDocument != null)
      {
        using (var collector = new DB.FilteredElementCollector(Revit.ActiveDBDocument))
        {
          foreach (var level in collector.OfClass(typeof(DB.Level)).Cast<DB.Level>().OrderByDescending((x) => x.Elevation))
          {
            var item = new GH_ValueListItem(level.Name, level.Id.IntegerValue.ToString());
            item.Selected = selectedItems.Contains(item.Expression);
            ListItems.Add(item);
          }
        }
      }
    }

    protected override void CollectVolatileData_Custom()
    {
      NickName = "Level";
      RefreshList();
      base.CollectVolatileData_Custom();
    }
  }
}

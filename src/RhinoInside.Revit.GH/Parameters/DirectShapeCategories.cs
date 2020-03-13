using System;
using System.Linq;
using Autodesk.Revit.DB;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace RhinoInside.Revit.GH.Parameters
{
  public class DirectShapeCategories : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("7BAFE137-332B-481A-BE22-09E8BD4C86FC");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public DirectShapeCategories()
    {
      Name = "DirectShape Categories";
      NickName = "Categories";
      MutableNickName = false;
      Description = "Provides a picker of a valid DirectShape category";
      Category = "Revit";
      SubCategory = "DirectShape";

      ListItems.Clear();

      var ActiveDBDocument = Revit.ActiveDBDocument;
      if (ActiveDBDocument is null)
        return;

      var directShapeCategories = ActiveDBDocument.Settings.Categories.Cast<Autodesk.Revit.DB.Category>().Where((x) => DirectShape.IsValidCategoryId(x.Id, ActiveDBDocument));
      foreach (var group in directShapeCategories.GroupBy((x) => x.CategoryType).OrderBy((x) => x.Key))
      {
        foreach (var category in group.OrderBy(x => x.Name))
        {
          ListItems.Add(new GH_ValueListItem(category.Name, category.Id.IntegerValue.ToString()));
          if (category.Id.IntegerValue == (int) BuiltInCategory.OST_GenericModel)
            SelectItem(ListItems.Count - 1);
        }
      }
    }
  }
}

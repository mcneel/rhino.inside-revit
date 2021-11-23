using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class CategoryTypes : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("5FFB1339-8521-44A1-9075-2984637725E9");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public CategoryTypes()
    {
      Category = "Revit";
      SubCategory = "Category";
      Name = "CategoryTypes";
      NickName = "CategoryTypes";
      Description = "Provides a picker of a CategoryType";

      ListItems.Clear();
      ListItems.Add(new GH_ValueListItem("Model", ((int) ARDB.CategoryType.Model).ToString()));
      ListItems.Add(new GH_ValueListItem("Annotation", ((int) ARDB.CategoryType.Annotation).ToString()));
      ListItems.Add(new GH_ValueListItem("Internal", ((int) ARDB.CategoryType.Internal).ToString()));
      ListItems.Add(new GH_ValueListItem("Analytical", ((int) ARDB.CategoryType.AnalyticalModel).ToString()));
    }
  }
}

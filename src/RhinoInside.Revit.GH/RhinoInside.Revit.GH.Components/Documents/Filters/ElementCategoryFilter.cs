using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents.Filters
{
  public class ElementCategoryFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("D08F7AB1-BE36-45FA-B006-0078022DB140");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "C";

    public ElementCategoryFilter()
    : base("Element.CategoryFilter", "Category Filter", "Filter used to match elements by their category", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Documents.Categories.Category(), "Categories", "C", "Categories to match", GH_ParamAccess.list);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var categoryIds = new List<DB.ElementId>();
      if (!DA.GetDataList("Categories", categoryIds))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      var ids = categoryIds.Select(x => x is null ? DB.ElementId.InvalidElementId : x).ToArray();
      if (ids.Length > 0)
      {
        if (ids.Length == 1)
          DA.SetData("Filter", new DB.ElementCategoryFilter(ids[0], inverted));
        else
          DA.SetData("Filter", new DB.ElementMulticategoryFilter(ids, inverted));
      }
      else DA.DisableGapLogic();
    }
  }
}

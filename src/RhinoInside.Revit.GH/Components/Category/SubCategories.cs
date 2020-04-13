using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class CategorySubCategories : Component
  {
    public override Guid ComponentGuid => new Guid("4915AB87-0BD5-4541-AC43-3FBC450DD883");

    public CategorySubCategories()
    : base("Category SubCategories", "SubCategories", "Returns a list of all the subcategories of Category", "Revit", "Category")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Category", "C", "Category to query", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "SubCategories", "S", string.Empty, GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Category parent = null;
      if (!DA.GetData("Category", ref parent))
        return;

      if (parent.Parent is object)
      {
        DA.SetDataList("SubCategories", null);
      }
      else
      {
        using (var subCategories = parent.SubCategories)
        {
          var doc = parent.Document();
          var SubCategories = new HashSet<int>(subCategories.Cast<DB.Category>().Select(x => x.Id.IntegerValue));  

          if (parent.Id.IntegerValue == (int) DB.BuiltInCategory.OST_Stairs)
            SubCategories.Add((int) DB.BuiltInCategory.OST_StairsStringerCarriage);

          if (parent.Id.IntegerValue == (int) DB.BuiltInCategory.OST_Walls)
            SubCategories.Add((int) DB.BuiltInCategory.OST_StackedWalls);

          DA.SetDataList("SubCategories", SubCategories.Select(x => new Types.Category(doc, new DB.ElementId(x))));
        }
      }
    }
  }
}

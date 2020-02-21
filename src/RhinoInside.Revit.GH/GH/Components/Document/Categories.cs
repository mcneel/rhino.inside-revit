using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentCategories : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("D150E40E-0970-4683-B517-038F8BA8B0D8");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override DB.ElementFilter ElementFilter => null;

    public override bool NeedsToBeExpired(DB.Events.DocumentChangedEventArgs e)
    {
      var document = e.GetDocument();

      var added = e.GetAddedElementIds().Where(x => x.IsCategoryId(document));
      if (added.Any())
        return true;

      var modified = e.GetModifiedElementIds().Where(x => x.IsCategoryId(document));
      if (modified.Any())
        return true;

      var deleted = e.GetDeletedElementIds();
      if (deleted.Any())
      {
        var empty = new DB.ElementId[0];
        var deletedSet = new HashSet<DB.ElementId>(deleted);
        foreach (var param in Params.Output.OfType<Parameters.IGH_ElementIdParam>())
        {
          if (param.NeedsToBeExpired(document, empty, deletedSet, empty))
            return true;
        }
      }

      return false;
    }

    public DocumentCategories() : base
    (
      "Document.Categories", "Categories",
      "Get active document categories list",
      "Revit", "Document"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      var type = manager[manager.AddParameter(new Parameters.Param_Enum<Types.CategoryType>(), "Type", "T", "Category type", GH_ParamAccess.item)] as Parameters.Param_Enum<Types.CategoryType>;
      type.SetPersistentData(DB.CategoryType.Model);
      type.Optional = true;
      manager[manager.AddBooleanParameter("AllowsParameters", "A", "Allows bound parameters", GH_ParamAccess.item, true)].Optional = true;
      manager[manager.AddBooleanParameter("HasMaterialQuantities", "M", "Has material quantities", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddBooleanParameter("Cuttable", "C", "Has material quantities", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddBooleanParameter("Hidden", "H", "Is hidden category", GH_ParamAccess.item, false)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Categories", "Categories", "Categories list", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var categoryType = DB.CategoryType.Invalid;
      DA.GetData("Type", ref categoryType);

      bool AllowsParameters = false;
      bool nofilterParams = (!DA.GetData("AllowsParameters", ref AllowsParameters) && Params.Input[1].Sources.Count == 0);

      bool HasMaterialQuantities = false;
      bool nofilterMaterials = (!DA.GetData("HasMaterialQuantities", ref HasMaterialQuantities) && Params.Input[2].Sources.Count == 0);

      bool Cuttable = false;
      bool nofilterCuttable = (!DA.GetData("Cuttable", ref Cuttable) && Params.Input[3].Sources.Count == 0);

      bool Hidden = false;
      bool nofilterHidden = (!DA.GetData("Hidden", ref Hidden) && Params.Input[4].Sources.Count == 0);

      var categories = Revit.ActiveDBDocument.Settings.Categories.Cast<DB.Category>();

      if (categoryType != DB.CategoryType.Invalid)
        categories = categories.Where((x) => x.CategoryType == categoryType);

      if (!nofilterParams)
        categories = categories.Where((x) => x.AllowsBoundParameters == AllowsParameters);

      if (!nofilterMaterials)
        categories = categories.Where((x) => x.HasMaterialQuantities == HasMaterialQuantities);

      if (!nofilterCuttable)
        categories = categories.Where((x) => x.IsCuttable == Cuttable);

      if (!nofilterHidden)
        categories = categories.Where((x) => x.IsHidden() == Hidden);

      IEnumerable<DB.Category> list = null;
      foreach (var group in categories.GroupBy((x) => x.CategoryType).OrderBy((x) => x.Key))
      {
        var orderedGroup = group.OrderBy((x) => x.Name);
        list = list?.Concat(orderedGroup) ?? orderedGroup;
      }

      if (list is object)
        DA.SetDataList("Categories", list);
      else
        DA.DisableGapLogic();
    }
  }
}

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
    public override GH_Exposure Exposure => GH_Exposure.secondary;
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
        foreach (var param in Params.Output.OfType<Kernel.IGH_ElementIdParam>())
        {
          if (param.NeedsToBeExpired(document, empty, deletedSet, empty))
            return true;
        }
      }

      return false;
    }

    public DocumentCategories() : base
    (
      "Document Categories", "Categories",
      "Get document categories list",
      "Revit", "Document"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);

      var type = manager[manager.AddParameter(new Parameters.Param_Enum<Types.CategoryType>(), "Type", "T", "Category type", GH_ParamAccess.item)] as Parameters.Param_Enum<Types.CategoryType>;
      type.SetPersistentData(DB.CategoryType.Model);
      type.Optional = true;
      manager[manager.AddBooleanParameter("AllowsParameters", "A", "Allows bound parameters", GH_ParamAccess.item, true)].Optional = true;
      manager[manager.AddBooleanParameter("HasMaterialQuantities", "M", "Has material quantities", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddBooleanParameter("Cuttable", "C", "Has material quantities", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddTextParameter("Name", "N", "Level name", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Categories", "Categories", "Categories list", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      var categoryType = DB.CategoryType.Invalid;
      DA.GetData("Type", ref categoryType);

      bool AllowsParameters = false;
      var _AllowsParameters_ = Params.IndexOfInputParam("AllowsParameters");
      bool nofilterParams = (!DA.GetData(_AllowsParameters_, ref AllowsParameters) && Params.Input[_AllowsParameters_].Sources.Count == 0);

      bool HasMaterialQuantities = false;
      var _HasMaterialQuantities_ = Params.IndexOfInputParam("HasMaterialQuantities");
      bool nofilterMaterials = (!DA.GetData(_HasMaterialQuantities_, ref HasMaterialQuantities) && Params.Input[_HasMaterialQuantities_].Sources.Count == 0);

      bool Cuttable = false;
      var _Cuttable_ = Params.IndexOfInputParam("Cuttable");
      bool nofilterCuttable = (!DA.GetData(_Cuttable_, ref Cuttable) && Params.Input[_Cuttable_].Sources.Count == 0);

      var Name = default(string);
      var _Name_ = Params.IndexOfInputParam("Name");
      bool nofilterName = (!DA.GetData(_Name_, ref Name) && Params.Input[_Name_].Sources.Count == 0);

      var categories = doc.Settings.Categories.Cast<DB.Category>();

      if (categoryType != DB.CategoryType.Invalid)
        categories = categories.Where((x) => x.CategoryType == categoryType);

      if (!nofilterParams)
        categories = categories.Where((x) => x.AllowsBoundParameters == AllowsParameters);

      if (!nofilterMaterials)
        categories = categories.Where((x) => x.HasMaterialQuantities == HasMaterialQuantities);

      if (!nofilterCuttable)
        categories = categories.Where((x) => x.IsCuttable == Cuttable);

      if (!nofilterName)
        categories = categories.Where((x) => x.Name.IsSymbolNameLike(Name));

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

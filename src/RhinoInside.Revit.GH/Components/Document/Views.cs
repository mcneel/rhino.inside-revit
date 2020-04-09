using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentViews : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("DF691659-B75B-4455-AF5F-8A5DE485FA05");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "V";
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.View));

    public DocumentViews() : base
    (
      name: "Document Views",
      nickname: "Views",
      description: "Get all document views",
      category: "Revit",
      subCategory: "Document"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);

      var discipline = manager[manager.AddParameter(new Parameters.Param_Enum<Types.ViewDiscipline>(), "Discipline", "Discipline", "View discipline", GH_ParamAccess.item)] as Parameters.Param_Enum<Types.ViewDiscipline>;
      discipline.Optional = true;

      var type = manager[manager.AddParameter(new Parameters.Param_Enum<Types.ViewType>(), "Type", "T", "View type", GH_ParamAccess.item)] as Parameters.Param_Enum<Types.ViewType>;
      type.SetPersistentData(DB.ViewType.Undefined);
      type.Optional = true;

      manager[manager.AddTextParameter("Name", "N", "View name", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.View(), "Template", "T", "Views template", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddBooleanParameter("IsTemplate", "T", "View is template", GH_ParamAccess.item, false)].Optional = true;
      manager[manager.AddBooleanParameter("IsAssembly", "A", "View is assembly", GH_ParamAccess.item, false)].Optional = true;
      manager[manager.AddBooleanParameter("IsPrintable", "P", "View is printable", GH_ParamAccess.item, true)].Optional = true;
      manager[manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", "Filter", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.View(), "Views", "V", "Views list", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      var viewDiscipline = default(DB.ViewDiscipline);
      var _Discipline_ = Params.IndexOfInputParam("Discipline");
      bool nofilterDiscipline = (!DA.GetData(_Discipline_, ref viewDiscipline) && Params.Input[_Discipline_].Sources.Count == 0);

      var viewType = DB.ViewType.Undefined;
      DA.GetData("Type", ref viewType);

      string name = null;
      DA.GetData("Name", ref name);

      var Template = default(DB.View);
      var _Template_ = Params.IndexOfInputParam("Template");
      bool nofilterTemplate = (!DA.GetData(_Template_, ref Template) && Params.Input[_Template_].DataType == GH_ParamData.@void);

      bool IsTemplate = false;
      var _IsTemplate_ = Params.IndexOfInputParam("IsTemplate");
      bool nofilterIsTemplate = (!DA.GetData(_IsTemplate_, ref IsTemplate) && Params.Input[_IsTemplate_].DataType == GH_ParamData.@void);

      bool IsAssembly = false;
      var _IsAssembly_ = Params.IndexOfInputParam("IsAssembly");
      bool nofilterIsAssembly = (!DA.GetData(_IsAssembly_, ref IsAssembly) && Params.Input[_IsAssembly_].DataType == GH_ParamData.@void);

      bool IsPrintable = false;
      var _IsPrintable_ = Params.IndexOfInputParam("IsPrintable");
      bool nofilterIsPrintable = (!DA.GetData(_IsPrintable_, ref IsPrintable) && Params.Input[_IsPrintable_].DataType == GH_ParamData.@void);

      DB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var viewsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          viewsCollector = viewsCollector.WherePasses(filter);

        if (!nofilterDiscipline && TryGetFilterIntegerParam(DB.BuiltInParameter.VIEW_DISCIPLINE, (int) viewDiscipline, out var viewDisciplineFilter))
          viewsCollector = viewsCollector.WherePasses(viewDisciplineFilter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.VIEW_NAME, ref name, out var viewNameFilter))
          viewsCollector = viewsCollector.WherePasses(viewNameFilter);

        if (!nofilterTemplate && TryGetFilterElementIdParam(DB.BuiltInParameter.VIEW_TEMPLATE, Template?.Id ?? DB.ElementId.InvalidElementId, out var templateFilter))
          viewsCollector = viewsCollector.WherePasses(templateFilter);

        var views = collector.Cast<DB.View>();

        if (!nofilterIsTemplate)
          views = views.Where((x) => x.IsTemplate == IsTemplate);

        if (!nofilterIsAssembly)
          views = views.Where((x) => x.IsAssemblyView == IsAssembly);

        if (!nofilterIsPrintable)
          views = views.Where((x) => x.CanBePrinted == IsPrintable);

        if (viewType != DB.ViewType.Undefined)
          views = views.Where((x) => x.ViewType == viewType);

        if (name is object)
          views = views.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("Views", views);
      }
    }
  }
}

using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents
{
  public class DocumentViews : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("DF691659-B75B-4455-AF5F-8A5DE485FA05");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "V";

    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.View));

    public DocumentViews() : base
    (
      "Document.Views", "Views",
      "Get active document views list",
      "Revit", "Document"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);

      var discipline = manager[manager.AddParameter(new Param_Enum<Types.Elements.View.ViewDiscipline>(), "Discipline", "Discipline", "View discipline", GH_ParamAccess.item)] as Param_Enum<Types.Elements.View.ViewDiscipline>;
      discipline.Optional = true;

      var type = manager[manager.AddParameter(new Param_Enum<Types.Elements.View.ViewType>(), "Type", "T", "View type", GH_ParamAccess.item)] as Param_Enum<Types.Elements.View.ViewType>;
      type.SetPersistentData(DB.ViewType.Undefined);
      type.Optional = true;

      manager[manager.AddTextParameter("Name", "N", "View name", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.Documents.Filters.ElementFilter(), "Filter", "F", "Filter", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Elements.View.View(), "Views", "Views", "Views list", GH_ParamAccess.list);
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

      DB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var viewsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          viewsCollector = viewsCollector.WherePasses(filter);

        var views = collector.Cast<DB.View>();

        if (!nofilterDiscipline)
          views = views.Where((x) =>
          {
            try { return x.Discipline == viewDiscipline; }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
          });

        if (viewType != DB.ViewType.Undefined)
          views = views.Where((x) => x.ViewType == viewType);

        if (!string.IsNullOrEmpty(name))
          views = views.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("Views", views);
      }
    }
  }
}

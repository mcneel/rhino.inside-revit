using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentGroupTypes : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("97E9C6BB-8442-4F77-BCA1-6BE8AAFBDC96");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "G";

    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.GroupType));

    public DocumentGroupTypes() : base
    (
      "Document GroupTypes", "GroupTypes",
      "Get document group types list",
      "Revit", "Document"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);
      manager[manager.AddTextParameter("Name", "N", "Group name", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", "Filter", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "GroupTypes", "G", "Groups list", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      string name = null;
      DA.GetData("Name", ref name);

      DB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var viewsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          viewsCollector = viewsCollector.WherePasses(filter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.SYMBOL_NAME_PARAM, ref name, out var nameFilter))
          viewsCollector = viewsCollector.WherePasses(nameFilter);

        var groupTypes = collector.Cast<DB.GroupType>();

        if (!string.IsNullOrEmpty(name))
          groupTypes = groupTypes.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("GroupTypes", groupTypes);
      }
    }
  }
}

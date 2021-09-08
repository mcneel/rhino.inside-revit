using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using RhinoInside.Revit.External.DB.Extensions;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class QuerySheets : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("97c8cb27-955f-44cf-948d-dfbde285cd7a");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.ViewSheet));

    public QuerySheets() : base
    (
      name: "Query Sheets",
      nickname: "Sheets",
      description: "Get all document sheets",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Number", "NO", "Sheet number", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Name", "N", "Sheet name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Boolean>("Is Placeholder", "IPH", "Sheet is placeholder", false, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Boolean>("Is Indexed", "IIDX", "Sheet appears on sheet lists", true, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Sheet>("Sheets", "S", "Sheets list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      string number = null;
      DA.GetData("Number", ref number);

      string name = null;
      DA.GetData("Name", ref name);

      bool IsPlaceholder = false;
      var _IsPlaceholder_ = Params.IndexOfInputParam("Is Placeholder");
      bool nofilterIsPlaceholder = (!DA.GetData(_IsPlaceholder_, ref IsPlaceholder) && Params.Input[_IsPlaceholder_].DataType == GH_ParamData.@void);

      bool IsIndexed = false;
      var _IsIndexed_ = Params.IndexOfInputParam("Is Indexed");
      bool nofilterIsIndexed = (!DA.GetData(_IsIndexed_, ref IsIndexed) && Params.Input[_IsIndexed_].DataType == GH_ParamData.@void);

      DB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var sheetsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          sheetsCollector = sheetsCollector.WherePasses(filter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.SHEET_NUMBER, ref number, out var sheetNumberFilter))
          sheetsCollector = sheetsCollector.WherePasses(sheetNumberFilter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.SHEET_NAME, ref name, out var sheetNameFilter))
          sheetsCollector = sheetsCollector.WherePasses(sheetNameFilter);

        var sheets = collector.Cast<DB.ViewSheet>();

        if (!nofilterIsPlaceholder)
          sheets = sheets.Where((x) => x.IsPlaceholder == IsPlaceholder);

        if (!nofilterIsIndexed)
          sheets = sheets.Where((x) => x.GetParameterValue<bool>(DB.BuiltInParameter.SHEET_SCHEDULED) == IsIndexed);

        DA.SetDataList("Sheets", sheets);
      }
    }
  }
}

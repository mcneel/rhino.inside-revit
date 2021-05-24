using System;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.GH.Components
{
  public class QueryTypes : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("7B00F940-4C6E-4F3F-AB81-C3EED430DE96");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementIsElementTypeFilter(false);

    public QueryTypes() : base
    (
      name: "Query Types",
      nickname: "Types",
      description: "Get document element types list",
      category: "Revit",
      subCategory: "Type"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.FromParam(new Parameters.Document(), ParamVisibility.Voluntary),
      ParamDefinition.Create<Parameters.Category>("Category", "C", string.Empty, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Family Name", "FN", string.Empty, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Name", "N",string.Empty, GH_ParamAccess.item,optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ElementType>("Types", "E", "Element types list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      var categoryId = default(DB.ElementId);
      DA.GetData("Category", ref categoryId);

      string familyName = null;
      DA.GetData("Family Name", ref familyName);

      string name = null;
      DA.GetData("Name", ref name);

      DB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        if (categoryId is object)
          elementCollector.WhereCategoryIdEqualsTo(categoryId);

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.ALL_MODEL_FAMILY_NAME, ref familyName, out var familyNameFilter))
          elementCollector = elementCollector.WherePasses(familyNameFilter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.ALL_MODEL_TYPE_NAME, ref name, out var nameFilter))
          elementCollector = elementCollector.WherePasses(nameFilter);

        var elementTypes = elementCollector.Cast<DB.ElementType>();

        if (familyName is object)
          elementTypes = elementTypes.Where(x => x.FamilyName.IsSymbolNameLike(familyName));

        if (name is object)
          elementTypes = elementTypes.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("Types", elementTypes.Select(x => new Types.ElementType(x)));
      }
    }

    protected override string HtmlHelp_Source()
    {
      var nTopic = new Grasshopper.GUI.HTML.GH_HtmlFormatter(this)
      {
        Title = Name,
        Description =
        @"<p>This component returns all ElementTypes in the document filtered by Filter.</p>" +
        @"<p>You can also specify a name pattern as a <i>Family Name</i> and-or <i>Name</i>." +
        @"<p>Several kind of patterns are supported, the method used depends on the first pattern character:</p>" +
        @"<dl>" +
        @"<dt><b><</b></dt><dd>Starts with</dd>" +
        @"<dt><b>></b></dt><dd>Ends with</dd>" +
        @"<dt><b>?</b></dt><dd>Contains, same as a regular search</dd>" +
        @"<dt><b>:</b></dt><dd>Wildcards, see Microsoft.VisualBasic " + "<a target=\"_blank\" href=\"https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/operators/like-operator#pattern-options\">LikeOperator</a></dd>" +
        @"<dt><b>;</b></dt><dd>Regular expresion, see " + "<a target=\"_blank\" href=\"https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference\">here</a> as reference</dd>" +
        @"</dl>" +
        @"Else it looks for an exact match.",
        ContactURI = AssemblyInfo.ContactURI,
        WebPageURI = AssemblyInfo.WebPageURI
      };

      return nTopic.HtmlFormat();
    }
  }
}

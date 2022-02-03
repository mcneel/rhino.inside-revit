using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Families
{
  using External.DB;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.4")]
  public class QueryFamilies : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("B6C377BA-BC46-495C-8250-F09DB0219C91");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override ARDB.ElementFilter ElementFilter => CompoundElementFilter.ElementIsElementTypeFilter();

    public QueryFamilies() : base
    (
      name: "Query Families",
      nickname: "Families",
      description: "Get document families list",
      category: "Revit",
      subCategory: "Family"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ElementKind>>("Kind", "K", "Kind to match", defaultValue: ElementKind.System | ElementKind.Component, optional: true),
      ParamDefinition.Create<Parameters.Category>                     ("Category", "C",  optional: true),
      ParamDefinition.Create<Param_String>                            ("Family Name", "FN", optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>                ("Filter", "F", "Filter", optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Param_String>("Families", "F", "Family list", GH_ParamAccess.list)
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Input<IGH_Param>("Name") is IGH_Param name)
        name.Name = "Family Name";

      base.AddedToDocument(document);
    }

    struct FamilyNameComparer : IEqualityComparer<ARDB.ElementType>
    {
      public  bool Equals(ARDB.ElementType x, ARDB.ElementType y)
      {
        if (ReferenceEquals(x, y))
          return true;

        var categoryIdX = x?.Category?.Id ?? ARDB.ElementId.InvalidElementId;
        var familyNameX = x?.FamilyName;
        var kindX = x.GetElementKind();

        var categoryIdY = y?.Category?.Id ?? ARDB.ElementId.InvalidElementId;
        var familyNameY = y?.FamilyName;
        var kindY = y.GetElementKind();

        return (kindX == kindY) && (categoryIdX == categoryIdY) && (familyNameX == familyNameY);
      }

      public int GetHashCode(ARDB.ElementType obj) => new
      {
        Category = (obj?.Category?.Id ?? ARDB.ElementId.InvalidElementId).IntegerValue,
        Kind = obj.GetElementKind(),
        FamilyName = obj?.FamilyName
      }.
      GetHashCode();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc)) return;
      if (!Params.TryGetData(DA, "Kind", out Types.ElementKind kind)) return;
      if (!Params.TryGetData(DA, "Category", out Types.Category category)) return;
      if (!Params.TryGetData(DA, "Family Name", out string familyName)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter)) return;

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        if (kind is object && CompoundElementFilter.ElementKindFilter(kind.Value, elementType: true) is ARDB.ElementFilter kindFilter)
          elementCollector = elementCollector.WherePasses(kindFilter);

        if (category is object)
          elementCollector = elementCollector.WhereCategoryIdEqualsTo(category.Id);

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.SYMBOL_FAMILY_NAME_PARAM, ref familyName, out var nameFilter))
          elementCollector = elementCollector.WherePasses(nameFilter);

        var familiesSet = new HashSet<ARDB.ElementType>
        (
          elementCollector.
          TakeWhileIsNotEscapeKeyDown(this).
          Cast<ARDB.ElementType>(),
          default(FamilyNameComparer)
        );

        var families = familyName is null ?
          familiesSet :
          familiesSet.Where(x => x.FamilyName.IsSymbolNameLike(familyName));

        DA.SetDataList
        (
          "Families",
          families.
          Select(x => x.FamilyName)
        );
      }
    }

    protected override string HtmlHelp_Source()
    {
      var nTopic = new Grasshopper.GUI.HTML.GH_HtmlFormatter(this)
      {
        Title = Name,
        Description =
        @"<p>This component returns all Families in the document filtered by Filter.</p>" +
        @"<p>You can also specify a name pattern as Name." +
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

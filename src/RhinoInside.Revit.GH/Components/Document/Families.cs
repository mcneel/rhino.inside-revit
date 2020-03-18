using System;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Grasshopper.Kernel;
using System.Collections.Generic;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentFamilies : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("B6C377BA-BC46-495C-8250-F09DB0219C91");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementIsElementTypeFilter(false);

    public DocumentFamilies() : base
    (
      "Document Families", "Families",
      "Get document families list",
      "Revit", "Document"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);

      manager[manager.AddTextParameter("Name", "N", string.Empty, GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", "Filter", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddTextParameter("Families", "F", "Requested families", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      string name = null;
      DA.GetData("Name", ref name);

      DB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        var elementTypes = elementCollector.Cast<DB.ElementType>();

        var familiesSet = new HashSet<string>(elementTypes.Select(x => x.FamilyName));

        var families = familiesSet.AsEnumerable().Where(x => x != string.Empty);

        if (name is object)
          families = families.Where(x => x.IsSymbolNameLike(name));

        DA.SetDataList("Families", families);
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
        @"<dt><b>></b></dt><dd>Starts with</dd>" +
        @"<dt><b><</b></dt><dd>Ends with</dd>" +
        @"<dt><b>?</b></dt><dd>Contains, same as a regular search</dd>" +
        @"<dt><b>:</b></dt><dd>Wildcards, see Microsoft.VisualBasic " + "<a target=\"_blank\" href=\"https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/operators/like-operator#pattern-options\">LikeOperator</a></dd>" +
        @"<dt><b>;</b></dt><dd>Regular expresion, see " + "<a target=\"_blank\" href=\"https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference\">here</a> as reference</dd>" +
        @"<dt><b>></b></dt><dd>Else it looks for an exact match</dd>" +
        @"</dl>",
        ContactURI = AssemblyInfo.ContactURI,
        WebPageURI = AssemblyInfo.WebPageURI
      };

      return nTopic.HtmlFormat();
    }
  }
}

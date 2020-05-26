using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentElements : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("0F7DA57E-6C05-4DD0-AABF-69E42DF38859");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementIsElementTypeFilter(true);

    public DocumentElements() : base
    (
      name: "Elements",
      nickname: "Elements",
      description: "Get document model elements list",
      category: "Revit",
      subCategory: "Query"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.FromParam(DocumentComponent.CreateDocumentParam(), ParamVisibility.Voluntary),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Element>("Elements", "E", "Elements list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      DA.DisableGapLogic();

      DB.ElementFilter filter = null;
      if (!DA.GetData("Filter", ref filter))
        return;

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        DA.SetDataList
        (
          "Elements",
          collector.
          WherePasses(ElementFilter).
          WherePasses(filter).
          Select(x => Types.Element.FromElement(x))
        );
      }
    }
  }
}

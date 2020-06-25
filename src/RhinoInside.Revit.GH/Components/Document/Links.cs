using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentLinks : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("EBCCFDD8-9F3B-44F4-A209-72D06C8082A5");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "L";
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.RevitLinkInstance));

    public DocumentLinks() : base
    (
      name: "Document Links",
      nickname: "Links",
      description: "Gets Revit documents that are linked into given document",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.FromParam(DocumentComponent.CreateDocumentParam(), ParamVisibility.Voluntary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Document>("Linked Documents", "LD", "Revit documents that are linked into given document", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      using (var collector = new DB.FilteredElementCollector(doc))
      {
        DA.SetDataList
        (
          "Linked Documents",
          // find all link instances in the model, and grab their source document reference
          collector.OfClass(typeof(DB.RevitLinkInstance)).Cast<DB.RevitLinkInstance>().Select(x => Types.Document.FromDocument(x.GetLinkDocument()))
        );
      }
    }
  }
}

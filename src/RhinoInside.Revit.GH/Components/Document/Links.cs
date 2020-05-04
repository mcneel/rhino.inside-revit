using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentLinks : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("EBCCFDD8-9F3B-44F4-A209-72D06C8082A5");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "L";


    public DocumentLinks() : base
    (
      name: "Document Links",
      nickname: "Links",
      description: "Gets Revit documents that are linked into given document",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Document(),
        name: "Linked Documents",
        nickname: "LD",
        description: "Revit documents that are linked into given document",
        access: GH_ParamAccess.list
        );

    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      using (var collector = new DB.FilteredElementCollector(doc))
      {
        DA.SetDataList(
          "Linked Documents",
          // find all link instances in the model, and grab their source document reference
          collector.OfClass(typeof(DB.RevitLinkInstance)).Cast<DB.RevitLinkInstance>().Select(x => Types.Document.FromDocument(x.GetLinkDocument()))
          );
      }
    }
  }
}

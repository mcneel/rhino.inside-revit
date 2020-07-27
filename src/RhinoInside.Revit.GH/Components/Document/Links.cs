using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentLinks : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("EBCCFDD8-9F3B-44F4-A209-72D06C8082A5");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "L";
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.RevitLinkType));

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
      ParamDefinition.Create<Parameters.Document>("Documents", "D", "Revit documents that are linked into given document", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      var docs = new List<DB.Document>();
      using (var documents = Revit.ActiveDBApplication.Documents)
      {
        foreach (var id in DB.ExternalFileUtils.GetAllExternalFileReferences(doc))
        {
          var reference = DB.ExternalFileUtils.GetExternalFileReference(doc, id);
          if (reference.ExternalFileReferenceType == DB.ExternalFileReferenceType.RevitLink)
          {
            var modelPath = reference.PathType == DB.PathType.Relative ? reference.GetAbsolutePath() : reference.GetPath();
            docs.Add(documents.Cast<DB.Document>().Where(x => x.IsLinked && x.HasModelPath(modelPath)).FirstOrDefault());
          }
        }

        DA.SetDataList("Documents", docs);
      }
    }
  }
}

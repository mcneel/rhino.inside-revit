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
      ParamDefinition.FromParam(new Parameters.Document(), ParamVisibility.Voluntary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Document>("Documents", "D", "Revit documents that are linked into given document", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      // Note: linked documents that are not loaded in Revit memory,
      // are not reported since no interaction can be done if not loaded
      var docs = new List<DB.Document>();
      using (var documents = Revit.ActiveDBApplication.Documents)
      {
        /* NOTES:
         * 1) On a cloud host model with links (that are also on cloud)
         *    .GetAllExternalFileReferences does not return the "File" references
         *    to the linked cloud models
         * 2) doc.PathName is not equal to DB.ExternalResourceReference.InSessionPath
         *    e.g. Same modle but reported paths are different. Respectively:
         *    "BIM 360://Default Test/Host_Model1.rvt"
         *    vs
         *    "BIM 360://Default Test/Project Files/Linked_Project2.rvt"
         */
        foreach (var id in DB.ExternalFileUtils.GetAllExternalFileReferences(doc))
        {
          var reference = DB.ExternalFileUtils.GetExternalFileReference(doc, id);
          if (reference.ExternalFileReferenceType == DB.ExternalFileReferenceType.RevitLink)
          {
            var modelPath = reference.PathType == DB.PathType.Relative ? reference.GetAbsolutePath() : reference.GetPath();
            docs.Add(documents.Cast<DB.Document>().Where(x => x.IsLinked && x.HasModelPath(modelPath)).FirstOrDefault());
          }
        }

#if REVIT_2020
        // if no document is reported using DB.ExternalFileUtils then links
        // are in the cloud. try getting linked documents from DB.RevitLinkType
        // element types inside the host model
        if (docs.Count() == 0)
        {
          // find all the revit link types in the host model
          using (var collector = new DB.FilteredElementCollector(doc).OfClass(typeof(DB.RevitLinkType)))
          {
            foreach (var revitLinkType in collector)
            {
              // extract the path of external document that is wrapped by the revit link type
              var linkInfo = revitLinkType.GetExternalResourceReferences().FirstOrDefault();
              if (linkInfo.Key != null && linkInfo.Value.HasValidDisplayPath())
              {
                // stores custom info about the reference (project::model ids)
                var refInfo = linkInfo.Value.GetReferenceInformation();
                var linkedDoc = documents.Cast<DB.Document>()
                                         .Where(x => x.IsLinked &&
                                                     x.GetCloudModelPath().GetModelGUID() == Guid.Parse(refInfo["LinkedModelModelId"]))
                                         .FirstOrDefault();
                if (linkedDoc != null)
                  docs.Add(linkedDoc);
              }
            }
          }
        }
#endif

        DA.SetDataList("Documents", docs);
      }
    }
  }
}

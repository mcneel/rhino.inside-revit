using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents
{
  public class DocumentLinks : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("EBCCFDD8-9F3B-44F4-A209-72D06C8082A5");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "L";
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.RevitLinkType));

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.ManageLinks);
      Menu_AppendItem
      (
        menu, $"Manage Links…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

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
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
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

      // Note:
      // linked documents that are not loaded in Revit memory,
      // are not reported since no interaction can be done if not loaded
      var docs = new Dictionary<string, DB.Document>();
      using (var collector = new DB.FilteredElementCollector(doc).OfClass(typeof(DB.RevitLinkInstance)))
      {
        foreach (DB.RevitLinkInstance linkInstance in collector.Cast<DB.RevitLinkInstance>())
        {
          var linkedDoc = linkInstance.GetLinkDocument();
          if (!docs.ContainsKey(linkedDoc.PathName))
            docs[linkedDoc.PathName] = linkedDoc;
        }
      }

      DA.SetDataList("Documents", docs.Values);
    }
  }
}

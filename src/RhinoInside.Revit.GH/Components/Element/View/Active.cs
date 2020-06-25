using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ViewActive : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("7CCF350C-80CC-42D0-85BA-78544FD59F4A");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "A";

    public ViewActive() : base
    (
      name: "Active Graphical View",
      nickname: "AGraphView",
      description: "Gets the active graphical view",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.FromParam(CreateDocumentParam(), ParamVisibility.Voluntary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.View>("Active View", "V", string.Empty, GH_ParamAccess.item)
    };

    static bool IsGraphicalViewType(DB.ViewType viewType)
    {
      switch (viewType)
      {
        case DB.ViewType.Undefined:
        case DB.ViewType.ProjectBrowser:
        case DB.ViewType.SystemBrowser:
          return false;
      }

      return true;
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      using (var uiDocument = new Autodesk.Revit.UI.UIDocument(doc))
      {
        var activeView = uiDocument.ActiveGraphicalView;
        if (activeView is null)
        {
          var openViews = uiDocument.GetOpenUIViews().
          Select(x => doc.GetElement(x.ViewId)).
          OfType<DB.View>().
          Where(x => IsGraphicalViewType(x.ViewType));

          activeView = openViews.FirstOrDefault();
        }

        DA.SetData("Active View", activeView);
      }
    }
  }
}

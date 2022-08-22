using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.GH.Components.Site
{
  public class ActiveProjectLocation : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("B8677884-61E8-4D3F-8ACB-0873B2A40053");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "⌖";

    #region UI
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);
      Menu_AppendSeparator(menu);

      var activeApp = Revit.ActiveUIApplication;
      var LocationId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.Location);
      Menu_AppendItem
      (
        menu, "Location…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, LocationId),
        activeApp.CanPostCommand(LocationId), false
      );

      var online = Menu_AppendItem(menu, "Online");
      Menu_AppendItem
      (
        online.DropDown, "Bing Maps…",
        (sender, arg) =>
        {
          if (activeApp.ActiveUIDocument.Document.SiteLocation is Autodesk.Revit.DB.SiteLocation location)
            using (System.Diagnostics.Process.Start($@"https://bing.com/maps/default.aspx?cp={Rhino.RhinoMath.ToDegrees(location.Latitude)}~{Rhino.RhinoMath.ToDegrees(location.Longitude)}&lvl=18")) { }
        },
        activeApp.ActiveUIDocument is object, false
      );

      Menu_AppendItem
      (
        online.DropDown, "DuckDuckGo…",
        (sender, arg) =>
        {
          if (activeApp.ActiveUIDocument.Document.SiteLocation is Autodesk.Revit.DB.SiteLocation location)
            using (System.Diagnostics.Process.Start($@"https://duckduckgo.com/?q={Rhino.RhinoMath.ToDegrees(location.Latitude)}%2C{Rhino.RhinoMath.ToDegrees(location.Longitude)}&iaxm=maps")) { }
        },
        activeApp.ActiveUIDocument is object, false
      );

      Menu_AppendItem
      (
        online.DropDown, "Google Maps…",
        (sender, arg) =>
        {
          if (activeApp.ActiveUIDocument.Document.SiteLocation is Autodesk.Revit.DB.SiteLocation location)
            using (System.Diagnostics.Process.Start($@"https://www.google.com/maps/@{Rhino.RhinoMath.ToDegrees(location.Latitude)},{Rhino.RhinoMath.ToDegrees(location.Longitude)},18z")) { }
        },
        activeApp.ActiveUIDocument is object, false
      );

      Menu_AppendItem
      (
        online.DropDown, "OpenStreetMap…",
        (sender, arg) =>
        {
          if (activeApp.ActiveUIDocument.Document.SiteLocation is Autodesk.Revit.DB.SiteLocation location)
            using (System.Diagnostics.Process.Start($@"https://www.openstreetmap.org/?mlat={Rhino.RhinoMath.ToDegrees(location.Latitude)}&mlon={Rhino.RhinoMath.ToDegrees(location.Longitude)}")) { }
        },
        activeApp.ActiveUIDocument is object, false
      );
    }
    #endregion

    public ActiveProjectLocation()
    : base
    (
      "Project Location",
      "Location",
      "Project location.",
      "Revit",
      "Site"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Document>("Project", "P", optional: true, relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Parameters.ProjectLocation>("Shared Site", "SS", "New current Shared Site", optional: true, relevance: ParamRelevance.Tertiary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.SiteLocation>("Site Location", "SL", "Project site location", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.ProjectLocation>("Shared Site", "SS", "Project current Shared Site", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.BasePoint>("Survey Point", "SP", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.BasePoint>("Project Base Point", "PBP", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.BasePoint>("Internal Origin", "IO", relevance: ParamRelevance.Occasional),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Project", out var doc)) return;
      if (doc.IsFamilyDocument)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "'Project' is not a valid Project document");
        return;
      }

      if (Params.GetData(DA, "Shared Site", out Types.ProjectLocation location, x => x.IsValid))
      {
        if (!doc.Equals(location.Document))
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "'Shared Site' is not valid on 'Project' document");
          return;
        }

        StartTransaction(doc);
        doc.ActiveProjectLocation = location.Value;
      }

      Params.TrySetData(DA, "Site Location", () => new Types.SiteLocation(doc.SiteLocation));
      Params.TrySetData(DA, "Shared Site", () => new Types.ProjectLocation(doc.ActiveProjectLocation));
      Params.TrySetData(DA, "Survey Point", () => new Types.BasePoint(BasePointExtension.GetSurveyPoint(doc)));
      Params.TrySetData(DA, "Project Base Point", () => new Types.BasePoint(BasePointExtension.GetProjectBasePoint(doc)));
      Params.TrySetData(DA, "Internal Origin", () => new Types.InternalOrigin(InternalOriginExtension.Get(doc)));
    }
  }
}

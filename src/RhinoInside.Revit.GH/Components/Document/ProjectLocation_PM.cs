using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents
{
  public class ProjectLocation_PM : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("6111B626-9F63-410C-9241-D623824C2352");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "i";

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.ProjectInformation);
      Menu_AppendItem
      (
        menu, $"Edit Project Information…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

    public ProjectLocation_PM()
    : base
    (
      "Project Information",
      "Information",
      "Project information.",
      "Revit",
      "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Document>("Project", "P", relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Boolean>("Import", "IT", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("City", "C", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Latitude", "LT", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Longitutde", "LG", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("TimeZone", "TZ", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("North", "N", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Location", "L", optional: true, relevance: ParamRelevance.Primary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var info = default(ARDB.ProjectInfo);


      if (!Parameters.Document.GetDataOrDefault(this, DA, "Project", out var doc)) return;
      if (doc.IsFamilyDocument || (info = doc.ProjectInformation) == null)
      {
        info = doc.ProjectInformation;
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "'Project' is not a valid Project document");
        return;
      }
      if (!Params.TryGetData(DA, "Import", out bool? Import)) return;
      if (!Params.TryGetData(DA, "City", out string CityLocation)) return;
      if (!Params.TryGetData(DA, "Latitude", out string LocLatitude)) return;
      if (!Params.TryGetData(DA, "Longitutde", out string LocLongitude)) return;
      if (!Params.TryGetData(DA, "TimeZone", out string TimeZone)) return;
      if (!Params.TryGetData(DA, "North", out string LocNorth)) return;
      if (!Params.TryGetData(DA, "Location", out string LocLocation)) return;

      Import = false;
      if (Import == false)
      {
        return;
      }
      double NumberLocLatitude;
      double NumberLocLongitude;
      StartTransaction(doc);
      if (CityLocation != null) info.Address = CityLocation;
      if (CityLocation != null) doc.SiteLocation.PlaceName = CityLocation;
      if (LocLatitude != null)
      {
        if (Double.TryParse(LocLatitude, out NumberLocLatitude))
          doc.SiteLocation.Latitude = NumberLocLatitude;
        else
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please enter a numerical value for Latitude");
         return;
      }
      if (LocLongitude != null)
      {
        if (Double.TryParse(LocLatitude, out NumberLocLongitude))
          doc.SiteLocation.Latitude = NumberLocLongitude;
        else
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please enter a numerical value for Longitude");
        return;
      }


    }
  }
}


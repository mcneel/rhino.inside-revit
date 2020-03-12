using System;
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
      "View.Active", "View.Active",
      "Gets the active document",
      "Revit", "View"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager) { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.View(), "Active View", "Active View", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      DA.SetData("Active View", doc?.ActiveView);
    }
  }
}

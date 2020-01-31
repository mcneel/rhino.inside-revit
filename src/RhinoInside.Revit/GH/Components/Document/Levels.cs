using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentLevels : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("87715CAF-92A9-4B14-99E5-F8CCB2CC19BD");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.Level));

    public DocumentLevels() : base
    (
      "Document.Levels", "Levels",
      "Get active document levels list",
      "Revit", "Document"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Level(), "Levels", "Levels", "Levels list", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      using (var collector = new DB.FilteredElementCollector(Revit.ActiveDBDocument).OfClass(typeof(DB.Level)))
        DA.SetDataList("Levels", collector);
    }
  }
}

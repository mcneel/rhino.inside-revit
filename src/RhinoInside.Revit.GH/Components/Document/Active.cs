using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentActive : Component
  {
    public override Guid ComponentGuid => new Guid("EE033516-C1DC-4C72-8FCD-F85F38A0F267");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "A";

    public DocumentActive() : base
    (
      "Active Document", "Active",
      "Gets the active document",
      "Revit", "Document"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager) { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Document(), "Active Document", "Active Document", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (Parameters.Document.GetDataOrDefault(this, default, default, out var Document))
        DA.SetData("Active Document", Document);
    }
  }
}

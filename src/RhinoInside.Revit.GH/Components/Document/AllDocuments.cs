using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Documents
{
  using External.DB.Extensions;
  public class AllDocuments : Component
  {
    public override Guid ComponentGuid => new Guid("5B935CA4-E96D-4E8F-A36E-31708017634B");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "D";

    public AllDocuments() : base
    (
      name: "Open Documents",
      nickname: "Documents",
      description: "Gets the list of all open documents",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager) { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Document(), "Projects", "P", "Active Project documents list", GH_ParamAccess.list);
      manager.AddParameter(new Parameters.Document(), "Families", "F", "Active Family documents list", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Revit.ActiveDBApplication.GetOpenDocuments(out var projects, out var families);

      DA.SetDataList("Projects", projects);
      DA.SetDataList("Families", families);
    }
  }
}

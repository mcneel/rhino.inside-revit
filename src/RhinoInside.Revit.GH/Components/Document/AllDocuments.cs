using System;
using System.Collections;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AllDocuments : Component
  {
    public override Guid ComponentGuid => new Guid("5B935CA4-E96D-4E8F-A36E-31708017634B");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "D";

    public AllDocuments() : base
    (
      name: "All Documents",
      nickname: "Documents",
      description: "Gets the list of all active documents",
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
      using (var Documents = Revit.ActiveDBApplication.Documents)
      {
        IList projects = Documents.Cast<DB.Document>().Where(x => !x.IsFamilyDocument && !x.IsLinked).ToArray();
        IList families = Documents.Cast<DB.Document>().Where(x =>  x.IsFamilyDocument && !x.IsLinked).ToArray();

        DA.SetDataList("Projects", projects);
        DA.SetDataList("Families", families);
      }
    }
  }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class RevitActiveDocument : Component
  {
    public override Guid ComponentGuid => new Guid("EE033516-C1DC-4C72-8FCD-F85F38A0F267");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "A";

    public RevitActiveDocument() : base
    (
      "Revit.ActiveDocument", "ActiveDocument",
      "Get the active document",
      "Revit", "Document"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager) { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Document(), "Document", "D", "Active document", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DA.SetData("Document", Revit.ActiveDBDocument);
    }
  }

  public class RevitDocuments : Component
  {
    public override Guid ComponentGuid => new Guid("5B935CA4-E96D-4E8F-A36E-31708017634B");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "D";

    public RevitDocuments() : base
    (
      "Revit.Documents", "Documents",
      "Get list of active documents",
      "Revit", "Document"
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

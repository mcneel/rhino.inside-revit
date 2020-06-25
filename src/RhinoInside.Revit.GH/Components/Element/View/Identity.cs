using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ViewIdentity : Component
  {
    public override Guid ComponentGuid => new Guid("B0440885-4AF3-4890-8E84-3BC2A8342B9F");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ID";

    public ViewIdentity() : base
    (
      name: "View Identity",
      nickname: "Identity",
      description: "Query view identity information",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.View(), "View", "View", string.Empty, GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Param_Enum<Types.ViewDiscipline>(), "Discipline", "D", "View discipline", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Param_Enum<Types.ViewFamily>(), "Family", "F", "View family", GH_ParamAccess.item);
      manager.AddTextParameter("Name", "N", "View name", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.View(), "Template", "T", "View template", GH_ParamAccess.list);
      manager.AddBooleanParameter("Is Template", "IT", "View is template", GH_ParamAccess.item);
      manager.AddBooleanParameter("Is Assembly", "IA", "View is assembly", GH_ParamAccess.item);
      manager.AddBooleanParameter("Is Printable", "IP", "View is printable", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var view = default(DB.View);
      if (!DA.GetData("View", ref view))
        return;

      if (view.HasViewDiscipline())
        DA.SetData("Discipline", view.Discipline);
      else
        DA.SetData("Discipline", null);

      DA.SetData("Family", (view.Document.GetElement(view.GetTypeId()) as DB.ViewFamilyType).ViewFamily);
      DA.SetData("Name", view.Name);
      DA.SetData("Template", view.Document.GetElement(view.ViewTemplateId) as DB.View);
      DA.SetData("Is Template", view.IsTemplate);
      DA.SetData("Is Assembly", view.IsAssemblyView);
      DA.SetData("Is Printable", view.CanBePrinted);
    }
  }
}

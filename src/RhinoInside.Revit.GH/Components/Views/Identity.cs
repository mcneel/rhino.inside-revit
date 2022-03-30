using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.2.1")]
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
      manager.AddBooleanParameter("Is Printable", "IP", "View is printable", GH_ParamAccess.item);
      manager.AddBooleanParameter("Is Assembly", "IA", "View is assembly", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.AssemblyInstance(), "Assembly", "A", "Assembly instance that owns the assembly view", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var view = default(ARDB.View);
      if (!DA.GetData("View", ref view))
        return;

      if (view.HasViewDiscipline())
        DA.SetData("Discipline", view.Discipline);
      else
        DA.SetData("Discipline", null);

      DA.SetData("Family", view.GetViewFamily());
      DA.SetData("Name", view.Name);
      DA.SetData("Template", new Types.View(view.Document, view.ViewTemplateId));
      DA.SetData("Is Template", view.IsTemplate);
      DA.SetData("Is Printable", view.CanBePrinted);
      DA.SetData("Is Assembly", view.IsAssemblyView);
      DA.SetData("Assembly", new Types.AssemblyInstance(view.Document, view.AssociatedAssemblyInstanceId));
    }
  }
}

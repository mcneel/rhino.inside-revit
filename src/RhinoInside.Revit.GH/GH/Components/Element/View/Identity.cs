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
      "View.Identity", "View.Identity",
      "Query view identity information",
      "Revit", "View"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.View(), "View", "View", string.Empty, GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Param_Enum<Types.ViewDiscipline>(), "Discipline", "Discipline", "View discipline", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Param_Enum<Types.ViewType>(), "Type", "Type", "View type", GH_ParamAccess.item);
      manager.AddTextParameter("Name", "Name", "View name", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var view = default(DB.View);
      if (!DA.GetData("View", ref view))
        return;

      if (view.HasViewDiscipline())
        DA.SetData("Discipline", view.Discipline);

      DA.SetData("Type", view.ViewType);
      DA.SetData("Name", view.Name);
    }
  }
}

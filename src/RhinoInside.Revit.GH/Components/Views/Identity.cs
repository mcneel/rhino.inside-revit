using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  using External.DB.Extensions;
  using Grasshopper.Kernel.Parameters;

  [ComponentVersion(introduced: "1.0", updated: "1.7")]
  public class ViewIdentity : ZuiComponent
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

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.View>("View", "V", "View to query")
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Param_Enum<Types.ViewDiscipline>>("Discipline", "D", "View discipline", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ViewFamily>>("View Family", "VF", "View family"),
      ParamDefinition.Create<Param_String>("View Name", "VN", "View name"),
      ParamDefinition.Create<Param_Boolean>("Is Dependent", "D", "View is dependent", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.View>("View Dependency", "VD", "View depedency", relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Boolean>("Is Template", "IT", "View is template", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.View>("View Template", "T", "View template", relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Boolean>("Is Assembly", "IA", "View is assembly", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.AssemblyInstance>("Associated Assembly", "AA", "The assembly the view is associated with", relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Boolean>("Is Printable", "IP", "View is printable", relevance: ParamRelevance.Primary),
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Output<IGH_Param>("Family") is IGH_Param family)
        family.Name = "View Family";

      if (Params.Output<IGH_Param>("Name") is IGH_Param name)
        name.Name = "View Name";

      if (Params.Output<IGH_Param>("Template") is IGH_Param template)
        template.Name = "View Template";

      if (Params.Output<IGH_Param>("Assembly") is IGH_Param assembly)
        assembly.Name = "Associated Assembly";

      base.AddedToDocument(document);
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

      Params.TrySetData(DA, "View Family", () => view.GetViewFamily());
      Params.TrySetData(DA, "View Name", () => view.Name);
      Params.TrySetData(DA, "Is Dependent", () => view.GetPrimaryViewId() != ARDB.ElementId.InvalidElementId);
      Params.TrySetData(DA, "View Depedency", () => Types.View.FromElementId(view.Document, view.GetPrimaryViewId()));
      Params.TrySetData(DA, "Is Template", () => view.IsTemplate);
      Params.TrySetData(DA, "View Template", () => Types.View.FromElementId(view.Document, view.ViewTemplateId));
      Params.TrySetData(DA, "Is Assembly", () => view.IsAssemblyView);
      Params.TrySetData(DA, "Associated Assembly", () => new Types.AssemblyInstance(view.Document, view.AssociatedAssemblyInstanceId));
      Params.TrySetData(DA, "Is Printable", () => view.CanBePrinted);
    }
  }
}

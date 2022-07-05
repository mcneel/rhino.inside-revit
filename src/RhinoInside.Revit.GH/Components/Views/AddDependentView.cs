using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  [ComponentVersion(introduced: "1.7")]
  public class AddDependentView : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("36842B86-7C55-42CB-AD8F-28CA779495D0");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public AddDependentView() : base
    (
      name: "Add Dependent View",
      nickname: "DependatView",
      description: "Add a dependent Revit View.",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "View",
          Description = "Parent View",
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "View",
          Description = "Parent View",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = _Dependent_,
          NickName = _Dependent_.Substring(0, 1),
          Description = "Dependent view",
        }
      )
    };

    const string _Dependent_ = "Dependent";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      else Params.TrySetData(DA, "View", () => view);

      ReconstructElement<ARDB.View>
      (
        view.Document, _Dependent_, dependent =>
        {
          if (!view.Value.CanViewBeDuplicated(ARDB.ViewDuplicateOption.AsDependent))
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"'{view.DisplayName}' view can not be duplicated as Dependent. {{{view.Id}}}");
            return null;
          }

          // Compute
          if
          (
            dependent is null ||
            dependent.GetPrimaryViewId() != view.Id ||
            !dependent.Document.Equals(view.Document)
          )
          {
            dependent = view.Document.GetElement(view.Value.Duplicate(ARDB.ViewDuplicateOption.AsDependent)) as ARDB.View;
          }

          DA.SetData(_Dependent_, dependent);
          return dependent;
        }
      );
    }
  }
}

using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Topology
{
  [ComponentVersion(introduced: "1.9")]
  public class SpatialElementIdentity : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("E3D32938-0E10-4B93-A40D-0781D8842ECE");
    protected override string IconTag => "ID";

    public SpatialElementIdentity() : base
    (
      name: "Spatial Element Identity",
      nickname: "Identity",
      description: "Query spatial element identity information",
      category: "Revit",
      subCategory: "Topology"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.SpatialElement()
        {
          Name = "Spatial Element",
          NickName = "SE",
          Description = "Spatial element to extract identity.",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.SpatialElement()
        {
          Name = "Spatial Element",
          NickName = "SE",
          Description = "Spatial element.",
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Location",
          NickName = "L",
          Description = "Element location point.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Placed",
          NickName = "PD",
          Description = "Wheter element is placed or not.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Number",
          NickName = "NUM",
          Description = "Element Number.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Element Name.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Level",
          NickName = "LV",
          Description = "Level on which the element resides.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Phase()
        {
          Name = "Phase",
          NickName = "PH",
          Description = "Project phase to which the element belongs.",
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Enclosed",
          NickName = "ED",
          Description = "Wheter element is on a properly enclosed region or not.",
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Perimeter",
          NickName = "PE",
          Description = "Element perimeter.",
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Area",
          NickName = "AR",
          Description = "Element area.",
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Volume",
          NickName = "VO",
          Description = "Element volume.",
        }, ParamRelevance.Occasional
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Spatial Element", out Types.SpatialElement element, x => x.IsValid)) return;
      else Params.TrySetData(DA, "Spatial Element", () => element);

      Params.TrySetData(DA, "Point", () => element.Position);
      Params.TrySetData(DA, "Placed", () => element.IsPlaced);
      Params.TrySetData(DA, "Number", () => element.Number);
      Params.TrySetData(DA, "Name", () => element.Name);
      Params.TrySetData(DA, "Level", () => element.Level);
      Params.TrySetData(DA, "Phase", () => element.Phase);
      Params.TrySetData(DA, "Enclosed", () => element.IsEnclosed);
      Params.TrySetData(DA, "Perimeter", () => element.Perimeter);
      Params.TrySetData(DA, "Area", () => element.Area);
      Params.TrySetData(DA, "Volume", () => element.Volume);
    }
  }
}

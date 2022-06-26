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
          Name = "Point",
          NickName = "Pt",
          Description = "Point to query.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Placed",
          NickName = "Pd",
          Description = "Wheter element is placed or not.",
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Number",
          NickName = "Nu",
          Description = "Element Number.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "Nm",
          Description = "Element Name.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Level",
          NickName = "Lv",
          Description = "Element level.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Phase()
        {
          Name = "Phase",
          NickName = "Ph",
          Description = "Element phase.",
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Enclosed",
          NickName = "Ed",
          Description = "Wheter element is enclosed or not.",
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Perimeter",
          NickName = "Pe",
          Description = "Element Perimeter.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Area",
          NickName = "Ar",
          Description = "Element Area.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Volume",
          NickName = "Vo",
          Description = "Element Volume.",
        }, ParamRelevance.Primary
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

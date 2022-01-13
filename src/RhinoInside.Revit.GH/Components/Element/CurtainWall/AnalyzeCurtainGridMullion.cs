using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Walls
{
  public class AnalyzeCurtainGridMullion : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("4EECA86B-551C-4ADA-8FDA-03B7326735ED");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "ACGM";

    public AnalyzeCurtainGridMullion() : base
    (
      name: "Analyze Mullion",
      nickname: "A-M",
      description: "Analyze given mullion element",
      category: "Revit",
      subCategory: "Wall"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Mullion(),
        name: "Mullion",
        nickname: "M",
        description: "Mullion element to analyze",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Type",
        nickname: "T",
        description: "Curtain Grid Mullion Type",
        access: GH_ParamAccess.item
        );
      manager.AddCurveParameter(
        name: "Axis Curve",
        nickname: "C",
        description: "Axis curve of the given curtain grid mullion instance",
        access: GH_ParamAccess.item
        );
      manager.AddPointParameter(
        name: "Base Point",
        nickname: "BP",
        description: "Base point of given given curtain grid mullion instance",
        access: GH_ParamAccess.item
        );
      //manager.AddBooleanParameter(
      //  name: "Locked",
      //  nickname: "LD",
      //  description: "Whether curtain grid mullion line is locked",
      //  access: GH_ParamAccess.item
      //  );
      //manager.AddBooleanParameter(
      //  name: "Lockable",
      //  nickname: "L",
      //  description: "Whether curtain grid mullion line is lockable",
      //  access: GH_ParamAccess.item
      //  );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Mullion", out Types.Mullion mullion, x => x.IsValid))
        return;

      DA.SetData("Type", mullion.Type);
      DA.SetData("Axis Curve", mullion.Curve);
      DA.SetData("Base Point", mullion.Location.Origin);
      //DA.SetData("Locked", mullion.Value?.Lock);
      //DA.SetData("Lockable", mullion.Value?.Lockable);
    }
  }
}

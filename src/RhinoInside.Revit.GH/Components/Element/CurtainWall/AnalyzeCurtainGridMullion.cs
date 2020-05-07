using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AnalyseCurtainGridMullion : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("4EECA86B-551C-4ADA-8FDA-03B7326735ED");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ACGM";

    public AnalyseCurtainGridMullion() : base(
      name: "Analyze Curtain Grid Mullion",
      nickname: "A-CGM",
      description: "Analyze given curtain grid mullion",
      category: "Revit",
      subCategory: "Analyze"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Mullion(),
        name: "Mullion",
        nickname: "M",
        description: "Curtain Grid Mullion",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Type",
        nickname: "CGMT",
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
      manager.AddBooleanParameter(
        name: "Locked",
        nickname: "LD",
        description: "Whether curtain grid mullion line is locked",
        access: GH_ParamAccess.item
        );
      manager.AddBooleanParameter(
        name: "Lockable",
        nickname: "L",
        description: "Whether curtain grid mullion line is lockable",
        access: GH_ParamAccess.item
        );
      //manager.AddNumberParameter(
      //  name: "Mullion Length",
      //  nickname: "L",
      //  description: "Mullion Length",
      //  access: GH_ParamAccess.item
      //  );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // get input
      DB.Mullion mullionInstance = default;
      if (!DA.GetData("Mullion", ref mullionInstance))
        return;

      DA.SetData("Type", Types.ElementType.FromElement(mullionInstance.MullionType));
      DA.SetData("Axis Curve", mullionInstance.LocationCurve?.ToCurve());
      DA.SetData("Base Point", ((DB.LocationPoint) mullionInstance.Location).Point.ToPoint3d());
      DA.SetData("Locked", mullionInstance.Lock);
      DA.SetData("Lockable", mullionInstance.Lockable);
      // Length can be acquired from axis curve
      // Conversion to GH_Curve results in a zero length curve
      //PipeHostParameter(DA, mullionInstance, DB.BuiltInParameter.CURVE_ELEM_LENGTH, "Mullion Length");
    }
  }
}

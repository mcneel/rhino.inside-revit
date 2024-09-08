using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  public class AnalyzeCurtainGridCell : Component
  {
    public override Guid ComponentGuid => new Guid("FC7D5729-7D27-453A-A4A2-0E150C749083");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "CCG";

    public AnalyzeCurtainGridCell() : base
    (
      name: "Curtain Cell Profile",
      nickname: "CC-Profile",
      description: "Deconstruct given curtain grid cell in to geometry",
      category: "Revit",
      subCategory: "Architecture"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter
      (
        param: new Parameters.CurtainCell(),
        name: "Curtain Grid Cell",
        nickname: "CGC",
        description: "Curtain Grid Cell",
        access: GH_ParamAccess.item
      );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddCurveParameter
      (
        name: "Curves",
        nickname: "C",
        description: "Boundary curves of the given grid cell",
        access: GH_ParamAccess.item
      );
      manager.AddCurveParameter
      (
        name: "Planarized Curves",
        nickname: "PC",
        description: "Boundary curves of the flat surface fitted inside a curved grid cell",
        access: GH_ParamAccess.item
      );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Curtain Grid Cell", out Types.CurtainCell cell)) return;

      DA.SetDataList("Curves", cell.CurveLoops);
      DA.SetDataList("Planarized Curves", cell.PlanarizedCurveLoops);
    }
  }
}

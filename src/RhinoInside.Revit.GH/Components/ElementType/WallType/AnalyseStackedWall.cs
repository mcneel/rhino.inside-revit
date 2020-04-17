using System;
using System.Linq;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AnalyzeStackedWall : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("DF10B918-A30F-4609-AE77-14314E6CDBF1");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ASW";

    public AnalyzeStackedWall() : base(
      name: "Analyze Stacked Wall",
      nickname: "A-SW",
      description: "Analyze given Stacked Wall element",
      category: "Revit",
      subCategory: "Analyze"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Stacked Wall",
        nickname: "SW",
        description: "Stacked Wall element",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Basic Walls",
        nickname: "BW",
        description: "Basic Wall instances that are part of given Stacked Wall",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input wall type
      DB.Wall wallInstance = default;
      if (!DA.GetData("Stacked Wall", ref wallInstance))
        return;

      if (wallInstance.IsStackedWall)
      {
        DA.SetDataList("Basic Walls", wallInstance.GetStackedWallMemberIds().Select(x => wallInstance.Document.GetElement(x)).Select(x => Types.Element.FromElement(x)).ToList());
      }
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AnalyzeFootPrintRoof : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("d7800083-7950-4c3f-8b86-f162ba36144c");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "ASGR";

    public AnalyzeFootPrintRoof() : base(
      name: "Analyze Sloped Glazing Roof",
      nickname: "A-SGR",
      description: "Analyze given Sloped Glazing Roof element",
      category: "Revit",
      subCategory: "Roof"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Roof(),
        name: "Sloped Glazing Roof",
        nickname: "SGR",
        description: "Sloped Glazing Roof element",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.DataObject<DB.CurtainGrid>(),
        name: "Curtain Grids",
        nickname: "CG",
        description: "Curtain Grid definition associated with each face of the input Sloped Glazing Roof",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input
      DB.FootPrintRoof footPrintRoofInstance = default;
      if (!DA.GetData("Sloped Glazing Roof", ref footPrintRoofInstance))
        return;

      if (footPrintRoofInstance.CurtainGrids != null)
      {
        var cGrids = new List<DB.CurtainGrid>();
        foreach (DB.CurtainGrid cgrid in footPrintRoofInstance.CurtainGrids)
          cGrids.Add(cgrid);

        DA.SetDataList("Curtain Grids", cGrids.Select(x => new Types.DataObject<DB.CurtainGrid>(x, srcDocument: footPrintRoofInstance.Document)));
      }
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public class AnalyzeStructuralAsset : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("ec93f8e0-d2af-4a44-a040-89a7c40b9fc7");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public AnalyzeStructuralAsset() : base(
      name: "Analyze Structural Asset",
      nickname: "A-STAST",
      description: "Analyze given structural asset",
      category: "Revit",
      subCategory: "Material"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {

    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {

    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {

    }
  }
}

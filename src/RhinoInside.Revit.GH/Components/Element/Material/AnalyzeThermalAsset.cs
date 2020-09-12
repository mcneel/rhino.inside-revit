using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public class AnalyzeThermalAsset : AnalysisComponent
  {
    public override Guid ComponentGuid =>
      new Guid("c3be363d-c01d-4cf3-b8d2-c345734ae66d");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public AnalyzeThermalAsset() : base(
      name: "Analyze Thermal Asset",
      nickname: "A-THAST",
      description: "Analyze given thermal asset",
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

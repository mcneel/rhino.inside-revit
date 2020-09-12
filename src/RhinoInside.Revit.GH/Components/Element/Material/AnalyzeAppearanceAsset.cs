using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public class AnalyzeAppearanceAsset : AnalysisComponent
  {
    public override Guid ComponentGuid =>
      new Guid("5b18389b-5e25-4428-b1a6-1a55109a7a3c");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public AnalyzeAppearanceAsset() : base(
      name: "Analyze Appearance Asset",
      nickname: "A-APAST",
      description: "Analyze given appearance asset",
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

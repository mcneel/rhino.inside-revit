using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public class AnalyzePhysicalAsset : AnalysisComponent
  {
    public override Guid ComponentGuid =>
      new Guid("ec93f8e0-d2af-4a44-a040-89a7c40b9fc7");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public AnalyzePhysicalAsset() : base(
      name: "Analyze Physical Asset",
      nickname: "A-PHAST",
      description: "Analyze given physical asset",
      category: "Revit",
      subCategory: "Material"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(
        param: new Parameters.PhysicalAsset(),
        name: "Physical Asset",
        nickname: "PA",
        description: string.Empty,
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {

    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.PropertySetElement psetElement = default;
      if (!DA.GetData("Physical Asset", ref psetElement))
        return;

      var structAsset = psetElement.GetStructuralAsset();

    }
  }
}

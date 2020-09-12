using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public class ModifyAssetsOfMaterial : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("2f1ec561-2c4b-4c44-9587-12b32c6b8351");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public ModifyAssetsOfMaterial() : base(
      name: "Replace Material's Assets",
      nickname: "R-MAST",
      description: "Replace existing assets on the given material, with given assets",
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

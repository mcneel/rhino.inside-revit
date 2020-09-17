using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;

  using Grasshopper.Kernel;
  using DB = Autodesk.Revit.DB;

  namespace RhinoInside.Revit.GH.Components.Element.Material
  {
    public class ModifyThermalAsset : AnalysisComponent
    {
      public override Guid ComponentGuid =>
        new Guid("2c8f541a-f831-41e1-9e19-3c5a9b07aed4");
      public override GH_Exposure Exposure => GH_Exposure.quinary;

      public ModifyThermalAsset() : base(
        name: "Modify Thermal Asset",
        nickname: "M-THAST",
        description: "Modify given thermal asset",
        category: "Revit",
        subCategory: "Material"
      )
      {
      }

      protected override void RegisterInputParams(GH_InputParamManager pManager)
      {
        pManager.AddParameter(
          param: new Parameters.ThermalAsset(),
          name: "Thermal Asset",
          nickname: "TA",
          description: string.Empty,
          access: GH_ParamAccess.item
          );
      }

      protected override void RegisterOutputParams(GH_OutputParamManager pManager)
      {

      }

      protected override void TrySolveInstance(IGH_DataAccess DA)
      {

      }
    }
  }
}

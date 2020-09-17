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
    public class ModifyPhysicalAsset : AnalysisComponent
    {
      public override Guid ComponentGuid =>
        new Guid("67a74d31-0878-4b48-8efb-f4ca97389f74");
      public override GH_Exposure Exposure => GH_Exposure.quinary;

      public ModifyPhysicalAsset() : base(
        name: "Modify Physical Asset",
        nickname: "M-PHAST",
        description: "Modify given physical asset",
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

      }
    }
  }
}

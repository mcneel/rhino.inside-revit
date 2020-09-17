using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public class CreateThermalAsset : DocumentComponent
  {
    public override Guid ComponentGuid =>
      new Guid("bd9164c4-effb-4145-bb96-006daeaeb99a");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
    };
    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ThermalAsset>(
        name: "Thermal Asset",
        nickname: "TA",
        description: string.Empty,
        access: GH_ParamAccess.item
        )
    };

    public CreateThermalAsset() : base(
      name: "Create Thermal Asset",
      nickname: "C-THAST",
      description: "Create a new instance of thermal asset inside document",
      category: "Revit",
      subCategory: "Material"
    )
    {
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {

    }
  }
}

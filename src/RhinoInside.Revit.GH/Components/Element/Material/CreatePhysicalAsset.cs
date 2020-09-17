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
  public class CreatePhysicalAsset : DocumentComponent
  {
    public override Guid ComponentGuid =>
      new Guid("af2678c8-2a53-4056-9399-5a06dd9ac14d");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
    };
    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.PhysicalAsset>(
        name: "Physical Asset",
        nickname: "PA",
        description: string.Empty,
        access: GH_ParamAccess.item
        )
    };

    public CreatePhysicalAsset() : base(
      name: "Create Physical Asset",
      nickname: "C-PHAST",
      description: "Create a new instance of physical asset inside document",
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

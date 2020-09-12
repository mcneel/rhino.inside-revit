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
  public class CreateAppearanceAsset : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("0f251f87-317b-4669-bc70-22b29d3eba6a");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
    };
    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
    };

    public CreateAppearanceAsset() : base(
      name: "Add Appearance Asset",
      nickname: "C-APAST",
      description: "Create a new instance of appearance asset inside document",
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

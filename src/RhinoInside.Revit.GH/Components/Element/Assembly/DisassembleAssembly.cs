using System;
using System.Linq;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Assembly
{
  [Since("v1.2")]
  public class DisassembleAssembly : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("ff0f49ca-16ec-4287-8bd8-b903b6a6e781");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "DAS";

    public DisassembleAssembly() : base
    (
      name: "Disassemble Assembly",
      nickname: "DAS",
      description: "Disassemble given assembly and release the members",
      category: "Revit",
      subCategory: "Assembly"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.AssemblyInstance()
        {
          Name = "Assembly",
          NickName = "A",
          Description = "Assembly to disassembly",
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Previous members of assembly, after the disassembly",
          Access = GH_ParamAccess.list
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var assembly = default(DB.AssemblyInstance);
      if (!DA.GetData("Assembly", ref assembly))
        return;

      StartTransaction(assembly.Document);
      DA.SetDataList("Elements", assembly.Disassemble().Select(x => Types.Element.FromElementId(assembly.Document, x)));
    }
  }
}

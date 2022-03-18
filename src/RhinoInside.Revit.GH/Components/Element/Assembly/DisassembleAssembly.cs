using System;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Assemblies
{
  [ComponentVersion(introduced: "1.2")]
  public class DisassembleAssembly : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("FF0F49CA-16EC-4287-8BD8-B903B6A6E781");
    public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;

    public DisassembleAssembly() : base
    (
      name: "Disassemble Assembly",
      nickname: "Disassemble",
      description: "Disassemble given assembly and release the members",
      category: "Revit",
      subCategory: "Model"
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
      var assembly = default(ARDB.AssemblyInstance);
      if (!DA.GetData("Assembly", ref assembly))
        return;

      StartTransaction(assembly.Document);
      DA.SetDataList("Elements", assembly.Disassemble().Select(x => Types.Element.FromElementId(assembly.Document, x)));
    }
  }
}

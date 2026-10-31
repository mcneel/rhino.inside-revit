using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Assemblies
{
  [ComponentVersion(introduced: "1.2")]
  public class AssemblyMembers : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("33ead71b-647b-4783-b0ce-c840cd50c15d");
    public override GH_Exposure Exposure => GH_Exposure.quinary | GH_Exposure.obscure;

    public AssemblyMembers() : base
    (
      name: "Assembly Members",
      nickname: "Members",
      description: "Get-Set accessor for assembly members",
      category: "Revit",
      subCategory: "Type"
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
          Description = "Assembly to analyze or modify",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Members",
          NickName = "M",
          Description = "Members to be set on given assembly",
          Access = GH_ParamAccess.list,
          Optional = true
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AssemblyInstance()
        {
          Name = "Assembly",
          NickName = "A",
          Description = "Analyzed or modified Assembly",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Members",
          NickName = "M",
          Description = "Members of given assembly",
          Access = GH_ParamAccess.list
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Assembly", out Types.AssemblyInstance assembly, x => x.IsValid)) return;
      else DA.SetData("Assembly", assembly);

      if (Params.GetDataList(DA, "Members", out IList<ARDB.Element> members))
      {
        var prevNamingCategoryId = assembly.Value.NamingCategoryId;
        var memberdIds = members.OfType<ARDB.Element>().Where(x => x.Document.Equals(assembly.Document)).Select(x => x.Id).ToList();

        StartTransaction(assembly.Document);

        if (memberdIds.Count == 0 || ARDB.AssemblyInstance.AreElementsValidForAssembly(assembly.Document, memberdIds, assembly.Id))
          assembly.Value.SetMemberIds(memberdIds);
        else
          throw new Exception("At least one element is not valid to be a memeber of this assembly.");
      }

      DA.SetDataList("Members", assembly.Value.GetMemberIds().Select(x => assembly.GetElement<Types.GraphicalElement>(x)));
    }
  }
}

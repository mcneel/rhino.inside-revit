using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Assembly
{
  [Since("v1.2")]
  public class AssemblyMembers : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("33ead71b-647b-4783-b0ce-c840cd50c15d");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "AAS";

    public AssemblyMembers() : base(
      name: "Assembly Members",
      nickname: "AM",
      description: "Get-Set accessor for assembly members",
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
          Description = "Assembly to analyze or modify",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Element()
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
        new Parameters.Element()
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
      var assembly = default(DB.AssemblyInstance);
      if (!DA.GetData("Assembly", ref assembly))
        return;

      var _Members_ = Params.IndexOfInputParam("Members");
      if (_Members_ >= 0 && Params.Input[_Members_].DataType != GH_ParamData.@void)
      {
        DB.ElementId prevNamingCategoryId = assembly.NamingCategoryId;
        var newMembers = new List<DB.Element>();
        if (DA.GetDataList("Members", newMembers))
        {
          // set the assembly members to a new list. previous is cleared
          var memberdIds = newMembers.Select(x => x.Id).ToList();
          var handler = new AssemblyHandler(memberdIds, assembly.NamingCategoryId);

          StartTransaction(assembly.Document);
          handler.UpdateAssemblyMembers(assembly);

          if (!assembly.NamingCategoryId.Equals(prevNamingCategoryId))
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The naming category of given assembly automatically changed to match the new members");
        }
      }

      DA.SetData("Assembly", assembly);
      DA.SetDataList("Members", assembly.GetMemberIds().Select(x => Types.Element.FromElementId(assembly.Document, x)));
    }
  }
}

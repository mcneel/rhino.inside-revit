using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Hosts
{
  public class HostObjectTypeCompoundStructure : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("08CB62F1-68CB-4FD3-971C-25B0C82AC25A");
    public override GH_Exposure Exposure => GH_Exposure.senary;
    protected override string IconTag => "CS";

    public HostObjectTypeCompoundStructure() : base
    (
      name: "Host Type Compound Structure",
      nickname: "CompStruct",
      description: "Get-Set host type compound structure",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.HostObjectType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Host Type to access compound structure",
        }
      ),
      new ParamDefinition
      (
        new Parameters.CompoundStructure()
        {
          Name = "Structure",
          NickName = "S",
          Description = "New component structure for Type",
          Optional = true
        },
        ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.HostObjectType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Accessed Host Type",
        }
      ),
      new ParamDefinition
      (
        new Parameters.CompoundStructure()
        {
          Name = "Structure",
          NickName = "S",
          Description = "Compound structure definition of given type",
        },
        ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Type", out Types.HostObjectType type, x => x.IsValid))
        return;

      Params.TrySetData(DA, "Type", () => type);

      if (Params.GetData(DA, "Structure", out Types.CompoundStructure structure, x => x.IsValid))
      {
        StartTransaction(type.Document);
        try
        {
          type.CompoundStructure = structure;
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException)
        {
          if (structure.Audit(out var errors))
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{Types.CompoundStructure.ToString((ARDB.CompoundStructureError)(-1))}");
          else foreach (var error in errors.Values)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{Types.CompoundStructure.ToString(error)}");

          return;
        }
      }

      Params.TrySetData(DA, "Structure", () =>  type.CompoundStructure);
    }
  }
}

namespace RhinoInside.Revit.GH.Components.Hosts.Obsolete
{
  [Obsolete("Since 2021-03-23")]
  public class HostObjectTypeCompoundStructure : Component
  {
    public override Guid ComponentGuid => new Guid("024619EF-58FF-47C1-8833-96BA2F2B677B");
    public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.hidden;
    protected override string IconTag => "CS";

    public HostObjectTypeCompoundStructure() : base
    (
      name: "Host Type Compound Structure",
      nickname: "CompStruct",
      description: "Get host object type compound structure",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter
      (
        new Parameters.HostObjectType(),
        name: "Type",
        nickname: "T",
        description: string.Empty,
        GH_ParamAccess.item
      );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter
      (
        param: new Parameters.CompoundStructure(),
        name: "Compound Structure",
        nickname: "CS",
        description: "Compound Structure definition of given type",
        access: GH_ParamAccess.item
      );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var type = default(Types.HostObjectType);
      if (!DA.GetData("Type", ref type))
        return;

      DA.SetData("Compound Structure", type.CompoundStructure);
    }
  }
}

using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.ParameterElements
{
  public class ParameterIdentity : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("3BDE5890-FB80-4AF2-B9AC-373661756BDA");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ID";

    public ParameterIdentity() : base
    (
      "Parameter Identity",
      "Identity",
      "Query parameter identity data",
      "Revit",
      "Parameter"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.ParameterKey()
        {
          Name = "Parameter",
          NickName = "P",
          Description = $"Parameter to grab identity",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.ParameterGroup>()
        {
          Name = "Group",
          NickName = "G",
          Description = "Parameter group",
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
        new Param_Guid()
        {
          Name = "Guid",
          NickName = "ID",
          Description = "Parameter global unique ID",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Parameter Name"
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.ParameterType>()
        {
          Name = "Type",
          NickName = "T",
          Description = "Parameter type",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.ParameterGroup>()
        {
          Name = "Group",
          NickName = "G",
          Description = "Parameter group",
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Description",
          NickName = "D",
          Description = "Tooltip Description",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Parameter", out Types.ParameterKey key, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Group", out Types.ParameterGroup group, x => x.IsValid)) return;

      if (group is object)
      {
        if (key.IsReferencedData) StartTransaction(key.Document);
        else key = key.Duplicate();

        key.Group = group.Value;
      }

      Params.TrySetData(DA, "Guid", () => key.GUID);
      Params.TrySetData(DA, "Name", () => key.Name);
      Params.TrySetData(DA, "Type", () => key.DataType);
      Params.TrySetData(DA, "Group", () => key.Group);
      Params.TrySetData(DA, "Description", () => key.Description);
    }
  }
}

namespace RhinoInside.Revit.GH.Components.ParameterElements.Obsolete
{
  using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;
  using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;

  [EditorBrowsable(EditorBrowsableState.Never)]
  public class ParameterIdentityUpgrader : ComponentUpgrader
  {
    public ParameterIdentityUpgrader() { }
    public override DateTime Version => new DateTime(2021, 07, 30);
    public override Guid UpgradeFrom => new Guid("A80F4919-2387-4C78-BE2B-2F35B2E60298");
    public override Guid UpgradeTo => new Guid("3BDE5890-FB80-4AF2-B9AC-373661756BDA");

    public override IReadOnlyDictionary<string, string> GetInputAliases(IGH_Component _) =>
      new Dictionary<string, string>()
      {
        {"ParameterKey", "Parameter"}
      };

    public override IReadOnlyDictionary<string, string> GetOutputAliases(IGH_Component _) =>
      new Dictionary<string, string>()
      {
        {"StorageType", "Type"}
      };
  }

  [Obsolete("Obsolete since 2021-07-30")]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public class ParameterIdentity : Component
  {
    public override Guid ComponentGuid => new Guid("A80F4919-2387-4C78-BE2B-2F35B2E60298");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.hidden;

    public ParameterIdentity() : base
    (
      "Parameter Identity",
      "Identity",
      "Query parameter identity data",
      "Revit",
      "Parameter"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ParameterKey(), "ParameterKey", "K", "Parameter key to decompose", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddTextParameter("Name", "N", "Parameter name", GH_ParamAccess.item);
      manager.AddParameter(new Param_Integer(), "StorageType", "S", "Parameter value type", GH_ParamAccess.item);
      manager.AddParameter(new Param_Integer(), "Class", "C", "Identifies where the parameter is defined", GH_ParamAccess.item);
      manager.AddParameter(new Param_Guid(), "Guid", "ID", "Shared Parameter global identifier", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA) =>
      ComponentUpgrader.SolveInstance(this, DA);
  }
}

using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.DesignOptions
{
  public class DesignOptionSetIdentity : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("01080B5E-A771-41DA-A323-ACF16DC176CE");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "ID";

    public DesignOptionSetIdentity()
    : base("Design Option Set Identity", "Identity", "Design Option Set identity information", "Revit", "Document")
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Element>("Design Option Set", "DOS", string.Empty, GH_ParamAccess.item),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Param_String>("Name", "N", string.Empty, GH_ParamAccess.item),
      ParamDefinition.Create<Parameters.Element>("Design Options", "DO", string.Empty, GH_ParamAccess.list, relevance: ParamRelevance.Primary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Design Option Set", out Types.DesignOptionSet set)) return;

      Params.TrySetData(DA, "Name", () => set.Nomen);
      Params.TrySetDataList(DA, "Design Options", () => set.Options);
    }
  }

  public class DesignOptionIdentity : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("677DDF10-FFE0-4635-A612-35AC17D8A409");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "ID";

    public DesignOptionIdentity()
    : base("Design Option Identity", "Identity", "Design Option identity information", "Revit", "Document")
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Element>("Design Option", "DO", string.Empty, GH_ParamAccess.item),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Element>("Design Option Set", "DOS", string.Empty, GH_ParamAccess.item, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Name", "N", string.Empty, GH_ParamAccess.item),
      ParamDefinition.Create<Param_Boolean>("Primary", "P", string.Empty, GH_ParamAccess.item, relevance: ParamRelevance.Primary)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Design Option", out Types.DesignOption option)) return;

      Params.TrySetData(DA, "Design Option Set", () => option.OptionSet);
      Params.TrySetData(DA, "Name", () => option.Nomen);
      Params.TrySetData(DA, "Primary", () => option.IsPrimary);
    }
  }
}

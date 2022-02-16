using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Materials
{
  public class MaterialIdentity : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("222B42DF-16F6-4866-B065-FB77AADBD973");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public MaterialIdentity()
    : base
    (
      "Material Identity",
      "Identity",
      "Material Identity Data.",
      "Revit",
      "Material"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Material>("Material", "M"),

      ParamDefinition.Create<Param_String>("Description", "D", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Class", "CL", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Comments", "C", optional: true, relevance: ParamRelevance.Primary),

      ParamDefinition.Create<Param_String>("Manufacturer", "MAN", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Model", "MOD", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Number>("Cost", "COS", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("URL", "URL", optional: true, relevance: ParamRelevance.Primary),

      ParamDefinition.Create<Param_String>("Keynote", "KN", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Mark", "MK", optional: true, relevance: ParamRelevance.Primary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Material>("Material", "M", relevance: ParamRelevance.Occasional),

      ParamDefinition.Create<Param_String>("Name", "N", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Description", "D", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Class", "CL", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Comments", "C", relevance: ParamRelevance.Primary),

      ParamDefinition.Create<Param_String>("Manufacturer", "MAN", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Model", "MOD", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Number>("Cost", "COS", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("URL", "URL", relevance: ParamRelevance.Primary),

      ParamDefinition.Create<Param_String>("Keynote", "KN", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Mark", "MK", relevance: ParamRelevance.Primary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Material", out Types.Material material, x => x.IsValid))
        return;

      bool update = false;
      update |= Params.GetData(DA, "Name", out string name);
      update |= Params.GetData(DA, "Description", out string descritpion);
      update |= Params.GetData(DA, "Class", out string materialClass);
      update |= Params.GetData(DA, "Comments", out string comments);

      update |= Params.GetData(DA, "Manufacturer", out string manufacturer);
      update |= Params.GetData(DA, "Model", out string model);
      update |= Params.GetData(DA, "Cost", out double? cost);
      update |= Params.GetData(DA, "URL", out string url);

      update |= Params.GetData(DA, "Keynote", out string keynote);
      update |= Params.GetData(DA, "Mark", out string mark);

      if (update)
      {
        StartTransaction(material.Document);
        material.Description = descritpion;
        material.MaterialClass = materialClass;
        material.Comments = comments;
        material.Manufacturer = manufacturer;
        material.Model = model;
        material.Cost = cost;
        material.Url = url;
        material.Keynote = keynote;
        material.Mark = mark;
      }

      Params.TrySetData(DA, "Material", () => material);

      Params.TrySetData(DA, "Name", () => material.Nomen);
      Params.TrySetData(DA, "Description", () => material.Description);
      Params.TrySetData(DA, "Class", () => material.MaterialClass);
      Params.TrySetData(DA, "Comments", () => material.Comments);

      Params.TrySetData(DA, "Manufacturer", () => material.Manufacturer);
      Params.TrySetData(DA, "Model", () => material.Model);
      Params.TrySetData(DA, "Cost", () => material.Cost);
      Params.TrySetData(DA, "URL", () => material.Url);

      Params.TrySetData(DA, "Keynote", () => material.Keynote);
      Params.TrySetData(DA, "Mark", () => material.Mark);
    }
  }

  namespace Obsolete
  {
    [Obsolete("Since 2020-09-25")]
    public class MaterialIdentity : Component
    {
      public override Guid ComponentGuid => new Guid("06E0CF55-B10C-433A-B6F7-AAF3885055DB");
      public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.hidden;
      protected override string IconTag => "ID";

      public MaterialIdentity() : base
      (
        name: "Material Identity",
        nickname: "Identity",
        description: "Query material identity information",
        category: "Revit",
        subCategory: "Material"
      )
      { }

      protected override void RegisterInputParams(GH_InputParamManager manager)
      {
        manager.AddParameter(new Parameters.Material(), "Material", "Material", string.Empty, GH_ParamAccess.item);
      }

      protected override void RegisterOutputParams(GH_OutputParamManager manager)
      {
        manager.AddTextParameter("Class", "Class", "Material class", GH_ParamAccess.item);
        manager.AddTextParameter("Name", "Name", "Material name", GH_ParamAccess.item);
      }

      protected override void TrySolveInstance(IGH_DataAccess DA)
      {
        var material = default(Types.Material);
        if (!DA.GetData("Material", ref material) || !material.IsValid)
          return;

        DA.SetData("Class", material.MaterialClass);
        DA.SetData("Name", material.Nomen);
      }
    }
  }
}

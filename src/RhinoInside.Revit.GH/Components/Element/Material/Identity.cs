using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Material
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

      ParamDefinition.Create<Param_String>("Description", "D", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Class", "CL", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Comments", "C", optional: true, relevance: ParamVisibility.Default),

      ParamDefinition.Create<Param_String>("Manufacturer", "MAN", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Model", "MOD", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Cost", "COS", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("URL", "URL", optional: true, relevance: ParamVisibility.Default),

      ParamDefinition.Create<Param_String>("Keynote", "KN", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Mark", "MK", optional: true, relevance: ParamVisibility.Default),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Material>("Material", "M", relevance: ParamVisibility.Voluntary),

      ParamDefinition.Create<Param_String>("Name", "N", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Description", "D", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Class", "CL", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Comments", "C", relevance: ParamVisibility.Default),

      ParamDefinition.Create<Param_String>("Manufacturer", "MAN", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Model", "MOD", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Cost", "COS", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("URL", "URL", relevance: ParamVisibility.Default),

      ParamDefinition.Create<Param_String>("Keynote", "KN", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Mark", "MK", relevance: ParamVisibility.Default),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.TryGetData(DA, "Material", out Types.Material material))
        return;

      bool update = false;
      update |= Params.TryGetData(DA, "Name", out string name);
      update |= Params.TryGetData(DA, "Description", out string descritpion);
      update |= Params.TryGetData(DA, "Class", out string materialClass);
      update |= Params.TryGetData(DA, "Comments", out string comments);

      update |= Params.TryGetData(DA, "Manufacturer", out string manufacturer);
      update |= Params.TryGetData(DA, "Model", out string model);
      update |= Params.TryGetData(DA, "Cost", out string cost);
      update |= Params.TryGetData(DA, "URL", out string url);

      update |= Params.TryGetData(DA, "Keynote", out string keynote);
      update |= Params.TryGetData(DA, "Mark", out string mark);

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

      Params.TrySetData(DA, "Name", () => material.Name);
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
        var material = default(DB.Material);
        if (!DA.GetData("Material", ref material))
          return;

        DA.SetData("Class", material?.MaterialClass);
        DA.SetData("Name", material?.Name);
      }
    }
  }
}

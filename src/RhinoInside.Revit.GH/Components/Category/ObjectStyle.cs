using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.System.Drawing;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class CategoryObjectStyle : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("CA3C1CF9-BF5D-4B20-ADCE-3307943A1A51");

    public CategoryObjectStyle()
    : base
    (
      name: "Category Object Styles",
      nickname: "CatStyles",
      description: string.Empty,
      category: "Revit",
      subCategory: "Category"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Category>("Category", "C"),

      ParamDefinition.Create<Param_Integer>("Line Weight [projection]", "LWP", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Integer>("Line Weight [cut]", "LWC", optional: true, relevance: ParamRelevance.Primary),

      ParamDefinition.Create<Param_Colour>("Line Color", "LC", optional: true, relevance: ParamRelevance.Primary),

      ParamDefinition.Create<Parameters.LinePatternElement>("Line Pattern [projection]", "LPP", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.LinePatternElement>("Line Pattern [cut]", "LPC", optional: true, relevance: ParamRelevance.Occasional),

      ParamDefinition.Create<Parameters.Material>("Material", "M", optional: true, relevance: ParamRelevance.Primary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Category>("Category", "C"),

      ParamDefinition.Create<Param_Integer>("Line Weight [projection]", "LWP", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Integer>("Line Weight [cut]", "LWC", relevance: ParamRelevance.Primary),

      ParamDefinition.Create<Param_Colour>("Line Color", "LC", relevance: ParamRelevance.Primary),

      ParamDefinition.Create<Parameters.LinePatternElement>("Line Pattern [projection]", "LPP", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.LinePatternElement>("Line Pattern [cut]", "LPC", relevance: ParamRelevance.Occasional),

      ParamDefinition.Create<Parameters.Material>("Material", "M", relevance: ParamRelevance.Primary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Category", out Types.Category category, x => x.IsValid))
        return;

      bool update = false;
      update |= Params.GetData(DA, "Line Weight [projection]", out int? lwp);
      update |= Params.GetData(DA, "Line Weight [cut]", out int? lwc);
      update |= Params.GetData(DA, "Line Color", out System.Drawing.Color? color);
      update |= Params.GetData(DA, "Line Pattern [projection]", out Types.LinePatternElement lpp);
      update |= Params.GetData(DA, "Line Pattern [cut]", out Types.LinePatternElement lpc);
      update |= Params.GetData(DA, "Material", out Types.Material material);

      if (update)
      {
        StartTransaction(category.Document);
        category.ProjectionLineWeight = lwp;
        category.CutLineWeight = lwc;
        category.LineColor = color;
        category.ProjectionLinePattern = lpp;
        category.CutLinePattern = lpc;
        category.Material = material;
      }

      Params.TrySetData(DA, "Category", () => category);
      Params.TrySetData(DA, "Line Weight [projection]", () => category.ProjectionLineWeight);
      Params.TrySetData(DA, "Line Weight [cut]", () => category.CutLineWeight);
      Params.TrySetData(DA, "Line Color", () => category.LineColor);
      Params.TrySetData(DA, "Line Pattern [projection]", () => category.ProjectionLinePattern);
      Params.TrySetData(DA, "Line Pattern [cut]", () => category.CutLinePattern);
      Params.TrySetData(DA, "Material", () => category.Material);
    }
  }
}

namespace RhinoInside.Revit.GH.Components.Obsolete
{
  [Obsolete("Obsolete since 2020-10-08")]
  public class CategoryObjectStyle : Component
  {
    public override Guid ComponentGuid => new Guid("1DD8AE78-F7DA-4F26-8353-4CCE6B925DC6");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.hidden;

    public CategoryObjectStyle()
    : base("Category ObjectStyle", "ObjectStyle", string.Empty, "Revit", "Category")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Category", "C", "Category to query", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddIntegerParameter("LineWeight [projection]", "LWP", "Category line weight [projection]", GH_ParamAccess.item);
      manager.AddIntegerParameter("LineWeight [cut]", "LWC", "Category line weigth [cut]", GH_ParamAccess.item);
      manager.AddColourParameter("LineColor", "LC", "Category line color", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Element(), "LinePattern [projection]", "LPP", "Category line pattern [projection]", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Element(), "LinePattern [cut]", "LPC", "Category line pattern [cut]", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Material(), "Material", "M", "Category material", GH_ParamAccess.item);
      manager.AddBooleanParameter("Cuttable", "C", "Indicates if the category is cuttable or not", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Category category = null;
      if (!DA.GetData("Category", ref category))
        return;

      var doc = category?.Document();

      DA.SetData("LineWeight [projection]", category?.GetLineWeight(DB.GraphicsStyleType.Projection));
      DA.SetData("LineWeight [cut]", category?.GetLineWeight(DB.GraphicsStyleType.Cut));
      DA.SetData("LineColor", category?.LineColor.ToColor());
      DA.SetData("LinePattern [projection]", doc?.GetElement(category.GetLinePatternId(DB.GraphicsStyleType.Projection)));
      DA.SetData("LinePattern [cut]", doc?.GetElement(category.GetLinePatternId(DB.GraphicsStyleType.Cut)));
      DA.SetData("Material", category?.Material);
      DA.SetData("Cuttable", category?.IsCuttable);
    }
  }
}

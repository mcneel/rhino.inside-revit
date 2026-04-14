using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  using Autodesk.Revit.DB;

  [ComponentVersion(introduced: "1.27")]
  public class DatumBubbles : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("198CA8B8-366C-40C7-803F-C00110B58A77");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public DatumBubbles() : base
    (
      name: "Datum Bubbles",
      nickname: "D-Bubbles",
      description: "Get-Set Datum bubbles visibility",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Datum",
          NickName = "D",
          Description = "Level and Grid are accepted",
        }
      ),
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Start",
          NickName = "S",
          Optional = true,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "End",
          NickName = "E",
          Optional = true,
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Datum",
          NickName = "D",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Start",
          NickName = "S",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "End",
          NickName = "E",
        }, ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Datum", out Types.DatumPlane datum, x => x.IsValid && (x is Types.Grid || x is Types.Level))) return;
      else Params.TrySetData(DA, "Datum", () => datum);

      if (!Params.GetData(DA, "View", out Types.View view)) return;
      else Params.TrySetData(DA, "View", () => view);

      if (!Params.TryGetData(DA, "Start", out bool? start)) return;
      if (!Params.TryGetData(DA, "End", out bool? end)) return;

      if (start is object || end is object)
      {
        StartTransaction(datum.Document);
        switch (start)
        {
          case false: datum.Value.HideBubbleInView(DatumEnds.End0, view.Value); break;
          case true: datum.Value.ShowBubbleInView(DatumEnds.End0, view.Value); break;
        }
        switch (end)
        {
          case false: datum.Value.HideBubbleInView(DatumEnds.End1, view.Value); break;
          case true: datum.Value.ShowBubbleInView(DatumEnds.End1, view.Value); break;
        }
      }

      Params.TrySetData(DA, "Start", () => datum.Value.IsBubbleVisibleInView(DatumEnds.End0, view.Value));
      Params.TrySetData(DA, "End", () => datum.Value.IsBubbleVisibleInView(DatumEnds.End1, view.Value));
    }
  }
}

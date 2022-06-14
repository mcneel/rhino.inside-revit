using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.ObjectStyles
{
  [ComponentVersion(introduced: "1.8")]
  public class CurveLineStyle : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("60BE53C5-11C8-42BC-8634-294540D59580");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public CurveLineStyle()
    : base
    (
      "Curve Line Style",
      "Linestyle",
      "Curve Line Style Property. Get-Set accessor to Curve Line Style property.",
      "Revit",
      "Object Styles"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.CurveElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Curve element to access Line Style",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicsStyle()
        {
          Name = "Line Style",
          NickName = "LS",
          Description = "Curve linestyle",
          Optional = true
        },ParamRelevance.Secondary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.CurveElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Curve element to access Line Style",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicsStyle()
        {
          Name = "Line Style",
          NickName = "SC",
          Description = "Curve element Line Style",
        },ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.CurveElement element, x => x.IsValid)) return;
      else DA.SetData("Element", element);

      if (Params.GetData(DA, "Linestyle", out Types.GraphicsStyle style))
        UpdateElement(element.Value, () => element.LineStyle = style);

      Params.TrySetData(DA, "Linestyle", () => element.LineStyle);
    }
  }
}

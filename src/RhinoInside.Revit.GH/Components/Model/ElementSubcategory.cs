using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  [ComponentVersion(introduced: "1.8")]
  public class ElementSubcategory : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("495330DB-5733-4718-ADBB-73C2FB5787A7");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "SC";

    public ElementSubcategory()
    : base
    (
      "Element Subcategory",
      "Subcategory",
      "Element Subcategory Property. Get-Set accessor to Element Subcategory property.",
      "Revit",
      "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Subcategory",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Subcategory",
          NickName = "SC",
          Description = "Element Subcategory",
          Optional = true
        },ParamRelevance.Secondary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Subcategory",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Subcategory",
          NickName = "SC",
          Description = "Element Subcategory",
        },ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.GraphicalElement element, x => x.IsValid)) return;
      else DA.SetData("Element", element);

      if (Params.GetData(DA, "Subcategory", out Types.Category subcategory))
        UpdateElement(element.Value, () => element.Subcategory = subcategory);

      Params.TrySetData(DA, "Subcategory", () => element.Subcategory);
    }
  }
}

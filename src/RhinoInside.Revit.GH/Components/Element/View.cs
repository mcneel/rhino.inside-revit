using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Elements
{
  [ComponentVersion(introduced: "1.15")]
  public class ElementPropertyView : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("440B6BEB-CBBB-450A-AC32-0529E59E9E27");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public ElementPropertyView()
    : base
    (
      "Element View",
      "E-View",
      "Element Owner View Property. Get access component to Element Owner View property.",
      "Revit",
      "Element"
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
          Description = "Element to access owner view",
        }
      )
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
          Description = "Element to access owner view",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "View Specific",
          NickName = "S",
          Description = "Wheter element is view specific or not",
        }
      ),
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "Element owner view",
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.GraphicalElement element, x => x.IsValid)) return;
      else DA.SetData("Element", element);

      DA.SetData("View Specific", element.ViewSpecific);
      DA.SetData("View", element.OwnerView);
    }
  }
}

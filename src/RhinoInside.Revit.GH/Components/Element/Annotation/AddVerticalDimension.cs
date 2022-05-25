using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.GH.Components.Element.Annotation;

namespace RhinoInside.Revit.GH.Components.Annotation
{
  [ComponentVersion(introduced: "1.8")]
  public class AddVerticalDimension : AddDimension
  {
    public override Guid ComponentGuid => new Guid("0DBE67E7-7D8E-41F9-85B0-139C0B7F1745");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public AddVerticalDimension() : base
    (
      name: "Add Vertical Dimension",
      nickname: "AddVerDimension",
      description: "Given a point, it adds a vertical dimension to the given View",
      category: "Revit",
      subCategory: "Annotation"
    )
    { }

    protected override bool IsHorizontal => false;

  }
}


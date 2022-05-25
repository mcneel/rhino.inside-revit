using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.GH.Components.Element.Annotation;

namespace RhinoInside.Revit.GH.Components.Annotation
{
  [ComponentVersion(introduced: "1.8")]
  public class AddHorizontalDimension : AddDimension
  {
    public override Guid ComponentGuid => new Guid("DF47C980-EF08-4BBE-A624-C956C07B04EC");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public AddHorizontalDimension() : base
    (
      name: "Add Horizontal Dimension",
      nickname: "AddHorDimension",
      description: "Given a point, it adds a horizontal dimension to the given View",
      category: "Revit",
      subCategory: "Annotation"
    )
    { }

    protected override bool IsHorizontal => true;
  }
}

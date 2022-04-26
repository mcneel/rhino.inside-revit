using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Openings
{
  [ComponentVersion(introduced: "1.7")]
  public class AddVerticalOpening : AddOpening
  {
    public override Guid ComponentGuid => new Guid("C9C0F4D2-B75E-42C8-A98F-909DF4AB4A1A");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddVerticalOpening() : base
    (
      name: "Add Vertical Opening",
      nickname: "VerticalOpen",
      description: "Given its outline boundary and a host element, it adds a vertical opening to the active Revit document",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override bool IsCutPerpendicularToFace => false;
  }
}


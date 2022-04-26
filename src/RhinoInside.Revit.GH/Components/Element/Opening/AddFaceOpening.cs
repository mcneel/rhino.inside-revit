using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Openings
{
  [ComponentVersion(introduced: "1.7")]
  public class AddFaceOpening : AddOpening
  {
    public override Guid ComponentGuid => new Guid("69A10E5D-5DF0-4227-95D3-2629529C1DEF");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddFaceOpening() : base
    (
      name: "Add Face Opening",
      nickname: "FaceOpen",
      description: "Given its outline boundary and a host element, it adds an opening to the active Revit document",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override bool IsCutPerpendicularToFace => true;
  }
}


using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Openings
{
  [ComponentVersion(introduced: "1.6")]
  public class OpeningBoundaryProfile : Component
  {
    public override Guid ComponentGuid => new Guid("E76B0F6B-4EE1-413D-825D-4A8EDD86D55F");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public OpeningBoundaryProfile() : base
    (
      name: "Opening Boundary Profile",
      nickname: "OpeningBoundProf",
      description: "Get the boundary profile of the given opening",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter
      (
        param: new Parameters.Opening(),
        name: "Opening",
        nickname: "O",
        description: "Opening object to query for its boundary profile",
        access: GH_ParamAccess.item
      );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddPlaneParameter
      (
        name: "Plane",
        nickname: "P",
        description: "Plane of a given opening element",
        access: GH_ParamAccess.item
      );

      manager.AddCurveParameter
      (
        name: "Profile",
        nickname: "PC",
        description: "Profile curves of a given opening element",
        access: GH_ParamAccess.list
      );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Opening", out Types.Opening opening, x => x.IsValid)) return;

      DA.SetData("Plane", opening.Location);
      DA.SetDataList("Profile", opening.Profiles);
    }
  }
}

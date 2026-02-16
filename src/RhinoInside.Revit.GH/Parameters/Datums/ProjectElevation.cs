using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
  public sealed class ProjectElevation : Param<Types.ProjectElevation>
  {
    public override Guid ComponentGuid => new Guid("63F4A581-6065-4F90-BAD2-714DA8B97C08");

    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;
    protected override string IconTag => "⦻";

    protected override Types.ProjectElevation PreferredCast(object data)
    {
      return data is External.DB.ElevationElementReference height ? new Types.ProjectElevation(height) : default;
    }

    public ProjectElevation() : base
    (
      name: "Project Elevation",
      nickname: "Project Elevation",
      description: "Contains a collection of project elevation values",
      category: "Params",
      subcategory: "Revit"
    )
    { }
  }
}

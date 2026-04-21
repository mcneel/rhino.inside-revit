using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
  public sealed class LevelConstraint : Param<Types.LevelConstraint>
  {
    public override Guid ComponentGuid => new Guid("4150D40A-7C02-4633-B3B5-CFE4B16855B5");

    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.hidden;
    protected override string IconTag => string.Empty;

    protected override Types.LevelConstraint PreferredCast(object data)
    {
      return data is External.DB.ElevationElementReference height ? new Types.LevelConstraint(height) : default;
    }

    public LevelConstraint() : base
    (
      name: "Level Constraint",
      nickname: "Level Constraint",
      description: "Contains a collection of level constrait values",
      category: "Params",
      subcategory: "Revit"
    )
    { }
  }
}

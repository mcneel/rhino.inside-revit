using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class LevelConstraint : ProjectElevation
  {
    public LevelConstraint() { }
    public LevelConstraint(External.DB.ElevationElementReference height) : base(height) { }
    public LevelConstraint(double offset) : base(offset) { }
    public LevelConstraint(Level level, double? offset) :
      base(new External.DB.ElevationElementReference(level.Value, offset / Revit.ModelUnits))
    { }

    public override bool IsValid => Value != default;

    public override string TypeName => "Level Constraint";

    public override string TypeDescription => "A signed distance along Z-axis relative to a Level";

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(External.DB.ElevationElementReference)))
      {
        target = (Q) (object) Value;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Number)))
      {
        target = (Q) (object) new GH_Number(GeometryDecoder.ToModelLength(Value.Elevation));
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(double)))
      {
        target = (Q) (object) GeometryDecoder.ToModelLength(Value.Elevation);
        return true;
      }

      return base.CastTo(ref target);
    }

    public override bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      switch (source)
      {
        case double offset: Value = new External.DB.ElevationElementReference(GeometryEncoder.ToInternalLength(offset)); return true;
        case ARDB.Level level: Value = new External.DB.ElevationElementReference(level, default); return true;
        case External.DB.ElevationElementReference elevation: Value = elevation; return true;
      }

      return base.CastFrom(source);
    }
  }
}

namespace RhinoInside.Revit.GH.Parameters
{
  public class LevelConstraint : Param<Types.LevelConstraint>
  {
    public override Guid ComponentGuid => new Guid("4150D40A-7C02-4633-B3B5-CFE4B16855B5");

    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;
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
      subcategory: "Revit Primitives"
    )
    { }
  }
}

namespace RhinoInside.Revit.GH.Components.Input
{
  public class ConstructLevelConstraint : Component
  {
    public override Guid ComponentGuid => new Guid("01C853D8-87A3-4A76-8855-130BECA30DA1");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    public ConstructLevelConstraint() : base
    (
      name: "Level Constraint",
      nickname: "LevelConst",
      description: "Constructs a level constraint",
      category: "Revit",
      subCategory: "Input"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Level(), "Level", "L", "Reference level", GH_ParamAccess.item);
      manager[manager.AddNumberParameter("Offset", "O", "Height above or below the Level", GH_ParamAccess.item, 0.0)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.LevelConstraint(), "Constraint", "C", "Level constraint", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Level", out Types.Level level)) return;
      if (!Params.TryGetData(DA, "Offset", out double? offset)) return;

      DA.SetData("Constraint", new Types.LevelConstraint(level, offset));
    }
  }
}

using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class LevelConstraint : ProjectElevation
  {
    public LevelConstraint() { }
    internal LevelConstraint(External.DB.ElevationElementReference value) : base(value) { }
    public LevelConstraint(double? value) :
      base(new External.DB.ElevationElementReference(value / Revit.ModelUnits))
    { }
    public LevelConstraint(double? value, Level level) :
      base(new External.DB.ElevationElementReference(value / Revit.ModelUnits, level?.Value))
    { }

    public override bool IsValid => Value != default;

    public override string TypeName => "Level Elevation";

    public override string TypeDescription => "A signed distance along Z-axis relative to a Level";

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
      {
        if (IsLevelConstraint(out var level, out var offset))
        {
          var location = level.Location;
          location.Translate(Vector3d.ZAxis * offset);
          target = (Q) (object) new GH_Plane(location);
          return true;
        }

        return false;
      }

      return base.CastTo(ref target);
    }

    public override bool CastFrom(object source)
    {
      switch (source)
      {
        case Level l: Value = new External.DB.ElevationElementReference(default, l.Value); return true;
        case IGH_Goo goo: source = goo.ScriptVariable(); break;
      }

      switch (source)
      {
        case string text: if (!GH_Convert.ToDouble(text, out var number, GH_Conversion.Secondary)) return false;
            Value = new External.DB.ElevationElementReference(GeometryEncoder.ToInternalLength(number), null);
            return true;
        case int offset: Value = new External.DB.ElevationElementReference(GeometryEncoder.ToInternalLength(offset), null); return true;
        case double offset: Value = new External.DB.ElevationElementReference(GeometryEncoder.ToInternalLength(offset), null); return true;
        case ARDB.View view: Value = new External.DB.ElevationElementReference(default, view.GenLevel); return true;
        case ARDB.Level level: Value = new External.DB.ElevationElementReference(default, level); return true;
        case External.DB.ElevationElementReference elevation: Value = elevation; return true;
      }

      return false;
    }

    public static LevelConstraint operator %(LevelConstraint constraint, Level level)
    {
      if (level is null) return constraint;
      if (constraint?.IsElevation(out var elevation) is true) return new LevelConstraint(elevation - level.Elevation, level);
      if (constraint?.IsOffset(out var offset) is true) return new LevelConstraint(offset, level);
      if (constraint?.IsUnlimited() is true) return new LevelConstraint(default, default);
      return new LevelConstraint(null, level);
    }

    public static LevelConstraint operator +(LevelConstraint constraint, double? value)
    {
      if (value is null) return constraint;
      if (constraint?.IsLevelConstraint(out var level, out var elevation) is true) return new LevelConstraint(elevation + value, level);
      if (constraint?.IsElevation(out elevation) is true) return new LevelConstraint(elevation + value);
      if (constraint?.IsOffset(out var offset) is true) return new LevelConstraint(offset + value, null);
      if (constraint?.IsUnlimited() is true) return new LevelConstraint(default, default);
      return new LevelConstraint(value, null);
    }

    public bool IsLevelConstraint(out Level level, out double offset)
    {
      if (Value.IsLevelConstraint(out var l, out var o))
      {
        level = Level.FromElement(l) as Level;
        offset = (o ?? 0.0) * Revit.ModelUnits;
        return true;
      }

      level = default;
      offset = double.NaN;
      return false;
    }
 }
}

namespace RhinoInside.Revit.GH.Parameters
{
  public class LevelConstraint : Param<Types.LevelConstraint>
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

namespace RhinoInside.Revit.GH.Components.Annotations.Levels
{
  [ComponentVersion(introduced: "1.0", updated: "1.14")]
  public class LevelOffset : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("01C853D8-87A3-4A76-8855-130BECA30DA1");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    public LevelOffset() : base
    (
      name: "Level Offset",
      nickname: "LvlOffset",
      description: "Get-Set a level offset",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.LevelConstraint>("Elevation", "E", "Level offset elevation", optional: true),
      ParamDefinition.Create<Parameters.Level>("Level", "L", "Reference level", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Number>("Offset", "O", "Offset above or below the Level", optional: true, relevance: ParamRelevance.Primary)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.LevelConstraint>("Elevation", "E", "Level offset elevation"),
      ParamDefinition.Create<Parameters.Level>("Level", "L", "Reference level", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Number>("Offset", "O", "Offset above or below the Level", relevance: ParamRelevance.Primary)
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Output<IGH_Param>("Constraint") is IGH_Param constraint)
        constraint.Name = "Elevation";

      base.AddedToDocument(document);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.TryGetData(DA, "Elevation", out Types.LevelConstraint elevation)) return;
      if (!Params.TryGetData(DA, "Level", out Types.Level level)) return;
      if (!Params.TryGetData(DA, "Offset", out double? offset)) return;

      if (level is object) elevation %= level;
      if (offset is object) elevation += offset;

      Params.TrySetData(DA, "Elevation", () => elevation);
      if (elevation?.IsLevelConstraint(out var elevationLevel, out var elevationOffset) is true)
      {
        Params.TrySetData(DA, "Level", () => elevationLevel);
        Params.TrySetData(DA, "Offset", () => elevationOffset);
      }
      else if (elevation?.IsOffset(out elevationOffset) is true)
      {
        Params.TrySetData(DA, "Offset", () => elevationOffset);
      }
    }
  }
}

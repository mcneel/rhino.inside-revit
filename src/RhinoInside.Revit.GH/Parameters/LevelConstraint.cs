using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB
{
  public struct LevelConstraint : IEquatable<LevelConstraint>, IComparable<LevelConstraint>
  {
    readonly ARDB.Level Level;
    readonly double? height;

    public LevelConstraint(ARDB.Level level, double? offset = default)
    {
      if (level is null)
        throw new ArgumentNullException(nameof(level));

      Level = level;
      height = offset;
    }

    public LevelConstraint(double elevation)
    {
      Level = default;
      height = elevation;
    }

    public bool IsRelative(out ARDB.Level level, out double? offset)
    {
      if ((level = Level) is object)
      {
        offset = height;
        return true;
      }

      offset = default;
      return false;
    }

    public bool IsAbsolute => Level is null && height is object;

    public double Elevation => IsRelative(out var level, out var offset) ?
      level.ProjectElevation + offset ?? 0.0 :
      height ?? double.NaN;

    public override string ToString()
    {
      if (Level is object)
      {
        var token = $"'{Level?.Name ?? "Invalid Level"}'";
        if (Math.Abs(height ?? 0.0) > 1e-9)
          token += $" {(height.Value < 0.0 ? "-" : "+")} {GH_Format.FormatDouble(Math.Abs(height.Value))}ft";

        return token;
      }
      else if (height.HasValue)
      {
        return $"{GH_Format.FormatDouble(Math.Abs(height.Value))}ft";
      }

      return string.Empty;
    }

    #region IEquatable
    public override int GetHashCode() =>
      (Level?.Document.GetHashCode() ?? 0) ^
      (Level?.Id.IntegerValue.GetHashCode() ?? 0) ^
      (height?.GetHashCode() ?? 0);

    public override bool Equals(object obj) => obj is LevelConstraint other && Equals(other);

    public bool Equals(LevelConstraint other)
    {
      return Level.IsEquivalent(other.Level) && height == other.height;
    }

    public static bool operator ==(LevelConstraint left, LevelConstraint right) => left.Equals(right);
    public static bool operator !=(LevelConstraint left, LevelConstraint right) => !left.Equals(right);
    #endregion

    #region IComparable
    public int CompareTo(LevelConstraint other) => Elevation.CompareTo(other.Elevation);
    #endregion

    public static implicit operator double(LevelConstraint value) => value.Elevation;
    public static implicit operator LevelConstraint(double value) => double.IsNaN(value) ? default : new LevelConstraint(value);
  }
}

namespace RhinoInside.Revit.GH.Types
{
  public class LevelConstraint : GH_Goo<External.DB.LevelConstraint>
  {
    public LevelConstraint() { }
    public LevelConstraint(External.DB.LevelConstraint height) : base(height) { }
    public LevelConstraint(Level level, double? offset) :
      base(new External.DB.LevelConstraint(level.Value, offset / Revit.ModelUnits))
    { }

    public override bool IsValid => Value != default;

    public override string TypeName => "Height";

    public override string TypeDescription => "Height";

    public override IGH_Goo Duplicate() => MemberwiseClone() as IGH_Goo;

    public override string ToString() => Value.ToString();

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(External.DB.LevelConstraint)))
      {
        target = (Q) (object) Value;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Number)))
      {
        target = (Q) (object) new GH_Number(Value.Elevation * Revit.ModelUnits);
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
        case double elevation: Value = new External.DB.LevelConstraint(elevation / Revit.ModelUnits); return true;
        case ARDB.Level level: Value = new External.DB.LevelConstraint(level); return true;
      }

      return base.CastFrom(source);
    }
  }
}

namespace RhinoInside.Revit.GH.Parameters
{
  public class LevelConstraint : Param<Types.LevelConstraint>
  {
    public override Guid ComponentGuid => new Guid("E1EAA9FC-CBB0-443E-A0B9-2F27D2841609");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override string IconTag => string.Empty;

    protected override Types.LevelConstraint PreferredCast(object data)
    {
      return data is External.DB.LevelConstraint height ? new Types.LevelConstraint(height) : default;
    }

    public LevelConstraint() : base
    (
      name: "Height",
      nickname: "Height",
      description: "Contains a collection of height values",
      category: "Params",
      subcategory: "Revit"
    )
    { }
  }
}

namespace RhinoInside.Revit.GH.Components
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

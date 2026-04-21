using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Level Elevation"), Kernel.Attributes.Description("A signed distance along Z-axis relative to a Level")]
  public sealed class LevelConstraint : ProjectElevation
  {
    public LevelConstraint() { }
    internal LevelConstraint(External.DB.ElevationElementReference value) : base(value) { }
    public LevelConstraint(double? value) :
      base(new External.DB.ElevationElementReference(value / Revit.ModelUnits))
    { }
    public LevelConstraint(double? value, Level level) :
      base(new External.DB.ElevationElementReference(value / Revit.ModelUnits, level?.Value))
    { }

    public static LevelConstraint operator %(LevelConstraint constraint, Level level)
    {
      if (level is null) return constraint;
      if (constraint?.IsValid is true)
      {
        if (constraint.IsElevation(out var elevation) is true) return new LevelConstraint(elevation - level.Elevation, level);
        if (constraint.IsOffset(out var offset) is true) return new LevelConstraint(offset, level);
        if (constraint.IsUnlimited() is true) return Unlimited;
      }
      return new LevelConstraint(null, level);
    }

    public static LevelConstraint operator +(LevelConstraint constraint, double? value)
    {
      if (value is null) return constraint;
      if (constraint?.IsValid is true)
      {
        if (constraint.IsLevelConstraint(out var level, out var elevation) is true) return new LevelConstraint(elevation + value, level);
        if (constraint.IsElevation(out elevation) is true) return new LevelConstraint(elevation + value);
        if (constraint.IsOffset(out var offset) is true) return new LevelConstraint(offset + value, null);
        if (constraint.IsUnlimited() is true) return Unlimited;
      }
      return new LevelConstraint(value, null);
    }

    public static new LevelConstraint Unlimited => new LevelConstraint(External.DB.ElevationElementReference.Unlimited);

    public bool IsLevelConstraint(out Level level, out double offset)
    {
      if (Value.IsLevelConstraint(out var l, out var o) is true)
      {
        level = Level.FromElement(l) as Level;
        offset = (o ?? 0.0) * Revit.ModelUnits;
        return true;
      }

      level = default;
      offset = double.NaN;
      return false;
    }

    #region Convertible
    public override bool ConvertFrom(object source)
    {
      ResetValue();

      switch (source)
      {
        case Level l: Value = new External.DB.ElevationElementReference(default, l.Value); return true;
        case IGH_Goo goo: source = goo.ScriptVariable(); break;
      }

      switch (source)
      {
        case string text:
          if (!GH_Convert.ToDouble(text, out var number, GH_Conversion.Secondary)) return false;
          Value = new External.DB.ElevationElementReference(GeometryEncoder.ToInternalLength(number), null);
          return true;
        case int offset: Value = new External.DB.ElevationElementReference(GeometryEncoder.ToInternalLength(offset), null); return true;
        case double offset: Value = new External.DB.ElevationElementReference(GeometryEncoder.ToInternalLength(offset), null); return true;
        case ARDB.View view: Value = new External.DB.ElevationElementReference(default, view.GenLevel); return true;
        case ARDB.Level level: Value = new External.DB.ElevationElementReference(default, level); return true;
        case External.DB.ElevationElementReference elevation: Value = elevation; return true;
        case Vector3d vector: Value = new External.DB.ElevationElementReference(GeometryEncoder.ToInternalLength(vector.Z), null); return true;
      }

      return false;
    }

    public override bool ConvertTo<Q>(out Q target)
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

        target = default;
        return false;
      }

      return base.ConvertTo(out target);
    }
    #endregion
  }
}

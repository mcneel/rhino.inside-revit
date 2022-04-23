using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB
{
  public struct ElevationElementReference : IEquatable<ElevationElementReference> //, IComparable<ElevationElementReference>
  {
    readonly ARDB.Document Document;
    readonly ARDB.ElementId BaseId;
    readonly double? Value;

    ARDB.Element Base => Document?.GetElement(BaseId);

    public ElevationElementReference(double offset)
    {
      Document = default;
      BaseId = ARDB.ElementId.InvalidElementId;
      Value = offset;
    }

    public ElevationElementReference(double height, ARDB.BasePoint basePoint)
    {
      Document = basePoint?.Document;
      BaseId = basePoint?.Id;
      Value = height;
    }

    public ElevationElementReference(ARDB.Level level)
    {
      var basePoint = default(ARDB.BasePoint);
      switch ((ElevationBase) level.Document.GetElement(level.GetTypeId()).get_Parameter(ARDB.BuiltInParameter.LEVEL_RELATIVE_BASE_TYPE).AsInteger())
      {
        case ElevationBase.ProjectBasePoint:  basePoint = BasePointExtension.GetProjectBasePoint(level.Document); break;
        case ElevationBase.SurveyPoint:       basePoint = BasePointExtension.GetSurveyPoint(level.Document); break;
      }

      Document = basePoint?.Document;
      BaseId = basePoint?.Id;
      Value = basePoint is object ? level.Elevation : level.ProjectElevation;
    }

    public ElevationElementReference(ARDB.Level level, double? offset)
    {
      Document = level?.Document;
      BaseId = level?.Id;
      Value = offset;
    }

    public bool IsAbsolute => BaseId is null && Value is object;

    public bool IsOffset(out double offset)
    {
      if (BaseId == ARDB.ElementId.InvalidElementId)
      {
        offset = Offset;
        return true;
      }

      offset = double.NaN;
      return false;
    }

    public bool IsElevation(out double elevation)
    {
      if (BaseId != ARDB.ElementId.InvalidElementId)
      {
        elevation = Elevation;
        return true;
      }

      elevation = double.NaN;
      return false;
    }

    public bool IsRelative(out double offset, out ARDB.Element baseElement)
    {
      if ((baseElement = Base) is object)
      {
        offset = Value ?? 0.0;
        return true;
      }

      offset = default;
      return false;
    }

    public bool IsLevelConstraint(out ARDB.Level level, out double? offset)
    {
      if (Base is ARDB.Level baseLevel)
      {
        level = baseLevel;
        offset = Value;
        return true;
      }

      level = default;
      offset = default;
      return false;
    }

    public double BaseElevation
    {
      get
      {
        switch (Base)
        {
          case ARDB.Level level: return level.ProjectElevation;
          case ARDB.BasePoint basePoint: return basePoint.GetPosition().Z;
        }

        return 0.0;
      }
    }

    public double Offset => Value ?? 0.0;

    public double Elevation => BaseElevation + Offset;

    public override string ToString()
    {
      if (IsLevelConstraint(out var level, out var levelOffset))
      {
        var name = level.Name;
        var token = $"'{name ?? "Invalid Level"}'";
        if (levelOffset.HasValue && Math.Abs(levelOffset.Value) > 1e-9)
          token += $" {(levelOffset.Value < 0.0 ? "-" : "+")} {Math.Abs(levelOffset.Value)} ft";

        return token;
      }
      else if (IsRelative(out var relativeOffset, out var relativeElement))
      {
        var name = relativeElement.Name;
        if (string.IsNullOrEmpty(name)) name = relativeElement.Category?.Name;
        var token = $"'{name ?? "Invalid Element"}'";
        token += $" {(relativeOffset < 0.0 ? "-" : "+")} {Math.Abs(relativeOffset)} ft";

        return token;
      }
      else if (IsOffset(out var offset))
      {
        return $"Δ {(offset < 0.0 ? "-" : "+")}{Math.Abs(offset)} ft";
      }
      else if (IsElevation(out var elevation))
      {
        return $"{elevation} ft";
      }

      return string.Empty;
    }

    #region IEquatable
    public override int GetHashCode() =>
      (Base?.Document.GetHashCode() ?? 0) ^
      (Base?.Id.IntegerValue.GetHashCode() ?? 0) ^
      (Value?.GetHashCode() ?? 0);

    public override bool Equals(object obj) => obj is ElevationElementReference other && Equals(other);

    public bool Equals(ElevationElementReference other) => Base.IsEquivalent(other.Base) && Value == other.Value;

    public static bool operator ==(ElevationElementReference left, ElevationElementReference right) => left.Equals(right);
    public static bool operator !=(ElevationElementReference left, ElevationElementReference right) => !left.Equals(right);
    #endregion

    //#region IComparable
    //public int CompareTo(ElevationElementReference other) => Elevation.CompareTo(other.Elevation);
    //#endregion
  }
}

namespace RhinoInside.Revit.GH.Types
{
  public class ProjectElevation : GH_Goo<External.DB.ElevationElementReference>
  {
    public ProjectElevation() { }
    public ProjectElevation(External.DB.ElevationElementReference height) :
      base(height)
    { }
    public ProjectElevation(double offset) :
      base(new External.DB.ElevationElementReference(GeometryEncoder.ToInternalLength(offset)))
    { }
    public ProjectElevation(double elevation, BasePoint basePoint) :
      base(new External.DB.ElevationElementReference(GeometryEncoder.ToInternalLength(elevation), basePoint?.Value))
    { }

    public ProjectElevation(double elevation, IGH_BasePoint basePoint) :
      base(new External.DB.ElevationElementReference(GeometryEncoder.ToInternalLength(elevation), basePoint?.Value as ARDB.BasePoint))
    { }

    public override bool IsValid => Value != default;

    public override string TypeName => "Project Elevation";

    public override string TypeDescription => "A signed distance along Z-axis";

    public override IGH_Goo Duplicate() => MemberwiseClone() as IGH_Goo;

    public override string ToString()
    {
      if (Value.IsLevelConstraint(out var level, out var levelOffset))
      {
        var name = level.Name;
        var token = $"'{name ?? "Invalid Level"}'";
        if (levelOffset.HasValue && Math.Abs(levelOffset.Value) > 1e-9)
          token += $" {(levelOffset.Value < 0.0 ? "-" : "+")} {GH_Format.FormatDouble(Math.Abs(GeometryDecoder.ToModelLength(levelOffset.Value)))} {GH_Format.RhinoUnitSymbol()}";

        return token;
      }
      else if (Value.IsRelative(out var relativeOffset, out var relativeElement))
      {
        var name = relativeElement.Name;
        if (string.IsNullOrEmpty(name)) name = relativeElement.Category?.Name;
        var token = $"'{name ?? "Invalid Element"}'";
        token += $" {(relativeOffset < 0.0 ? "-" : "+")} {GH_Format.FormatDouble(Math.Abs(GeometryDecoder.ToModelLength(relativeOffset)))} {GH_Format.RhinoUnitSymbol()}";

        return token;
      }
      else if (Value.IsOffset(out var offset))
      {
        return $"Δ {(offset < 0.0 ? "-" : "+")}{GH_Format.FormatDouble(Math.Abs(GeometryDecoder.ToModelLength(offset)))} {GH_Format.RhinoUnitSymbol()}";
      }
      else if (Value.IsElevation(out var elevation))
      {
        return $"{GH_Format.FormatDouble(GeometryDecoder.ToModelLength(elevation))} {GH_Format.RhinoUnitSymbol()}";
      }

      return string.Empty;
    }

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
        case ARDB.BasePoint basePoint: Value = new External.DB.ElevationElementReference(default, basePoint); return true;
        case External.DB.ElevationElementReference elevation: Value = elevation; return true;
      }

      return base.CastFrom(source);
    }
  }
}

namespace RhinoInside.Revit.GH.Parameters
{
  public class ProjectElevation : Param<Types.ProjectElevation>
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
      subcategory: "Revit Primitives"
    )
    { }
  }
}

namespace RhinoInside.Revit.GH.Components.Input
{
  public class ConstructProjectElevation : Component
  {
    public override Guid ComponentGuid => new Guid("54C795D0-38F8-4703-8968-0336C9D9B066");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    public ConstructProjectElevation() : base
    (
      name: "Project Elevation",
      nickname: "Project Elevation",
      description: "Constructs a project elevation",
      category: "Revit",
      subCategory: "Input"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager[manager.AddParameter(new Parameters.BasePoint(), "Base Point", "BP", "Reference base point", GH_ParamAccess.item)].Optional = true;
      manager.AddNumberParameter("Elevation", "E", "Elevation above or below the base point", GH_ParamAccess.item, 0.0);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ProjectElevation(), "Project Elevation", "PE", "Absolute elevation in the project", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.TryGetData(DA, "Base Point", out Types.IGH_BasePoint basePoint)) return;
      if (!Params.GetData(DA, "Elevation", out double? elevation)) return;

      DA.SetData("Project Elevation", new Types.ProjectElevation(elevation.Value, basePoint));
    }
  }
}

using System;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB
{
  public readonly struct ElevationElementReference : IEquatable<ElevationElementReference>
  {
    readonly ARDB.Document Document;
    readonly ARDB.ElementId BaseId;
    readonly double? Value;

    ARDB.Element Base => Document?.GetElement(BaseId);

    public ElevationElementReference(double? value)
    {
      Document = default;
      BaseId = default;
      Value = value;
    }

    public ElevationElementReference(double? value, ARDB.Element baseElement)
    {
      if (baseElement is object)
      {
        switch (baseElement)
        {
          case ARDB.Level _:          break;
          case ARDB.BasePoint _:      break;
          case ARDB.Element e:
          if (e.Category?.ToBuiltInCategory() != ARDB.BuiltInCategory.OST_IOS_GeoSite)
            throw new ArgumentException("Invalid element", nameof(baseElement));
          break;
        }
      }

      Document = baseElement?.Document;
      BaseId = baseElement?.Id ?? ElementIdExtension.Invalid;
      Value = value;
    }

    public ElevationElementReference(ARDB.Level level)
    {
      var basePoint = default(ARDB.Element);
      var elevationBase = level?.Document.GetElement(level.GetTypeId()).GetParameterValue<ElevationBase>(ARDB.BuiltInParameter.LEVEL_RELATIVE_BASE_TYPE);
      switch (elevationBase)
      {
        case ElevationBase.InternalOrigin:    basePoint = InternalOriginExtension.Get(level.Document); break;
        case ElevationBase.ProjectBasePoint:  basePoint = BasePointExtension.GetProjectBasePoint(level.Document); break;
        case ElevationBase.SurveyPoint:       basePoint = BasePointExtension.GetSurveyPoint(level.Document); break;
      }

      Document = basePoint?.Document;
      BaseId = basePoint?.Id ?? ElementIdExtension.Invalid;
      Value = elevationBase == ElevationBase.InternalOrigin ? level?.ProjectElevation : level?.Elevation;
    }

    public bool IsUnlimited() => !BaseId.IsValid() && (!Value.HasValue || (double.IsInfinity(Value.Value) || double.IsNaN(Value.Value)));

    public bool IsAbsolute => BaseId is null && Value is object;

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

    public bool IsOffset(out double offset)
    {
      if (BaseId == ARDB.ElementId.InvalidElementId && Value.HasValue)
      {
        offset = Value.Value;
        return true;
      }

      offset = double.NaN;
      return false;
    }

    public bool IsElevation(out double elevation)
    {
      if (BaseId != ElementIdExtension.Invalid)
      {
        elevation = Elevation;
        return true;
      }

      elevation = double.NaN;
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

    public bool IsProjectElevation(out ARDB.Element basePoint, out double offset)
    {
      if (IsRelative(out var o, out var e))
      {
        switch (e)
        {
          case ARDB.Level l:
            if (new ElevationElementReference(l).IsRelative(out var le, out e))
            {
              basePoint = e;
              offset = le + o;
              return true;
            }
            break;

          case ARDB.BasePoint bp: basePoint = bp; offset = o; return true;

#if REVIT_2021
          case ARDB.InternalOrigin io: basePoint = io; offset = o; return true;
#else
          case ARDB.Element element:
            if (InternalOriginExtension.Get(element.Document).Id == element.Id)
            {
              basePoint = element;
              offset = o;
              return true;
            }
            break;
#endif
        }
      }

      basePoint = default;
      offset = double.NaN;
      return false;
    }

    public double? BaseElevation
    {
      get
      {
        switch (Base)
        {
          case ARDB.Level level: return level.ProjectElevation;
          case ARDB.BasePoint basePoint: return basePoint.GetPosition().Z;
#if REVIT_2021
          case ARDB.InternalOrigin internalOrigin: return internalOrigin.GetPosition().Z;
#else
          case ARDB.Element element: if (InternalOriginExtension.Get(element.Document).Id == element.Id) return 0.0;
            break;
#endif
        }

        return default;
      }
    }

    public double Offset => Value ?? 0.0;

    public double Elevation => (BaseElevation ?? 0.0) + Offset;

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
      else if (IsUnlimited())
      {
        return "Unlimited";
      }

      return string.Empty;
    }

    #region IEquatable
    public override int GetHashCode() =>
      (Base?.Document.GetHashCode() ?? 0) ^
      (Base?.Id.GetHashCode() ?? 0) ^
      (Value?.GetHashCode() ?? 0);

    public override bool Equals(object obj) => obj is ElevationElementReference other && Equals(other);

    public bool Equals(ElevationElementReference other) => Base.IsEquivalent(other.Base) && Value == other.Value;

    public static bool operator ==(ElevationElementReference left, ElevationElementReference right) => left.Equals(right);
    public static bool operator !=(ElevationElementReference left, ElevationElementReference right) => !left.Equals(right);
    #endregion

    #region Solve Base & Top
    public static void SolveBase
    (
      ARDB.Document document,
      double projectElevation, double defaultBaseElevation,
      ref ElevationElementReference? baseElevation,
      double defaultBaseOffset = 0.0
    )
    {
      if (!baseElevation.HasValue || !baseElevation.Value.IsLevelConstraint(out var baseLevel, out var bottomOffset))
      {
        baseLevel = document.GetNearestLevel(projectElevation + defaultBaseElevation);

        double elevation = projectElevation, offset = defaultBaseElevation;
        if (baseElevation.HasValue)
        {
          if (baseElevation.Value.IsElevation(out elevation)) { offset = 0.0; }
          else if (baseElevation.Value.IsOffset(out offset)) { elevation = projectElevation; }
        }

        baseElevation = new ElevationElementReference(elevation - baseLevel.ProjectElevation + offset, baseLevel);
      }
      else if (bottomOffset is null)
      {
        baseElevation = new ElevationElementReference(defaultBaseOffset, baseLevel);
      }
    }

    public static void SolveBaseAndTop
    (
      ARDB.Document document,
      double projectElevation, double defaultBaseElevation, double defaultTopElevation,
      ref ElevationElementReference? baseElevation, ref ElevationElementReference? topElevation,
      double defaultBaseOffset = 0.0, double defaultTopOffset = 0.0
    )
    {
      if (!baseElevation.HasValue || !baseElevation.Value.IsLevelConstraint(out var baseLevel, out var bottomOffset))
      {
        baseLevel = document.GetNearestLevel(projectElevation + defaultBaseElevation);

        double elevation = projectElevation, offset = defaultBaseElevation;
        if (baseElevation.HasValue)
        {
          if (baseElevation.Value.IsUnlimited())
          {
            baseLevel = document.GetNearestBaseLevel(projectElevation + defaultBaseElevation, out var t);
            if (baseLevel is null)
            {
              baseLevel = t;
              elevation = projectElevation;
            }
            else elevation = baseLevel?.ProjectElevation ?? defaultBaseOffset;
            offset = 0.0;
          }
          else if (baseElevation.Value.IsElevation(out elevation)) { offset = 0.0; }
          else if (baseElevation.Value.IsOffset(out offset)) { elevation = projectElevation; }
        }

        baseElevation = new ElevationElementReference(elevation - (baseLevel?.ProjectElevation ?? 0.0) + offset, baseLevel);
      }
      else if (bottomOffset is null)
      {
        baseElevation = new ElevationElementReference(defaultBaseOffset, baseLevel);
      }

      if (!topElevation.HasValue || !topElevation.Value.IsLevelConstraint(out var topLevel, out var topOffset))
      {
        topLevel = baseLevel;

        double elevation = projectElevation, offset = defaultTopElevation;
        if (topElevation.HasValue)
        {
          if (topElevation.Value.IsUnlimited())
          {
            topLevel = document.GetNearestTopLevel(projectElevation + defaultBaseElevation, out var b);
            elevation = topLevel?.ProjectElevation ?? defaultTopElevation;
            offset = 0.0;
          }
          else if (topElevation.Value.IsElevation(out elevation)) { offset = 0.0; }
          else if (topElevation.Value.IsOffset(out offset)) { elevation = projectElevation; }
        }

        topElevation = new ElevationElementReference(elevation - (topLevel?.ProjectElevation ?? 0.0) + offset, topLevel);
      }
      else if (topOffset is null)
      {
        topElevation = new ElevationElementReference(defaultTopOffset, topLevel);
      }
    }
    #endregion
  }
}

namespace RhinoInside.Revit.GH.Types
{
  public class ProjectElevation : GH_Goo<External.DB.ElevationElementReference>, IGH_QuickCast
  {
    public ProjectElevation() { }
    internal ProjectElevation(External.DB.ElevationElementReference value) :
      base(value) { }

    public ProjectElevation(double? value) :
      base(new External.DB.ElevationElementReference(value / Revit.ModelUnits))
    { }

    public ProjectElevation(double? elevation, IGH_BasePoint basePoint = null) :
      base(new External.DB.ElevationElementReference(elevation / Revit.ModelUnits, basePoint?.Value as ARDB.Element)) { }

    public override bool IsValid => Value != default;

    public override string TypeName => "Project Elevation";

    public override string TypeDescription => "A signed distance along Z-axis";

    public override IGH_Goo Duplicate() => MemberwiseClone() as IGH_Goo;

    public override string ToString()
    {
      if (Value.IsUnlimited())
      {
        return "Unlimited";
      }
      else if (Value.IsLevelConstraint(out var level, out var levelOffset))
      {
        var name = level.Name;
        var token = $"'{name ?? "Invalid Level"}'";
        if (levelOffset.HasValue && Math.Abs(levelOffset.Value) > 1e-9)
        {
          token += $" {(levelOffset.Value < 0.0 ? "-" : "+")} ";
          if (double.IsInfinity(levelOffset.Value))
            token += $"{Math.Abs(levelOffset.Value)} {GH_Format.RhinoUnitSymbol()}";
          else
            token += $"{GH_Format.FormatDouble(Math.Abs(GeometryDecoder.ToModelLength(levelOffset.Value)))} {GH_Format.RhinoUnitSymbol()}";
        }

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
        return $"{(elevation < 0.0 ? "-" : "+")} {GH_Format.FormatDouble(Math.Abs(GeometryDecoder.ToModelLength(elevation)))} {GH_Format.RhinoUnitSymbol()}";
      }

      return string.Empty;
    }

    internal double Elevation => GeometryDecoder.ToModelLength(Value.Elevation);

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(External.DB.ElevationElementReference)))
      {
        target = (Q) (object) Value;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Number)))
      {
        if (IsElevation(out var elevation))
        {
          target = (Q) (object) new GH_Number(elevation);
          return true;
        }

        return false;
      }

      if (typeof(Q).IsAssignableFrom(typeof(double)))
      {
        if (IsElevation(out var elevation))
        {
          target = (Q) (object) elevation;
          return true;
        }

        return false;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
      {
        if (IsElevation(out var elevation))
        {
          var location = Plane.WorldXY;
          location.Translate(Vector3d.ZAxis * elevation);
          target = (Q) (object) new GH_Plane(location);
          return true;
        }
      }

      return base.CastTo(ref target);
    }

    public override bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      switch (source)
      {
        case int elevation: Value = new External.DB.ElevationElementReference(GeometryEncoder.ToInternalLength(elevation)); return true;
        case double elevation: Value = new External.DB.ElevationElementReference(GeometryEncoder.ToInternalLength(elevation)); return true;
        case ARDB.Level level: Value = new External.DB.ElevationElementReference(level); return true;
        case ARDB.BasePoint basePoint: Value = new External.DB.ElevationElementReference(default, basePoint); return true;
        case External.DB.ElevationElementReference elevation: Value = elevation; return true;
      }

      return base.CastFrom(source);
    }

    public static ProjectElevation operator %(ProjectElevation constraint, IGH_BasePoint basePoint)
    {
      if (basePoint is null) return constraint;
      if (constraint?.IsElevation(out var elevation) is true) return new ProjectElevation(elevation - basePoint.Location.Origin.Z, basePoint);
      if (constraint?.IsOffset(out var offset) is true) return new ProjectElevation(offset, basePoint);
      if (constraint?.IsUnlimited() is true) return new ProjectElevation(default, default);
      return new ProjectElevation(null, basePoint);
    }

    public static ProjectElevation operator +(ProjectElevation constraint, double? value)
    {
      if (value is null) return constraint;
      if (constraint?.IsProjectElevation(out var basePoint, out var elevation) is true) return new ProjectElevation(elevation + value, basePoint);
      if (constraint?.IsElevation(out elevation) is true) return new ProjectElevation(elevation + value);
      if (constraint?.IsOffset(out var offset) is true) return new ProjectElevation(offset + value, null);
      if (constraint?.IsUnlimited() is true) return new ProjectElevation(default, default);
      return new ProjectElevation(value, null);
    }

    public bool IsProjectElevation(out IGH_BasePoint basePoint, out double offset)
    {
      if (Value.IsProjectElevation(out var b, out var o))
      {
        basePoint = GraphicalElement.FromValue(b) as IGH_BasePoint;
        offset = o * Revit.ModelUnits;
        return true;
      }

      basePoint = default;
      offset = double.NaN;
      return false;
    }

    public bool IsOffset(out double offset)
    {
      if (Value.IsOffset(out var o))
      {
        offset = o * Revit.ModelUnits;
        return true;
      }

      offset = double.NaN;
      return false;
    }

    public bool IsElevation(out double elevation)
    {
      if (Value.IsElevation(out var e))
      {
        elevation = e * Revit.ModelUnits;
        return true;
      }

      elevation = double.NaN;
      return false;
    }

    public bool IsUnlimited() => Value.IsUnlimited();

    #region IGH_QuickCast
    GH_QuickCastType IGH_QuickCast.QC_Type => GH_QuickCastType.text;
    private double QC_Value => IsElevation(out var elevation) ? elevation : double.NaN;

    double IGH_QuickCast.QC_Distance(IGH_QuickCast other)
    {
      switch (other.QC_Type)
      {
        case GH_QuickCastType.@bool:  return Math.Abs((other.QC_Bool() ? 1.0 : 0.0) - QC_Value);
        case GH_QuickCastType.@int:   return Math.Abs(other.QC_Int() - QC_Value);
        case GH_QuickCastType.num:    return Math.Abs(other.QC_Num() - QC_Value);
        case GH_QuickCastType.text:   return other.QC_Distance(new GH_String(((IGH_QuickCast) this).QC_Text()));
        default: throw new InvalidOperationException($"{nameof(ProjectElevation)}.QC_Distance cannot be called with a parameter of type {other.GetType().FullName}");
      }
    }

    int IGH_QuickCast.QC_Hash() => Math.Round(QC_Value, 9).GetHashCode();

    bool IGH_QuickCast.QC_Bool() => Math.Abs(QC_Value) > 0.0; // NaN is also False

    int IGH_QuickCast.QC_Int() => System.Convert.ToInt32(Math.Round(QC_Value, MidpointRounding.AwayFromZero));

    double IGH_QuickCast.QC_Num() => QC_Value;

    string IGH_QuickCast.QC_Text() => QC_Value.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);

    Color IGH_QuickCast.QC_Col()
    {
      var c = System.Convert.ToInt32(Math.Min(Math.Max(QC_Value, 0.0), 1.0) * 255);
      return Color.FromArgb(c, c, c);
    }

    Point3d IGH_QuickCast.QC_Pt() => throw new InvalidCastException($"{TypeName} cannot be cast to Rhino.Geometry.Point3d");
    Vector3d IGH_QuickCast.QC_Vec() => throw new InvalidCastException($"{TypeName} cannot be cast to Rhino.Geometry.Vector3d");
    Complex IGH_QuickCast.QC_Complex() => new Complex(QC_Value);
    Matrix IGH_QuickCast.QC_Matrix() => throw new InvalidCastException($"{TypeName} cannot be cast to Rhino.Geometry.Matrix");
    Interval IGH_QuickCast.QC_Interval() => throw new InvalidCastException($"{TypeName} cannot be cast to Rhino.Geometry.Interval");

    int IGH_QuickCast.QC_CompareTo(IGH_QuickCast other)
    {
      if (GH_QuickCastType.num != other.QC_Type) return other.QC_Type.CompareTo(GH_QuickCastType.num);

      var num = other.QC_Num();
      if(Math.Abs(num - QC_Value) < 0.000000001) return 0;

      return QC_Value.CompareTo(num);
    }
    #endregion
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
      subcategory: "Revit"
    )
    { }
  }
}

namespace RhinoInside.Revit.GH.Components.Site
{
  [ComponentVersion(introduced: "1.0", updated: "1.14")]
  public class ConstructProjectElevation : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("54C795D0-38F8-4703-8968-0336C9D9B066");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    public ConstructProjectElevation() : base
    (
      name: "Project Elevation",
      nickname: "Elevation",
      description: "Constructs a project elevation",
      category: "Revit",
      subCategory: "Site"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.ProjectElevation>("Elevation", "E", "Elevation in a project", optional: true),
      ParamDefinition.Create<Parameters.BasePoint>("Base Point", "BP", "Reference base point", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Number>("Offset", "O", "Offset above or below the base point", optional: true, relevance: ParamRelevance.Primary)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ProjectElevation>("Elevation", "E", "Elevation in a project"),
      ParamDefinition.Create<Parameters.BasePoint>("Base Point", "BP", "Reference base point", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Number>("Offset", "O", "Offset above or below the base point", relevance: ParamRelevance.Primary)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.TryGetData(DA, "Elevation", out Types.ProjectElevation elevation)) return;
      if (!Params.TryGetData(DA, "Base Point", out Types.IGH_BasePoint basePoint)) return;
      if (!Params.TryGetData(DA, "Offset", out double? offset)) return;

      if (basePoint is object) elevation %= basePoint;
      if (offset is object) elevation += offset;

      Params.TrySetData(DA, "Elevation",  () => elevation);
      if (elevation?.IsProjectElevation(out var elevationBase, out var elevationOffset) is true)
      {
        Params.TrySetData(DA, "Base Point", () => elevationBase);
        Params.TrySetData(DA, "Offset", () => elevationOffset);
      }
      else if (elevation?.IsOffset(out elevationOffset) is true)
      {
        Params.TrySetData(DA, "Offset", () => elevationOffset);
      }
    }
  }
}

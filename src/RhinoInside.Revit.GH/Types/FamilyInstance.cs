using System;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public interface IGH_Instance : IGH_InstanceElement { }

  public class Instance : InstanceElement, IGH_Instance
  {
    protected override Type ScriptVariableType => typeof(DB.Instance);
    public static explicit operator DB.Instance(Instance value) => value?.Value;
    public new DB.Instance Value => base.Value as DB.Instance;

    public Instance() { }
    public Instance(DB.Instance instance) : base(instance) { }

    protected override bool SetValue(DB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(DB.Element element)
    {
      if (element is DB.ElementType)
        return false;

      if (element is DB.View)
        return false;

      return element.Category is object && element.CanHaveTypeAssigned();
    }

    public override Plane Location
    {
      get
      {
        if (Value is DB.Instance instance)
        {
          instance.GetLocation(out var origin, out var basisX, out var basisY);
          return new Plane(origin.ToPoint3d(), basisX.ToVector3d(), basisY.ToVector3d());
        }

        return base.Location;
      }
    }
  }

  public interface IGH_FamilyInstance : IGH_Instance { }

  public class FamilyInstance : Instance, IGH_FamilyInstance
  {
    public override string TypeName => Value is DB.FamilyInstance instance ?
      $"Revit {instance.Category.Name}" :
      "Revit Component";
    public override string TypeDescription => "Represents a Revit component";
    protected override Type ScriptVariableType => typeof(DB.FamilyInstance);
    public static explicit operator DB.FamilyInstance(FamilyInstance value) => value?.Value;
    public new DB.FamilyInstance Value => base.Value as DB.FamilyInstance;

    public FamilyInstance() { }
    public FamilyInstance(DB.FamilyInstance value) : base(value) { }

    #region Location
    public override Level Level
    {
      get
      {
        if (Value is DB.FamilyInstance instance)
        {
          var levelId = instance.LevelId;
          if (levelId == DB.ElementId.InvalidElementId)
          {
            var levelParam = instance.get_Parameter(DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
            if (levelParam is null)
              levelParam = instance.get_Parameter(DB.BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM);
            if (levelParam is object)
              levelId = levelParam.AsElementId();
          }

          return new Level(instance.Document, levelId);
        }

        return default;
      }
    }

    public override Plane Location
    {
      get
      {
        var baseLocation = base.Location;

        if (Value?.Mirrored == true)
        {
          baseLocation.XAxis = -baseLocation.XAxis;
          baseLocation.YAxis = -baseLocation.YAxis;
        }

        return baseLocation;
      }
    }

    public override Vector3d FacingOrientation
    {
      get
      {
        if (Value?.CanFlipFacing == true)
          return Value.FacingOrientation.ToVector3d();

        return base.FacingOrientation;
      }
    }

    public override Vector3d HandOrientation
    {
      get
      {
        if (Value?.CanFlipHand == true)
          return Value.HandOrientation.ToVector3d();

        return base.HandOrientation;
      }
    }

    public override Curve Curve
    {
      get
      {
        if(Value is DB.FamilyInstance instance && instance.Location is DB.LocationPoint location)
        {
          if (instance.Symbol.Family.FamilyPlacementType == DB.FamilyPlacementType.TwoLevelsBased)
          {
            var baseLevel = instance.GetParameterValue<DB.Level>(DB.BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
            var topLevel = instance.GetParameterValue<DB.Level>(DB.BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);

            var baseLevelOffset = instance.GetParameterValue<double>(DB.BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
            var topLevelOffset = instance.GetParameterValue<double>(DB.BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);

            var baseElevation = (baseLevel.Elevation + baseLevelOffset) * Revit.ModelUnits;
            var topElevation = (topLevel.Elevation + topLevelOffset) * Revit.ModelUnits;

            var origin = location.Point.ToPoint3d();
            return new LineCurve
            (
              new Line
              (
                origin + Vector3d.ZAxis * baseElevation,
                origin + Vector3d.ZAxis * topElevation
              )
              ,
              baseElevation,
              topElevation
            );
          }
        }

        return base.Curve;
      }
    }
    #endregion

    #region Flip
    public override bool CanFlipFacing => Value?.CanFlipFacing ?? false;
    public override bool? FacingFlipped
    {
      get
      {
        return Value is DB.FamilyInstance instance && instance.CanFlipFacing ?
          (bool?) instance.FacingFlipped :
          default;
      }
      set
      {
        if (value.HasValue && Value is DB.FamilyInstance instance)
        {
          if (!instance.CanFlipFacing)
            throw new InvalidOperationException("Facing can not be flipped for this element.");

          if (instance.FacingFlipped != value)
          {
            InvalidateGraphics();
            instance.flipFacing();
          }
        }
      }
    }

    public override bool CanFlipHand => Value?.CanFlipHand ?? false;
    public override bool? HandFlipped
    {
      get
      {
        return Value is DB.FamilyInstance instance && instance.CanFlipHand ?
          (bool?) instance.HandFlipped :
          default;
      }
      set
      {
        if (value.HasValue && Value is DB.FamilyInstance instance)
        {
          if (!instance.CanFlipHand)
            throw new InvalidOperationException("Hand can not be flipped for this element.");

          if (instance.HandFlipped != value)
          {
            InvalidateGraphics();
            instance.flipHand();
          }
        }
      }
    }

    public override bool CanFlipWorkPlane => Value?.CanFlipWorkPlane ?? false;
    public override bool? WorkPlaneFlipped
    {
      get
      {
        return Value is DB.FamilyInstance instance && instance.CanFlipWorkPlane ?
          (bool?) instance.IsWorkPlaneFlipped :
          default;
      }
      set
      {
        if (value.HasValue && Value is DB.FamilyInstance instance)
        {
          if (!instance.CanFlipWorkPlane)
            throw new InvalidOperationException("Work Plane can not be flipped for this element.");

          if (instance.IsWorkPlaneFlipped != value)
          {
            InvalidateGraphics();
            instance.IsWorkPlaneFlipped = value.Value;
          }
        }
      }
    }
    #endregion

    #region Joins
    public override bool? IsJoinAllowedAtStart
    {
      get => Value is DB.FamilyInstance frame && frame.StructuralType != DB.Structure.StructuralType.NonStructural ?
        (bool?) DB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(frame, 0) :
        default;

      set
      {
        if (value is object &&  Value is DB.FamilyInstance frame)
        {
          if (frame.StructuralType != DB.Structure.StructuralType.NonStructural)
            throw new InvalidOperationException("Join at start can not be set for this element.");

          InvalidateGraphics();

          if (value == true)
            DB.Structure.StructuralFramingUtils.AllowJoinAtEnd(frame, 0);
          else
            DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(frame, 0);
        }
      }
    }

    public override bool? IsJoinAllowedAtEnd
    {
      get => Value is DB.FamilyInstance frame && frame.StructuralType != DB.Structure.StructuralType.NonStructural ?
        (bool?) DB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(frame, 1) :
        default;

      set
      {
        if (value is object && Value is DB.FamilyInstance frame)
        {
          if (frame.StructuralType != DB.Structure.StructuralType.NonStructural)
            throw new InvalidOperationException("Join at end can not be set for this element.");

          InvalidateGraphics();

          if (value == true)
            DB.Structure.StructuralFramingUtils.AllowJoinAtEnd(frame, 1);
          else
            DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(frame, 1);
        }
      }
    }
    #endregion
  }

  public interface IGH_FamilySymbol : IGH_ElementType
  {
    Family Family { get; }
  }

  public class FamilySymbol : ElementType, IGH_FamilySymbol
  {
    public override string TypeName => Value is DB.FamilySymbol symbol ?
      $"Revit {symbol.Category.Name} Type" :
      "Revit Component Type";
    public override string TypeDescription => "Represents a Revit component type";
    protected override Type ScriptVariableType => typeof(DB.FamilySymbol);
    public static explicit operator DB.FamilySymbol(FamilySymbol value) => value?.Value;
    public new DB.FamilySymbol Value => base.Value as DB.FamilySymbol;

    public FamilySymbol() { }
    protected FamilySymbol(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public FamilySymbol(DB.FamilySymbol elementType) : base(elementType) { }

    public Family Family => Value is DB.FamilySymbol symbol ? new Family(symbol.Family) : default;
  }
}

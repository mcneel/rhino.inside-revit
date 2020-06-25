using System;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Instance : InstanceElement
  {
    protected override Type ScriptVariableType => typeof(DB.Instance);
    public static explicit operator DB.Instance(Instance value) =>
      value.Document?.GetElement(value) as DB.Instance;

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
        var instance = (DB.Instance) this;
        if (instance is object)
        {
          var transform = instance.GetTransform();
          var origin = transform.Origin.ToPoint3d();
          var axis = transform.BasisX.ToVector3d();
          var perp = transform.BasisY.ToVector3d();
          var normal = transform.BasisZ.ToVector3d();

          switch (instance.Location)
          {
            case DB.LocationPoint pointLocation:
              origin = pointLocation.Point.ToPoint3d();
              axis.Rotate(pointLocation.Rotation, normal);
              perp.Rotate(pointLocation.Rotation, normal);
              break;
            case DB.LocationCurve curveLocation:
              var start = curveLocation.Curve.Evaluate(0.0, normalized: true).ToPoint3d();
              var end = curveLocation.Curve.Evaluate(1.0, normalized: true).ToPoint3d();
              axis = end - start;
              origin = start + (axis * 0.5);
              perp = Vector3d.CrossProduct(normal, axis);
              break;
          }

          return new Plane(origin, axis, perp);
        }

        return base.Location;
      }
    }
  }

  public class FamilyInstance : Instance
  {
    public override string TypeName
    {
      get
      {
        var instance = (DB.FamilyInstance) this;
        if (instance is object)
          return $"Revit {instance.Category.Name}";

        return "Revit Family Instance";
      }
    }

    public override string TypeDescription => "Represents a Revit Family Instance";
    protected override Type ScriptVariableType => typeof(DB.FamilyInstance);
    public static explicit operator DB.FamilyInstance(FamilyInstance value) =>
      value.Document?.GetElement(value) as DB.FamilyInstance;

    public FamilyInstance() { }
    public FamilyInstance(DB.FamilyInstance value) : base(value) { }

    public override Level Level
    {
      get
      {
        if (base.Level is Level baseLevel)
          return baseLevel;

        var instance = (DB.FamilyInstance) this;
        return Level.FromElement(instance.Document.GetElement(instance.get_Parameter(DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)?.AsElementId() ?? DB.ElementId.InvalidElementId)) as Level;
      }
    }

    public override Plane Location
    {
      get
      {
        var baseLocation = base.Location;

        var instance = (DB.FamilyInstance) this;
        if (instance is object & instance.Mirrored)
        {
          baseLocation.XAxis = -baseLocation.XAxis;
          baseLocation.YAxis = -baseLocation.YAxis;
        }

        return baseLocation;
      }
    }

    public override Vector3d Orientation
    {
      get
      {
        var instance = (DB.FamilyInstance) this;
        if (instance?.CanFlipFacing == true)
          return instance.FacingOrientation.ToVector3d();

        return base.Orientation;
      }
    }

    public override Vector3d Handing
    {
      get
      {
        var instance = (DB.FamilyInstance) this;
        if (instance?.CanFlipHand == true)
          return instance.HandOrientation.ToVector3d();

        return base.Handing;
      }
    }

    public override Curve Curve
    {
      get
      {
        var instance = (DB.FamilyInstance) this;

        if (instance?.Location is DB.LocationPoint location)
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
  }
}

using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Component")]
  public interface IGH_FamilyInstance : IGH_InstanceElement { }

  [Kernel.Attributes.Name("Component")]
  public class FamilyInstance : InstanceElement,
    IGH_FamilyInstance,
    Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.FamilyInstance);
    public new ARDB.FamilyInstance Value => base.Value as ARDB.FamilyInstance;

    public FamilyInstance() { }
    public FamilyInstance(ARDB.FamilyInstance value) : base(value) { }

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    public new bool BakeElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      // 3. Update if necessary
      if (Value is ARDB.FamilyInstance element)
      {
        using (var options = new ARDB.Options() { DetailLevel = ARDB.ViewDetailLevel.Fine })
        {
          using (var context = GeometryDecoder.Context.Push())
          {
            context.Element = element;
            context.Category = element.Category;
            context.Material = element.Category?.Material;

            using (var geometry = element.GetGeometry(options))
            {
              if (geometry is ARDB.GeometryElement geometryElement)
              {
                var transform = element.GetTransform();
                var location = new Plane(transform.Origin.ToPoint3d(), transform.BasisX.ToVector3d(), transform.BasisY.ToVector3d());
                var worldToElement = Transform.PlaneToPlane(location, Plane.WorldXY);

                if (BakeGeometryElement(idMap, overwrite, doc, att, worldToElement, element, geometry, out var idefIndex))
                {
                  att = att?.Duplicate() ?? doc.CreateDefaultAttributes();
                  att.Name = element.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;
                  att.Url = element.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_URL)?.AsString() ?? string.Empty;

                  var category = Category;
                  if (category is object && category.BakeElement(idMap, false, doc, att, out var layerGuid))
                    att.LayerIndex = doc.Layers.FindId(layerGuid).Index;

                  guid = doc.Objects.AddInstanceObject(idefIndex, Transform.PlaneToPlane(Plane.WorldXY, location), att);

                  // AddInstanceObject places the object on the active view if is a Page
                  if (doc.Views.ActiveView is Rhino.Display.RhinoPageView)
                    doc.Objects.ModifyAttributes(guid, att, quiet: true);
                }
              }

              if (guid != Guid.Empty)
              {
                idMap.Add(Id, guid);
                return true;
              }
            }
          }
        }
      }

      return false;
    }
    #endregion

    #region Location
    public override ARDB.ElementId LevelId
    {
      get
      {
        if (Value is ARDB.FamilyInstance instance)
        {
          var levelId = instance.LevelId;
          if (levelId == ARDB.ElementId.InvalidElementId)
          {
            var levelParam = instance.get_Parameter(ARDB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
            if (levelParam is null)
              levelParam = instance.get_Parameter(ARDB.BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM);
            if (levelParam is object)
              levelId = levelParam.AsElementId();
          }

          return levelId;
        }

        return default;
      }
    }

    public override Vector3d HandOrientation => WorkPlaneFlipped == true ? -base.HandOrientation : base.HandOrientation;
    public override Vector3d FacingOrientation => Value?.HandFlipped != Value?.FacingFlipped ? -base.FacingOrientation : base.FacingOrientation;
    public override Vector3d WorkPlaneOrientation => base.WorkPlaneOrientation;

    public override Curve Curve
    {
      get
      {
        if (Value is ARDB.FamilyInstance instance && instance.Location is ARDB.LocationPoint location)
        {
          if (instance.Symbol.Family.FamilyPlacementType == ARDB.FamilyPlacementType.TwoLevelsBased)
          {
            var baseLevel = instance.GetParameterValue<ARDB.Level>(ARDB.BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
            var topLevel = instance.GetParameterValue<ARDB.Level>(ARDB.BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);

            var baseLevelOffset = instance.GetParameterValue<double>(ARDB.BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
            var topLevelOffset = instance.GetParameterValue<double>(ARDB.BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);

            var baseElevation = (baseLevel.GetElevation() + baseLevelOffset) * Revit.ModelUnits;
            var topElevation = (topLevel.GetElevation() + topLevelOffset) * Revit.ModelUnits;

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

    public override void SetCurve(Curve curve, bool keepJoins = false)
    {
      if (curve is object && Value is ARDB.FamilyInstance instance && curve is object)
      {
        if (instance.Location is ARDB.LocationCurve locationCurve)
        {
          var newCurve = curve.ToCurve();
          if (!locationCurve.Curve.AlmostEquals(newCurve, GeometryTolerance.Internal.VertexTolerance))
          {
            using (!keepJoins ? ElementJoins.DisableJoinsScope(instance) : default)
              locationCurve.Curve = newCurve;

            InvalidateGraphics();
          }
        }
        else base.SetCurve(curve, keepJoins);
      }
    }
    #endregion

    #region Flip
    public override bool CanFlipHand => Value?.CanFlipHand ?? false;
    public override bool? HandFlipped
    {
      get
      {
        return Value is ARDB.FamilyInstance instance && instance.CanFlipHand ?
          (bool?) instance.HandFlipped :
          default;
      }
      set
      {
        if (value.HasValue && Value is ARDB.FamilyInstance instance)
        {
          if (!instance.CanFlipHand)
            throw new Exceptions.RuntimeErrorException("Hand can not be flipped for this element.");

          if (instance.HandFlipped != value)
          {
            InvalidateGraphics();
            instance.flipHand();
          }
        }
      }
    }

    public override bool CanFlipFacing => Value?.CanFlipFacing ?? false;
    public override bool? FacingFlipped
    {
      get
      {
        return Value is ARDB.FamilyInstance instance && instance.CanFlipFacing ?
          (bool?) instance.FacingFlipped :
          default;
      }
      set
      {
        if (value.HasValue && Value is ARDB.FamilyInstance instance)
        {
          if (!instance.CanFlipFacing)
            throw new Exceptions.RuntimeErrorException("Facing can not be flipped for this element.");

          if (instance.FacingFlipped != value)
          {
            InvalidateGraphics();
            instance.flipFacing();
          }
        }
      }
    }

    public override bool CanFlipWorkPlane => Value?.CanFlipWorkPlane ?? false;
    public override bool? WorkPlaneFlipped
    {
      get
      {
        return Value is ARDB.FamilyInstance instance && instance.CanFlipWorkPlane ?
          (bool?) instance.IsWorkPlaneFlipped :
          default;
      }
      set
      {
        if (value.HasValue && Value is ARDB.FamilyInstance instance)
        {
          if (!instance.CanFlipWorkPlane)
            throw new Exceptions.RuntimeErrorException("Work Plane can not be flipped for this element.");

          if (instance.IsWorkPlaneFlipped != value)
          {
            InvalidateGraphics();
            instance.IsWorkPlaneFlipped = value.Value;
          }
        }
      }
    }
    #endregion

    #region IHostElementAccess
    public override GraphicalElement HostElement
    {
      get
      {
        if (Value is ARDB.FamilyInstance instance)
        {
          var host = GetElement<GraphicalElement>(instance.Host);
          return instance.HostFace is ARDB.Reference hostFace ? host?.GetElementFromReference<GraphicalElement>(hostFace) : host;
        }

        return default;
      }
    }

    public GeometryFace HostFace
    {
      get
      {
        if (Value is ARDB.FamilyInstance instance && instance.HostFace is ARDB.Reference reference)
          return GetGeometryObjectFromReference<GeometryFace>(reference);

        return default;
      }
    }
    #endregion

    #region References
    public IList<ARDB.Reference> GetReferences(ARDB.FamilyInstanceReferenceType referenceType)
    {
      return Value?.GetReferences(referenceType) ?? Array.Empty<ARDB.Reference>();
    }
    public ARDB.Reference GetReference(string referenceName)
    {
      return Value?.GetReferenceByName(referenceName);
    }
    #endregion
  }

  [Kernel.Attributes.Name("Component Type")]
  public interface IGH_FamilySymbol : IGH_ElementType
  {
    Family Family { get; }
  }

  [Kernel.Attributes.Name("Component Type")]
  public class FamilySymbol : ElementType, IGH_FamilySymbol, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.FamilySymbol);
    public static explicit operator ARDB.FamilySymbol(FamilySymbol value) => value?.Value;
    public new ARDB.FamilySymbol Value => base.Value as ARDB.FamilySymbol;

    public FamilySymbol() { }
    protected FamilySymbol(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public FamilySymbol(ARDB.FamilySymbol elementType) : base(elementType) { }

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      // 3. Update if necessary
      if (Value is ARDB.FamilySymbol element)
      {
        using (var options = new ARDB.Options() { DetailLevel = ARDB.ViewDetailLevel.Fine })
        {
          using (var context = GeometryDecoder.Context.Push())
          {
            context.Element = element;
            context.Category = element.Category;
            context.Material = element.Category?.Material;

            using (var geometry = element.GetGeometry(options))
            {
              if (geometry is ARDB.GeometryElement geometryElement)
              {
                if (GeometricElement.BakeGeometryElement(idMap, overwrite, doc, att, Transform.Identity, element, geometry, out var idefIndex))
                  guid = doc.InstanceDefinitions[idefIndex].Id;
              }

              if (guid != Guid.Empty)
              {
                idMap.Add(Id, guid);
                return true;
              }
            }
          }
        }
      }

      return false;
    }
    #endregion

    public Family Family => Value is ARDB.FamilySymbol symbol ? new Family(symbol.Family) : default;

    internal void AssertPlacementType(ARDB.FamilyPlacementType placementType)
    {
      if (Value?.Family.FamilyPlacementType == placementType)
        return;

      switch (Value?.Family.FamilyPlacementType)
      {
        case ARDB.FamilyPlacementType.Invalid:
          throw new Exceptions.RuntimeArgumentException("Type", $"Type '{DisplayName}' is not a valid type.");

        case ARDB.FamilyPlacementType.OneLevelBased:
          throw new Exceptions.RuntimeArgumentException("Type", $"Type '{DisplayName}' is a level-based type.{Environment.NewLine}Consider use 'Add Component (Location)' component.");

        case ARDB.FamilyPlacementType.OneLevelBasedHosted:
          throw new Exceptions.RuntimeArgumentException("Type", $"Type '{DisplayName}' is a host-based type.{Environment.NewLine}Consider use 'Add Component (Location)' component.");

        case ARDB.FamilyPlacementType.TwoLevelsBased:
          throw new Exceptions.RuntimeArgumentException("Type", $"Type '{DisplayName}' is a host-based type.{Environment.NewLine}Consider use 'Add Column' or 'Add Structural Column' component.");

        case ARDB.FamilyPlacementType.ViewBased:
          throw new Exceptions.RuntimeArgumentException("Type", $"Type '{DisplayName}' is a view-based type.{Environment.NewLine}Consider use 'Add Detail Item (Location)' component.");

        case ARDB.FamilyPlacementType.WorkPlaneBased:
          throw new Exceptions.RuntimeArgumentException("Type", $"Type '{DisplayName}' is a work plane-based type.{Environment.NewLine}Consider use 'Add Component (Work Plane)' component.");

        case ARDB.FamilyPlacementType.CurveBased:
          throw new Exceptions.RuntimeArgumentException("Type", $"Type '{DisplayName}' is a curve based type.{Environment.NewLine}Consider use 'Add Component (Curve)' component.");

        case ARDB.FamilyPlacementType.CurveBasedDetail:
          throw new Exceptions.RuntimeArgumentException("Type", $"Type '{DisplayName}' is a curve based type.{Environment.NewLine}Consider use 'Add Detail Item (Curve)' component.");

        case ARDB.FamilyPlacementType.CurveDrivenStructural:
          throw new Exceptions.RuntimeArgumentException("Type", $"Type '{DisplayName}' is a structural curve based type.{Environment.NewLine}Consider use 'Add Structural Beam' or 'Add Structural Brace' component.");

        case ARDB.FamilyPlacementType.Adaptive:
          throw new Exceptions.RuntimeArgumentException("Type", $"Type '{DisplayName}' is an adaptive family type.{Environment.NewLine}Consider use 'Add Component (Adaptive)' component.");
      }

      throw new Exceptions.RuntimeArgumentException("Type", $"Type '{DisplayName}' is not a valid {placementType} type.");
    }
  }
}

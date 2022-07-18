using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Autodesk.Revit.Creation;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Families
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using Kernel.Attributes;

  [ComponentVersion(introduced: "1.0", updated: "1.9")]
  public class FamilyInstanceByLocation : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("0C642D7D-897B-479E-8668-91E09222D7B9");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public FamilyInstanceByLocation () : base
    (
      name: "Add Component (Location)",
      nickname: "CompLoca",
      description: "Given its location, it reconstructs a Component element into the active Revit document",
      category: "Revit",
      subCategory: "Build"
    )
    { }

    bool Reuse(ref ARDB.FamilyInstance element, Plane location, ARDB.FamilySymbol type, ARDB.Level level, ARDB.Element host)
    {
      if (element is null) return false;

      if (!element.Host.IsEquivalent(host)) return false;
      if (element.LevelId != (level?.Id ?? ARDB.ElementId.InvalidElementId)) return false;
      if (element.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(element.Document, new ARDB.ElementId[] { element.Id }, type.Id))
        {
          if (element.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            element = element.Document.GetElement(id) as ARDB.FamilyInstance;
        }
        else return false;
      }

      var pinned = element.Pinned;
      try
      {
        element.Pinned = false;
        element.SetLocation(location.Origin.ToXYZ(), location.XAxis.ToXYZ(), location.YAxis.ToXYZ());
      }
      catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
      finally { element.Pinned = pinned; }

      return true;
    }

    void ReconstructFamilyInstanceByLocation
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New Component element")]
      ref ARDB.FamilyInstance component,

      [Description("Location where to place the element. Point or plane is accepted.")]
      Plane location,
      ARDB.FamilySymbol type,
      Optional<ARDB.Level> level,
      [ParamType(typeof(Parameters.GraphicalElement))]
      [Optional] ARDB.Element host
    )
    {
      if (!location.IsValid)
        ThrowArgumentException(nameof(location));

      if (type?.Document.IsEquivalent(document) == false)
        ThrowArgumentException(nameof(type));

      if (level.HasValue && level.Value.Document.IsEquivalent(document) == false)
        ThrowArgumentException(nameof(level));

      if (host?.Document.IsEquivalent(document) == false)
        ThrowArgumentException(nameof(host));

      if (!type.IsActive)
        type.Activate();

      SolveOptionalLevel(document, location, type, ref level, host);

      if (!Reuse(ref component, location, type, level.GetValueOrDefault(), host))
      {
        FamilyInstanceCreationData creationData;
        switch (type.Family.FamilyPlacementType)
        {
          case ARDB.FamilyPlacementType.OneLevelBased:
            creationData = CreateOneLevelBased(document, location, type, level, host);
            break;

          case ARDB.FamilyPlacementType.OneLevelBasedHosted:
            creationData = CreateOneLevelBasedHosted(document, location, type, level, host);
            break;

          case ARDB.FamilyPlacementType.TwoLevelsBased:
            creationData = CreateTwoLevelsBased(document, location, type, level, host);
            break;

          case ARDB.FamilyPlacementType.WorkPlaneBased:
            creationData = CreateWorkPlaneBased(document, location, type, level, host);
            break;

          default:
            creationData = CreateDefault(document, location, type, level, host);
            break;
        }

        var dataList = new List<FamilyInstanceCreationData>() { creationData };
        var newElementIds = document.IsFamilyDocument ?
                            document.FamilyCreate.NewFamilyInstances2(dataList) :
                            document.Create.NewFamilyInstances2(dataList);

        if (newElementIds.Count != 1)
          throw new Exceptions.RuntimeErrorException("Failed to create Family Instance element.");

        var newElement = document.GetElement(newElementIds.First()) as ARDB.FamilyInstance;

        var parametersMask = new ARDB.BuiltInParameter[]
        {
          ARDB.BuiltInParameter.ELEMENT_LOCKED_PARAM,
          ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
          ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
          ARDB.BuiltInParameter.FAMILY_LEVEL_PARAM,
          ARDB.BuiltInParameter.FAMILY_BASE_LEVEL_PARAM,
          ARDB.BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM,
          ARDB.BuiltInParameter.FAMILY_TOP_LEVEL_PARAM,
          ARDB.BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM,
          ARDB.BuiltInParameter.SCHEDULE_LEVEL_PARAM,
          ARDB.BuiltInParameter.SCHEDULE_BASE_LEVEL_PARAM,
          ARDB.BuiltInParameter.SCHEDULE_BASE_LEVEL_OFFSET_PARAM,
          ARDB.BuiltInParameter.SCHEDULE_TOP_LEVEL_PARAM,
          ARDB.BuiltInParameter.SCHEDULE_TOP_LEVEL_OFFSET_PARAM,
          ARDB.BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM,
          ARDB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM
        };

        ReplaceElement(ref component, newElement, parametersMask);

        // Regenerate here to allow SetLocation get the current element location correctly.
        if (component.Symbol.Family.FamilyPlacementType != ARDB.FamilyPlacementType.OneLevelBasedHosted)
        {
          document.Regenerate();
          component.SetLocation(location.Origin.ToXYZ(), location.XAxis.ToXYZ(), location.YAxis.ToXYZ());
        }
      }
    }

    void SolveOptionalLevel(ARDB.Document doc, Plane location, ARDB.FamilySymbol type, ref Optional<ARDB.Level> level, ARDB.Element host)
    {
      switch (type.Family.FamilyPlacementType)
      {
        case ARDB.FamilyPlacementType.OneLevelBased:
          SolveOptionalLevel(doc, host, ref level);
          SolveOptionalLevel(doc, location.Origin, ref level, out var _);
          break;

        case ARDB.FamilyPlacementType.OneLevelBasedHosted:
          if (host is null)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "This family requires a host.");
          else
            SolveOptionalLevel(doc, host, ref level);

          SolveOptionalLevel(doc, location.Origin, ref level, out var _);
          break;

        case ARDB.FamilyPlacementType.TwoLevelsBased:
          SolveOptionalLevel(doc, host, ref level);
          SolveOptionalLevel(doc, location.Origin, ref level, out var _);
          break;

        case ARDB.FamilyPlacementType.WorkPlaneBased:
          break;

        default:
          if (host is null)
            SolveOptionalLevel(doc, location.Origin, ref level, out var _);

          break;
        }
      }

    FamilyInstanceCreationData CreateOneLevelBased(ARDB.Document doc, Plane location, ARDB.FamilySymbol type, Optional<ARDB.Level> level, ARDB.Element host)
    {
      if (host is null)
      {
        return new FamilyInstanceCreationData
        (
          location.Origin.ToXYZ(),
          type,
          level.Value,
          ARDB.Structure.StructuralType.NonStructural
        );
      }
      else
      {
        return new FamilyInstanceCreationData
        (
          location.Origin.ToXYZ(),
          type,
          host,
          level.Value,
          ARDB.Structure.StructuralType.NonStructural
        );
      }
    }

    FamilyInstanceCreationData CreateOneLevelBasedHosted(ARDB.Document doc, Plane location, ARDB.FamilySymbol type, Optional<ARDB.Level> level, ARDB.Element host)
    {
      return new FamilyInstanceCreationData
      (
        location.Origin.ToXYZ(),
        type,
        host,
        level.Value,
        ARDB.Structure.StructuralType.NonStructural
      );
    }

    FamilyInstanceCreationData CreateTwoLevelsBased(ARDB.Document doc, Plane location, ARDB.FamilySymbol type, Optional<ARDB.Level> level, ARDB.Element host)
    {
      return new FamilyInstanceCreationData
      (
        location.Origin.ToXYZ(),
        type,
        host,
        level.Value,
        ARDB.Structure.StructuralType.NonStructural
      );
    }

    FamilyInstanceCreationData CreateWorkPlaneBased(ARDB.Document doc, Plane location, ARDB.FamilySymbol type, Optional<ARDB.Level> level, ARDB.Element host)
    {
      if (host is null)
        host = ARDB.SketchPlane.Create(doc, location.ToPlane());

      if (level.HasValue)
      {
        return new FamilyInstanceCreationData
        (
          location.Origin.ToXYZ(),
          type,
          host,
          level.Value,
          ARDB.Structure.StructuralType.NonStructural
        );
      }
      else
      {
        return new FamilyInstanceCreationData
        (
          location.Origin.ToXYZ(),
          type,
          host,
          ARDB.Structure.StructuralType.NonStructural
        );
      }
    }

    FamilyInstanceCreationData CreateDefault(ARDB.Document doc, Plane location, ARDB.FamilySymbol type, Optional<ARDB.Level> level, ARDB.Element host)
    {
      if (host is null)
      {
        return new FamilyInstanceCreationData
        (
          location.Origin.ToXYZ(),
          type,
          level.Value,
          ARDB.Structure.StructuralType.NonStructural
        );
      }
      else if (level.HasValue)
      {
        return new FamilyInstanceCreationData
        (
          location.Origin.ToXYZ(),
          type,
          host,
          level.Value,
          ARDB.Structure.StructuralType.NonStructural
        );
      }
      else
      {
        return new FamilyInstanceCreationData
        (
          location.Origin.ToXYZ(),
          type,
          host,
          ARDB.Structure.StructuralType.NonStructural
        );
      }
    }
  }
}

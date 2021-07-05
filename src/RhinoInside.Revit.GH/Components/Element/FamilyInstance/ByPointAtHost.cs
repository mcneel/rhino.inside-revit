using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Autodesk.Revit.Creation;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Kernel.Attributes;

  public class FamilyInstanceByLocation : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("0C642D7D-897B-479E-8668-91E09222D7B9");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public FamilyInstanceByLocation () : base
    (
      name: "Add Component (Location)",
      nickname: "CompLoca",
      description: "Given its location, it reconstructs a Component element into the active Revit document",
      category: "Revit",
      subCategory: "Build"
    )
    { }

    bool Reuse(ref DB.FamilyInstance element, Plane location, DB.FamilySymbol type, DB.Level level, DB.Element host)
    {
      if (element is null) return false;

      if (!element.Host.IsEquivalent(host)) return false;
      if (element.LevelId != level.Id) return false;
      if (element.GetTypeId() != type.Id)
      {
        if (DB.Element.IsValidType(element.Document, new DB.ElementId[] { element.Id }, type.Id))
          element = element.Document.GetElement(element.ChangeTypeId(type.Id)) as DB.FamilyInstance;
        else
          return false;
      }

      // Unpin here to allow SetLocation update location correctly.
      if (element.Pinned == true)
      {
        try { element.Pinned = false; }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
      }

      return true;
    }

    void ReconstructFamilyInstanceByLocation
    (
      [Optional, NickName("DOC")]
      DB.Document document,

      [Description("New Component element")]
      ref DB.FamilyInstance component,

      [Description("Location where to place the element. Point or plane is accepted.")]
      Plane location,
      DB.FamilySymbol type,
      Optional<DB.Level> level,
      [Optional] DB.Element host
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

      if (!Reuse(ref component, location, type, level.Value, host))
      {
        FamilyInstanceCreationData creationData;
        switch (type.Family.FamilyPlacementType)
        {
          case DB.FamilyPlacementType.OneLevelBased:
            creationData = CreateOneLevelBased(document, location, type, level, host);
            break;

          case DB.FamilyPlacementType.OneLevelBasedHosted:
            creationData = CreateOneLevelBasedHosted(document, location, type, level, host);
            break;

          case DB.FamilyPlacementType.TwoLevelsBased:
            creationData = CreateTwoLevelsBased(document, location, type, level, host);
            break;

          case DB.FamilyPlacementType.WorkPlaneBased:
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
          throw new InvalidOperationException();

        var newElement = document.GetElement(newElementIds.First()) as DB.FamilyInstance;

        var parametersMask = new DB.BuiltInParameter[]
        {
          DB.BuiltInParameter.ELEMENT_LOCKED_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_PARAM,
          DB.BuiltInParameter.ELEM_TYPE_PARAM,
          DB.BuiltInParameter.FAMILY_LEVEL_PARAM,
          DB.BuiltInParameter.FAMILY_BASE_LEVEL_PARAM,
          DB.BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM,
          DB.BuiltInParameter.FAMILY_TOP_LEVEL_PARAM,
          DB.BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM,
          DB.BuiltInParameter.SCHEDULE_LEVEL_PARAM,
          DB.BuiltInParameter.SCHEDULE_BASE_LEVEL_PARAM,
          DB.BuiltInParameter.SCHEDULE_BASE_LEVEL_OFFSET_PARAM,
          DB.BuiltInParameter.SCHEDULE_TOP_LEVEL_PARAM,
          DB.BuiltInParameter.SCHEDULE_TOP_LEVEL_OFFSET_PARAM,
          DB.BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM,
          DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM
        };

        ReplaceElement(ref component, newElement, parametersMask);

        // Regenerate here to allow SetLocation get the current element location correctly.
        document.Regenerate();
      }

      component?.SetLocation(location.Origin.ToXYZ(), location.XAxis.ToXYZ(), location.YAxis.ToXYZ());
    }

    void SolveOptionalLevel(DB.Document doc, Plane location, DB.FamilySymbol type, ref Optional<DB.Level> level, DB.Element host)
    {
      switch (type.Family.FamilyPlacementType)
      {
        case DB.FamilyPlacementType.OneLevelBased:
          SolveOptionalLevel(doc, host, ref level);
          SolveOptionalLevel(doc, location.Origin, ref level, out var _);
          break;

        case DB.FamilyPlacementType.OneLevelBasedHosted:
          if (host is null)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "This family requires a host.");
          else
            SolveOptionalLevel(doc, host, ref level);

          SolveOptionalLevel(doc, location.Origin, ref level, out var _);
          break;

        case DB.FamilyPlacementType.TwoLevelsBased:
          SolveOptionalLevel(doc, host, ref level);
          SolveOptionalLevel(doc, location.Origin, ref level, out var _);
          break;

        case DB.FamilyPlacementType.WorkPlaneBased:
          break;

        default:
          if (host is null)
            SolveOptionalLevel(doc, location.Origin, ref level, out var _);

          break;
        }
      }

    FamilyInstanceCreationData CreateOneLevelBased(DB.Document doc, Plane location, DB.FamilySymbol type, Optional<DB.Level> level, DB.Element host)
    {
      if (host is null)
      {
        return new FamilyInstanceCreationData
        (
          location.Origin.ToXYZ(),
          type,
          level.Value,
          DB.Structure.StructuralType.NonStructural
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
          DB.Structure.StructuralType.NonStructural
        );
      }
    }

    FamilyInstanceCreationData CreateOneLevelBasedHosted(DB.Document doc, Plane location, DB.FamilySymbol type, Optional<DB.Level> level, DB.Element host)
    {
      return new FamilyInstanceCreationData
      (
        location.Origin.ToXYZ(),
        type,
        host,
        level.Value,
        DB.Structure.StructuralType.NonStructural
      );
    }

    FamilyInstanceCreationData CreateTwoLevelsBased(DB.Document doc, Plane location, DB.FamilySymbol type, Optional<DB.Level> level, DB.Element host)
    {
      return new FamilyInstanceCreationData
      (
        location.Origin.ToXYZ(),
        type,
        host,
        level.Value,
        DB.Structure.StructuralType.NonStructural
      );
    }

    FamilyInstanceCreationData CreateWorkPlaneBased(DB.Document doc, Plane location, DB.FamilySymbol type, Optional<DB.Level> level, DB.Element host)
    {
      if (host is null)
        host = DB.SketchPlane.Create(doc, location.ToPlane());

      if (level.HasValue)
      {
        return new FamilyInstanceCreationData
        (
          location.Origin.ToXYZ(),
          type,
          host,
          level.Value,
          DB.Structure.StructuralType.NonStructural
        );
      }
      else
      {
        return new FamilyInstanceCreationData
        (
          location.Origin.ToXYZ(),
          type,
          host,
          DB.Structure.StructuralType.NonStructural
        );
      }
    }

    FamilyInstanceCreationData CreateDefault(DB.Document doc, Plane location, DB.FamilySymbol type, Optional<DB.Level> level, DB.Element host)
    {
      if (host is null)
      {
        return new FamilyInstanceCreationData
        (
          location.Origin.ToXYZ(),
          type,
          level.Value,
          DB.Structure.StructuralType.NonStructural
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
          DB.Structure.StructuralType.NonStructural
        );
      }
      else
      {
        return new FamilyInstanceCreationData
        (
          location.Origin.ToXYZ(),
          type,
          host,
          DB.Structure.StructuralType.NonStructural
        );
      }
    }
  }
}

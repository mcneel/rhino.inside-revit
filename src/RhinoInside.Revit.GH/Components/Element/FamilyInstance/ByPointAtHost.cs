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
      "Add Component (Location)", "CompLoca",
      "Given its location, it reconstructs a Component element into the active Revit document",
      "Revit", "Build"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.FamilyInstance(), "Component", "C", "New Component element", GH_ParamAccess.item);
    }

    void ReconstructFamilyInstanceByLocation
    (
      DB.Document doc,
      ref DB.FamilyInstance element,

      [Description("Location where to place the element. Point or plane is accepted.")]
      Plane location,
      DB.FamilySymbol type,
      Optional<DB.Level> level,
      [Optional] DB.Element host
    )
    {
      if (!location.IsValid)
        ThrowArgumentException(nameof(location));

      if (type?.Document.IsEquivalent(doc) == false)
        ThrowArgumentException(nameof(type));

      if (level.HasValue && level.Value.Document.IsEquivalent(doc) == false)
        ThrowArgumentException(nameof(level));

      if (host?.Document.IsEquivalent(doc) == false)
        ThrowArgumentException(nameof(host));

      if (!type.IsActive)
        type.Activate();

      SolveOptionalLevel(doc, location, type, ref level, host);

      var newElement = element;

      // Check if current Instance can be modified
      if (newElement is DB.FamilyInstance)
      {
        if (!newElement.Host.IsEquivalent(host)) newElement = default;
        else if (newElement.LevelId != level.Value.Id) newElement = default;
        else if (newElement.GetTypeId() != type.Id)
        {
          if (DB.Element.IsValidType(type.Document, new DB.ElementId[] { newElement.Id }, type.Id))
          {
            element = default;
            newElement = type.Document.GetElement(newElement.ChangeTypeId(type.Id)) as DB.FamilyInstance;
          }
          else newElement = default;
        }

        if (newElement?.Pinned == true)
        {
          try { newElement.Pinned = false; }
          catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }
        }
      }

      if (newElement is null)
      {
        FamilyInstanceCreationData creationData;
        switch (type.Family.FamilyPlacementType)
        {
          case DB.FamilyPlacementType.OneLevelBased:
            creationData = CreateOneLevelBased(doc, location, type, level, host);
            break;

          case DB.FamilyPlacementType.OneLevelBasedHosted:
            creationData = CreateOneLevelBasedHosted(doc, location, type, level, host);
            break;

          case DB.FamilyPlacementType.TwoLevelsBased:
            creationData = CreateTwoLevelsBased(doc, location, type, level, host);
            break;

          case DB.FamilyPlacementType.WorkPlaneBased:
            creationData = CreateWorkPlaneBased(doc, location, type, level, host);
            break;

          default:
            creationData = CreateDefault(doc, location, type, level, host);
            break;
        }

        // Create a new Instance
        {
          var dataList = new List<FamilyInstanceCreationData>() { creationData };
          var newElementIds = doc.IsFamilyDocument ?
                              doc.FamilyCreate.NewFamilyInstances2(dataList) :
                              doc.Create.NewFamilyInstances2(dataList);

          if (newElementIds.Count != 1)
            throw new InvalidOperationException();

          newElement = doc.GetElement(newElementIds.First()) as DB.FamilyInstance;
        }
      }

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

      ReplaceElement(ref element, newElement, parametersMask);

      // Regenerate here to allow SetLocation get the current element location correctly.
      doc.Regenerate();

      element?.SetLocation(location.Origin.ToXYZ(), location.XAxis.ToXYZ(), location.YAxis.ToXYZ());
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

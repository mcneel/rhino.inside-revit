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
        ThrowArgumentException(nameof(location), "Location is not valid.");

      if (!type.IsActive)
        type.Activate();

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

        var dataList = new List<FamilyInstanceCreationData>() { creationData };
        var newElementIds = doc.IsFamilyDocument ?
                            doc.FamilyCreate.NewFamilyInstances2(dataList) :
                            doc.Create.NewFamilyInstances2(dataList);

        if (newElementIds.Count != 1)
        {
          doc.Delete(newElementIds);
          throw new InvalidOperationException();
        }

        var parametersMask = new DB.BuiltInParameter[]
        {
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

        ReplaceElement(ref element, doc.GetElement(newElementIds.First()) as DB.FamilyInstance, parametersMask);
        doc.Regenerate();

        if (element.Pinned)
        {
          try { element.Pinned = false; }
          catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }
        }
      }

      element?.SetLocation(location.Origin.ToXYZ(), location.XAxis.ToXYZ(), location.YAxis.ToXYZ());
    }

    FamilyInstanceCreationData CreateOneLevelBased(DB.Document doc, Plane location, DB.FamilySymbol type, Optional<DB.Level> level, DB.Element host)
    {
      SolveOptionalLevel(doc, host, ref level);
      SolveOptionalLevel(doc, location.Origin, ref level, out var _);

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
      if (host is null)
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "This family requires a host.");
      else
        SolveOptionalLevel(doc, host, ref level);

      SolveOptionalLevel(doc, location.Origin, ref level, out var _);

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
      SolveOptionalLevel(doc, host, ref level);
      SolveOptionalLevel(doc, location.Origin, ref level, out var _);

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
        SolveOptionalLevel(doc, location.Origin, ref level, out var _);

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

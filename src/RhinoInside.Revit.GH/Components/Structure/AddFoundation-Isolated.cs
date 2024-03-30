using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.Structure
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.9")]
  public class AddFoundationIsolated : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("C1C7CDBB-EE50-40FC-A398-E01465EC65EB");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public AddFoundationIsolated() : base
    (
      name: "Add Foundation (Isolated)",
      nickname: "I-Foundation",
      description: "Given its Location, it adds a structural foundation element to the active Revit document",
      category: "Revit",
      subCategory: "Structure"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_Plane()
        {
          Name = "Location",
          NickName = "L",
          Description = "Structural Foundation location.",
        }
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = "Type",
          NickName = "T",
          Description = "Structural Foundation type.",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_StructuralFoundation
        }
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Level",
          NickName = "L",
          Description = "Level.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Host",
          NickName = "H",
          Description = "Host element.",
          Optional = true
        }, ParamRelevance.Secondary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.FamilyInstance()
        {
          Name = _Foundation_,
          NickName = _Foundation_.Substring(0, 1),
          Description = $"Output {_Foundation_}",
        }
      )
    };

    const string _Foundation_ = "Foundation";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.INSTANCE_MOVES_WITH_GRID_PARAM,
      ARDB.BuiltInParameter.FAMILY_LEVEL_PARAM,
      ARDB.BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM,
    };

    static void AssertValidType(ARDB.FamilySymbol type, ARDB.Element host)
    {
      if (type.Category.ToBuiltInCategory() != ARDB.BuiltInCategory.OST_StructuralFoundation)
        throw new Exceptions.RuntimeArgumentException("Type", $"Type '{type.Name}' is not a valid structural foundation type.");

      var family = type.Family;
      switch (family.FamilyPlacementType)
      {
        case ARDB.FamilyPlacementType.OneLevelBased:
        case ARDB.FamilyPlacementType.TwoLevelsBased:
          if (!(host is null)) throw new Exceptions.RuntimeArgumentException("Type", $"Type '{type.Name}' instances shouldn't be hosted.");
          return;

        case ARDB.FamilyPlacementType.OneLevelBasedHosted:
          switch (family.GetHostingBehavior())
          {
            case ARDB.FamilyHostingBehavior.None:
              if (!(host is null)) throw new Exceptions.RuntimeArgumentException("Type", $"Type '{type.Name}' instances shouldn't be hosted.");
              break;

            case ARDB.FamilyHostingBehavior.Wall:
              if (!(host is ARDB.Wall)) throw new Exceptions.RuntimeArgumentException("Type", $"Type '{type.Name}' instances should be hosted on a Wall.");
              break;

            case ARDB.FamilyHostingBehavior.Floor:
              if (!(host is ARDB.Floor)) throw new Exceptions.RuntimeArgumentException("Type", $"Type '{type.Name}' instances should be hosted on a Floor.");
              break;

            case ARDB.FamilyHostingBehavior.Ceiling:
              if (!(host is ARDB.Ceiling)) throw new Exceptions.RuntimeArgumentException("Type", $"Type '{type.Name}' instances should be hosted on a Ceiling.");
              break;

            case ARDB.FamilyHostingBehavior.Roof:
              if (!(host is ARDB.RoofBase)) throw new Exceptions.RuntimeArgumentException("Type", $"Type '{type.Name}' instances should be hosted on a Roof.");
              break;
          }
          return;

        case ARDB.FamilyPlacementType.WorkPlaneBased:
          if (!(host is null || host is ARDB.SketchPlane || host is ARDB.DatumPlane))
            throw new Exceptions.RuntimeArgumentException("Type", $"Type '{type.Name}' should be hosted on a Datum or a Work Plane.");
          return;
      }

      throw new Exceptions.RuntimeArgumentException("Type", $"Type '{type.Name}' is not a valid one level or work-plane based type.");
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.FamilyInstance>
      (
        doc.Value, _Foundation_, foundation =>
        {
          var tol = GeometryTolerance.Model;

          // Input
          if (!Params.GetData(DA, "Location", out Plane? location, x => x.IsValid)) return null;
          if (!Parameters.FamilySymbol.GetDataOrDefault(this, DA, "Type", out Types.FamilySymbol type, doc, ARDB.BuiltInCategory.OST_StructuralFoundation)) return null;
          if (!Parameters.Level.GetDataOrDefault(this, DA, "Level", out Types.Level level, doc, location.Value.Origin.Z)) return null;
          if (!Params.TryGetData(DA, "Host", out Types.GraphicalElement host)) return null;

          var hostElement = host?.Value;
          AssertValidType(type.Value, hostElement);

          // Compute
          foundation = Reconstruct
          (
            foundation,
            doc.Value,
            location.Value.Origin.ToXYZ(),
            (ERDB.UnitXYZ) location.Value.XAxis.ToXYZ(),
            (ERDB.UnitXYZ) location.Value.YAxis.ToXYZ(),
            type.Value,
            level.Value,
            hostElement
          );

          DA.SetData(_Foundation_, foundation);
          return foundation;
        }
      );
    }

    static bool IsEquivalentHost(ARDB.FamilyInstance foundation, ARDB.Element host)
    {
      switch (host)
      {
        case ARDB.SketchPlane sketchPlane:
          var hostElement = sketchPlane.GetHost(out var hostFace);
          if (hostElement is null) return false;
          if (!sketchPlane.Document.AreEquivalentReferences(hostFace, foundation.HostFace)) return false;
          if (!hostElement.IsEquivalent(foundation.Host)) return false;
          return true;
      }

      return foundation.Host.IsEquivalent(host);
    }

    bool Reuse
    (
      ARDB.FamilyInstance foundation,
      ARDB.FamilySymbol type,
      ARDB.Level level,
      ARDB.Element host
    )
    {
      if (foundation is null) return false;

      if (!IsEquivalentHost(foundation, host)) return false;
      if (type.Id != foundation.GetTypeId()) foundation.ChangeTypeId(type.Id);

      if (foundation.LevelId == ElementIdExtension.Invalid)
      {
        var levelParam = foundation.get_Parameter(ARDB.BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM);
        if (levelParam?.IsReadOnly is false && levelParam.Update(level.Id) == false) return false;
      }
      else if (foundation.LevelId != (level?.Id ?? ARDB.ElementId.InvalidElementId))
      {
        var levelParam = foundation.get_Parameter(ARDB.BuiltInParameter.FAMILY_LEVEL_PARAM);
        if (levelParam.IsReadOnly)
        {
          levelParam = foundation.get_Parameter(ARDB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
          if (levelParam?.Update(level.Id) == false) return false;
        }
        else if(!levelParam.Update(level.Id)) return false;
      }

      return true;
    }

    ARDB.FamilyInstance Create(ARDB.Document doc, ARDB.XYZ point, ARDB.FamilySymbol type, ARDB.Level level, ARDB.Element host)
    {
      if (!type.IsActive) type.Activate();

      if (type.Family.FamilyPlacementType == ARDB.FamilyPlacementType.WorkPlaneBased)
      {
        switch (host)
        {
          case ARDB.SketchPlane _:                                                                break;
          case ARDB.DatumPlane datum:       host = datum.GetSketchPlane(ensureSketchPlane: true); break;
          case ARDB.HostObject hostObject:  host = hostObject.GetSketch()?.SketchPlane;           break;
          default:                          host = null; break;
        }

        if (host is null)
          host = level.GetSketchPlane(ensureSketchPlane: true);
      }

      var list = new List<Autodesk.Revit.Creation.FamilyInstanceCreationData>(1)
      {
        new Autodesk.Revit.Creation.FamilyInstanceCreationData
        (
          location: point,
          symbol: type,
          host: host,
          level: level,
          structuralType: ARDB.Structure.StructuralType.Footing
        )
      };

      var ids = doc.Create().NewFamilyInstances2(list);
      if (ids.Count == 1)
      {
        var instance = doc.GetElement(ids.First()) as ARDB.FamilyInstance;

        // We turn analytical model off by default
        instance.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);
        return instance;
      }

      throw new Exceptions.RuntimeArgumentException("Type", $"Type '{type.FamilyName} : {type.Name}' is not a valid structural foundation type.");
    }

    ARDB.FamilyInstance Reconstruct
    (
      ARDB.FamilyInstance foundation,
      ARDB.Document doc,
      ARDB.XYZ origin,
      ERDB.UnitXYZ basisX,
      ERDB.UnitXYZ basisY,
      ARDB.FamilySymbol type,
      ARDB.Level level,
      ARDB.Element host
    )
    {
      if (type.Family.FamilyPlacementType == ARDB.FamilyPlacementType.WorkPlaneBased)
      {
        if (host is null) host = level;
      }

      if (!Reuse(foundation, type, level, host))
      {
        foundation = foundation.ReplaceElement
        (
          Create(doc, origin, type, level, host),
          ExcludeUniqueProperties
        );
      }

      foundation.get_Parameter(ARDB.BuiltInParameter.INSTANCE_MOVES_WITH_GRID_PARAM)?.Update(false);

      {
        foundation.Document.Regenerate();
        foundation.Pinned = false;
        if (foundation.Host is object)
          foundation.SetLocation(origin, basisX);
        else
          foundation.SetLocation(origin, basisX, basisY);
      }

      return foundation;
    }
  }
}

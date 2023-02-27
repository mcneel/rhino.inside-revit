using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using External.DB;

  [ComponentVersion(introduced: "1.0", updated: "1.13")]
  public class AddComponentLocation : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("0C642D7D-897B-479E-8668-91E09222D7B9");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AddComponentLocation() : base
    (
      name: "Add Component (Location)",
      nickname: "L-Component",
      description: "Given its Location, it adds a component element to the active Revit document",
      category: "Revit",
      subCategory: "Build"
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
          Description = "Component location.",
        }
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = "Type",
          NickName = "T",
          Description = "Component type.",
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
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.FamilyInstance()
        {
          Name = _Component_,
          NickName = _Component_.Substring(0, 1),
          Description = $"Output {_Component_}",
        }
      )
    };

    const string _Component_ = "Component";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.INSTANCE_MOVES_WITH_GRID_PARAM,
      ARDB.BuiltInParameter.FAMILY_LEVEL_PARAM,
    };

    void AssertIsValidType(ARDB.FamilySymbol type, ARDB.Element host)
    {
      var family = type.Family;
      switch (family.FamilyPlacementType)
      {
        case ARDB.FamilyPlacementType.OneLevelBased:
          if (!(host is null) && TrackingMode == ElementTracking.TrackingMode.Reconstruct)
            AddRuntimeMessage
            (
              GH_RuntimeMessageLevel.Warning,
              $"Host input on non Hosted family types does not support tracking mode 'Update' a new element will be created on each solution.{Environment.NewLine}" +
              $"Consider using a Hosted family or enable 'Work Plane-Based' on '{type.FamilyName}' family instead."
            );
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
            throw new Exceptions.RuntimeArgumentException("Type", $"Type '{type.Name}' should be hosted on a Work Plane.");
          return;
      }

      throw new Exceptions.RuntimeArgumentException("Type", $"Type '{type.Name}' is not a valid one-level or work-plane based type.");
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.FamilyInstance>
      (
        doc.Value, _Component_, component =>
        {
          var tol = GeometryTolerance.Model;

          // Input
          if (!Params.GetData(DA, "Location", out Plane? location, x => x.IsValid)) return null;
          if (!Params.GetData(DA, "Type", out Types.FamilySymbol type, x => x.IsValid)) return null;
          if (!Parameters.Level.GetDataOrDefault(this, DA, "Level", out Types.Level level, doc, location.Value.Origin.Z)) return null;
          if (!Params.TryGetData(DA, "Host", out Types.GraphicalElement host)) return null;

          var levelElement = level?.Value;
          var hostElement = host?.Value;
          if (hostElement is ARDB.Level hostLevel)
          {
            levelElement = hostLevel;
            hostElement = default;
          }
          AssertIsValidType(type.Value, hostElement);

          // Compute
          component = Reconstruct
          (
            component,
            doc.Value,
            location.Value.Origin.ToXYZ(),
            location.Value.XAxis.ToXYZ(),
            location.Value.YAxis.ToXYZ(),
            type.Value,
            levelElement,
            hostElement
          );

          DA.SetData(_Component_, component);
          return component;
        }
      );
    }

    static bool IsEquivalentHost(ARDB.FamilyInstance component, ARDB.Element host)
    {
      if (component.Symbol.Family.IsWorkPlaneBased())
      {
        switch (host)
        {
          case ARDB.SketchPlane sketchPlane:

            // <not associated>
            var dependents = sketchPlane.GetDependentElements(CompoundElementFilter.ExclusionFilter(component.Id, inverted: true));
            if (dependents.Contains(component.Id)) return true;

            // Associated to a Datum or a Face
            var hostElement = sketchPlane.GetHost(out var hostFace);
            if (hostElement is null) return false;
            if (!sketchPlane.Document.AreEquivalentReferences(hostFace, component.HostFace)) return false;
            if (!hostElement.IsEquivalent(component.Host)) return false;

            return true;
        }

        if (host is null && component.Host is object)
          host = component.get_Parameter(ARDB.BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM)?.AsElement();
      }
      else
      {
        var levelId = component.LevelId;
        if (!levelId.IsValid()) return false;

        var hostParam = component.get_Parameter(ARDB.BuiltInParameter.INSTANCE_FREE_HOST_PARAM)?.AsString();
        var hostText = hostParam?.Split(':');
        if (hostText?.Length == 2)
        {
          var componentLevel = component.Document.GetElement(levelId);
          if (componentLevel.Name != hostText[1].Trim()) return false;

          var componentLevelType = component.Document.GetElement(componentLevel.GetTypeId()) as ARDB.ElementType;
          if (componentLevelType.FamilyName != hostText[0].Trim()) return false;
        }
      }

      return component.Host.IsEquivalent(host);
    }

    bool Reuse
    (
      ARDB.FamilyInstance component,
      ARDB.FamilySymbol type,
      ARDB.Level level,
      ARDB.Element host
    )
    {
      if (component is null) return false;

      if (!IsEquivalentHost(component, host)) return false;
      if (type.Id != component.GetTypeId())
      {
        if (!component.IsValidType(type.Id)) return false;
        component.ChangeTypeId(type.Id);
      }

      if (component.LevelId == ElementIdExtension.InvalidElementId)
      {
        var levelParam = component.get_Parameter(ARDB.BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM);
        if (levelParam.AsElementId() != level.Id)
        {
          if (levelParam.IsReadOnly) return false;
          if (!levelParam.Update(level.Id)) return false;
        }
      }
      else if (component.LevelId != (level?.Id ?? ARDB.ElementId.InvalidElementId))
      {
        var levelParam = component.get_Parameter(ARDB.BuiltInParameter.FAMILY_LEVEL_PARAM);
        if (levelParam.IsReadOnly)
        {
          levelParam = component.get_Parameter(ARDB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
          if (levelParam?.Update(level.Id) == false) return false;
        }
        else if(!levelParam.Update(level.Id)) return false;
      }

      return true;
    }

    ARDB.FamilyInstance Create(ARDB.Document doc, ARDB.XYZ point, ARDB.FamilySymbol type, ARDB.Level level, ARDB.Element host)
    {
      if (type.Family.FamilyPlacementType == ARDB.FamilyPlacementType.WorkPlaneBased)
      {
        switch (host)
        {
          case ARDB.SketchPlane _:                                                                break;
          case ARDB.DatumPlane datum:       host = datum.GetSketchPlane(ensureSketchPlane: true); break;
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
          host: host is ARDB.Level ? null : host,
          level: host as ARDB.Level ?? level,
          structuralType: ARDB.Structure.StructuralType.NonStructural
        )
      };

      var ids = doc.Create().NewFamilyInstances2(list);
      var instance = doc.GetElement(ids.First()) as ARDB.FamilyInstance;

      // We turn analytical model off by default
      instance.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);
      return instance;
    }

    ARDB.FamilyInstance Reconstruct
    (
      ARDB.FamilyInstance component,
      ARDB.Document doc,
      ARDB.XYZ origin,
      ARDB.XYZ basisX,
      ARDB.XYZ basisY,
      ARDB.FamilySymbol type,
      ARDB.Level level,
      ARDB.Element host
    )
    {
      if (!Reuse(component, type, level, host))
      {
        component = component.ReplaceElement
        (
          Create(doc, origin, type, level, host),
          ExcludeUniqueProperties
        );
      }

      component.get_Parameter(ARDB.BuiltInParameter.INSTANCE_OFFSET_POS_PARAM)?.Update(false);

      {
        component.Document.Regenerate();
        //if (host is object)
          component.SetLocation(origin, basisX);
        //else
        //  component.SetLocation(origin, basisX, basisY);
      }

      return component;
    }
  }
}

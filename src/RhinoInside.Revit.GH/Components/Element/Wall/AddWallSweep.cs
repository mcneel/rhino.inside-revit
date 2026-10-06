using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  using External.DB.Extensions;
  using RhinoInside.Revit.GH.Exceptions;

  [ComponentVersion(introduced: "1.37")]
  public class AddWallSweep : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("CD4BECC6-751A-40B3-8115-315F7E2045C4");
    public override GH_Exposure Exposure => SDKCompliancy(GH_Exposure.primary);

    public AddWallSweep() : base
    (
      name: "Add Wall (Sweep)",
      nickname: "S-Wall",
      description: "Given a Wall, it adds a Wall Sweep or Reveal element to the active Revit document",
      category: "Revit",
      subCategory: "Architecture"
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
        new Parameters.Wall
        {
          Name = "Wall",
          NickName = "W",
          Description = "Wall to host the sweep or reveal"
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.WallSweepType>
        {
          Name = "Kind",
          NickName = "K",
          Description = "Whether it is a sweep or a reveal",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Vertical",
          NickName = "V",
          Description = "Whether the sweep runs vertically along the wall",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.ElementType
        {
          Name = "Type",
          NickName = "T",
          Description = "Wall sweep or reveal type",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = "Distance",
          NickName = "D",
          Description = "Distance from the level, or from the wall start when vertical",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = "Offset",
          NickName = "O",
          Description = "Offset from the wall face",
          Optional = true
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.Level
        {
          Name = "Level",
          NickName = "L",
          Description = "Level the distance is measured from. Wall base level is used when empty",
          Optional = true
        }, ParamRelevance.Occasional
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _WallSweep_,
          NickName = "S",
          Description = $"Output {_WallSweep_}",
        }
      )
    };

    const string _WallSweep_ = "Wall Sweep";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.WALL_SWEEP_LEVEL_PARAM,
      ARDB.BuiltInParameter.WALL_SWEEP_OFFSET_PARAM,
      ARDB.BuiltInParameter.WALL_SWEEP_WALL_OFFSET_PARAM
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.WallSweep>
      (
        doc.Value, _WallSweep_, wallSweep =>
        {
          // Input
          if (!Params.GetData(DA, "Wall", out Types.Wall wall, x => x.IsValid)) return null;

          if (!doc.Value.IsEquivalent(wall.Document))
            throw new RuntimeArgumentException(nameof(wall), $"Wall '{wall.Nomen}' belongs to a different document.", wall);

          if (!ARDB.WallSweep.WallAllowsWallSweep(wall.Value))
            throw new RuntimeArgumentException(nameof(wall), $"Wall '{wall.Nomen}' does not allow wall sweeps.", wall);

          if (!Params.TryGetData(DA, "Kind", out Types.WallSweepType kind)) return null;
          var sweepKind = kind?.Value ?? ARDB.WallSweepType.Sweep;

          if (!Params.TryGetData(DA, "Vertical", out bool? vertical)) return null;

          var typeGroup = sweepKind == ARDB.WallSweepType.Reveal ? ARDB.ElementTypeGroup.RevealType : ARDB.ElementTypeGroup.CorniceType;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out Types.ElementType type, doc, typeGroup)) return null;

          var typeCategory = sweepKind == ARDB.WallSweepType.Reveal ? ARDB.BuiltInCategory.OST_Reveals : ARDB.BuiltInCategory.OST_Cornices;
          if (type.Value.Category?.ToBuiltInCategory() != typeCategory)
            throw new RuntimeArgumentException(nameof(type), $"Type '{type.Nomen}' is not a valid wall {(sweepKind == ARDB.WallSweepType.Reveal ? "reveal" : "sweep")} type.", type);

          if (!Params.TryGetData(DA, "Distance", out double? distance)) return null;
          if (!Params.TryGetData(DA, "Offset", out double? offset)) return null;
          if (!Params.TryGetData(DA, "Level", out Types.Level level)) return null;

          if (level is object && !doc.Value.IsEquivalent(level.Document))
            throw new RuntimeArgumentException(nameof(level), $"Level '{level.Nomen}' belongs to a different document.", level);

          // Compute
          wallSweep = Reconstruct
          (
            wallSweep, wall.Value, sweepKind, vertical ?? false, type.Value,
            (distance ?? 0.0) / Revit.ModelUnits,
            (offset ?? 0.0) / Revit.ModelUnits,
            level?.Value
          );

          DA.SetData(_WallSweep_, wallSweep);
          return wallSweep;
        }
      );
    }

    bool Reuse
    (
      ref ARDB.WallSweep wallSweep, ARDB.Wall wall,
      ARDB.WallSweepType kind, bool vertical, ARDB.ElementType type,
      double distance, double offset, ARDB.Level level
    )
    {
      if (wallSweep is null) return false;

      // Kind and orientation are fixed on creation.
      using (var info = wallSweep.GetWallSweepInfo())
      {
        if (info.WallSweepType != kind) return false;
        if (info.IsVertical != vertical) return false;
      }

      if (!wallSweep.GetHostIds().Contains(wall.Id)) return false;

      if (wallSweep.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(wallSweep.Document, new ARDB.ElementId[] { wallSweep.Id }, type.Id))
        {
          if (wallSweep.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            wallSweep = wallSweep.Document.GetElement(id) as ARDB.WallSweep;
        }
        else return false;
      }

      bool succeed = true;
      if (level is object) succeed &= wallSweep.get_Parameter(ARDB.BuiltInParameter.WALL_SWEEP_LEVEL_PARAM)?.Update(level.Id) != false;
      succeed &= wallSweep.get_Parameter(ARDB.BuiltInParameter.WALL_SWEEP_OFFSET_PARAM)?.Update(distance) != false;
      succeed &= wallSweep.get_Parameter(ARDB.BuiltInParameter.WALL_SWEEP_WALL_OFFSET_PARAM)?.Update(offset) != false;

      return succeed;
    }

    ARDB.WallSweep Create
    (
      ARDB.Wall wall,
      ARDB.WallSweepType kind, bool vertical, ARDB.ElementType type,
      double distance, double offset, ARDB.Level level
    )
    {
      var wallSweep = default(ARDB.WallSweep);
      using
      (
        var info = new ARDB.WallSweepInfo(kind, vertical)
        {
          Distance = distance,
          WallOffset = offset,
          DistanceMeasuredFrom = ARDB.DistanceMeasuredFrom.Base,
          WallSide = ARDB.WallSide.Exterior
        }
      )
      {
        wallSweep = ARDB.WallSweep.Create(wall, type.Id, info);
      }

      // The level is only available after Revit regenerates the new sweep.
      if (wallSweep is object && level is object)
      {
        wallSweep.Document.Regenerate();
        wallSweep.get_Parameter(ARDB.BuiltInParameter.WALL_SWEEP_LEVEL_PARAM)?.Update(level.Id);
        wallSweep.get_Parameter(ARDB.BuiltInParameter.WALL_SWEEP_OFFSET_PARAM)?.Update(distance);
      }

      return wallSweep;
    }

    ARDB.WallSweep Reconstruct
    (
      ARDB.WallSweep wallSweep, ARDB.Wall wall,
      ARDB.WallSweepType kind, bool vertical, ARDB.ElementType type,
      double distance, double offset, ARDB.Level level
    )
    {
      if (!Reuse(ref wallSweep, wall, kind, vertical, type, distance, offset, level))
      {
        wallSweep = wallSweep.ReplaceElement
        (
          Create(wall, kind, vertical, type, distance, offset, level),
          ExcludeUniqueProperties
        );
      }

      return wallSweep;
    }
  }
}

using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.SpatialElements
{
  [ComponentVersion(introduced: "1.7")]
  public class AddSpace : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("07711559-9681-4035-8D20-F00E4435D412");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    public AddSpace() : base
    (
      name: "Add Space",
      nickname: "Space",
      description: "Given a point, it adds a Space to the given Revit view",
      category: "Revit",
      subCategory: "Topology"
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
        new Param_Point
        {
          Name = "Location",
          NickName = "L",
          Description = $"{_Space_} location point",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Number",
          NickName = "NUM",
          Description = $"{_Space_} number",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Name",
          NickName = "N",
          Description = $"{_Space_} name",
          Optional = true
        }, ParamRelevance.Tertiary
      ),
      new ParamDefinition
      (
        new Parameters.Phase()
        {
          Name = "Phase",
          NickName = "P",
          Description = $"Project phase to which the space belongs.{Environment.NewLine}Space will be placed in the last project phase if this parameter is not set.",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Level",
          NickName = "L",
          Description = $"{_Space_} base level.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Base Offset",
          NickName = "LO",
          Description = "Specifies the distance at which the room occurs.",
          Optional = true
        }, ParamRelevance.Tertiary
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Upper Limit",
          NickName = "UL",
          Description = $"{_Space_} upper level.",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Limit Offset",
          NickName = "LO",
          Description = "Specifies the distance at which the room occurs.",
          Optional = true
        }, ParamRelevance.Tertiary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.SpaceElement()
        {
          Name = _Space_,
          NickName = _Space_.Substring(0, 1),
          Description = $"Output {_Space_}",
        }
      )
    };

    const string _Space_ = "Space";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ROOM_NUMBER,
      ARDB.BuiltInParameter.ROOM_LOWER_OFFSET,
      ARDB.BuiltInParameter.ROOM_UPPER_LEVEL,
      ARDB.BuiltInParameter.ROOM_UPPER_OFFSET,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Mechanical.Space>
      (
        doc.Value, _Space_, (space) =>
        {
          // Input
          if (!Params.TryGetData(DA, "Location", out Point3d? location)) return null;
          if (!Params.TryGetData(DA, "Number", out string number)) return null;
          if (location is null && number is null)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A Location or a Room Number is necessary.");
            return null;
          }

          if (!Params.TryGetData(DA, "Name", out string name)) return null;
          if (!Params.TryGetData(DA, "Level", out ARDB.Level level)) return null;
          if (!Params.TryGetData(DA, "Base Offset", out double? baseOffset)) return null;
          if (!Params.TryGetData(DA, "Upper Limit", out ARDB.Level upperLimit)) return null;
          if (!Params.TryGetData(DA, "Limit Offset", out double? limitOffset)) return null;
          if (!Params.TryGetData(DA, "Phase", out ARDB.Phase phase)) return null;

          // Compute
          if (CanReconstruct(_Space_, out var untracked, ref space, doc.Value, number, categoryId: ARDB.BuiltInCategory.OST_MEPSpaces))
          {
            if (untracked)
            {
              // To avoid `Reconstruct` recereates the element in an other phase unless is necessary…
              phase = phase ?? space?.Document.GetElement(space?.get_Parameter(ARDB.BuiltInParameter.ROOM_PHASE).AsElementId()) as ARDB.Phase;
            }

            // Solve Level from Location
            {
              if (level is null && upperLimit is null && location.HasValue)
              {
                using (var provider = new ARDB.ParameterValueProvider(new ARDB.ElementId(ARDB.BuiltInParameter.LEVEL_IS_BUILDING_STORY)))
                using (var evaluator = new ARDB.FilterNumericEquals())
                using (var rule = new ARDB.FilterIntegerRule(provider, evaluator, 1))
                using (var filter = new ARDB.ElementParameterFilter(rule))
                  level = doc.Value.GetNearestBaseLevel(location.Value.Z / Revit.ModelUnits, out upperLimit, filter);
              }
              if (upperLimit is null) upperLimit = level;
              if (level is null) level = upperLimit;

              phase = phase ?? doc.Value.Phases.Cast<ARDB.Phase>().LastOrDefault();
            }

            // Snap Location to the 'Level' 'Computation Height'
            if (location.HasValue && level is object)
            {
              location = new Point3d
              (
                location.Value.X,
                location.Value.Y,
                level.ProjectElevation * Revit.ModelUnits +
                level.get_Parameter(ARDB.BuiltInParameter.LEVEL_ROOM_COMPUTATION_HEIGHT).AsDouble() * Revit.ModelUnits
              );
            }

            space = Reconstruct
            (
              doc.Value,
              space,
              location?.ToXYZ(),
              number, name,
              level, baseOffset,
              upperLimit, limitOffset,
              phase
            );
          }

          DA.SetData(_Space_, space);
          return untracked ? null : space;
        }
      );
    }

    bool Reuse(ARDB.Mechanical.Space space, ARDB.Level level, ARDB.Phase phase, ARDB.XYZ location)
    {
      if (space is null) return false;
      if (!space.Level.IsEquivalent(level)) return false;
      if (space.get_Parameter(ARDB.BuiltInParameter.ROOM_PHASE)?.AsElementId() != phase.Id) return false;
      if (location is null != space.Location is null) return false;

      return true;
    }

    ARDB.Mechanical.Space Create(ARDB.Document document, ARDB.Level level, ARDB.Phase phase, ARDB.XYZ location)
    {
      if (level is object && location is object)
        return document.Create.NewSpace(level, phase, new ARDB.UV(location.X, location.Y));

      return document.Create.NewSpace(phase);
    }

    ARDB.Mechanical.Space Reconstruct
    (
      ARDB.Document document,
      ARDB.Mechanical.Space space, ARDB.XYZ location,
      string number, string name,
      ARDB.Level level, double? baseOffset,
      ARDB.Level upperLimit, double? limitOffset,
      ARDB.Phase phase
    )
    {
      var isNew = false;
      if (!Reuse(space, level, phase, location))
      {
        isNew = true;
        space = space.ReplaceElement
        (
          Create(document, level, phase, location),
          ExcludeUniqueProperties
        );
      }

      // Move Space to 'Location'
      if (space.Location is ARDB.LocationPoint roomLocation && location is object)
      {
        var target = new ARDB.XYZ(location.X, location.Y, roomLocation.Point.Z);
        var position = roomLocation.Point;
        if (!target.IsAlmostEqualTo(position))
        {
          var pinned = space.Pinned;
          space.Pinned = false;
          roomLocation.Move(target - position);
          space.Pinned = pinned;
        }
      }

      // We use ROOM_NAME here because Room.Name returns us a werid combination of "{Name} {Number}".
      if (number is object) space.get_Parameter(ARDB.BuiltInParameter.ROOM_NUMBER).Update(number);
      if (name is object) space.get_Parameter(ARDB.BuiltInParameter.ROOM_NAME).Update(name);

      var newBaseOffset = baseOffset.HasValue ? baseOffset.Value / Revit.ModelUnits : 0.0;
      if (space.BaseOffset != newBaseOffset)
        space.BaseOffset = newBaseOffset;

      if (space.Location is object && space.Level.IsEquivalent(level) && upperLimit is object)
        space.UpperLimit = upperLimit;

      var newLimitOffset = limitOffset.HasValue ? limitOffset.Value / Revit.ModelUnits : (level.IsEquivalent(upperLimit) ? 8.0 : 0.0);
      if (space.LimitOffset != newLimitOffset)
        space.LimitOffset = newLimitOffset;

      if (!isNew && space.Location is object && space.Area == 0.0)
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"'{space.Name}' is not in a properly enclosed region. {{{space.Id}}}");

      return space;
    }
  }
}

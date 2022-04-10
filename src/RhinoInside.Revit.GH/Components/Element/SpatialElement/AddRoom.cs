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
  public class AddRoom : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("DE5E832B-9671-4AD7-9A8B-86EEECF58FA4");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddRoom () : base
    (
      name: "Add Room",
      nickname: "Room",
      description: "Given a point, it adds a Room to the given Revit view",
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
          Description = $"{_Room_} location point",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Number",
          NickName = "NUM",
          Description = $"{_Room_} number",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Name",
          NickName = "N",
          Description = $"{_Room_} name",
          Optional = true
        }, ParamRelevance.Tertiary
      ),
      new ParamDefinition
      (
        new Parameters.Phase()
        {
          Name = "Phase",
          NickName = "P",
          Description = $"Project phase to which the room belongs.{Environment.NewLine}Room will be placed in the last project phase if this parameter is not set.",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Level",
          NickName = "L",
          Description = $"{_Room_} base level.",
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
          Description = $"{_Room_} upper level.",
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
        new Parameters.RoomElement()
        {
          Name = _Room_,
          NickName = _Room_.Substring(0, 1),
          Description = $"Output {_Room_}",
        }
      )
    };

    const string _Room_ = "Room";
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

      ReconstructElement<ARDB.Architecture.Room>
      (
        doc.Value, _Room_, (room) =>
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
          if (CanReconstruct(_Room_, out var untracked, ref room, doc.Value, number, categoryId: ARDB.BuiltInCategory.OST_Rooms))
          {
            if (untracked)
            {
              // To avoid `Reconstruct` recereates the element in an other phase unless is necessary…
              phase = phase ?? room?.Document.GetElement(room?.get_Parameter(ARDB.BuiltInParameter.ROOM_PHASE).AsElementId()) as ARDB.Phase;
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

            room = Reconstruct
            (
              doc.Value,
              room,
              location?.ToXYZ(),
              number, name,
              level, baseOffset,
              upperLimit, limitOffset,
              phase
            );
          }

          DA.SetData(_Room_, room);
          return untracked ? null : room;
        }
      );
    }

    bool Reuse(ARDB.Architecture.Room room, ARDB.Phase phase)
    {
      return room?.get_Parameter(ARDB.BuiltInParameter.ROOM_PHASE)?.AsElementId() == phase.Id;
    }

    ARDB.Architecture.Room Reconstruct
    (
      ARDB.Document document,
      ARDB.Architecture.Room room, ARDB.XYZ location,
      string number, string name,
      ARDB.Level level, double? baseOffset,
      ARDB.Level upperLimit, double? limitOffset,
      ARDB.Phase phase
    )
    {
      var isNew = false;
      if (!Reuse(room, phase))
      {
        isNew = true;
        room = room.ReplaceElement
        (
          document.Create.NewRoom(phase),
          ExcludeUniqueProperties
        );
      }

      // If we should place-unplace or change the 'Level'
      if (location is null != room.Location is null || !room.Level.IsEquivalent(level))
      {
        // Look for a plan-circuit at 'Level' on 'Phase':
        //
        // 1. One that contains 'Location'
        // 2. An arbirtary plan-circuit (the "simplest" one on 'Level')
        // 3. No plan-circuit, in case 'Level' has no one.

        var newCircuit = default(ARDB.PlanCircuit);
        var minSides = int.MaxValue;
        if (location is object && level is object)
        {
          using (var topology = room.Document.get_PlanTopology(level, phase))
          {
            if (!topology.Circuits.IsEmpty)
            {
              using (room.Document.RollBackScope())
              {
                using (var testRoom = room.Document.Create.NewRoom(phase))
                {
                  var uv = new ARDB.UV(location?.X ?? 0.0, location?.Y ?? 0.0);
                  foreach (var circuit in topology.Circuits.Cast<ARDB.PlanCircuit>().OrderBy(x => x.GetPointInside().DistanceTo(uv)))
                  {
                    // Place testRoom at circuit and test if contains 'Location'.
                    var inside = room.Document.Create.NewRoom(testRoom, circuit)?.IsPointInRoom(location);
                    if (inside == true)
                    {
                      newCircuit = circuit;
                      break;
                    }
                    else if (inside == false)
                    {
                      // Save "simplest" plan-circuit.
                      var sideNum = circuit.SideNum;
                      if (sideNum < minSides)
                      {
                        minSides = sideNum;
                        newCircuit = circuit;
                      }
                    }
                  }
                }
              }
            }
          }
        }

        // Place Room on `newCircuit`
        if (location is object && newCircuit is object)
        {
          using (var tagsFilter = CompoundElementFilter.ElementClassFilter(typeof(ARDB.Architecture.RoomTag)))
          {
            var beforeTagIds = room.GetDependentElements(tagsFilter);
            if (room.Location is object) room.Unplace();
            room = room.Document.Create.NewRoom(room, newCircuit);
            var afterTagIds = room.GetDependentElements(tagsFilter);

            // NewRoom from PlanCircuit has the undesired side effect of
            // adding a RoomTag in the first ViewPlan that has View.GenLevel equals to Room.Level.
            if (afterTagIds.LastOrDefault() is ARDB.ElementId newTagId && beforeTagIds.LastOrDefault() != newTagId)
              room.Document.Delete(newTagId);
          }
        }
        else if (room.Location is object)
        {
          room.Unplace();
        }
      }

      // Move Room to 'Location'
      if (room.Location is ARDB.LocationPoint roomLocation && location is object)
      {
        var target = new ARDB.XYZ(location.X, location.Y, roomLocation.Point.Z);
        var position = roomLocation.Point;
        if (!target.IsAlmostEqualTo(position))
        {
          var pinned = room.Pinned;
          room.Pinned = false;
          roomLocation.Move(target - position);
          room.Pinned = pinned;
        }
      }

      // We use ROOM_NAME here because Room.Name returns us a werid combination of "{Name} {Number}".
      if (number is object) room.get_Parameter(ARDB.BuiltInParameter.ROOM_NUMBER).Update(number);
      if (name is object) room.get_Parameter(ARDB.BuiltInParameter.ROOM_NAME).Update(name);

      var newBaseOffset = baseOffset.HasValue ? baseOffset.Value / Revit.ModelUnits : 0.0;
      if (room.BaseOffset != newBaseOffset)
        room.BaseOffset = newBaseOffset;

      if (room.Location is object && room.Level.IsEquivalent(level) && upperLimit is object)
        room.UpperLimit = upperLimit;

      var newLimitOffset = limitOffset.HasValue ? limitOffset.Value / Revit.ModelUnits : (level.IsEquivalent(upperLimit) ? 8.0 : 0.0);
      if (room.LimitOffset != newLimitOffset)
        room.LimitOffset = newLimitOffset;

      if (!isNew && room.Location is object && room.Area == 0.0)
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"'{room.Name}' is not in a properly enclosed region. {{{room.Id}}}");

      return room;
    }
  }
}

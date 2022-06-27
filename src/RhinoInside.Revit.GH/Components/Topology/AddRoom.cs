using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.Topology
{
  [ComponentVersion(introduced: "1.7")]
  public class AddRoom : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("DE5E832B-9671-4AD7-9A8B-86EEECF58FA4");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

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
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to add a specific room",
        }
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
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.LevelConstraint
        {
          Name = "Base",
          NickName = "BA",
          Description = $"Base of the {_Room_.ToLowerInvariant()}.{Environment.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Location'.",
          Optional = true,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.LevelConstraint
        {
          Name = "Top",
          NickName = "TO",
          Description = $"Top of the {_Room_.ToLowerInvariant()}.{Environment.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Location'.",
          Optional = true,
        }, ParamRelevance.Primary
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
      )
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
      ARDB.BuiltInParameter.ROOM_LEVEL_ID,
      ARDB.BuiltInParameter.ROOM_LOWER_OFFSET,
      ARDB.BuiltInParameter.ROOM_UPPER_LEVEL,
      ARDB.BuiltInParameter.ROOM_UPPER_OFFSET,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;

      ReconstructElement<ARDB.Architecture.Room>
      (
        view.Document, _Room_, room =>
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
          if (!Params.TryGetData(DA, "Base", out ERDB.ElevationElementReference? baseElevation)) return null;
          if (!Params.TryGetData(DA, "Top", out ERDB.ElevationElementReference? topElevation)) return null;
          if (!Params.TryGetData(DA, "Phase", out ARDB.Phase phase)) return null;

          // Compute
          if (CanReconstruct(_Room_, out var untracked, ref room, view.Document, number, categoryId: ARDB.BuiltInCategory.OST_Rooms))
          {
            if (phase is null)
            {
              // We avoid `Reconstruct` recreates the element in an other phase unless is necessary…
              phase = untracked ?
                room?.Document.GetElement(room.get_Parameter(ARDB.BuiltInParameter.ROOM_PHASE).AsElementId()) as ARDB.Phase:
                view.Document.Phases.Cast<ARDB.Phase>().LastOrDefault();
            }

            if (location.HasValue)
            {
              // Solve missing Base & Top
              ERDB.ElevationElementReference.SolveBaseAndTop
              (
                view.Document, view.Value.GenLevel.ProjectElevation,
                0.0, 10.0,
                ref baseElevation, ref topElevation
              );

              // Snap Location to the 'Level' 'Computation Height'
              if (baseElevation.Value.IsLevelConstraint(out var level, out var _))
              {
                location = new Point3d
                (
                  location.Value.X,
                  location.Value.Y,
                  level.ProjectElevation * Revit.ModelUnits +
                  level.get_Parameter(ARDB.BuiltInParameter.LEVEL_ROOM_COMPUTATION_HEIGHT).AsDouble() * Revit.ModelUnits
                );
              }
            }

            room = Reconstruct
            (
              view.Document,
              room,
              location?.ToXYZ(),
              number, name,
              baseElevation ?? default,
              topElevation ?? default,
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
      ERDB.ElevationElementReference baseElevation,
      ERDB.ElevationElementReference topElevation,
      ARDB.Phase phase
    )
    {
      // If there are no Levels!!
      if (!baseElevation.IsLevelConstraint(out var baseLevel, out var baseOffset) && location is object)
        return default;

      if (!Reuse(room, phase))
      {
        room = room.ReplaceElement
        (
          document.Create.NewRoom(phase),
          ExcludeUniqueProperties
        );
      }

      // We use ROOM_NAME here because `SpatialElment.Name` returns us a werid combination of "{Name} {Number}".
      if (number is object) room.get_Parameter(ARDB.BuiltInParameter.ROOM_NUMBER).Update(number);
      if (name is object)   room.get_Parameter(ARDB.BuiltInParameter.ROOM_NAME).Update(name);

      // If we should place-unplace or change the 'Level'
      if (location is null != room.Location is null || !baseLevel.IsEquivalent(room.Level))
      {
        // Look for a plan-circuit at 'Level' on 'Phase':
        //
        // 1. One that contains 'Location'
        // 2. An arbirtary plan-circuit (the "simplest" one on 'Level')
        // 3. No plan-circuit, in case 'Level' has no one.

        var newCircuit = default(ARDB.PlanCircuit);
        var minSides = int.MaxValue;
        if (location is object)
        {
          using (var topology = room.Document.get_PlanTopology(baseLevel, phase))
          {
            if (!topology.Circuits.IsEmpty)
            {
              using (ERDB.DisposableScope.RollBackScope(room.Document))
              {
                using (var testRoom = room.Document.Create.NewRoom(phase))
                {
                  var uv = new ARDB.UV(location?.X ?? 0.0, location?.Y ?? 0.0);
                  foreach (var circuit in topology.Circuits.Cast<ARDB.PlanCircuit>().OrderBy(x => x.GetPointInside().DistanceTo(uv)))
                  {
                    // Place testRoom at circuit and test if contains 'Location'.
                    var inside = room.Document.Create.NewRoom(testRoom, circuit)?.IsPointInRoom(location);
                    testRoom.Unplace();

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
          using (var tagsFilter = ERDB.CompoundElementFilter.ElementClassFilter(typeof(ARDB.Architecture.RoomTag)))
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
      if (location is object)
      {
        if (room.Location is ARDB.LocationPoint roomLocation)
        {
          var position = roomLocation.Point;
          var target = new ARDB.XYZ(location.X, location.Y, position.Z);
          if (!target.IsAlmostEqualTo(position))
          {
            var pinned = room.Pinned;
            room.Pinned = false;
            roomLocation.Move(target - position);
            room.Pinned = pinned;
          }

          baseOffset = Math.Min
          (
            baseOffset.Value,
            room.Level.get_Parameter(ARDB.BuiltInParameter.LEVEL_ROOM_COMPUTATION_HEIGHT).AsDouble()
          );
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"'{room.Name}' is not in a properly enclosed region on Phase '{phase.Name}'. {{{room.Id}}}");

        if (room.BaseOffset != baseOffset.Value)
          room.BaseOffset = baseOffset.Value;

        if (topElevation.IsLevelConstraint(out var topLevel, out var topOffset))
        {
          if (room.Location is object && baseLevel.IsEquivalent(room.Level) && topLevel is object)
            room.UpperLimit = topLevel;

          if (room.LimitOffset != topOffset.Value)
            room.LimitOffset = topOffset.Value;
        }
        else
        {
          if (room.Location is object && baseLevel.IsEquivalent(room.Level) && room.UpperLimit is object)
            room.UpperLimit = baseLevel;

          if (room.LimitOffset != topElevation.Offset)
            room.LimitOffset = topElevation.Offset;
        }
      }
      else AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"'{room.Name}' is unplaced. {{{room.Id}}}");

      return room;
    }
  }
}

using System;
using System.Linq;
using System.Collections.Generic;
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
  public class AddSpace : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("07711559-9681-4035-8D20-F00E4435D412");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

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
        new Parameters.LevelConstraint
        {
          Name = "Base",
          NickName = "BA",
          Description = $"Base of the {_Space_.ToLowerInvariant()}.{Environment.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Location'.",
          Optional = true,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.LevelConstraint
        {
          Name = "Top",
          NickName = "TO",
          Description = $"Top of the {_Space_.ToLowerInvariant()}.{Environment.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Location'.",
          Optional = true,
        }, ParamRelevance.Primary
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
      )
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
      ARDB.BuiltInParameter.ROOM_LEVEL_ID,
      ARDB.BuiltInParameter.ROOM_LOWER_OFFSET,
      ARDB.BuiltInParameter.ROOM_UPPER_LEVEL,
      ARDB.BuiltInParameter.ROOM_UPPER_OFFSET,
    };

    internal override TransactionExtent TransactionExtent => TransactionExtent.Scope;

    ARDB.View ActiveView = default;
    readonly List<ARDB.View> ViewsToClose = new List<ARDB.View>();
    readonly Dictionary<(Types.View View, Types.Phase Phase), Types.View> TemporaryViews = new Dictionary<(Types.View View, Types.Phase Phase), Types.View>();

    protected override void BeforeSolveInstance()
    {
      ActiveView = Revit.ActiveDBDocument?.GetActiveGraphicalView();

      base.BeforeSolveInstance();
    }

    protected override void AfterSolveInstance()
    {
      base.AfterSolveInstance();

      ActiveView?.Document.SetActiveGraphicalView(ActiveView);
      ActiveView = default;

      foreach (var view in (ViewsToClose as IEnumerable<ARDB.View>).Reverse())
        view.Close();
      ViewsToClose.Clear();

      if (TemporaryViews.Count > 0)
      {
        foreach (var views in TemporaryViews.Values.GroupBy(x => x.Document).Reverse())
        {
          using (var scope = External.DB.DisposableScope.CommitScope(views.Key))
          {
            views.Key.Delete(views.Select(x => x.Id).ToArray());
            scope.Commit();
          }
        }

        TemporaryViews.Clear();
      }

      var spaces = Params.Output<IGH_Param>(_Space_).
        VolatileData.AllData(skipNulls: true).Cast<Types.SpatialElement>();

      foreach (var space in spaces)
      {
        if (space.Value.Location is object && space.Value.Area == 0.0)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"'{space.DisplayName}' is not in a properly enclosed region on Phase '{space.Phase.DisplayName}'. {{{space.Id}}}");
      }
    }

    bool SetActiveViewInPhase(ref Types.View view, ref Types.Phase phase)
    {
      if (!view.Phase.Equals(phase))
      {
        if (!TemporaryViews.TryGetValue((view, phase), out var temporaryView))
        {
          StartTransaction(view.Document);
          try
          {
            temporaryView = Types.View.FromElementId(view.Document, view.Value.Duplicate(ARDB.ViewDuplicateOption.Duplicate)) as Types.View;
            temporaryView.Phase = phase;
            CommitTransaction();

            TemporaryViews.Add((view, phase), temporaryView);
          }
          catch
          {
            RollBackTransaction();
          }
        }

        view = temporaryView;
      }

      if (!view.Document.ActiveView.IsEquivalent(view.Value))
      {
        if (!view.Document.SetActiveGraphicalView(view.Value, out var viewWasOpen))
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to activate view '{view.DisplayName}'");
          return false;
        }
        else if (!viewWasOpen)
        {
          ViewsToClose.Add(view.Value);
        }
      }

      return true;
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;

      bool invalidInput = false;
      invalidInput |= !Params.TryGetData(DA, "Location", out Point3d? location);
      invalidInput |= !Params.TryGetData(DA, "Number", out string number);
      if (location is null && number is null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A Location or a Room Number is necessary.");
        invalidInput |= true;
      }

      invalidInput |= !Params.TryGetData(DA, "Name", out string name);
      invalidInput |= !Params.TryGetData(DA, "Base", out ERDB.ElevationElementReference? baseElevation);
      invalidInput |= !Params.TryGetData(DA, "Top", out ERDB.ElevationElementReference? topElevation);
      invalidInput |= !Params.TryGetData(DA, "Phase", out Types.Phase phase);
      if (phase is null) phase = view.Phase;

      if (!invalidInput)
      {
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
      }

      ReconstructElement<ARDB.Mechanical.Space>
      (
        view.Document, _Space_,
        space => // Validate Input
        {
          if (invalidInput)
            return false;

          // If there are no Levels!!
          var baseLevel = default(ARDB.Level);
          if (location is object && !baseElevation.Value.IsLevelConstraint(out baseLevel, out var baseOffset))
            return false;

          if (Reuse(space, baseLevel, phase.Value, location?.ToXYZ()))
            return true;

          // Ensure we have an acceptable View on the requested Phase active on the UI.
          // Else NewSpace does not take the correct Phase.
          return SetActiveViewInPhase(ref view, ref phase);
        },
        space => // Compute
        {
          if (CanReconstruct(_Space_, out var untracked, ref space, view.Document, number, categoryId: ARDB.BuiltInCategory.OST_MEPSpaces))
          {
            space = Reconstruct
            (
              view.Document,
              space,
              location?.ToXYZ(),
              number, name,
              baseElevation ?? default,
              topElevation ?? default,
              phase.Value
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
      ERDB.ElevationElementReference baseElevation,
      ERDB.ElevationElementReference topElevation,
      ARDB.Phase phase
    )
    {
      // If there are no Levels!!
      if (!baseElevation.IsLevelConstraint(out var baseLevel, out var baseOffset) && location is object)
        return default;

      if (!Reuse(space, baseLevel, phase, location))
      {
        space = space.ReplaceElement
        (
          Create(document, baseLevel, phase, location),
          ExcludeUniqueProperties
        );
      }

      // We use ROOM_NAME here because `SpatialElment.Name` returns us a werid combination of "{Name} {Number}".
      if (number is object) space.get_Parameter(ARDB.BuiltInParameter.ROOM_NUMBER).Update(number);
      if (name is object) space.get_Parameter(ARDB.BuiltInParameter.ROOM_NAME).Update(name);

      // Move Space to 'Location'
      if (location is object)
      {
        if (space.Location is ARDB.LocationPoint spaceLocation)
        {
          var position = spaceLocation.Point;
          var target = new ARDB.XYZ(location.X, location.Y, position.Z);
          if (!target.IsAlmostEqualTo(position))
          {
            var pinned = space.Pinned;
            space.Pinned = false;
            spaceLocation.Move(target - position);
            space.Pinned = pinned;
          }

          baseOffset = Math.Min
          (
            baseOffset.Value,
            space.Level.get_Parameter(ARDB.BuiltInParameter.LEVEL_ROOM_COMPUTATION_HEIGHT).AsDouble()
          );
        }

        if (space.BaseOffset != baseOffset.Value)
          space.BaseOffset = baseOffset.Value;

        if (topElevation.IsLevelConstraint(out var topLevel, out var topOffset))
        {
          if (space.Location is object && baseLevel.IsEquivalent(space.Level) && topLevel is object)
            space.UpperLimit = topLevel;

          if (space.LimitOffset != topOffset.Value)
            space.LimitOffset = topOffset.Value;
        }
        else
        {
          if (space.Location is object && baseLevel.IsEquivalent(space.Level) && space.UpperLimit is object)
            space.UpperLimit = baseLevel;

          if (space.LimitOffset != topElevation.Offset)
            space.LimitOffset = topElevation.Offset;
        }
      }
      else AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"'{space.Name}' is unplaced. {{{space.Id}}}");

      return space;
    }
  }
}

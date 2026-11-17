using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  using Convert.Geometry;
  using ElementTracking;
  using External.DB;
  using External.DB.Extensions;
  using GH.Exceptions;

  [ComponentVersion(introduced: "1.0", updated: "1.37")]
  public class WallByProfile : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("78b02ae8-2b78-45a7-962e-92e7d9097598");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public WallByProfile() : base
    (
      name: "Add Wall (Profile)",
      nickname: "P-Wall",
      description: "Given a profile, it adds a Wall element to the active Revit document",
      category: "Revit",
      subCategory: "Architecture"
    )
    { }

    protected override void OnStarted(ARDB.Document document)
    {
      base.OnStarted(document);

      // Disable all previous walls joins
      var walls = Params.TrackedElements<ARDB.Wall>("Wall", document);
      var pinnedWalls = walls.Where(x => x.Pinned);

      foreach (var wall in pinnedWalls)
      {
        if (ARDB.WallUtils.IsWallJoinAllowedAtEnd(wall, 0))
          ARDB.WallUtils.DisallowWallJoinAtEnd(wall, 0);

        if (ARDB.WallUtils.IsWallJoinAllowedAtEnd(wall, 1))
          ARDB.WallUtils.DisallowWallJoinAtEnd(wall, 1);
      }
    }

    List<ARDB.Wall> joinedWalls = new List<ARDB.Wall>();
    protected override void OnPrepare(IReadOnlyCollection<ARDB.Document> documents)
    {
      base.OnPrepare(documents);

      if (joinedWalls.Count > 0)
      {
        // Wall joins need regenerated geometry to work properly.
        foreach (var doc in documents)
          doc.Regenerate();

        foreach (var wallToJoin in joinedWalls)
        {
          ARDB.WallUtils.AllowWallJoinAtEnd(wallToJoin, 0);
          ARDB.WallUtils.AllowWallJoinAtEnd(wallToJoin, 1);
        }

        joinedWalls = new List<ARDB.Wall>();
      }
    }

    static readonly ARDB.FailureDefinitionId[] failureDefinitionIdsToFix = new ARDB.FailureDefinitionId[]
    {
      ARDB.BuiltInFailures.DimensionFailures.RegenConstraintUnsatisfied,
      ARDB.BuiltInFailures.CreationFailures.CannotDrawWallsError,
      ARDB.BuiltInFailures.JoinElementsFailures.CannotJoinElementsError,
      ARDB.BuiltInFailures.WallFailures.ParameterChangeNotAffectingGeometry,
    };
    protected override IEnumerable<ARDB.FailureDefinitionId> FailureDefinitionIdsToFix => failureDefinitionIdsToFix;

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        Parameters.Document.AddTarget(), ParamRelevance.Occasional
      ),
      new ParamDefinition
       (
        new Param_Surface
        {
          Name = "Profile",
          NickName = "P",
          Description = "Wall profile",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
       (
        new Parameters.HostObjectType
        {
          Name = "Type",
          NickName = "T",
          Description = "Wall type",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_Walls
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Level
        {
          Name = "Level",
          NickName = "L",
          Optional = true,
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.WallLocationLine>
        {
          Name = "Location Line",
          NickName = "LL",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Flipped",
          NickName = "F",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Allow Joins",
          NickName = "J",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.StructuralWallUsage>
        {
          Name = "Structural Usage",
          NickName = "Su",
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Wall()
        {
          Name = _Wall_,
          NickName = _Wall_.Substring(0, 1),
          Description = $"Output {_Wall_}",
        }
      )
    };

    const string _Wall_ = "Wall";

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Input<IGH_Param>("LocationLine") is IGH_Param locationLine) locationLine.Name = "Location Line";
      if (Params.Input<IGH_Param>("AllowJoins") is IGH_Param allowJoins) allowJoins.Name = "Allow Joins";
      if (Params.Input<IGH_Param>("StructuralUsage") is IGH_Param structuralUsage) structuralUsage.Name = "Structural Usage";

      base.AddedToDocument(document);
    }

    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.WALL_KEY_REF_PARAM,
      ARDB.BuiltInParameter.WALL_HEIGHT_TYPE,
      ARDB.BuiltInParameter.WALL_USER_HEIGHT_PARAM,
      ARDB.BuiltInParameter.WALL_BASE_CONSTRAINT,
      ARDB.BuiltInParameter.WALL_BASE_OFFSET,
      ARDB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT,
      ARDB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM,
#if REVIT_2021
      ARDB.BuiltInParameter.WALL_CROSS_SECTION,
      ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL,
#endif
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      var tol = GeometryTolerance.Model;
      ReconstructElement<ARDB.Wall>
      (
        doc.Value, _Wall_, wall =>
        {
          // Input
          if (!Params.GetDataList(DA, "Profile", out IList<Brep> profile)) return null;

          var loops = profile.OfType<Brep>().SelectMany(x => x.Loops).Select(x => x.To3dCurve()).ToArray();

          if (loops.Length < 1) return null;

          var tol = GeometryTolerance.Model;
          var boundaryPlane = default(Plane);
          var maxArea = 0.0;
          for (int index = 0; index < loops.Length; ++index)
          {
            var loop = loops[index];
            var plane = default(Plane);
            if
            (
               loop is null ||
               loop.IsShort(tol.ShortCurveTolerance) ||
              !loop.IsClosed ||
              !loop.TryGetPlane(out plane, tol.VertexTolerance)
            )
              throw new RuntimeArgumentException(nameof(profile), "Profile should be a list of planar surfaces.", profile);

#if !REVIT_2021
            if (!plane.ZAxis.IsPerpendicularTo(Vector3d.ZAxis, tol.AngleTolerance))
              throw new RuntimeArgumentException(nameof(profile), "Profile should be a list of planar vertical surfaces.", profile);
#endif

            loops[index] = loop.Simplify(CurveSimplifyOptions.All & ~CurveSimplifyOptions.Merge, tol.VertexTolerance, tol.AngleTolerance) ?? loop;

            using (var properties = AreaMassProperties.Compute(loop, tol.VertexTolerance))
            {
              if (properties is null)
                throw new RuntimeArgumentException(nameof(profile), "Failed to compute Profile Area.", loop);

              if (properties.Area > maxArea)
              {
                maxArea = properties.Area;
                boundaryPlane = plane;
              }
              else if (plane.Normal.IsParallelTo(boundaryPlane.Normal) == 0 || Math.Abs(plane.DistanceTo(boundaryPlane.Origin)) > GeometryTolerance.Internal.DefaultTolerance)
              {
                throw new RuntimeArgumentException(nameof(profile), "Profile should be a list of coplanar surfaces.", loops);
              }
            }
          }

          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out Types.HostObjectType type, doc, ARDB.ElementTypeGroup.WallType)) return null;
          if (!(type.Value is ARDB.WallType wallType))
            throw new RuntimeArgumentException(nameof(type), $"Type '{type.Nomen}' is not a valid wall type.");

          var bbox = BoundingBox.Empty;
          foreach (var geometry in profile)
            bbox.Union(geometry.GetBoundingBox(true));

          if (!Parameters.Level.GetDataOrDefault(this, DA, "Level", out Types.Level level, doc, bbox.IsValid ? bbox.Min.Z : double.NaN)) return null;

          if (!Params.TryGetData(DA, "Location Line", out ARDB.WallLocationLine? locationLine)) return null;
          if (!Params.TryGetData(DA, "Flipped", out bool? flipped)) return null;
          if (!Params.TryGetData(DA, "Allow Joins", out bool? allowJoins)) return null;
          if (!Params.TryGetData(DA, "Structural Usage", out ARDB.Structure.StructuralWallUsage? structuralUsage)) return null;

          var levelPlane = Plane.WorldXY; levelPlane.Translate(new Vector3d(0.0, 0.0, level.Value.GetElevation() * Revit.ModelUnits));
          if (!Intersection.PlanePlane(boundaryPlane, levelPlane, out var line) || Vector3d.VectorAngle(boundaryPlane.Normal, Vector3d.ZAxis) < 5.0 * doc.Value.Application.AngleTolerance)
            throw new RuntimeArgumentException(nameof(profile), "Profile can't be horizontal.", profile);

          var normal = boundaryPlane.Normal;
          var orientation = new Vector3d(normal.X, normal.Y, 0.0); orientation.Unitize();
          var direction = new Vector3d(orientation.Y, -orientation.X, 0.0); direction.Unitize();
          var angle = Math.Atan2(-direction * Vector3d.CrossProduct(orientation, normal), orientation * normal);

          // LocationLine
          if (locationLine.HasValue && locationLine != ARDB.WallLocationLine.WallCenterline)
          {
            double offsetDist = type.Value.GetOffsetForLocationLine(locationLine.Value);
            if (offsetDist != 0.0)
            {
              offsetDist *= Revit.ModelUnits / Math.Cos(angle);
              var translation = orientation * ((wall?.Flipped is true) ? -offsetDist : offsetDist);

              line = new Line(line.From + translation, line.To + translation);
              boundaryPlane.Translate(translation);
              for (int l = 0; l < loops.Length; ++l)
                loops[l].Translate(translation);
            }
          }

          // Compute
          wall = Reconstruct
          (
            wall,
            doc.Value,
            loops,
            bbox,
            boundaryPlane,
            line,
            orientation,
            angle,
            wallType,
            level.Value,
            structuralUsage ?? ARDB.Structure.StructuralWallUsage.NonBearing
          );

          if (wall is object)
          {
            wall.get_Parameter(ARDB.BuiltInParameter.WALL_KEY_REF_PARAM).Update(locationLine ?? ARDB.WallLocationLine.WallCenterline);
            if (structuralUsage.HasValue && structuralUsage != ARDB.Structure.StructuralWallUsage.NonBearing)
            {
              wall.get_Parameter(ARDB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).Update(true);
              wall.get_Parameter(ARDB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM).Update(structuralUsage.Value);
            }
            else
            {
              wall.get_Parameter(ARDB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).Update(false);
            }

            if (wall.Flipped != (flipped ?? false))
              wall.Flip();

            // Setup joins in a last step
            if (allowJoins is true) joinedWalls.Add(wall);
          }

          DA.SetData(_Wall_, wall);
          return wall;
        }
      );
    }

    bool Reuse
    (
      ref ARDB.Wall wall,
      IList<Curve> boundaries,
      BoundingBox bbox,
      Plane plane,
      Line line,
      double slantAngle,
      ARDB.WallType type,
      ARDB.Level level
    )
    {
      if (wall is null) return false;

      if (wall.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(wall.Document, new ARDB.ElementId[] { wall.Id }, type.Id))
        {
          if (wall.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            wall = wall.Document.GetElement(id) as ARDB.Wall;
        }
        else return false;
      }

      wall.get_Parameter(ARDB.BuiltInParameter.WALL_HEIGHT_TYPE).Update(ARDB.ElementId.InvalidElementId);
      wall.get_Parameter(ARDB.BuiltInParameter.WALL_USER_HEIGHT_PARAM).Update((bbox.Max.Z - bbox.Min.Z) / Revit.ModelUnits);
      wall.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_CONSTRAINT).Update(level.Id);
      wall.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_OFFSET).Update(bbox.Min.Z / Revit.ModelUnits - level.GetElevation());

#if REVIT_2021
      if (slantAngle == 0.0)
      {
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).Update(ARDB.WallCrossSection.Vertical);
      }
      else
      {
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).Update(ARDB.WallCrossSection.SingleSlanted);
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL).Update(slantAngle);
      }
#endif

      if (wall.Location is ARDB.LocationCurve location && location.Curve is ARDB.Line locationLine)
      {
        var pinned = wall.Pinned;
        var mid0 = locationLine.Evaluate(0.5, normalized: true);
        var mid1 = line.ClosestPoint(mid0.ToPoint3d(), false).ToXYZ();
        if (!mid0.AlmostEqualPoints(mid1))
        {
          wall.Pinned = false;
          location.Move(mid1 - mid0);
        }

        var angle0 = Vector3d.VectorAngle(Vector3d.XAxis, locationLine.Direction.ToVector3d(), Vector3d.ZAxis);
        var angle1 = Vector3d.VectorAngle(Vector3d.XAxis, line.Direction, Vector3d.ZAxis);
        var angle = angle1 - angle0;
        if (Math.Abs(angle) > GeometryTolerance.Internal.DefaultTolerance)
        {
          wall.Pinned = false;
          using (var axis = ARDB.Line.CreateUnbound(mid1, UnitXYZ.BasisZ))
            location.Rotate(axis, angle);
        }

        if (wall.Pinned != pinned)
          wall.Pinned = pinned;
      }
      else return false;

      if (!(wall.GetSketch() is ARDB.Sketch sketch && Types.Sketch.SetProfile(sketch, boundaries, plane.Normal)))
        return false;

      return true;
    }

    ARDB.Wall Create
    (
      ARDB.Document doc,
      IList<Curve> loops,
      Plane plane,
      Line line,
      Vector3d orientation,
      double slantAngle,
      ARDB.WallType type,
      ARDB.Level level,
      ARDB.Structure.StructuralWallUsage structuralUsage
    )
    {
      if (slantAngle != 0.0)
      {
        for (int l = 0; l < loops.Count; ++l)
          loops[l].Rotate(-slantAngle, line.Direction, line.From);
      }

      var boundaries = loops.
        SelectMany(x => GeometryEncoder.ToCurveMany(x)).
        SelectMany(CurveExtension.ToBoundedCurves).
        ToList();

      var wall = ARDB.Wall.Create
      (
        doc,
        boundaries,
        type.Id,
        level.Id,
        structural: structuralUsage != ARDB.Structure.StructuralWallUsage.NonBearing,
        orientation.ToXYZ()
      );

      // Delay join at the end of the Transaction
      {
        ARDB.WallUtils.DisallowWallJoinAtEnd(wall, 0);
        ARDB.WallUtils.DisallowWallJoinAtEnd(wall, 1);
      }

      // Walls are created with the last LocationLine used in the Revit editor!!
      //newWall.get_Parameter(ARDB.BuiltInParameter.WALL_KEY_REF_PARAM).Update(ARDB.WallLocationLine.WallCenterline);

      // We turn off analytical model by default
      wall.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);

#if REVIT_2021
      if (slantAngle == 0.0)
      {
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).Update(ARDB.WallCrossSection.Vertical);
      }
      else
      {
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).Update(ARDB.WallCrossSection.SingleSlanted);
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL).Update(slantAngle);
      }
#endif
      return wall;
    }

    ARDB.Wall Reconstruct
    (
      ARDB.Wall wall,
      ARDB.Document doc,
      IList<Curve> boundaries,
      BoundingBox bbox,
      Plane plane,
      Line line,
      Vector3d orientation,
      double slantAngle,
      ARDB.WallType type,
      ARDB.Level level,
      ARDB.Structure.StructuralWallUsage structuralUsage
    )
    {
      if (!Reuse(ref wall, boundaries, bbox, plane, line, slantAngle, type, level))
      {
        wall = wall.ReplaceElement
        (
          Create(doc, boundaries, plane, line, orientation, slantAngle, type, level, structuralUsage),
          ExcludeUniqueProperties
        );

        wall.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_OFFSET).Update(bbox.Min.Z / Revit.ModelUnits - level.GetElevation());
      }

      return wall;
    }
  }
}

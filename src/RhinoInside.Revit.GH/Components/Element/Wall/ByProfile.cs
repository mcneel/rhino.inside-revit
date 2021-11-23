using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;
  using External.DB.Extensions;
  using ElementTracking;
  using Kernel.Attributes;

  public class WallByProfile : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("78b02ae8-2b78-45a7-962e-92e7d9097598");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public WallByProfile() : base
    (
      name: "Add Wall (Profile)",
      nickname: "WallPrfl",
      description: "Given a base curve and profile curves, it adds a Wall element to the active Revit document",
      category: "Revit",
      subCategory: "Wall"
    )
    { }

    public override void OnStarted(ARDB.Document document)
    {
      base.OnStarted(document);

      // Disable all previous walls joins
      var walls = Params.TrackedElements<ARDB.Wall>("Wall", document);
      var pinnedWalls = walls.Where(x => x.Pinned).
                        Select
                        (
                          x =>
                          (
                            x,
                            (x.Location as ARDB.LocationCurve).get_JoinType(0),
                            (x.Location as ARDB.LocationCurve).get_JoinType(1)
                          )
                        );

      foreach (var (wall, joinType0, joinType1) in pinnedWalls)
      {
        var location = wall.Location as ARDB.LocationCurve;
        if (ARDB.WallUtils.IsWallJoinAllowedAtEnd(wall, 0))
        {
          ARDB.WallUtils.DisallowWallJoinAtEnd(wall, 0);
          ARDB.WallUtils.AllowWallJoinAtEnd(wall, 0);
          location.set_JoinType(0, joinType0);
        }

        if (ARDB.WallUtils.IsWallJoinAllowedAtEnd(wall, 1))
        {
          ARDB.WallUtils.DisallowWallJoinAtEnd(wall, 1);
          ARDB.WallUtils.AllowWallJoinAtEnd(wall, 1);
          location.set_JoinType(1, joinType1);
        }
      }
    }

    List<ARDB.Wall> joinedWalls = new List<ARDB.Wall>();
    public override void OnPrepare(IReadOnlyCollection<ARDB.Document> documents)
    {
      base.OnPrepare(documents);

      // Reenable new joined walls
      foreach (var wallToJoin in joinedWalls)
      {
        if (!wallToJoin.IsValid()) continue;
        ARDB.WallUtils.AllowWallJoinAtEnd(wallToJoin, 0);
        ARDB.WallUtils.AllowWallJoinAtEnd(wallToJoin, 1);
      }

      joinedWalls = new List<ARDB.Wall>();
    }

    static readonly ARDB.FailureDefinitionId[] failureDefinitionIdsToFix = new ARDB.FailureDefinitionId[]
    {
      ARDB.BuiltInFailures.CreationFailures.CannotDrawWallsError,
      ARDB.BuiltInFailures.JoinElementsFailures.CannotJoinElementsError,
    };
    protected override IEnumerable<ARDB.FailureDefinitionId> FailureDefinitionIdsToFix => failureDefinitionIdsToFix;

    bool Reuse(ref ARDB.Wall element, IList<Curve> boundaries, Vector3d normal, ARDB.WallType type)
    {
      if (element is null) return false;

      if (element.GetSketch() is ARDB.Sketch sketch)
      {
        var plane = sketch.SketchPlane.GetPlane().ToPlane();
        if (normal.IsParallelTo(plane.Normal, Revit.AngleTolerance) == 0)
          return false;

        var profiles = sketch.Profile.ToPolyCurves();
        if (profiles.Length != boundaries.Count)
          return false;

        var loops = sketch.GetAllModelCurves();
        var pi = 0;
        foreach (var boundary in boundaries)
        {
          var profile = Curve.ProjectToPlane(boundary, plane);

          if
          (
            !Curve.GetDistancesBetweenCurves(profiles[pi], profile, Revit.VertexTolerance * Revit.ModelUnits, out var max, out var _, out var _, out var _, out var _, out var _) ||
            max > Revit.VertexTolerance * Revit.ModelUnits
          )
          {
            var segments = profile.TryGetPolyCurve(out var polyCurve, Revit.AngleTolerance) ?
              polyCurve.DuplicateSegments():
              profile.Split(profile.Domain.Mid);

            if (pi < loops.Count)
            {
              var loop = loops[pi];
              if (segments.Length != loop.Count)
                return false;

              var index = 0;
              foreach (var edge in loop)
              {
                var segment = segments[(++index) % segments.Length];

                var curve = default(ARDB.Curve);
                if
                (
                  edge.GeometryCurve is ARDB.HermiteSpline &&
                  segment.TryGetHermiteSpline(out var points, out var start, out var end, Revit.VertexTolerance * Revit.ModelUnits)
                )
                {
                  using (var tangents = new ARDB.HermiteSplineTangents() { StartTangent = start.ToXYZ(), EndTangent = end.ToXYZ() })
                  {
                    var xyz = points.ConvertAll(GeometryEncoder.ToXYZ);
                    curve = ARDB.HermiteSpline.Create(xyz, segment.IsClosed, tangents);
                  }
                }
                else curve = segment.ToCurve();

                if (!edge.GeometryCurve.IsAlmostEqualTo(curve))
                  edge.SetGeometryCurve(curve, false);
              }
            }
          }

          pi++;
        }
      }
      else return false;

      if (element.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(element.Document, new ARDB.ElementId[] { element.Id }, type.Id))
        {
          if (element.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            element = element.Document.GetElement(id) as ARDB.Wall;
        }
        else return false;
      }

      return true;
    }

    void ReconstructWallByProfile
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New Wall")]
      ref ARDB.Wall wall,

      IList<Curve> profile,
      Optional<ARDB.WallType> type,
      Optional<ARDB.Level> level,
      [Optional] ARDB.WallLocationLine locationLine,
      [Optional] bool flipped,
      [Optional, NickName("J")] bool allowJoins,
      [Optional] ARDB.Structure.StructuralWallUsage structuralUsage
    )
    {
      if (profile.Count < 1) return;

      var normal = default(Vector3d);
      var maxArea = 0.0;
      foreach (var boundary in profile)
      {
        var plane = default(Plane);
        if
        (
           boundary is null ||
          !boundary.IsClosed ||
          !boundary.TryGetPlane(out plane, Revit.VertexTolerance) ||
          !plane.ZAxis.IsPerpendicularTo(Vector3d.ZAxis, Revit.AngleTolerance)
        )
          ThrowArgumentException(nameof(profile), "Boundary profile should be a valid vertical planar closed curve.");

        using (var properties = AreaMassProperties.Compute(boundary))
        {
          if (properties is null)
          {
            AddGeometryRuntimeError(GH_RuntimeMessageLevel.Error, "Failed to compute Boundary Area", boundary);
            throw new Exceptions.RuntimeErrorException();
          }

          if (properties.Area > maxArea)
          {
            maxArea = properties.Area;
            normal = plane.Normal;

            var orientation = boundary.ClosedCurveOrientation(plane);
            if (orientation == CurveOrientation.CounterClockwise)
              normal.Reverse();
          }
        }
      }

      SolveOptionalType(document, ref type, ARDB.ElementTypeGroup.WallType, nameof(type));
      SolveOptionalLevel(document, profile, ref level, out var bbox);

      // LocationLine
      if (locationLine != ARDB.WallLocationLine.WallCenterline)
      {
        double offsetDist = 0.0;
        if (type.Value.GetCompoundStructure() is ARDB.CompoundStructure compoundStructure)
        {
          if (!compoundStructure.IsVerticallyHomogeneous())
            compoundStructure = ARDB.CompoundStructure.CreateSimpleCompoundStructure(compoundStructure.GetLayers());

          offsetDist = compoundStructure.GetOffsetForLocationLine(locationLine);
        }
        else
        {
          switch (locationLine)
          {
            case ARDB.WallLocationLine.WallCenterline:
            case ARDB.WallLocationLine.CoreCenterline:
              break;
            case ARDB.WallLocationLine.FinishFaceExterior:
            case ARDB.WallLocationLine.CoreExterior:
              offsetDist = type.Value.Width / +2.0;
              break;
            case ARDB.WallLocationLine.FinishFaceInterior:
            case ARDB.WallLocationLine.CoreInterior:
              offsetDist = type.Value.Width / -2.0;
              break;
          }
        }

        if (offsetDist != 0.0)
        {
          offsetDist *= Revit.ModelUnits;
          var translation = Transform.Translation(normal * (flipped ? -offsetDist : offsetDist));

          var newProfile = new Curve[profile.Count];
          for (int p = 0; p < profile.Count; ++p)
          {
            newProfile[p] = profile[p].DuplicateCurve();
            newProfile[p].Transform(translation);
          }

          profile = newProfile;
        }
      }

      normal = -normal;

      if (!Reuse(ref wall, profile, normal, type.Value))
      {
        var boundaries = profile.SelectMany(x => GeometryEncoder.ToCurveMany(x)).SelectMany(External.DB.Extensions.CurveExtension.ToBoundedCurves).ToList();
        var newWall = ARDB.Wall.Create
        (
          document,
          boundaries,
          type.Value.Id,
          level.Value.Id,
          structuralUsage != ARDB.Structure.StructuralWallUsage.NonBearing,
          normal.ToXYZ()
        );

        // Walls are created with the last LocationLine used in the Revit editor!!
        //newWall.get_Parameter(BuiltInParameter.WALL_KEY_REF_PARAM).Update((int) WallLocationLine.WallCenterline);

        var parametersMask = new ARDB.BuiltInParameter[]
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
          ARDB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM
        };

        ReplaceElement(ref wall, newWall, parametersMask);
      }

      if (wall is object)
      {
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_HEIGHT_TYPE).Update(ARDB.ElementId.InvalidElementId);
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_USER_HEIGHT_PARAM).Update((bbox.Max.Z - bbox.Min.Z) / Revit.ModelUnits);
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_CONSTRAINT).Update(level.Value.Id);
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_OFFSET).Update(bbox.Min.Z / Revit.ModelUnits - level.Value.GetHeight());
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_KEY_REF_PARAM).Update((int) locationLine);
        if (structuralUsage == ARDB.Structure.StructuralWallUsage.NonBearing)
        {
          wall.get_Parameter(ARDB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).Update(0);
        }
        else
        {
          wall.get_Parameter(ARDB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).Update(1);
          wall.get_Parameter(ARDB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM).Update((int) structuralUsage);
        }

        if (wall.Flipped != flipped)
          wall.Flip();

        // Setup joins in a last step
        if (allowJoins) joinedWalls.Add(wall);
        else
        {
          ARDB.WallUtils.DisallowWallJoinAtEnd(wall, 0);
          ARDB.WallUtils.DisallowWallJoinAtEnd(wall, 1);
        }
      }
    }
  }
}

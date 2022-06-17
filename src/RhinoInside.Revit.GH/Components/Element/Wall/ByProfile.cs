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
  using Grasshopper.Kernel.Parameters;

  [ComponentVersion(introduced: "1.0", updated: "1.8")]
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
    public override void OnPrepare(IReadOnlyCollection<ARDB.Document> documents)
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
      ARDB.BuiltInFailures.CreationFailures.CannotDrawWallsError,
      ARDB.BuiltInFailures.JoinElementsFailures.CannotJoinElementsError,
    };
    protected override IEnumerable<ARDB.FailureDefinitionId> FailureDefinitionIdsToFix => failureDefinitionIdsToFix;

    bool Reuse(ref ARDB.Wall element, IList<Curve> boundaries, Vector3d normal, ARDB.WallType type)
    {
      return false;
      if (element is null) return false;

      if (element.GetSketch() is ARDB.Sketch sketch)
      {
        var tol = GeometryTolerance.Model;
        var hack = new ARDB.XYZ(1.0, 1.0, 0.0);
        var plane = sketch.SketchPlane.GetPlane().ToPlane();
        if (normal.IsParallelTo(plane.Normal, tol.AngleTolerance) == 0)
          return false;

        var profiles = sketch.Profile.ToArray(GeometryDecoder.ToPolyCurve);
        if (profiles.Length != boundaries.Count)
          return false;

        var loops = sketch.GetAllModelCurves();
        var pi = 0;
        foreach (var boundary in boundaries)
        {
          var profile = Curve.ProjectToPlane(boundary, plane);

          if
          (
            !Curve.GetDistancesBetweenCurves(profiles[pi], profile, tol.VertexTolerance, out var max, out var _, out var _, out var _, out var _, out var _) ||
            max > tol.VertexTolerance
          )
          {
            var segments = profile.TryGetPolyCurve(out var polyCurve, tol.AngleTolerance) ?
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
                if (edge.GeometryCurve is ARDB.HermiteSpline)
                  curve = segment.ToHermiteSpline();
                else
                  curve = segment.ToCurve();

                if (!edge.GeometryCurve.IsSameKindAs(curve))
                  return false;

                if (!edge.GeometryCurve.AlmostEquals(curve, GeometryTolerance.Internal.VertexTolerance))
                {
                  // The following line allows SetGeometryCurve to work!!
                  edge.Location.Move(hack);
                  edge.SetGeometryCurve(curve, overrideJoins: true);
                }
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
      ARDB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM
    };

    void ReconstructWallByProfile
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New Wall")]
      ref ARDB.Wall wall,

      [ParamType(typeof(Param_Surface))]
      IList<Brep> profile,
      Optional<ARDB.WallType> type,
      Optional<ARDB.Level> level,
      [Optional] ARDB.WallLocationLine locationLine,
      [Optional] bool flipped,
      [Optional, NickName("J")] bool allowJoins,
      [Optional] ARDB.Structure.StructuralWallUsage structuralUsage
    )
    {
      var loops = profile.SelectMany(x => x.Loops).Select(x => x.To3dCurve()).ToArray();

      if (loops.Length < 1) return;

      var tol = GeometryTolerance.Model;
      var normal = default(Vector3d);
      var maxArea = 0.0;
      foreach (var boundary in loops)
      {
        var boundaryPlane = default(Plane);
        if
        (
           boundary is null ||
           boundary.IsShort(tol.ShortCurveTolerance) ||
          !boundary.IsClosed ||
          !boundary.TryGetPlane(out boundaryPlane, tol.VertexTolerance) ||
          !boundaryPlane.ZAxis.IsPerpendicularTo(Vector3d.ZAxis, tol.AngleTolerance)
        )
          ThrowArgumentException(nameof(loops), "Boundary profile should be a valid vertical planar closed curve.", boundary);

        using (var properties = AreaMassProperties.Compute(boundary))
        {
          if (properties is null)
            ThrowArgumentException(nameof(loops), "Failed to compute Boundary Area", boundary);

          if (properties.Area > maxArea)
          {
            maxArea = properties.Area;
            normal = boundaryPlane.Normal;

            var orientation = boundary.ClosedCurveOrientation(boundaryPlane);
            if (orientation == CurveOrientation.CounterClockwise)
              normal.Reverse();
          }
        }
      }

      SolveOptionalType(document, ref type, ARDB.ElementTypeGroup.WallType, nameof(type));
      SolveOptionalLevel(document, loops, ref level, out var bbox);

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

          var newLoops = new Curve[loops.Length];
          for (int p = 0; p < loops.Length; ++p)
          {
            newLoops[p] = loops[p].DuplicateCurve();
            newLoops[p].Transform(translation);
          }

          loops = newLoops;
        }
      }

      if (!Reuse(ref wall, loops, normal, type.Value))
      {
        var boundaries = loops.SelectMany(x => GeometryEncoder.ToCurveMany(x)).SelectMany(CurveExtension.ToBoundedCurves).ToList();
        var newWall = ARDB.Wall.Create
        (
          document,
          boundaries,
          type.Value.Id,
          level.Value.Id,
          structural: true,
          normal.ToXYZ()
        );

        // Wait to join at the end of the Transaction
        {
          ARDB.WallUtils.DisallowWallJoinAtEnd(newWall, 0);
          ARDB.WallUtils.DisallowWallJoinAtEnd(newWall, 1);
        }

        // Walls are created with the last LocationLine used in the Revit editor!!
        //newWall.get_Parameter(ARDB.BuiltInParameter.WALL_KEY_REF_PARAM).Update(ARDB.WallLocationLine.WallCenterline);

        // We turn off analytical model off by default
        newWall.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);

        ReplaceElement(ref wall, newWall, ExcludeUniqueProperties);

        newWall.get_Parameter(ARDB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT)?.Update(structuralUsage != ARDB.Structure.StructuralWallUsage.NonBearing);
      }

      if (wall is object)
      {
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_HEIGHT_TYPE).Update(ARDB.ElementId.InvalidElementId);
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_USER_HEIGHT_PARAM).Update((bbox.Max.Z - bbox.Min.Z) / Revit.ModelUnits);
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_CONSTRAINT).Update(level.Value.Id);
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_OFFSET).Update(bbox.Min.Z / Revit.ModelUnits - level.Value.GetElevation());
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_KEY_REF_PARAM).Update(locationLine);
        if (structuralUsage == ARDB.Structure.StructuralWallUsage.NonBearing)
        {
          wall.get_Parameter(ARDB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).Update(false);
        }
        else
        {
          wall.get_Parameter(ARDB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).Update(true);
          wall.get_Parameter(ARDB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM).Update(structuralUsage);
        }

        if (wall.Flipped != flipped)
          wall.Flip();

        // Setup joins in a last step
        if (allowJoins) joinedWalls.Add(wall);
      }
    }
  }
}

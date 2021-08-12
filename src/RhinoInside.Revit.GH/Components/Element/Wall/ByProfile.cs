using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.ElementTracking;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
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

    public override void OnStarted(DB.Document document)
    {
      base.OnStarted(document);

      // Disable all previous walls joins
      var walls = Params.TrackedElements<DB.Wall>("Wall", document);
      var pinnedWalls = walls.Where(x => x.Pinned).
                        Select
                        (
                          x =>
                          (
                            x,
                            (x.Location as DB.LocationCurve).get_JoinType(0),
                            (x.Location as DB.LocationCurve).get_JoinType(1)
                          )
                        );

      foreach (var (wall, joinType0, joinType1) in pinnedWalls)
      {
        var location = wall.Location as DB.LocationCurve;
        if (DB.WallUtils.IsWallJoinAllowedAtEnd(wall, 0))
        {
          DB.WallUtils.DisallowWallJoinAtEnd(wall, 0);
          DB.WallUtils.AllowWallJoinAtEnd(wall, 0);
          location.set_JoinType(0, joinType0);
        }

        if (DB.WallUtils.IsWallJoinAllowedAtEnd(wall, 1))
        {
          DB.WallUtils.DisallowWallJoinAtEnd(wall, 1);
          DB.WallUtils.AllowWallJoinAtEnd(wall, 1);
          location.set_JoinType(1, joinType1);
        }
      }
    }

    List<DB.Wall> joinedWalls = new List<DB.Wall>();
    public override void OnPrepare(IReadOnlyCollection<DB.Document> documents)
    {
      base.OnPrepare(documents);

      // Reenable new joined walls
      foreach (var wallToJoin in joinedWalls)
      {
        if (!wallToJoin.IsValid()) continue;
        DB.WallUtils.AllowWallJoinAtEnd(wallToJoin, 0);
        DB.WallUtils.AllowWallJoinAtEnd(wallToJoin, 1);
      }

      joinedWalls = new List<DB.Wall>();
    }

    static readonly DB.FailureDefinitionId[] failureDefinitionIdsToFix = new DB.FailureDefinitionId[]
    {
      DB.BuiltInFailures.CreationFailures.CannotDrawWallsError,
      DB.BuiltInFailures.JoinElementsFailures.CannotJoinElementsError,
    };
    protected override IEnumerable<DB.FailureDefinitionId> FailureDefinitionIdsToFix => failureDefinitionIdsToFix;

    bool Reuse(ref DB.Wall element, IList<Curve> boundaries, Vector3d normal, DB.WallType type)
    {
      if (element is null) return false;

      if (element.GetSketch() is DB.Sketch sketch)
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

                var curve = default(DB.Curve);
                if
                (
                  edge.GeometryCurve is DB.HermiteSpline &&
                  segment.TryGetHermiteSpline(out var points, out var start, out var end, Revit.VertexTolerance * Revit.ModelUnits)
                )
                {
                  using (var tangents = new DB.HermiteSplineTangents() { StartTangent = start.ToXYZ(), EndTangent = end.ToXYZ() })
                  {
                    var xyz = points.ConvertAll(GeometryEncoder.ToXYZ);
                    curve = DB.HermiteSpline.Create(xyz, segment.IsClosed, tangents);
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
        if (DB.Element.IsValidType(element.Document, new DB.ElementId[] { element.Id }, type.Id))
        {
          if (element.ChangeTypeId(type.Id) is DB.ElementId id && id != DB.ElementId.InvalidElementId)
            element = element.Document.GetElement(id) as DB.Wall;
        }
        else return false;
      }

      return true;
    }

    void ReconstructWallByProfile
    (
      [Optional, NickName("DOC")]
      DB.Document document,

      [Description("New Wall")]
      ref DB.Wall wall,

      IList<Curve> profile,
      Optional<DB.WallType> type,
      Optional<DB.Level> level,
      [Optional] DB.WallLocationLine locationLine,
      [Optional] bool flipped,
      [Optional, NickName("J")] bool allowJoins,
      [Optional] DB.Structure.StructuralWallUsage structuralUsage
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
            throw new RhinoInside.Revit.Exceptions.CancelException();
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

      SolveOptionalType(document, ref type, DB.ElementTypeGroup.WallType, nameof(type));
      SolveOptionalLevel(document, profile, ref level, out var bbox);

      // LocationLine
      if (locationLine != DB.WallLocationLine.WallCenterline)
      {
        double offsetDist = 0.0;
        if (type.Value.GetCompoundStructure() is DB.CompoundStructure compoundStructure)
        {
          if (!compoundStructure.IsVerticallyHomogeneous())
            compoundStructure = DB.CompoundStructure.CreateSimpleCompoundStructure(compoundStructure.GetLayers());

          offsetDist = compoundStructure.GetOffsetForLocationLine(locationLine);
        }
        else
        {
          switch (locationLine)
          {
            case DB.WallLocationLine.WallCenterline:
            case DB.WallLocationLine.CoreCenterline:
              break;
            case DB.WallLocationLine.FinishFaceExterior:
            case DB.WallLocationLine.CoreExterior:
              offsetDist = type.Value.Width / +2.0;
              break;
            case DB.WallLocationLine.FinishFaceInterior:
            case DB.WallLocationLine.CoreInterior:
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
        var newWall = DB.Wall.Create
        (
          document,
          boundaries,
          type.Value.Id,
          level.Value.Id,
          structuralUsage != DB.Structure.StructuralWallUsage.NonBearing,
          normal.ToXYZ()
        );

        // Walls are created with the last LocationLine used in the Revit editor!!
        //newWall.get_Parameter(BuiltInParameter.WALL_KEY_REF_PARAM).Set((int) WallLocationLine.WallCenterline);

        var parametersMask = new DB.BuiltInParameter[]
        {
          DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_PARAM,
          DB.BuiltInParameter.ELEM_TYPE_PARAM,
          DB.BuiltInParameter.WALL_KEY_REF_PARAM,
          DB.BuiltInParameter.WALL_HEIGHT_TYPE,
          DB.BuiltInParameter.WALL_USER_HEIGHT_PARAM,
          DB.BuiltInParameter.WALL_BASE_CONSTRAINT,
          DB.BuiltInParameter.WALL_BASE_OFFSET,
          DB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT,
          DB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM
        };

        ReplaceElement(ref wall, newWall, parametersMask);
      }

      if (wall is object)
      {
        wall.get_Parameter(DB.BuiltInParameter.WALL_HEIGHT_TYPE).Set(DB.ElementId.InvalidElementId);
        wall.get_Parameter(DB.BuiltInParameter.WALL_USER_HEIGHT_PARAM).Set((bbox.Max.Z - bbox.Min.Z) / Revit.ModelUnits);
        wall.get_Parameter(DB.BuiltInParameter.WALL_BASE_CONSTRAINT).Set(level.Value.Id);
        wall.get_Parameter(DB.BuiltInParameter.WALL_BASE_OFFSET).Set(bbox.Min.Z / Revit.ModelUnits - level.Value.GetHeight());
        wall.get_Parameter(DB.BuiltInParameter.WALL_KEY_REF_PARAM).Set((int) locationLine);
        if (structuralUsage == DB.Structure.StructuralWallUsage.NonBearing)
        {
          wall.get_Parameter(DB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).Set(0);
        }
        else
        {
          wall.get_Parameter(DB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).Set(1);
          wall.get_Parameter(DB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM).Set((int) structuralUsage);
        }

        if (wall.Flipped != flipped)
          wall.Flip();

        // Setup joins in a last step
        if (allowJoins) joinedWalls.Add(wall);
        else
        {
          DB.WallUtils.DisallowWallJoinAtEnd(wall, 0);
          DB.WallUtils.DisallowWallJoinAtEnd(wall, 1);
        }
      }
    }
  }
}

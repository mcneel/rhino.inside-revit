using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  using Convert.Geometry;
  using External.DB;
  using External.DB.Extensions;
  using ElementTracking;
  using Kernel.Attributes;

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
      ARDB.BuiltInFailures.CreationFailures.CannotDrawWallsError,
      ARDB.BuiltInFailures.JoinElementsFailures.CannotJoinElementsError,
    };
    protected override IEnumerable<ARDB.FailureDefinitionId> FailureDefinitionIdsToFix => failureDefinitionIdsToFix;

    bool Reuse(ref ARDB.Wall element, IList<Curve> boundaries, Plane plane, Line line, double slantAngle, ARDB.WallType type)
    {
      if (element is null) return false;

      if (element.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(element.Document, new ARDB.ElementId[] { element.Id }, type.Id))
        {
          if (element.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            element = element.Document.GetElement(id) as ARDB.Wall;
        }
        else return false;
      }

#if REVIT_2021
      if (Math.Abs(slantAngle) < element.Document.Application.AngleTolerance)
      {
        element.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).Update(ARDB.WallCrossSection.Vertical);
      }
      else
      {
        element.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).Update(ARDB.WallCrossSection.SingleSlanted);
        element.get_Parameter(ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL).Update(slantAngle);
      }
#endif

      if (element.Location is ARDB.LocationCurve location && location.Curve is ARDB.Line locationLine)
      {
        var pinned = element.Pinned;
        var mid0 = locationLine.Evaluate(0.5, normalized: true);
        var mid1 = line.ClosestPoint(mid0.ToPoint3d(), false).ToXYZ();
        var distance = mid1 - mid0;
        if (!distance.IsZeroLength())
        {
          element.Pinned = false;
          location.Move(distance);
        }

        var angle0 = Vector3d.VectorAngle(Vector3d.XAxis, locationLine.Direction.ToVector3d(), Vector3d.ZAxis);
        var angle1 = Vector3d.VectorAngle(Vector3d.XAxis, line.Direction, Vector3d.ZAxis);
        var angle = angle1 - angle0;
        if (Math.Abs(angle) > GeometryTolerance.Internal.DefaultTolerance)
        {
          element.Pinned = false;
          using (var axis = ARDB.Line.CreateUnbound(mid1, UnitXYZ.BasisZ))
            location.Rotate(axis, angle);
        }

        if (element.Pinned != pinned)
          element.Pinned = pinned;
      }
      else return false;

      if (!(element.GetSketch() is ARDB.Sketch sketch && Types.Sketch.SetProfile(sketch, boundaries, plane.Normal)))
        return false;

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
      ARDB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM,
#if REVIT_2021
      ARDB.BuiltInParameter.WALL_CROSS_SECTION,
      ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL,
#endif
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
      var loops = profile.OfType<Brep>().SelectMany(x => x.Loops).Select(x => x.To3dCurve()).ToArray();

      if (loops.Length < 1) return;

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
          ThrowArgumentException(nameof(profile), "Profile should be a list of coplanar surfaces.", loop);

#if !REVIT_2021
        if (!plane.ZAxis.IsPerpendicularTo(Vector3d.ZAxis, tol.AngleTolerance))
          ThrowArgumentException(nameof(profile), "Profile should be a list of coplanar vertical surfaces.", loop);
#endif

        loops[index] = loop.Simplify(CurveSimplifyOptions.All, tol.VertexTolerance, tol.AngleTolerance) ?? loop;

        using (var properties = AreaMassProperties.Compute(loop, tol.VertexTolerance))
        {
          if (properties is null)
            ThrowArgumentException(nameof(profile), "Failed to compute Boundary Area.", loop);

          if (properties.Area > maxArea)
          {
            maxArea = properties.Area;
            var orientation = loop.ClosedCurveOrientation(plane);

            if (orientation == CurveOrientation.CounterClockwise)
              plane.Flip();

            boundaryPlane = plane;
          }
          else if (plane.Normal.IsParallelTo(boundaryPlane.Normal) == 0)
          {
            ThrowArgumentException(nameof(profile), "Profile should be a list of coplanar surfaces.", loops);
          }
        }
      }

      SolveOptionalType(document, ref type, ARDB.ElementTypeGroup.WallType, nameof(type));
      SolveOptionalLevel(document, loops, ref level, out var bbox);

      var levelPlane = Plane.WorldXY; levelPlane.Translate(new Vector3d(0.0, 0.0, level.Value.GetElevation() * Revit.ModelUnits));
      if (!Intersection.PlanePlane(boundaryPlane, levelPlane, out var line) || Vector3d.VectorAngle(boundaryPlane.Normal, Vector3d.ZAxis) < 3.0 * document.Application.AngleTolerance)
        ThrowArgumentException(nameof(profile), "Profile can't be horizontal.");

      var normal = boundaryPlane.Normal;
      var flat = new Vector3d(normal.X, normal.Y, 0.0);
      flat.Unitize();
      var angle = Vector3d.VectorAngle(flat, normal, line.Direction);
      if (angle > Math.PI) angle -= 2.0 * Math.PI;

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
          offsetDist *= Revit.ModelUnits / Math.Cos(angle);
          var translation = flat * ((wall?.Flipped ?? flipped) ? -offsetDist : offsetDist);

          line = new Line(line.From + translation, line.To + translation);
          boundaryPlane.Translate(translation);
          for (int l = 0; l < loops.Length; ++l)
            loops[l].Translate(translation);
        }
      }

      if (!Reuse(ref wall, loops, boundaryPlane, line, angle, type.Value))
      {
        for (int l = 0; l < loops.Length; ++l)
          loops[l].Rotate(-angle, line.Direction, line.From);

        var boundaries = loops.
          SelectMany(x => GeometryEncoder.ToCurveMany(x)).
          SelectMany(CurveExtension.ToBoundedCurves).
          ToList();

        try
        {
          var newWall = ARDB.Wall.Create
          (
            document,
            boundaries,
            type.Value.Id,
            level.Value.Id,
            structural: structuralUsage != ARDB.Structure.StructuralWallUsage.NonBearing,
            flat.ToXYZ()
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

#if REVIT_2021
          if (Math.Abs(angle) < newWall.Document.Application.AngleTolerance)
          {
            newWall.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).Update(ARDB.WallCrossSection.Vertical);
          }
          else
          {
            newWall.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).Update(ARDB.WallCrossSection.SingleSlanted);
            newWall.get_Parameter(ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL).Update(angle);
          }
#endif

          ReplaceElement(ref wall, newWall, ExcludeUniqueProperties);
        }
        catch (Exception ex)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
          return;
        }
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

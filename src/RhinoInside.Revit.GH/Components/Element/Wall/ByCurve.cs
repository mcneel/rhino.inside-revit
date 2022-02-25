using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  using Convert.Geometry;
  using ElementTracking;
  using External.DB.Extensions;
  using Kernel.Attributes;

  public class WallByCurve : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("37A8C46F-CB5B-49FD-A483-B03D1FE14A22");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public WallByCurve() : base
    (
      name: "Add Wall (Curve)",
      nickname: "WallCrv",
      description: "Given a curve, it adds a Wall element to the active Revit document",
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
                          wall =>
                          (
                            wall,
                            (wall.Location as ARDB.LocationCurve).get_JoinType(0),
                            (wall.Location as ARDB.LocationCurve).get_JoinType(1)
                          )
                        );

      foreach (var (wall, joinType0, joinType1) in pinnedWalls)
      {
        var location = wall.Location as ARDB.LocationCurve;

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
      }

      joinedWalls = new List<ARDB.Wall>();
    }

    static readonly ARDB.FailureDefinitionId[] failureDefinitionIdsToFix = new ARDB.FailureDefinitionId[]
    {
      ARDB.BuiltInFailures.JoinElementsFailures.CannotJoinElementsError,
      ARDB.BuiltInFailures.CreationFailures.CannotMakeWall,
      ARDB.BuiltInFailures.CreationFailures.CannotDrawWallsError,
      ARDB.BuiltInFailures.CreationFailures.CannotDrawWalls,
    };
    protected override IEnumerable<ARDB.FailureDefinitionId> FailureDefinitionIdsToFix => failureDefinitionIdsToFix.Reverse();

    void ReconstructWallByCurve
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New Wall")]
      ref ARDB.Wall wall,

      Rhino.Geometry.Curve curve,
      Optional<ARDB.WallType> type,
      Optional<ARDB.Level> level,
      Optional<double> height,
      [Optional] ARDB.WallLocationLine locationLine,
      [Optional] bool flipped,
      [Optional, NickName("J")] bool allowJoins,
      [Optional] ARDB.Structure.StructuralWallUsage structuralUsage
    )
    {
      var tol = GeometryObjectTolerance.Model;

#if REVIT_2020
      if
      (
        !(curve.IsLinear(tol.VertexTolerance) || curve.IsArc(tol.VertexTolerance) || curve.IsEllipse(tol.VertexTolerance)) ||
        !curve.TryGetPlane(out var axisPlane, tol.VertexTolerance) ||
        axisPlane.ZAxis.IsParallelTo(Rhino.Geometry.Vector3d.ZAxis, tol.AngleTolerance) == 0
      )
        ThrowArgumentException(nameof(curve), "Curve must be a horizontal line, arc or ellipse curve.", curve);
#else
      if
      (
        !(curve.IsLinear(tol.VertexTolerance) || curve.IsArc(tol.VertexTolerance)) ||
        !curve.TryGetPlane(out var axisPlane, tol.VertexTolerance) ||
        axisPlane.ZAxis.IsParallelTo(Rhino.Geometry.Vector3d.ZAxis, tol.AngleTolerance) == 0
      )
        ThrowArgumentException(nameof(curve), "Curve must be a horizontal line or arc curve.", curve);
#endif

      SolveOptionalType(document, ref type, ARDB.ElementTypeGroup.WallType, nameof(type));

      bool levelIsEmpty = SolveOptionalLevel(document, curve, ref level, out var bbox);

      // Curve
      var levelPlane = new Rhino.Geometry.Plane(new Rhino.Geometry.Point3d(0.0, 0.0, level.Value.GetHeight() * Revit.ModelUnits), Rhino.Geometry.Vector3d.ZAxis);
      if(!TryGetCurveAtPlane(curve, levelPlane, out var centerLine))
        ThrowArgumentException(nameof(curve), "Failed to project curve in the level plane.", curve);

      // Height
      if (!height.HasValue)
        height = type.GetValueOrDefault()?.GetCompoundStructure()?.SampleHeight * Revit.ModelUnits ?? LiteralLengthValue(6.0);

      if (height.Value < 0.1 * Revit.ModelUnits)
        ThrowArgumentException(nameof(height), $"Height minimum value is {0.1 * Revit.ModelUnits} {Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem}");
      else if (height.Value > 3000 * Revit.ModelUnits)
        ThrowArgumentException(nameof(height), $"Height maximum value is {3000 * Revit.ModelUnits} {Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem}");

      // LocationLine
      if (locationLine != ARDB.WallLocationLine.WallCenterline)
      {
        double offsetDist = 0.0;
        var compoundStructure = type.Value.GetCompoundStructure();
        if (compoundStructure == null)
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
        else
        {
          if (!compoundStructure.IsVerticallyHomogeneous())
            compoundStructure = ARDB.CompoundStructure.CreateSimpleCompoundStructure(compoundStructure.GetLayers());

          offsetDist = compoundStructure.GetOffsetForLocationLine(locationLine);
        }

        if (offsetDist != 0.0)
          centerLine = centerLine.CreateOffset(flipped ? -offsetDist : offsetDist, ARDB.XYZ.BasisZ);
      }

      // Type
      ChangeElementTypeId(ref wall, type.Value.Id);

      ARDB.Wall newWall = null;
      if (wall is ARDB.Wall previousWall && previousWall.Location is ARDB.LocationCurve locationCurve && centerLine.IsSameKindAs(locationCurve.Curve))
      {
        newWall = previousWall;

        if(!locationCurve.Curve.IsAlmostEqualTo(centerLine))
          locationCurve.Curve = centerLine;
      }
      else
      {
        newWall = ARDB.Wall.Create
        (
          document,
          centerLine,
          type.Value.Id,
          level.Value.Id,
          height.Value / Revit.ModelUnits,
          levelIsEmpty ? bbox.Min.Z / Revit.ModelUnits - level.Value.GetHeight() : 0.0,
          flipped,
          structuralUsage != ARDB.Structure.StructuralWallUsage.NonBearing
        );

        // Wait to join at the end of the Transaction
        {
          ARDB.WallUtils.DisallowWallJoinAtEnd(newWall, 0);
          ARDB.WallUtils.DisallowWallJoinAtEnd(newWall, 1);
        }

        // Walls are created with the last LocationLine used in the Revit editor!!
        //newWall.get_Parameter(BuiltInParameter.WALL_KEY_REF_PARAM).Update((int) WallLocationLine.WallCenterline);

        var parametersMask = new ARDB.BuiltInParameter[]
        {
          ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
          ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
          ARDB.BuiltInParameter.WALL_KEY_REF_PARAM,
          ARDB.BuiltInParameter.WALL_USER_HEIGHT_PARAM,
          ARDB.BuiltInParameter.WALL_BASE_CONSTRAINT,
          ARDB.BuiltInParameter.WALL_BASE_OFFSET,
          ARDB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT,
          ARDB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM
        };

        ReplaceElement(ref wall, newWall, parametersMask);
      }

      if (newWall is object)
      {
        newWall.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_CONSTRAINT).Update(level.Value.Id);
        newWall.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_OFFSET).Update(bbox.Min.Z / Revit.ModelUnits - level.Value.GetHeight());
        newWall.get_Parameter(ARDB.BuiltInParameter.WALL_HEIGHT_TYPE).Update(ARDB.ElementId.InvalidElementId);
        newWall.get_Parameter(ARDB.BuiltInParameter.WALL_USER_HEIGHT_PARAM).Update(height.Value / Revit.ModelUnits);

        newWall.get_Parameter(ARDB.BuiltInParameter.WALL_KEY_REF_PARAM).Update((int) locationLine);
        if(structuralUsage == ARDB.Structure.StructuralWallUsage.NonBearing)
        {
          newWall.get_Parameter(ARDB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).Update(0);
        }
        else
        {
          newWall.get_Parameter(ARDB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).Update(1);
          newWall.get_Parameter(ARDB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM).Update((int) structuralUsage);
        }

        if (newWall.Flipped != flipped)
          newWall.Flip();

        // Setup joins in a last step
        if (allowJoins) joinedWalls.Add(newWall);
      }
    }
  }
}

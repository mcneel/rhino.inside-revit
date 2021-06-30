using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Units;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
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

    public override void OnStarted(DB.Document document)
    {
      base.OnStarted(document);

      // Disable all previous walls joins
      if (PreviousStructure(document) is Types.IGH_ElementId[] previous)
      {
        var unjoinedWalls = previous.OfType<Types.Element>().
                            Select(x => document.GetElement(x.Id)).
                            OfType<DB.Wall>().
                            Where(x => x.Pinned).
                            Select
                            (
                              x => Tuple.Create
                              (
                                x,
                                (x.Location as DB.LocationCurve).get_JoinType(0),
                                (x.Location as DB.LocationCurve).get_JoinType(1)
                              )
                            ).
                            ToArray();

        foreach(var unjoinedWall in unjoinedWalls)
        {
          var location = unjoinedWall.Item1.Location as DB.LocationCurve;
          if (DB.WallUtils.IsWallJoinAllowedAtEnd(unjoinedWall.Item1, 0))
          {
            DB.WallUtils.DisallowWallJoinAtEnd(unjoinedWall.Item1, 0);
            DB.WallUtils.AllowWallJoinAtEnd(unjoinedWall.Item1, 0);
            location.set_JoinType(0, unjoinedWall.Item2);
          }

          if (DB.WallUtils.IsWallJoinAllowedAtEnd(unjoinedWall.Item1, 1))
          {
            DB.WallUtils.DisallowWallJoinAtEnd(unjoinedWall.Item1, 1);
            DB.WallUtils.AllowWallJoinAtEnd(unjoinedWall.Item1, 1);
            location.set_JoinType(1, unjoinedWall.Item3);
          }
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
        DB.WallUtils.AllowWallJoinAtEnd(wallToJoin, 0);
        DB.WallUtils.AllowWallJoinAtEnd(wallToJoin, 1);
      }

      joinedWalls = new List<DB.Wall>();
    }

    static readonly DB.FailureDefinitionId[] failureDefinitionIdsToFix = new DB.FailureDefinitionId[]
    {
      DB.BuiltInFailures.CreationFailures.CannotMakeWall,
      DB.BuiltInFailures.CreationFailures.CannotDrawWallsError,
      DB.BuiltInFailures.CreationFailures.CannotDrawWalls,
      DB.BuiltInFailures.JoinElementsFailures.CannotJoinElementsError,
    };
    protected override IEnumerable<DB.FailureDefinitionId> FailureDefinitionIdsToFix => failureDefinitionIdsToFix;

    void ReconstructWallByCurve
    (
      DB.Document doc,

      [Description("New Wall")]
      ref DB.Wall wall,

      Rhino.Geometry.Curve curve,
      Optional<DB.WallType> type,
      Optional<DB.Level> level,
      Optional<double> height,
      [Optional] DB.WallLocationLine locationLine,
      [Optional] bool flipped,
      [Optional, NickName("J")] bool allowJoins,
      [Optional] DB.Structure.StructuralWallUsage structuralUsage
    )
    {
#if REVIT_2020
      if
      (
        !(curve.IsLinear(Revit.VertexTolerance * Revit.ModelUnits) || curve.IsArc(Revit.VertexTolerance * Revit.ModelUnits) || curve.IsEllipse(Revit.VertexTolerance * Revit.ModelUnits)) ||
        !curve.TryGetPlane(out var axisPlane, Revit.VertexTolerance * Revit.ModelUnits) ||
        axisPlane.ZAxis.IsParallelTo(Rhino.Geometry.Vector3d.ZAxis, Revit.AngleTolerance) == 0
      )
        ThrowArgumentException(nameof(curve), "Curve must be a horizontal line, arc or ellipse curve.");
#else
      if
      (
        !(curve.IsLinear(Revit.VertexTolerance * Revit.ModelUnits) || curve.IsArc(Revit.VertexTolerance * Revit.ModelUnits)) ||
        !curve.TryGetPlane(out var axisPlane, Revit.VertexTolerance * Revit.ModelUnits) ||
        axisPlane.ZAxis.IsParallelTo(Rhino.Geometry.Vector3d.ZAxis, Revit.AngleTolerance) == 0
      )
        ThrowArgumentException(nameof(curve), "Curve must be a horizontal line or arc curve.");
#endif

      SolveOptionalType(doc, ref type, DB.ElementTypeGroup.WallType, nameof(type));

      bool levelIsEmpty = SolveOptionalLevel(doc, curve, ref level, out var bbox);

      // Curve
      var levelPlane = new Rhino.Geometry.Plane(new Rhino.Geometry.Point3d(0.0, 0.0, level.Value.GetHeight() * Revit.ModelUnits), Rhino.Geometry.Vector3d.ZAxis);
      if(!TryGetCurveAtPlane(curve, levelPlane, out var centerLine))
        ThrowArgumentException(nameof(curve), "Failed to project curve in the level plane.");

      // Height
      if (!height.HasValue)
        height = type.GetValueOrDefault()?.GetCompoundStructure()?.SampleHeight * Revit.ModelUnits ?? LiteralLengthValue(6.0);

      if (height.Value < 0.1 * Revit.ModelUnits)
        ThrowArgumentException(nameof(height), $"Height minimum value is {0.1 * Revit.ModelUnits} {Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem}");
      else if (height.Value > 3000 * Revit.ModelUnits)
        ThrowArgumentException(nameof(height), $"Height maximum value is {3000 * Revit.ModelUnits} {Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem}");

      // LocationLine
      if (locationLine != DB.WallLocationLine.WallCenterline)
      {
        double offsetDist = 0.0;
        var compoundStructure = type.Value.GetCompoundStructure();
        if (compoundStructure == null)
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
        else
        {
          if (!compoundStructure.IsVerticallyHomogeneous())
            compoundStructure = DB.CompoundStructure.CreateSimpleCompoundStructure(compoundStructure.GetLayers());

          offsetDist = compoundStructure.GetOffsetForLocationLine(locationLine);
        }

        if (offsetDist != 0.0)
          centerLine = centerLine.CreateOffset(flipped ? -offsetDist : offsetDist, DB.XYZ.BasisZ);
      }

      // Type
      ChangeElementTypeId(ref wall, type.Value.Id);

      DB.Wall newWall = null;
      if (wall is DB.Wall previousWall && previousWall.Location is DB.LocationCurve locationCurve && centerLine.IsSameKindAs(locationCurve.Curve))
      {
        newWall = previousWall;

        locationCurve.Curve = centerLine;
      }
      else
      {
        newWall = DB.Wall.Create
        (
          doc,
          centerLine,
          type.Value.Id,
          level.Value.Id,
          height.Value / Revit.ModelUnits,
          levelIsEmpty ? bbox.Min.Z / Revit.ModelUnits - level.Value.GetHeight() : 0.0,
          flipped,
          structuralUsage != DB.Structure.StructuralWallUsage.NonBearing
        );

        // Walls are created with the last LocationLine used in the Revit editor!!
        //newWall.get_Parameter(BuiltInParameter.WALL_KEY_REF_PARAM).Set((int) WallLocationLine.WallCenterline);

        var parametersMask = new DB.BuiltInParameter[]
        {
          DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_PARAM,
          DB.BuiltInParameter.ELEM_TYPE_PARAM,
          DB.BuiltInParameter.WALL_KEY_REF_PARAM,
          DB.BuiltInParameter.WALL_USER_HEIGHT_PARAM,
          DB.BuiltInParameter.WALL_BASE_CONSTRAINT,
          DB.BuiltInParameter.WALL_BASE_OFFSET,
          DB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT,
          DB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM
        };

        ReplaceElement(ref wall, newWall, parametersMask);
      }

      if (newWall != null)
      {
        newWall.get_Parameter(DB.BuiltInParameter.WALL_BASE_CONSTRAINT).Set(level.Value.Id);
        newWall.get_Parameter(DB.BuiltInParameter.WALL_BASE_OFFSET).Set(bbox.Min.Z / Revit.ModelUnits - level.Value.GetHeight());
        newWall.get_Parameter(DB.BuiltInParameter.WALL_USER_HEIGHT_PARAM).Set(height.Value / Revit.ModelUnits);
        newWall.get_Parameter(DB.BuiltInParameter.WALL_KEY_REF_PARAM).Set((int) locationLine);
        if(structuralUsage == DB.Structure.StructuralWallUsage.NonBearing)
        {
          newWall.get_Parameter(DB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).Set(0);
        }
        else
        {
          newWall.get_Parameter(DB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).Set(1);
          newWall.get_Parameter(DB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM).Set((int) structuralUsage);
        }

        if (newWall.Flipped != flipped)
          newWall.Flip();

        // Setup joins in a last step
        if (allowJoins) joinedWalls.Add(newWall);
        else
        {
          DB.WallUtils.DisallowWallJoinAtEnd(newWall, 0);
          DB.WallUtils.DisallowWallJoinAtEnd(newWall, 1);
        }
      }
    }
  }
}

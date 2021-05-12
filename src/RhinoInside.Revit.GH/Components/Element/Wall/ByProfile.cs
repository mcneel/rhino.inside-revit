using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Units;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
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

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Wall(), "Wall", "W", "New Wall", GH_ParamAccess.item);
    }

    protected override void OnAfterStart(DB.Document document, string strTransactionName)
    {
      base.OnAfterStart(document, strTransactionName);

      // Disable all previous walls joins
      if (PreviousStructure is object)
      {
        var unjoinedWalls = PreviousStructure.OfType<Types.Element>().
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

        foreach (var unjoinedWall in unjoinedWalls)
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
    protected override void OnBeforeCommit(DB.Document document, string strTransactionName)
    {
      base.OnBeforeCommit(document, strTransactionName);

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
      DB.BuiltInFailures.CreationFailures.CannotDrawWallsError,
      DB.BuiltInFailures.JoinElementsFailures.CannotJoinElementsError,
    };
    protected override IEnumerable<DB.FailureDefinitionId> FailureDefinitionIdsToFix => failureDefinitionIdsToFix;

    void ReconstructWallByProfile
    (
      DB.Document doc,
      ref DB.Wall element,

      IList<Rhino.Geometry.Curve> profile,
      Optional<DB.WallType> type,
      Optional<DB.Level> level,
      [Optional] DB.WallLocationLine locationLine,
      [Optional] bool flipped,
      [Optional, NickName("J")] bool allowJoins,
      [Optional] DB.Structure.StructuralWallUsage structuralUsage
    )
    {
      var boundaryPlane = default(Rhino.Geometry.Plane);
      var maxArea = 0.0;
      foreach (var boundary in profile)
      {
        var plane = default(Rhino.Geometry.Plane);
        if
        (
          !boundary.IsClosed ||
          !boundary.TryGetPlane(out plane, Revit.VertexTolerance) ||
          !plane.ZAxis.IsPerpendicularTo(Rhino.Geometry.Vector3d.ZAxis, Revit.AngleTolerance)
        )
          ThrowArgumentException(nameof(profile), "Boundary profile must be a vertical planar closed curve.");

        using (var properties = Rhino.Geometry.AreaMassProperties.Compute(boundary))
        {
          if (properties.Area > maxArea)
          {
            maxArea = properties.Area;
            boundaryPlane = plane;
          }
        }
      }

      SolveOptionalType(doc, ref type, DB.ElementTypeGroup.WallType, nameof(type));
      SolveOptionalLevel(doc, profile, ref level, out var bbox);

      foreach (var curve in profile)
      {
        curve.RemoveShortSegments(Revit.ShortCurveTolerance * Revit.ModelUnits);
        var orientation = curve.ClosedCurveOrientation(boundaryPlane);
        if (orientation == Rhino.Geometry.CurveOrientation.CounterClockwise)
          curve.Reverse();
      }
      var boundaries = profile.SelectMany(x => GeometryEncoder.ToCurveMany(x)).SelectMany(CurveExtension.ToBoundedCurves).ToList();

      // Flipped - Adjust it to orient the wall facing to boundaryPlane.Normal
      if
      (
        Rhino.Geometry.Vector3d.VectorAngle
        (
          boundaryPlane.Normal,
          new Rhino.Geometry.Vector3d(-1.0, 1.0, 0.0)
        ) > Math.PI * 0.5
      )
        flipped = !flipped;

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
        {
          profile[0].TryGetPlane(out var plane);
          var translation = DB.Transform.CreateTranslation((plane.Normal * (flipped ? -offsetDist : offsetDist)).ToXYZ());
          for (int b = 0; b < boundaries.Count; ++b)
            boundaries[b] = boundaries[b].CreateTransformed(translation);
        }
      }

      var newWall = DB.Wall.Create
      (
        doc,
        boundaries,
        type.Value.Id,
        level.Value.Id,
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

      ReplaceElement(ref element, newWall, parametersMask);

      if (newWall != null)
      {
        newWall.get_Parameter(DB.BuiltInParameter.WALL_BASE_CONSTRAINT).Set(level.Value.Id);
        newWall.get_Parameter(DB.BuiltInParameter.WALL_BASE_OFFSET).Set(bbox.Min.Z / Revit.ModelUnits - level.Value.GetHeight());
        newWall.get_Parameter(DB.BuiltInParameter.WALL_KEY_REF_PARAM).Set((int) locationLine);
        if (structuralUsage == DB.Structure.StructuralWallUsage.NonBearing)
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

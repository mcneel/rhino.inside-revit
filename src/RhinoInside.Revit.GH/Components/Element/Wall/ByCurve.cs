using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  using Convert.Geometry;
  using ElementTracking;
  using External.DB.Extensions;
  using GH.Exceptions;

  public class WallByCurve : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("37A8C46F-CB5B-49FD-A483-B03D1FE14A22");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public WallByCurve() : base
    (
      name: "Add Wall (Curve)",
      nickname: "C-Wall",
      description: "Given a curve, it adds a Wall element to the active Revit document",
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

        try
        {
          foreach (var wallToJoin in joinedWalls)
          {
            ARDB.WallUtils.AllowWallJoinAtEnd(wallToJoin, 0);
            ARDB.WallUtils.AllowWallJoinAtEnd(wallToJoin, 1);
          }
        }
        finally
        {
          joinedWalls.Clear();
        }
      }
    }

    static readonly ARDB.FailureDefinitionId[] failureDefinitionIdsToFix = new ARDB.FailureDefinitionId[]
    {
      ARDB.BuiltInFailures.JoinElementsFailures.CannotJoinElementsError,
      ARDB.BuiltInFailures.CreationFailures.CannotMakeWall,
      ARDB.BuiltInFailures.CreationFailures.CannotDrawWallsError,
      ARDB.BuiltInFailures.CreationFailures.CannotDrawWalls,
    };
    protected override IEnumerable<ARDB.FailureDefinitionId> FailureDefinitionIdsToFix => failureDefinitionIdsToFix.Reverse();

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
       (
        new Param_Curve
        {
          Name = "Curve",
          NickName = "C",
          Description = "Wall axis"
        }
      ),
      new ParamDefinition
       (
        new Parameters.HostObjectType
        {
          Name = "Type",
          NickName = "T",
          Description = "Floor type",
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
          Description = "Wall base level",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = "Height",
          NickName = "H",
          Description = "Unconnected Height",
          Optional = true
        }, ParamRelevance.Primary
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
          Description = "Unconnected Height",
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
      if (Params.Input<IGH_Param>("LocationLine") is IGH_Param locationLine)
        locationLine.Name = "Location Line";
      if (Params.Input<IGH_Param>("AllowJoins") is IGH_Param allowJoins)
        allowJoins.Name = "Allow Joins";
      if (Params.Input<IGH_Param>("StructuralUsage") is IGH_Param structuralUsage)
        structuralUsage.Name = "Structural Usage";

      base.AddedToDocument(document);
    }

    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
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

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      var tol = GeometryTolerance.Model;
      ReconstructElement<ARDB.Wall>
      (
        doc.Value, _Wall_, wall =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve)) return null;

#if REVIT_2020
          if
          (
            !(curve.IsLinear(tol.VertexTolerance) || curve.IsArc(tol.VertexTolerance) || curve.IsEllipse(tol.VertexTolerance)) ||
            !curve.TryGetPlane(out var axisPlane, tol.VertexTolerance) ||
            axisPlane.ZAxis.IsParallelTo(Rhino.Geometry.Vector3d.ZAxis, tol.AngleTolerance) == 0
          )
            throw new RuntimeArgumentException(nameof(curve), "Curve must be a horizontal line, arc or ellipse curve.", curve);
#else
          if
          (
            !(curve.IsLinear(tol.VertexTolerance) || curve.IsArc(tol.VertexTolerance)) ||
            !curve.TryGetPlane(out var axisPlane, tol.VertexTolerance) ||
            axisPlane.ZAxis.IsParallelTo(Rhino.Geometry.Vector3d.ZAxis, tol.AngleTolerance) == 0
          )
            throw new RuntimeArgumentException(nameof(curve), "Curve must be a horizontal line or arc curve.", curve);
#endif

          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out Types.HostObjectType type, doc, ARDB.ElementTypeGroup.WallType)) return null;

          if (!(type.Value is ARDB.WallType wallType))
            throw new RuntimeArgumentException(nameof(type), $"Type '{type.Nomen}' is not a valid floor type.");

          var bbox = curve.GetBoundingBox(true);
          if (!Parameters.Level.GetDataOrDefault(this, DA, "Level", out Types.Level level, doc, bbox.IsValid ? bbox.Min.Z : double.NaN)) return null;
          if (!Params.TryGetData(DA, "Height", out double? height)) return null;
          if (!Params.TryGetData(DA, "Location Line", out ARDB.WallLocationLine? locationLine)) return null;
          if (!Params.TryGetData(DA, "Flipped", out bool? flipped)) return null;
          if (!Params.TryGetData(DA, "Allow Joins", out bool? allowJoins)) return null;
          if (!Params.TryGetData(DA, "Structural Usage", out ARDB.Structure.StructuralWallUsage? structuralUsage)) return null;

          // Curve
          var levelPlane = new Plane(new Point3d(0.0, 0.0, level.Elevation), Vector3d.ZAxis);
          if (!TryGetCurveAtPlane(curve, levelPlane, out var centerLine))
            throw new RuntimeArgumentException(nameof(curve), "Failed to project curve in the level plane.", curve);

          if (!height.HasValue)
            height ??= type?.Value.GetCompoundStructure()?.SampleHeight * Revit.ModelUnits ?? 20.0 * Revit.ModelUnits;

          if (height.Value < 0.1 * Revit.ModelUnits)
            throw new RuntimeArgumentException(nameof(height), $"Height minimum value is {0.1 * Revit.ModelUnits} {Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem}");
          else if (height.Value > 3000 * Revit.ModelUnits)
            throw new RuntimeArgumentException(nameof(height), $"Height maximum value is {3000 * Revit.ModelUnits} {Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem}");

          // LocationLine
          if (locationLine.HasValue && locationLine != ARDB.WallLocationLine.WallCenterline)
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
                  offsetDist = (type.Value as ARDB.WallType).Width / +2.0;
                  break;
                case ARDB.WallLocationLine.FinishFaceInterior:
                case ARDB.WallLocationLine.CoreInterior:
                  offsetDist = (type.Value as ARDB.WallType).Width / -2.0;
                  break;
              }
            }
            else
            {
              if (!compoundStructure.IsVerticallyHomogeneous())
                compoundStructure = ARDB.CompoundStructure.CreateSimpleCompoundStructure(compoundStructure.GetLayers());

              offsetDist = compoundStructure.GetOffsetForLocationLine(locationLine.Value);
            }

            if (offsetDist != 0.0)
              centerLine = centerLine.CreateOffset(flipped is true ? -offsetDist : offsetDist, ARDB.XYZ.BasisZ);
          }

          // Compute
          wall = Reconstruct
          (
            wall, doc.Value,
            centerLine,
            locationLine ?? ARDB.WallLocationLine.FinishFaceInterior,
            wallType,
            level.Value,
            bbox.Min.Z - level.Elevation,
            height ?? 6.0,
            flipped ?? false,
            allowJoins ?? false,
            structuralUsage ?? ARDB.Structure.StructuralWallUsage.NonBearing
          );

          DA.SetData(_Wall_, wall);
          return wall;
        }
      );
    }

    bool Reuse
    (
      ref ARDB.Wall wall,
      ARDB.Curve centerLine,
      ARDB.WallType type,
      ARDB.Level level,
      double height,
      double offset,
      bool flipped,
      ARDB.Structure.StructuralWallUsage structuralUsage
    )
    {
      if (wall is null) return false;
      if (wall.Location is ARDB.LocationCurve locationCurve && centerLine.IsSameKindAs(locationCurve.Curve))
      {
        if (!locationCurve.Curve.AlmostEquals(centerLine, GeometryTolerance.Internal.VertexTolerance))
          locationCurve.Curve = centerLine;
      }
      else return false;

      if (wall.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(wall.Document, new ARDB.ElementId[] { wall.Id }, type.Id))
        {
          if (wall.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            wall = wall.Document.GetElement(id) as ARDB.Wall;
        }
        else return false;
      }

      return true;
    }

    ARDB.Wall Create
    (
      ARDB.Document doc,
      ARDB.Curve centerLine,
      ARDB.WallType type,
      ARDB.Level level, double baseHeight,
      double unconnectedHeight,
      bool flipped,
      ARDB.Structure.StructuralWallUsage structuralUsage
    )
    {
      var newWall = ARDB.Wall.Create
      (
        doc,
        centerLine,
        type.Id,
        level.Id,
        unconnectedHeight / Revit.ModelUnits,
        baseHeight / Revit.ModelUnits - level.GetElevation(),
        flipped,
        structural: structuralUsage != ARDB.Structure.StructuralWallUsage.NonBearing
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

      return newWall;
    }

    ARDB.Wall Reconstruct
    (
      ARDB.Wall wall,
      ARDB.Document doc,
      ARDB.Curve centerLine,
      ARDB.WallLocationLine locationLine,
      ARDB.WallType type,
      ARDB.Level level, double baseHeight,
      double unconnectedHeight,
      bool flipped,
      bool allowJoins,
      ARDB.Structure.StructuralWallUsage structuralUsage
    )
    {
      if (!Reuse(ref wall, centerLine, type, level, baseHeight, unconnectedHeight, flipped, structuralUsage))
      {
        wall = wall.ReplaceElement
        (
          Create(doc, centerLine, type, level, baseHeight, unconnectedHeight, flipped, structuralUsage),
          ExcludeUniqueProperties
        );
      }

      if (wall is object)
      {
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_CONSTRAINT).Update(level.Id);
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_OFFSET).Update(baseHeight / Revit.ModelUnits);
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_HEIGHT_TYPE).Update(ARDB.ElementId.InvalidElementId);
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_USER_HEIGHT_PARAM).Update(unconnectedHeight / Revit.ModelUnits);

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

      return wall;
    }

    static bool TryGetCurveAtPlane(Curve curve, Plane plane, out ARDB.Curve projected)
    {
      if (curve.ProjectToPlane(plane) is Curve p)
      {
        var tol = GeometryTolerance.Model;

        if (p.TryGetLine(out var line, tol.VertexTolerance))
          projected = line.ToLine();
        else if (p.TryGetArc(plane, out var arc, tol.VertexTolerance))
          projected = arc.ToArc();
        else if (p.TryGetEllipse(plane, out var ellipse, out var interval, tol.VertexTolerance))
          projected = ellipse.ToCurve(interval);
        else
          projected = p.ToCurve();

        return true;
      }

      projected = default;
      return false;
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB.Structure;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.GH.Exceptions;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.Structure
{
  using External.DB.Extensions;
  using Rhino.Geometry.Intersect;

#if REVIT_2023
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalMember;
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalPanel;
#else
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalModelStick;
  using ARDB_AnalyticalElement = ARDB.Structure.AnalyticalModelStick;
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalModelSurface;
  using ARDB_AnalyticalOpening = ARDB.Structure.AnalyticalModelSurface;
#endif

  [ComponentVersion(introduced: "1.27")]
  public class AddModelElementByAnalytical : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("A373CE1F-16B3-46C0-B278-A1073D6ED1EF");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public AddModelElementByAnalytical() : base
    (
      name: "Add Model Element",
      nickname: "AE-Model",
      description: "Given an analytical element, it adds a model element representation to the active Revit document",
      category: "Revit",
      subCategory: "Structure"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document(), ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.AnalyticalElement()
        {
          Name = "Analytical Element",
          NickName = "AE",
          Description = "Analytical element",
          Access = GH_ParamAccess.item
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _ModelElement_,
          NickName = "ME",
          Description = $"Output {_ModelElement_}",
        }
      )
    };

    const string _ModelElement_ = "Model Element";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;
      if (!Params.GetData(DA, "Analytical Element", out Types.AnalyticalElement analyticalElement)) return;

      SolveInstance(DA, doc, analyticalElement);
    }

#if REVIT_2023
    private void SolveInstance(IGH_DataAccess DA, Types.Document doc, Types.AnalyticalElement analyticalElement)
    {
      switch (analyticalElement.Value.StructuralRole)
      {
        case AnalyticalStructuralRole.Unset:

        case AnalyticalStructuralRole.StructuralRolePanel:

        case AnalyticalStructuralRole.StructuralRoleMember:
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Analytical element does not match any valid structural role. {{{analyticalElement.Id}}}");
          return;

        case AnalyticalStructuralRole.StructuralRoleFloor:

          ReconstructElement<ARDB.Floor>
          (
            doc.Value, _ModelElement_, floor =>
            {
              var tol = GeometryTolerance.Model;
              var panel = analyticalElement.Value as ARDB_AnalyticalPanel;

              if (panel.Thickness == 0.0)
                throw new RuntimeException($"No floor type found with the same thickness as the analytical panel. {{{analyticalElement.Id}}}");

              // Getting the boundary
              var boundary = panel.GetOuterContour().ToPolyCurve();

              // Getting the floor type
              var collector = new ARDB.FilteredElementCollector(doc.Value);
              var floorType = collector.WhereElementIsElementType().
                OfCategory(ARDB.BuiltInCategory.OST_Floors).
                WhereParameterEqualsTo(ARDB.BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM, panel.Thickness, GeometryTolerance.Internal.DefaultTolerance).
                Cast<ARDB.FloorType>().
                FirstOrDefault();


              if (floorType is null)
                throw new RuntimeException($"No floor type found with the same thickness as the analytical panel. {{{analyticalElement.Id}}}");

              // Compute
              floor = Reconstruct
              (
                floor,
                doc.Value,
                new Curve[] { boundary },
                floorType,
                doc.Value.GetElement(panel.LevelId) as ARDB.Level,
                true
              );

              DA.SetData(_ModelElement_, floor);
              return floor;
            }
          );
          break;

        case AnalyticalStructuralRole.StructuralRoleWall:
          ReconstructElement<ARDB.Wall>
          (
            doc.Value, _ModelElement_, wall =>
            {
              var tol = GeometryTolerance.Model;
              var analyticalPanel = analyticalElement.Value as ARDB_AnalyticalPanel;

              // Getting the curve from the analytical member
              if (analyticalPanel.Thickness == 0.0)
                throw new RuntimeException($"No wall type found with the same thickness as the analytical panel: {analyticalElement.Id}");

              // Getting the boundary
              var boundary = analyticalPanel.GetOuterContour().ToPolyCurve();
              boundary.TryGetPlane(out var boundaryPlane);

              // Getting the wall type
              var collector = new ARDB.FilteredElementCollector(doc.Value);
              var wallType = collector.WhereElementIsElementType().
                OfCategory(ARDB.BuiltInCategory.OST_Walls).
                Cast<ARDB.WallType>().
                Where(t => Rhino.RhinoMath.EpsilonEquals(t.Width, analyticalPanel.Thickness, tol.DefaultTolerance)).
                FirstOrDefault();

              if (wallType is null)
                throw new RuntimeException($"No wall type found with the same thickness as the analytical panel:  {analyticalElement.Id}");

              // Getting the ref levels
              var bbox = boundary.GetBoundingBox(accurate: true);
              var baseLevel = Types.Level.FromElement(doc.Value.GetNearestLevel(bbox.Min.Z / Revit.ModelUnits));
              if (baseLevel is null)
                throw new Exceptions.RuntimeArgumentException(nameof(baseLevel), "No suitable level has been found.");

              var levelPlane = Plane.WorldXY;
              var level = baseLevel.Value as ARDB.Level;
              levelPlane.Translate(new Vector3d(0.0, 0.0, level.GetElevation() * Revit.ModelUnits));
              if (!Intersection.PlanePlane(boundaryPlane, levelPlane, out var line) || Vector3d.VectorAngle(boundaryPlane.Normal, Vector3d.ZAxis) < 3.0 * doc.Value.Application.AngleTolerance)
                throw new Exceptions.RuntimeArgumentException(nameof(boundary), "Profile can't be horizontal.");

              // Getting the angle
              var normal = boundaryPlane.Normal;
              var orientation = new Vector3d(normal.X, normal.Y, 0.0); orientation.Unitize();
              var direction = new Vector3d(orientation.Y, -orientation.X, 0.0); direction.Unitize();
              var angle = Math.Atan2(-direction * Vector3d.CrossProduct(orientation, normal), orientation * normal);

              // Compute
              wall = Reconstruct
              (
                wall,
                doc.Value,
                new Curve[] { boundary },
                orientation,
                boundaryPlane,
                line,
                angle,
                wallType,
                baseLevel.Value as ARDB.Level
              );

              DA.SetData(_ModelElement_, wall);
              return wall;
            }
          );
          break;

        case AnalyticalStructuralRole.StructuralRoleColumn:
          ReconstructElement<ARDB.FamilyInstance>
          (
            doc.Value, _ModelElement_, column =>
            {
              var tol = GeometryTolerance.Model;
              var analyticalMember = analyticalElement.Value as ARDB_AnalyticalMember;

              // Getting the curve from the analytical member
              var curve = analyticalMember.GetCurve().ToCurve();

              if (curve.IsShort(tol.ShortCurveTolerance))
                throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is too short.\nMin length is {tol.ShortCurveTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

              if (!curve.TryGetLine(out var line, tol.VertexTolerance))
                throw new RuntimeArgumentException("Curve", $"Curve should be a line like curve.\nTolerance is {tol.VertexTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

              if (line.ToZ - line.FromZ < tol.VertexTolerance)
                throw new RuntimeArgumentException("Curve", $"Curve start point must be below curve end point.\nTolerance is {tol.VertexTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

              // Getting the type
              if (!doc.Value.TryGetNamesakeElement(analyticalMember.Document, analyticalMember.SectionTypeId, out ARDB.FamilySymbol type))
                throw new RuntimeException($"No section type found in this analytical member to create a structural element: {analyticalMember.Id}");

              // Getting the top and base levels
              var bbox = curve.GetBoundingBox(accurate: true);

              var baseLevel = Types.Level.FromElement(doc.Value.GetNearestLevel(bbox.Min.Z / Revit.ModelUnits));
              if (baseLevel is null)
                throw new Exceptions.RuntimeArgumentException(nameof(baseLevel), "No suitable level has been found.");

              var topLevel = Types.Level.FromElement(doc.Value.GetNearestLevel(bbox.Max.Z / Revit.ModelUnits));
              if (topLevel is null)
                throw new Exceptions.RuntimeArgumentException(nameof(topLevel), "No suitable level has been found.");

              // Compute
              column = Reconstruct(
                column,
                doc.Value,
                line.ToLine(),
                type,
                baseLevel.Value as ARDB.Level,
                topLevel.Value as ARDB.Level
              );
              column.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE)?.Update(analyticalMember.CrossSectionRotation);

              DA.SetData(_ModelElement_, column);
              return column;
            }
          );
          break;

        case AnalyticalStructuralRole.StructuralRoleGirder:
          ReconstructElement<ARDB.FamilyInstance>
          (
            doc.Value, _ModelElement_, brace =>
            {
              var tol = GeometryTolerance.Model;
              var analyticalMember = analyticalElement.Value as ARDB_AnalyticalMember;

              // Getting the curve from the analytical member
              var curve = analyticalMember.GetCurve().ToCurve();

              if (curve.IsShort(tol.ShortCurveTolerance))
                throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is too short.\nMin length is {tol.ShortCurveTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

              if (!curve.TryGetLine(out var line, tol.VertexTolerance))
                throw new RuntimeArgumentException("Curve", $"Curve should be a line like curve.\nTolerance is {tol.VertexTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

              // Getting the type
              if (!doc.Value.TryGetNamesakeElement(analyticalMember.Document, analyticalMember.SectionTypeId, out ARDB.FamilySymbol type))
                throw new RuntimeException($"No section type found in this analytical member to create a structural element. {{{analyticalMember.Id}}}");

              // Finding the reference level
              var bbox = curve.GetBoundingBox(accurate: true);

              var refLevel = Types.Level.FromElement(doc.Value.GetNearestLevel(bbox.Center.Z / Revit.ModelUnits));
              if (refLevel is null)
                throw new Exceptions.RuntimeArgumentException(nameof(refLevel), "No suitable level has been found.");

              // Compute
              brace = Reconstruct(brace, doc.Value, line.ToLine(), type, refLevel.Value as ARDB.Level);
              brace.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE)?.Update(analyticalMember.CrossSectionRotation);

              DA.SetData(_ModelElement_, brace);
              return brace;
            }
          );
          break;

        case AnalyticalStructuralRole.StructuralRoleBeam:
          ReconstructElement<ARDB.FamilyInstance>
          (
            doc.Value, _ModelElement_, beam =>
            {
              var tol = GeometryTolerance.Model;
              var analyticalMember = analyticalElement.Value as ARDB_AnalyticalMember;

              // Getting the curve from the analytical member
              var curve = analyticalMember.GetCurve().ToCurve();

              if (curve.IsShort(tol.ShortCurveTolerance))
                throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is too short.\nMin length is {tol.ShortCurveTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

              if (curve.IsClosed(tol.VertexTolerance))
                throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is closed or end points are under tolerance.\nTolerance is {tol.VertexTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

              if (!curve.TryGetPlane(out var plane, tol.VertexTolerance))
                throw new Exceptions.RuntimeArgumentException("Curve", $"Curve should be planar.\nTolerance is {tol.VertexTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

              if (curve.GetNextDiscontinuity(Continuity.C1_continuous, curve.Domain.Min, curve.Domain.Max, Math.Cos(tol.AngleTolerance), Rhino.RhinoMath.SqrtEpsilon, out var _))
                throw new Exceptions.RuntimeArgumentException("Curve", $"Curve should be C1 continuous.\nTolerance is {Rhino.RhinoMath.ToDegrees(tol.AngleTolerance):N1}°", curve);

              // Getting the type
              if (!doc.Value.TryGetNamesakeElement(analyticalMember.Document, analyticalMember.SectionTypeId, out ARDB.FamilySymbol type))
                throw new RuntimeException($"No section type found in this analytical member to create a structural element. {{{analyticalMember.Id}}}");

              // Finding the reference level
              var bbox = curve.GetBoundingBox(accurate: true);

              var refLevel = Types.Level.FromElement(doc.Value.GetNearestLevel(bbox.Center.Z / Revit.ModelUnits));
              if (refLevel is null)
                throw new Exceptions.RuntimeArgumentException(nameof(refLevel), "No suitable level has been found.");

              // Compute
              beam = Reconstruct
              (
                beam,
                doc.Value,
                curve.ToCurve(),
                (ERDB.UnitXYZ) plane.Normal.ToXYZ(),
                type,
                refLevel.Value as ARDB.Level
              );
              beam.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE)?.Update(analyticalMember.CrossSectionRotation);

              DA.SetData(_ModelElement_, beam);
              return beam;
            }
          );
          break;
      }
    }

    #region Floor

    static readonly ARDB.BuiltInParameter[] ExcludeFloorUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.LEVEL_PARAM,
      ARDB.BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM,
      ARDB.BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL
    };

    bool Reuse(ref ARDB.Floor floor, IList<Curve> boundaries, ARDB.FloorType type, ARDB.Level level, bool structural)
    {
      if (floor is null) return false;

      if (!(floor.GetSketch() is ARDB.Sketch sketch && Types.Sketch.SetProfile(sketch, boundaries, Vector3d.ZAxis)))
        return false;

      if (floor.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(floor.Document, new ARDB.ElementId[] { floor.Id }, type.Id))
        {
          if (floor.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            floor = floor.Document.GetElement(id) as ARDB.Floor;
        }
        else return false;
      }

      bool succeed = true;
      succeed &= floor.get_Parameter(ARDB.BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL).Update(structural ? 1 : 0);
      succeed &= floor.get_Parameter(ARDB.BuiltInParameter.LEVEL_PARAM).Update(level.Id);

      return succeed;
    }

    ARDB.Floor Create(ARDB.Document document, IList<Curve> boundary, ARDB.FloorType type, ARDB.Level level, bool structural)
    {
#if REVIT_2022
      var curveLoops = boundary.ConvertAll(GeometryEncoder.ToCurveLoop);
      var floor = ARDB.Floor.Create(document, curveLoops, type.Id, level.Id, structural, default, 0.0);
#else
      var curveArray = boundary[0].ToBoundedCurveArray();
      var floor = document.Create.NewFloor(curveArray, type, level, structural, ARDB.XYZ.BasisZ);
#endif

      floor.get_Parameter(ARDB.BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL)?.Update(structural);
      return floor;
    }

    ARDB.Floor Reconstruct(ARDB.Floor floor, ARDB.Document doc, IList<Curve> boundary, ARDB.FloorType type, ARDB.Level level, bool structural)
    {
      if (!Reuse(ref floor, boundary, type, level, structural))
      {
        floor = floor.ReplaceElement
        (
          Create(doc, boundary, type, level, structural),
          ExcludeFloorUniqueProperties
        );
      }

      return floor;
    }

    #endregion

    #region Wall

    static readonly ARDB.BuiltInParameter[] ExcludeWallUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.WALL_KEY_REF_PARAM,
      ARDB.BuiltInParameter.WALL_HEIGHT_TYPE,
      ARDB.BuiltInParameter.WALL_USER_HEIGHT_PARAM,
      ARDB.BuiltInParameter.WALL_BASE_CONSTRAINT,
      ARDB.BuiltInParameter.WALL_BASE_OFFSET,
#if REVIT_2021
      ARDB.BuiltInParameter.WALL_CROSS_SECTION,
      ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL,
#endif
    };

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
      if (slantAngle == 0.0)
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
        if (!mid0.AlmostEqualPoints(mid1))
        {
          element.Pinned = false;
          location.Move(mid1 - mid0);
        }

        var angle0 = Vector3d.VectorAngle(Vector3d.XAxis, locationLine.Direction.ToVector3d(), Vector3d.ZAxis);
        var angle1 = Vector3d.VectorAngle(Vector3d.XAxis, line.Direction, Vector3d.ZAxis);
        var angle = angle1 - angle0;
        if (Math.Abs(angle) > GeometryTolerance.Internal.DefaultTolerance)
        {
          element.Pinned = false;
          using (var axis = ARDB.Line.CreateUnbound(mid1, ARDB.XYZ.BasisZ))
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

    ARDB.Wall Create(ARDB.Document document, IList<Curve> loops, Vector3d normal, Line line, double angle, ARDB.WallType type, ARDB.Level level)
    {
      var boundaries = loops.
          SelectMany(x => GeometryEncoder.ToCurveMany(x)).
          SelectMany(CurveExtension.ToBoundedCurves).
          ToList();

      // Wall.Create requires vertical boundaries.
      if (angle != 0.0)
      {
        for (int l = 0; l < loops.Count; ++l)
          loops[l].Rotate(-angle, line.Direction, line.From);
      }

      var wall = ARDB.Wall.Create(document, boundaries, type.Id, level.Id, true, normal.ToXYZ());

      // We turn off wall joins by default
      ARDB.WallUtils.DisallowWallJoinAtEnd(wall, 0);
      ARDB.WallUtils.DisallowWallJoinAtEnd(wall, 1);

      // Walls are created with the last LocationLine used in the Revit editor!!
      wall.get_Parameter(ARDB.BuiltInParameter.WALL_KEY_REF_PARAM).Update(ARDB.WallLocationLine.WallCenterline);

      // We turn off analytical model by default
      wall.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);

#if REVIT_2021
      if (angle == 0.0)
      {
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).Update(ARDB.WallCrossSection.Vertical);
      }
      else
      {
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).Update(ARDB.WallCrossSection.SingleSlanted);
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL).Update(angle);
      }
#endif

      return wall;
    }

    ARDB.Wall Reconstruct(ARDB.Wall wall, ARDB.Document doc, IList<Curve> boundary, Vector3d normal, Plane plane, Line line, double angle, ARDB.WallType type, ARDB.Level level)
    {
      if (!Reuse(ref wall, boundary, plane, line, angle, type))
      {
        wall = wall.ReplaceElement
        (
          Create(doc, boundary, normal, line, angle, type, level),
          ExcludeWallUniqueProperties
        );
      }

      if (wall is object)
      {
        var bbox = Rhino.Geometry.BoundingBox.Empty;
        foreach (var geometry in boundary)
          bbox.Union(geometry.GetBoundingBox(true));

        wall.get_Parameter(ARDB.BuiltInParameter.WALL_HEIGHT_TYPE).Update(ARDB.ElementId.InvalidElementId);
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_USER_HEIGHT_PARAM).Update((bbox.Max.Z - bbox.Min.Z) / Revit.ModelUnits);
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_CONSTRAINT).Update(level.Id);
        wall.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_OFFSET).Update(bbox.Min.Z / Revit.ModelUnits - level.GetElevation());
      }

      return wall;
    }

    #endregion

    #region Column

    static readonly ARDB.BuiltInParameter[] ExcludeColumnUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.SLANTED_COLUMN_TYPE_PARAM,
      ARDB.BuiltInParameter.INSTANCE_MOVE_BASE_WITH_GRIDS,
      ARDB.BuiltInParameter.INSTANCE_MOVE_TOP_WITH_GRIDS,
      ARDB.BuiltInParameter.FAMILY_BASE_LEVEL_PARAM,
      ARDB.BuiltInParameter.FAMILY_TOP_LEVEL_PARAM,
      ARDB.BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM,
      ARDB.BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM,
      ARDB.BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE,
    };

    bool Reuse
    (
      ARDB.FamilyInstance column,
      ARDB.FamilySymbol type
    )
    {
      if (column is null) return false;
      if (type.Id != column.GetTypeId()) column.ChangeTypeId(type.Id);

      return true;
    }

    ARDB.FamilyInstance Create(ARDB.Document doc, ARDB.Curve curve, ARDB.FamilySymbol type, ARDB.Level level)
    {
      if (!type.IsActive) type.Activate();

      var list = new List<Autodesk.Revit.Creation.FamilyInstanceCreationData>(1)
      {
        new Autodesk.Revit.Creation.FamilyInstanceCreationData
        (
          curve: curve,
          symbol: type,
          level: level,
          structuralType: ARDB.Structure.StructuralType.Column
        )
      };

      var ids = doc.Create().NewFamilyInstances2(list);
      if (ids.Count == 1)
      {
        var instance = doc.GetElement(ids.First()) as ARDB.FamilyInstance;

        // We turn analytical model off by default
        instance.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);
        return instance;
      }

      throw new Exceptions.RuntimeArgumentException("Type", $"Type '{type.FamilyName} : {type.Name}' is not a valid structural column type.");
    }

    ARDB.FamilyInstance Reconstruct
    (
      ARDB.FamilyInstance column,
      ARDB.Document doc,
      ARDB.Curve curve,
      ARDB.FamilySymbol type,
      ARDB.Level baselevel,
      ARDB.Level topLevel
    )
    {
      if (!Reuse(column, type))
      {
        column = column.ReplaceElement
        (
          Create(doc, curve, type, baselevel),
          ExcludeColumnUniqueProperties
        );

        column.Document.Regenerate();
      }

      column.get_Parameter(ARDB.BuiltInParameter.SLANTED_COLUMN_TYPE_PARAM).Update(2);
      column.get_Parameter(ARDB.BuiltInParameter.INSTANCE_MOVE_BASE_WITH_GRIDS)?.Update(false);
      column.get_Parameter(ARDB.BuiltInParameter.INSTANCE_MOVE_TOP_WITH_GRIDS)?.Update(false);
      column.get_Parameter(ARDB.BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).Update(baselevel.Id);
      column.get_Parameter(ARDB.BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).Update(topLevel.Id);
      column.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE)?.Update(0.0);

      if (column.Location is ARDB.LocationCurve locationCurve)
      {
        if (!locationCurve.Curve.AlmostEquals(curve, GeometryTolerance.Internal.VertexTolerance))
        {
          curve.TryGetLocation(out var origin, out var basisX, out var basisY);
          column.SetLocation(origin, basisX, basisY);

          locationCurve.Curve = curve;

          var startPoint = curve.GetEndPoint(ERDB.CurveEnd.Start);
          var endPoint = curve.GetEndPoint(ERDB.CurveEnd.End);
          column.get_Parameter(ARDB.BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).Update(Math.Min(startPoint.Z, endPoint.Z) - baselevel.ProjectElevation);
          column.get_Parameter(ARDB.BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).Update(Math.Max(startPoint.Z, endPoint.Z) - topLevel.ProjectElevation);
        }
      }

      return column;
    }

    #endregion

    #region Beam

    static readonly ARDB.BuiltInParameter[] ExcludeBeamUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
      ARDB.BuiltInParameter.Y_JUSTIFICATION,
      ARDB.BuiltInParameter.Z_JUSTIFICATION,
      ARDB.BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE,
    };

    bool Reuse
    (
      ARDB.FamilyInstance beam,
      ERDB.UnitXYZ normal,
      ARDB.FamilySymbol type
    )
    {
      if (beam is null) return false;
      if (type.Id != beam.GetTypeId()) beam.ChangeTypeId(type.Id);

      var (_, basisX, basisY) = beam.GetLocation();
      ERDB.UnitXYZ.Orthonormal(basisX, basisY, out var basisZ);

      var wasVertical = basisZ.IsPerpendicularTo(ERDB.UnitXYZ.BasisZ);
      var isVertical = normal.IsPerpendicularTo(ERDB.UnitXYZ.BasisZ);
      return isVertical == wasVertical;
    }

    ARDB.FamilyInstance Create
    (
      ARDB.Document doc,
      ARDB.Curve curve,
      ARDB.FamilySymbol type
    )
    {
      if (!type.IsActive) type.Activate();

      var list = new List<Autodesk.Revit.Creation.FamilyInstanceCreationData>(1)
      {
        new Autodesk.Revit.Creation.FamilyInstanceCreationData
        (
          curve: curve,
          symbol: type,
          level: default, // No work-plane based.
          structuralType: ARDB.Structure.StructuralType.Beam
        )
      };

      var ids = doc.Create().NewFamilyInstances2(list);
      if (ids.Count == 1)
      {
        var instance = doc.GetElement(ids.First()) as ARDB.FamilyInstance;

        // We turn analytical model off by default
        instance.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);
        return instance;
      }

      throw new Exceptions.RuntimeArgumentException("Type", $"Type '{type.FamilyName} : {type.Name}' is not a valid curve driven structural type.");
    }

    ARDB.FamilyInstance Reconstruct
    (
      ARDB.FamilyInstance beam,
      ARDB.Document doc,
      ARDB.Curve curve,
      ERDB.UnitXYZ normal,
      ARDB.FamilySymbol type,
      ARDB.Level level
    )
    {
      if (!Reuse(beam, normal, type))
      {
        beam = beam.ReplaceElement
        (
          Create(doc, curve, type),
          ExcludeBeamUniqueProperties
        );

        beam.Document.Regenerate();
      }

      if (level is object)
      {
        using (var referenceLevel = beam.get_Parameter(ARDB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM))
        {
          if (!referenceLevel.IsReadOnly) referenceLevel.Update(level.Id);
        }
      }

      if (ARDB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(beam, ERDB.CurveEnd.Start))
        ARDB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(beam, ERDB.CurveEnd.Start);
      if (ARDB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(beam, ERDB.CurveEnd.End))
        ARDB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(beam, ERDB.CurveEnd.End);

      if (beam.ExtensionUtility is ARDB.IExtension extension)
      {
        if (extension.get_IsMiterLocked(ERDB.CurveEnd.Start))
          extension.set_IsMiterLocked(ERDB.CurveEnd.Start, false);
        if (extension.get_IsMiterLocked(ERDB.CurveEnd.End))
          extension.set_IsMiterLocked(ERDB.CurveEnd.End, false);

        if (extension.get_SymbolicExtended(ERDB.CurveEnd.Start))
          extension.set_SymbolicExtended(ERDB.CurveEnd.Start, false);
        if (extension.get_SymbolicExtended(ERDB.CurveEnd.End))
          extension.set_SymbolicExtended(ERDB.CurveEnd.End, false);

        if (extension.get_Extended(ERDB.CurveEnd.Start))
          extension.set_Extended(ERDB.CurveEnd.Start, false);
        if (extension.get_Extended(ERDB.CurveEnd.End))
          extension.set_Extended(ERDB.CurveEnd.End, false);
      }

      beam.get_Parameter(ARDB.BuiltInParameter.Y_JUSTIFICATION)?.Update(ARDB.Structure.YJustification.Origin);
      beam.get_Parameter(ARDB.BuiltInParameter.Z_JUSTIFICATION)?.Update(ARDB.Structure.ZJustification.Origin);
      beam.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE)?.Update(0.0);

      if (beam.Location is ARDB.LocationCurve locationCurve)
      {
        if (!locationCurve.Curve.AlmostEquals(curve, GeometryTolerance.Internal.VertexTolerance))
        {
          curve.TryGetLocation(out var origin, out var basisX, out var basisY);
          beam.SetLocation(origin, basisX, basisY);
          locationCurve.Curve = curve;
        }
      }

      return beam;
    }

    #endregion

    #region Brace

    static readonly ARDB.BuiltInParameter[] ExcludeBraceUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
      ARDB.BuiltInParameter.Y_JUSTIFICATION,
      ARDB.BuiltInParameter.Z_JUSTIFICATION,
      ARDB.BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE,
    };

    ARDB.FamilyInstance CreateBrace
    (
      ARDB.Document doc,
      ARDB.Curve curve,
      ARDB.FamilySymbol type
    )
    {
      if (!type.IsActive) type.Activate();

      var list = new List<Autodesk.Revit.Creation.FamilyInstanceCreationData>(1)
      {
        new Autodesk.Revit.Creation.FamilyInstanceCreationData
        (
          curve: curve,
          symbol: type,
          level: default, // No work-plane based.
          structuralType: ARDB.Structure.StructuralType.Brace
        )
      };

      var ids = doc.Create().NewFamilyInstances2(list);
      if (ids.Count == 1)
      {
        var instance = doc.GetElement(ids.First()) as ARDB.FamilyInstance;

        // We turn analytical model off by default
        instance.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);
        return instance;
      }

      throw new Exceptions.RuntimeArgumentException("Type", $"Type '{type.FamilyName} : {type.Name}' is not a valid curve driven structural type.");
    }

    ARDB.FamilyInstance Reconstruct
    (
      ARDB.FamilyInstance brace,
      ARDB.Document doc,
      ARDB.Curve curve,
      ARDB.FamilySymbol type,
      ARDB.Level level
    )
    {
      if (!Reuse(brace, type))
      {
        brace = brace.ReplaceElement
        (
          CreateBrace(doc, curve, type),
          ExcludeBraceUniqueProperties
        );

        brace.Document.Regenerate();
      }

      if (level is object)
      {
        using (var referenceLevel = brace.get_Parameter(ARDB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM))
        {
          if (!referenceLevel.IsReadOnly) referenceLevel.Update(level.Id);
        }
      }

      if (ARDB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(brace, ERDB.CurveEnd.Start))
        ARDB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(brace, ERDB.CurveEnd.Start);
      if (ARDB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(brace, ERDB.CurveEnd.End))
        ARDB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(brace, ERDB.CurveEnd.End);

      if (brace.ExtensionUtility is ARDB.IExtension extension)
      {
        if (extension.get_IsMiterLocked(ERDB.CurveEnd.Start))
          extension.set_IsMiterLocked(ERDB.CurveEnd.Start, false);
        if (extension.get_IsMiterLocked(ERDB.CurveEnd.End))
          extension.set_IsMiterLocked(ERDB.CurveEnd.End, false);

        if (extension.get_SymbolicExtended(ERDB.CurveEnd.Start))
          extension.set_SymbolicExtended(ERDB.CurveEnd.Start, false);
        if (extension.get_SymbolicExtended(ERDB.CurveEnd.End))
          extension.set_SymbolicExtended(ERDB.CurveEnd.End, false);

        if (extension.get_Extended(ERDB.CurveEnd.Start))
          extension.set_Extended(ERDB.CurveEnd.Start, false);
        if (extension.get_Extended(ERDB.CurveEnd.End))
          extension.set_Extended(ERDB.CurveEnd.End, false);
      }

      brace.get_Parameter(ARDB.BuiltInParameter.Y_JUSTIFICATION)?.Update(ARDB.Structure.YJustification.Origin);
      brace.get_Parameter(ARDB.BuiltInParameter.Z_JUSTIFICATION)?.Update(ARDB.Structure.ZJustification.Origin);
      brace.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE)?.Update(0.0);

      if (brace.Location is ARDB.LocationCurve locationCurve)
      {
        if (!locationCurve.Curve.AlmostEquals(curve, GeometryTolerance.Internal.VertexTolerance))
        {
          curve.TryGetLocation(out var origin, out var basisX, out var basisY);
          brace.SetLocation(origin, basisX, basisY);
          locationCurve.Curve = curve;
        }
      }

      return brace;
    }

    #endregion

#else
    bool Reuse(ARDB.Element target, ARDB.Element source, ARDB.View view)
    {
      if (target is null) return false;
      if (target.OwnerViewId != (view?.Id ?? source.OwnerViewId)) return false;

      if (target.GetType() != source.GetType()) return false;
      if (target.Category.Id != source.Category.Id) return false;
      if (target.ViewSpecific != source.ViewSpecific) return false;

      var targetLocation = target.Location;
      var sourceLocation = source.Location;

      if (targetLocation.GetType() != sourceLocation.GetType()) return false;
      if (targetLocation is ARDB.LocationCurve xCurve && !(sourceLocation as ARDB.LocationCurve).Curve.IsSameKindAs(xCurve.Curve)) return false;

      // TODO : Implement a DeepCopy here
      // Duplicate any missing type, material on demand
      //x.DeepCopyParametersFrom(element.Value);

      target.GetNomen(out var nomenParameter);
      if (nomenParameter.IsValid())
        target.CopyParametersFrom(source, new ARDB.BuiltInParameter[] { nomenParameter });
      else
        target.CopyParametersFrom(source);

      return true;
    }

    private void SolveInstance(IGH_DataAccess DA, Types.Document doc, Types.AnalyticalElement analyticalElement)
    {
      if (analyticalElement.PhysicalElements.FirstOrDefault() is Types.GraphicalElement physicalElement)
      {
        if (doc.Value.IsEquivalent(physicalElement.Document))
        {
          DA.SetData(_ModelElement_, physicalElement);
        }
        else
        {
          var clone = Types.GraphicalElement.FromElement
          (
            ReconstructElement<ARDB.Element>
            (
              doc.Value, _ModelElement_,
              x =>
              {
                if (physicalElement.Value is ARDB.Element elementValue)
                {
                  if (!Reuse(x, elementValue, null))
                  {
                    x = elementValue.CloneElement(doc.Value, null);
                  }

                  return x;
                }

                return default;
              }
            )
          ) as Types.GraphicalElement;

          if (clone is object && clone.Pinned is true)
          {
            StartTransaction(clone.Document);

            switch (analyticalElement.Value.Location)
            {
              case ARDB.LocationCurve locationCurve:
                clone.SetCurve(locationCurve.Curve.ToCurve(), keepJoins: false);
                break;

              default:
                var physicalLocation = physicalElement.Location;
                if (!clone.Location.EpsilonEquals(physicalLocation, GeometryTolerance.Model.VertexTolerance))
                  clone.SetLocation(physicalLocation);

                break;
            }
          }

          DA.SetData(_ModelElement_, clone);
        }
      }
    }
#endif
  }
}

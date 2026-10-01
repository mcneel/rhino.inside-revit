using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;
  using External.DB.Extensions;
  using Kernel.Attributes;
  using RhinoInside.Revit.GH.Exceptions;


  [ComponentVersion(introduced: "1.37"), ComponentRevitAPIVersion(min: "2022.0")]
  public class AddSlopedFloor : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("FD260BDC-9F29-4410-8783-EC68A0500495");
    public override GH_Exposure Exposure => SDKCompliancy(GH_Exposure.primary);

    public AddSlopedFloor() : base
    (
      name: "Add Sloped Floor",
      nickname: "SlopedFloor",
      description: "Given its outline curve and a slope arrow, it adds a sloped Floor element to the active Revit document",
      category: "Revit",
      subCategory: "Architecture"
    )
    { }

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
          Name = "Boundary",
          NickName = "B",
          Description = "Floor boundary profile",
          Access = GH_ParamAccess.list
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
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_Floors
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
       (
        new Parameters.Level
        {
          Name = "Level",
          NickName = "L",
          Description = "Floor base level",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
       (
        new Param_Boolean
        {
          Name = "Structural",
          NickName = "S",
          Description = "Whether floor is structural or not",
        }.SetDefaultVale(true), ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Line
        {
          Name = "Slope Arrow",
          NickName = "SA",
          Description = "Slope direction in plan. It starts where the floor is at its height and points uphill",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Angle
        {
          Name = "Slope",
          NickName = "SL",
          Description = "Slope angle along the slope arrow",
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Floor()
        {
          Name = _Floor_,
          NickName = _Floor_.Substring(0, 1),
          Description = $"Output {_Floor_}",
        }
      )
    };

    const string _Floor_ = "Floor";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.LEVEL_PARAM,
      ARDB.BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM,
      ARDB.BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
#if REVIT_2022
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Floor>
      (
        doc.Value, _Floor_, floor =>
        {
          // Input
          if (!Params.GetDataList(DA, "Boundary", out IList<Curve> boundary)) return null;

          var tol = GeometryTolerance.Model;
          for (int index = 0; index < boundary.Count; ++index)
          {
            var loop = boundary[index];
            if (loop is null) return null;
            if
            (
              loop.IsShort(tol.ShortCurveTolerance) ||
              !loop.IsClosed ||
              !loop.TryGetPlane(out var plane, tol.VertexTolerance) ||
              plane.ZAxis.IsParallelTo(Vector3d.ZAxis, tol.AngleTolerance) == 0
            )
              throw new RuntimeArgumentException(nameof(boundary), "Boundary loop curves should be a set of valid horizontal, coplanar and closed curves.", boundary);

            boundary[index] = loop.Simplify(CurveSimplifyOptions.All & ~CurveSimplifyOptions.Merge, tol.VertexTolerance, tol.AngleTolerance) ?? loop;
          }

          var bbox = Rhino.Geometry.BoundingBox.Empty;
          foreach (var geometry in boundary)
            bbox.Union(geometry.GetBoundingBox(true));

          if (!bbox.IsValid) return null;

          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out Types.HostObjectType type, doc, ARDB.ElementTypeGroup.FloorType)) return null;

          if (!(type.Value is ARDB.FloorType floorType))
            throw new RuntimeArgumentException(nameof(type), $"Type '{type.Nomen}' is not a valid floor type.");
          else if (floorType.IsFoundationSlab)
            throw new RuntimeArgumentException(nameof(type), $"Type '{type.Nomen}' is not a valid floor type.{OS.NewLine}Consider use 'Add Foundation (Slab)' component.");

          if (!Parameters.Level.GetDataOrDefault(this, DA, "Level", out Types.Level level, doc, bbox.Min.Z)) return null;
          if (!Params.TryGetData(DA, "Structural", out bool? structural)) return null;
          if (!Params.TryGetData(DA, "Slope Arrow", out Line? arrow)) return null;
          if (!Params.TryGetData(DA, "Slope", out double? angle)) return null;

          if (angle.HasValue && Params.Input<Param_Number>("Slope")?.UseDegrees == true)
            angle = Rhino.RhinoMath.ToRadians(angle.Value);

          if (angle.HasValue && Math.Abs(angle.Value) >= Rhino.RhinoMath.ToRadians(89.0))
            throw new RuntimeArgumentException("Slope", "Slope should be less than 89°.", angle.Value);

          if (arrow.HasValue && arrow.Value.Length < tol.ShortCurveTolerance)
            arrow = default;

          if (arrow.HasValue && !angle.HasValue)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "'Slope Arrow' has no 'Slope', so the floor is flat.");
          else if (!arrow.HasValue && angle.HasValue && angle.Value != 0.0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "'Slope' needs a 'Slope Arrow' to know which way is up, so the floor is flat.");

          // The arrow lies on the boundary plane. Revit keeps the slope as rise over run.
          var sloped = arrow.HasValue && angle.HasValue;
          var slopeArrow = sloped ?
            new Line
            (
              new Point3d(arrow.Value.From.X, arrow.Value.From.Y, bbox.Min.Z),
              new Point3d(arrow.Value.To.X, arrow.Value.To.Y, bbox.Min.Z)
            ) :
            default(Line?);
          var slope = sloped ? Math.Tan(angle.Value) : 0.0;

          // Compute
          floor = Reconstruct(floor, doc.Value, boundary, bbox, floorType, level.Value, structural ?? true, slopeArrow, slope);

          if (floor is object)
          {
            var heightAboveLevel = bbox.Min.Z / Revit.ModelUnits - level.Value.GetElevation();
            floor.get_Parameter(ARDB.BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)?.Update(heightAboveLevel);
          }

          DA.SetData(_Floor_, floor);
          return floor;
        }
      );
#endif
    }

#if REVIT_2022
    static ARDB.CurveElement GetSlopeArrow(ARDB.Sketch sketch) =>
      new Types.Sketch(sketch).SlopeArrow?.Value as ARDB.CurveElement;

    bool Reuse
    (
      ref ARDB.Floor floor, IList<Curve> boundaries,
      ARDB.FloorType type, ARDB.Level level, bool structural,
      Line? slopeArrow, double slope
    )
    {
      if (floor is null) return false;
      if (!(floor.GetSketch() is ARDB.Sketch sketch)) return false;

      // A floor created without a slope arrow can not get one afterwards.
      if (!(GetSlopeArrow(sketch) is ARDB.CurveElement arrow)) return false;

      if (!Types.Sketch.SetProfile(sketch, boundaries, Vector3d.ZAxis))
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

      // A new arrow moves the current one, at the height the sketch keeps it.
      // No arrow leaves it where it is, with no slope.
      if (slopeArrow.HasValue && arrow.GeometryCurve is ARDB.Curve current)
      {
        var z = current.GetEndPoint(0).Z;
        var start = slopeArrow.Value.From.ToXYZ();
        var end = slopeArrow.Value.To.ToXYZ();
        start = new ARDB.XYZ(start.X, start.Y, z);
        end = new ARDB.XYZ(end.X, end.Y, z);

        var tol = GeometryTolerance.Internal.VertexTolerance;
        if (!current.GetEndPoint(0).IsAlmostEqualTo(start, tol) || !current.GetEndPoint(1).IsAlmostEqualTo(end, tol))
          arrow.SetGeometryCurve(ARDB.Line.CreateBound(start, end), overrideJoins: true);
      }

      succeed &= arrow.get_Parameter(ARDB.BuiltInParameter.ROOF_SLOPE)?.Update(slope) == true;
      arrow.get_Parameter(ARDB.BuiltInParameter.SLOPE_START_HEIGHT)?.Update(0.0);

      return succeed;
    }

    ARDB.Floor Create
    (
      ARDB.Document document, IList<Curve> boundary, BoundingBox bbox,
      ARDB.FloorType type, ARDB.Level level, bool structural,
      Line? slopeArrow, double slope
    )
    {
      var curveLoops = boundary.ConvertAll(GeometryEncoder.ToCurveLoop);

      // A flat floor gets an arrow of slope 0 across its middle, so a slope can be added later.
      var line = slopeArrow ?? Across(bbox);
      var arrow = ARDB.Line.CreateBound(line.From.ToXYZ(), line.To.ToXYZ());

      var floor = default(ARDB.Floor);
      try
      {
        floor = ARDB.Floor.Create(document, curveLoops, type.Id, level.Id, structural, arrow, slope);
      }
      catch (Autodesk.Revit.Exceptions.ArgumentException) when (!slopeArrow.HasValue)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Floor was created without a slope arrow, so adding a slope later will replace it.");
        floor = ARDB.Floor.Create(document, curveLoops, type.Id, level.Id, structural, default, 0.0);
      }

      // We turn off analytical model off by default
      floor.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);
      floor.get_Parameter(ARDB.BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL)?.Update(structural);
      return floor;
    }

    /// <summary>A line through the middle of the outline, half as long as it is wide.</summary>
    static Line Across(BoundingBox bbox)
    {
      var center = bbox.Center;
      var size = bbox.Diagonal;

      return size.X >= size.Y ?
        new Line(new Point3d(center.X - size.X / 4.0, center.Y, bbox.Min.Z), new Point3d(center.X + size.X / 4.0, center.Y, bbox.Min.Z)) :
        new Line(new Point3d(center.X, center.Y - size.Y / 4.0, bbox.Min.Z), new Point3d(center.X, center.Y + size.Y / 4.0, bbox.Min.Z));
    }

    ARDB.Floor Reconstruct
    (
      ARDB.Floor floor, ARDB.Document doc, IList<Curve> boundary, BoundingBox bbox,
      ARDB.FloorType type, ARDB.Level level, bool structural,
      Line? slopeArrow, double slope
    )
    {
      if (!Reuse(ref floor, boundary, type, level, structural, slopeArrow, slope))
      {
        floor = floor.ReplaceElement
        (
          Create(doc, boundary, bbox, type, level, structural, slopeArrow, slope),
          ExcludeUniqueProperties
        );
      }

      return floor;
    }
#endif
  }
}

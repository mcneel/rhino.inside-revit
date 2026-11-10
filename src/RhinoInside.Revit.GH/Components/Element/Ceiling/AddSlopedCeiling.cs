using System;
using System.Collections.Generic;
using System.Linq;
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
  using RhinoInside.Revit.GH.Exceptions;

  [ComponentVersion(introduced: "1.37"), ComponentRevitAPIVersion(min: "2022.0")]
  public class AddSlopedCeiling : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("F3E8A938-BE33-4EA7-8C71-8FE46B2D816B");
    public override GH_Exposure Exposure => SDKCompliancy(GH_Exposure.primary | GH_Exposure.obscure);

    public AddSlopedCeiling() : base
    (
      name: "Add Ceiling (Sloped)",
      nickname: "S-Ceiling",
      description: "Given its outline curve and a slope arrow, it adds a sloped Ceiling element to the active Revit document",
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
          Description = "Ceiling boundary profile",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
       (
        new Parameters.HostObjectType
        {
          Name = "Type",
          NickName = "T",
          Description = "Ceiling type",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_Ceilings
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
       (
        new Parameters.Level
        {
          Name = "Level",
          NickName = "L",
          Description = "Ceiling base level",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Line
        {
          Name = "Slope Arrow",
          NickName = "SA",
          Description = "Slope direction in plan. It starts where the ceiling is at its height and points uphill",
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
        new Parameters.Ceiling()
        {
          Name = _Ceiling_,
          NickName = _Ceiling_.Substring(0, 1),
          Description = $"Output {_Ceiling_}",
        }
      )
    };

    const string _Ceiling_ = "Ceiling";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.LEVEL_PARAM,
      ARDB.BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
#if REVIT_2022
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Ceiling>
      (
        doc.Value, _Ceiling_, ceiling =>
        {
          // Input
          if (!Params.GetDataList(DA, "Boundary", out IList<Curve> boundary)) return null;

          var tol = GeometryTolerance.Model;
          var normal = Vector3d.Zero;
          for (int index = 0; index < boundary.Count; ++index)
          {
            var loop = boundary[index];
            if (loop is null) return null;
            if
            (
              loop.IsShort(tol.ShortCurveTolerance) ||
              !loop.IsClosed ||
              !loop.TryGetPlane(out var plane, tol.VertexTolerance) ||
              plane.Normal.IsPerpendicularTo(Vector3d.ZAxis, tol.AngleTolerance) ||
              (!normal.IsZero && plane.Normal.IsParallelTo(normal, tol.AngleTolerance) == 0)
            )
              throw new RuntimeArgumentException(nameof(boundary), "Boundary loop curves should be a set of valid non-vertical coplanar and closed curves.", boundary);

            if (normal.IsZero) normal = plane.Normal;

            boundary[index] = loop.Simplify(CurveSimplifyOptions.All & ~CurveSimplifyOptions.Merge, tol.VertexTolerance, tol.AngleTolerance) ?? loop;
          }

          var bbox = Rhino.Geometry.BoundingBox.Empty;
          foreach (var geometry in boundary)
            bbox.Union(geometry.GetBoundingBox(true));

          if (!bbox.IsValid) return null;

          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out Types.HostObjectType type, doc, ARDB.ElementTypeGroup.CeilingType)) return null;

          if (!(type.Value is ARDB.CeilingType ceilingType))
            throw new RuntimeArgumentException(nameof(type), $"Type '{type.Nomen}' is not a valid ceiling type.");

          if (!Parameters.Level.GetDataOrDefault(this, DA, "Level", out Types.Level level, doc, bbox.Min.Z)) return null;
          if (!Params.TryGetData(DA, "Slope Arrow", out Line? arrow)) return null;
          if (!Params.TryGetData(DA, "Slope", out double? angle)) return null;

          if (angle.HasValue && Params.Input<Param_Number>("Slope")?.UseDegrees == true)
            angle = Rhino.RhinoMath.ToRadians(angle.Value);

          if (arrow.HasValue && (arrow.Value.Length < tol.ShortCurveTolerance || !arrow.Value.IsValid))
            arrow = null;
          else
            arrow = arrow ?? Across(boundary[0]);

          if (!arrow.HasValue && angle.HasValue && angle.Value != 0.0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "'Slope' needs a 'Slope Arrow' to know which way is up, so the ceiling is flat.");

          // The arrow lies on the boundary plane.
          var slopeArrow = arrow.HasValue ?
            new Line
            (
              new Point3d(arrow.Value.From.X, arrow.Value.From.Y, bbox.Min.Z),
              new Point3d(arrow.Value.To.X, arrow.Value.To.Y, bbox.Min.Z)
            ) :
            default(Line?);

          // Revit keeps the slope as rise over run.
          var slope = angle.HasValue ? Math.Tan(angle.Value):
                      arrow.HasValue ? (arrow.Value.ToZ - arrow.Value.FromZ) / new Vector2d(arrow.Value.Direction.X, arrow.Value.Direction.Y).Length:
                      double.NaN;

          boundary = boundary.Select(x => x.ProjectToPlane(new Plane(boundary[0].PointAtStart, Vector3d.XAxis, Vector3d.YAxis))).ToArray();

          // Compute
          ceiling = Reconstruct(ceiling, doc.Value, boundary, bbox, ceilingType, level.Value, slopeArrow, slope);

          if (ceiling is object)
          {
            var heightAboveLevel = (arrow?.FromZ ?? bbox.Min.Z) / Revit.ModelUnits - level.Value.GetElevation();
            ceiling.get_Parameter(ARDB.BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM)?.Update(heightAboveLevel);
          }

          DA.SetData(_Ceiling_, ceiling);
          return ceiling;
        }
      );
#endif
    }

#if REVIT_2022
    static ARDB.CurveElement GetSlopeArrow(ARDB.Sketch sketch) =>
      new Types.Sketch(sketch).SlopeArrow?.Value;

    bool Reuse
    (
      ref ARDB.Ceiling ceiling, IList<Curve> boundaries,
      ARDB.CeilingType type, ARDB.Level level,
      Line? slopeArrow, double slope
    )
    {
      if (ceiling is null) return false;
      if (!(ceiling.GetSketch() is ARDB.Sketch sketch)) return false;

      // A ceiling created without a slope arrow can not get one afterwards.
      if (!(GetSlopeArrow(sketch) is ARDB.CurveElement arrow)) return false;

      if (!Types.Sketch.SetProfile(sketch, boundaries, Vector3d.ZAxis))
        return false;

      if (ceiling.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(ceiling.Document, new ARDB.ElementId[] { ceiling.Id }, type.Id))
        {
          if (ceiling.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            ceiling = ceiling.Document.GetElement(id) as ARDB.Ceiling;
        }
        else return false;
      }

      bool succeed = true;
      succeed &= ceiling.get_Parameter(ARDB.BuiltInParameter.LEVEL_PARAM).Update(level.Id);

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

      arrow.get_Parameter(ARDB.BuiltInParameter.SPECIFY_SLOPE_OR_OFFSET).Update(1);
      succeed &= arrow.get_Parameter(ARDB.BuiltInParameter.ROOF_SLOPE)?.Update(slope) == true;

      return succeed;
    }

    ARDB.Ceiling Create
    (
      ARDB.Document document, IList<Curve> boundary, BoundingBox bbox,
      ARDB.CeilingType type, ARDB.Level level,
      Line? slopeArrow, double slope
    )
    {
      var line = slopeArrow ?? Across(boundary[0]);
      var curveLoops = boundary.ConvertAll(GeometryEncoder.ToCurveLoop);
      return ARDB.Ceiling.Create(document, curveLoops, type.Id, level.Id, line.ToLine(), slope);
    }

    static Line Across(Curve curve)
    {
      if (curve.TryGetPlane(out var plane))
      {
        if (plane.Normal.IsParallelTo(Vector3d.ZAxis, GeometryTolerance.Model.AngleTolerance) != 0)
        {
          plane = new Plane(curve.PointAtStart, curve.TangentAtStart, Vector3d.CrossProduct(plane.ZAxis, curve.TangentAtStart));
          var box = new Box(plane, curve.GetBoundingBox(plane));
          return new Line(box.Plane.Origin, box.Plane.Origin + plane.YAxis * box.Y.T1);
        }
        else
        {
          var points = curve.ExtremeParameters(Vector3d.ZAxis).
                       Select(x => curve.PointAt(x)).
                       OrderBy(x => x.Z);

          var xdir = Vector3d.CrossProduct(plane.ZAxis, new Vector3d(plane.ZAxis.X, plane.ZAxis.Y, 0.0));
          var ydir = Vector3d.CrossProduct(plane.ZAxis, xdir);

          plane = new Plane(points.First(), xdir, plane.Normal.Z < 0.0 ? -ydir : ydir);
          var box = new Box(plane, curve.GetBoundingBox(plane));
          return new Line(box.Plane.Origin, box.Plane.Origin + plane.YAxis * box.Y.T1);
        }
      }

      return default;
    }

    ARDB.Ceiling Reconstruct
    (
      ARDB.Ceiling ceiling, ARDB.Document doc, IList<Curve> boundary, BoundingBox bbox,
      ARDB.CeilingType type, ARDB.Level level,
      Line? slopeArrow, double slope
    )
    {
      if (!Reuse(ref ceiling, boundary, type, level, slopeArrow, slope))
      {
        ceiling = ceiling.ReplaceElement
        (
          Create(doc, boundary, bbox, type, level, slopeArrow, slope),
          ExcludeUniqueProperties
        );
      }

      return ceiling;
    }
#endif
  }
}

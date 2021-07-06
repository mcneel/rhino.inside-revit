using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class RoofByOutline : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("198E152B-6636-4D90-9443-AE77B8B1475E");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public RoofByOutline() : base
    (
      name: "Add Roof",
      nickname: "Roof",
      description: "Given its outline curve, it adds a Roof element to the active Revit document",
      category: "Revit",
      subCategory: "Build"
    )
    { }

    bool Reuse(ref DB.FootPrintRoof element, Curve boundary, DB.RoofType type, DB.Level level)
    {
      if (element is null) return false;

      if (element.GetSketch() is DB.Sketch sketch)
      {
        var profiles = sketch.Profile.ToPolyCurves();
        if (profiles.Length != 1)
          return false;

        var plane = sketch.SketchPlane.GetPlane().ToPlane();
        var profile = Curve.ProjectToPlane(boundary, plane);

        if
        (
          !Curve.GetDistancesBetweenCurves(profiles[0], profile, Revit.VertexTolerance * Revit.ModelUnits, out var max, out var _, out var _, out var _, out var _, out var _) ||
          max > Revit.VertexTolerance * Revit.ModelUnits
        )
        {
          var hack = new DB.XYZ(1.0, 1.0, 0.0);
          var segments = profile.TryGetPolyCurve(out var polyCurve, Revit.AngleTolerance) ?
            polyCurve.DuplicateSegments() :
            profile.Split(profile.Domain.Mid);

          var index = 0;
          var loops = sketch.GetAllModelCurves();
          foreach (var loop in loops)
          {
            if (segments.Length != loop.Count)
              return false;

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

              // The following line allows SetGeometryCurve to work!!
              edge.Location.Move(hack);
              edge.SetGeometryCurve(curve, false);
            }
          }
        }
      }
      else return false;

      if (element.GetTypeId() != type.Id)
      {
        if (DB.Element.IsValidType(element.Document, new DB.ElementId[] { element.Id }, type.Id))
        {
          if (element.ChangeTypeId(type.Id) is DB.ElementId id && id != DB.ElementId.InvalidElementId)
            element = element.Document.GetElement(id) as DB.FootPrintRoof;
        }
        else return false;
      }

      bool succeed = true;
      succeed &= element.get_Parameter(DB.BuiltInParameter.ROOF_BASE_LEVEL_PARAM).Set(level.Id);

      return succeed;
    }

    void ReconstructRoofByOutline
    (
      [Optional, NickName("DOC")]
      DB.Document document,

      [Description("New Roof")]
      ref DB.FootPrintRoof roof,

      Curve boundary,
      Optional<DB.RoofType> type,
      Optional<DB.Level> level
    )
    {
      if
      (
        boundary is null ||
        boundary.IsShort(Revit.ShortCurveTolerance * Revit.ModelUnits) ||
        !boundary.IsClosed ||
        !boundary.TryGetPlane(out var boundaryPlane, Revit.VertexTolerance * Revit.ModelUnits) ||
        boundaryPlane.ZAxis.IsParallelTo(Vector3d.ZAxis) == 0
      )
        ThrowArgumentException(nameof(boundary), "Boundary curve should be a valid horizontal planar closed curve.");

      if (type.HasValue && type.Value.Document.IsEquivalent(document) == false)
        ThrowArgumentException(nameof(type));

      if (level.HasValue && level.Value.Document.IsEquivalent(document) == false)
        ThrowArgumentException(nameof(level));

      SolveOptionalType(document, ref type, DB.ElementTypeGroup.RoofType, nameof(type));

      SolveOptionalLevel(document, boundary, ref level, out var bbox);

      var orientation = boundary.ClosedCurveOrientation(Plane.WorldXY);
      if (orientation == CurveOrientation.CounterClockwise)
        boundary.Reverse();

      if (!Reuse(ref roof, boundary, type.Value, level.Value))
      {
        var parametersMask = new DB.BuiltInParameter[]
        {
        DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
        DB.BuiltInParameter.ELEM_FAMILY_PARAM,
        DB.BuiltInParameter.ELEM_TYPE_PARAM,
        DB.BuiltInParameter.ROOF_BASE_LEVEL_PARAM,
        DB.BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM
        };

        using (var curveArray = boundary.ToCurveArray())
        {
          var footPrintToModelCurvesMapping = new DB.ModelCurveArray();
          ReplaceElement(ref roof, document.Create.NewFootPrintRoof(curveArray, level.Value, type.Value, out footPrintToModelCurvesMapping), parametersMask);
        }
      }

      if (roof != null)
      {
        var roofLevelOffset = bbox.Min.Z / Revit.ModelUnits - level.Value.GetHeight();
        if
        (
          roof.GetParameter(External.DB.Schemas.ParameterId.RoofLevelOffsetParam) is DB.Parameter RoofLevelOffsetParam &&
          RoofLevelOffsetParam.AsDouble() != roofLevelOffset
        )
        {
          RoofLevelOffsetParam.Set(roofLevelOffset);
        }
      }
    }
  }
}

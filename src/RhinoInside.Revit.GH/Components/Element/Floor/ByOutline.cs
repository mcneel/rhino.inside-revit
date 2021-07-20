using System;
using System.Linq;
using System.Collections.Generic;
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
  public class FloorByOutline : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("DC8DAF4F-CC93-43E2-A871-3A01A920A722");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public FloorByOutline() : base
    (
      name: "Add Floor",
      nickname: "Floor",
      description: "Given its outline curve, it adds a Floor element to the active Revit document",
      category: "Revit",
      subCategory: "Build"
    )
    { }

    bool Reuse(ref DB.Floor element, IList<Curve> boundaries, DB.FloorType type, DB.Level level, bool structural)
    {
      if (element is null) return false;

      if (element.GetSketch() is DB.Sketch sketch)
      {
        var profiles = sketch.Profile.ToPolyCurves();
        if (profiles.Length != boundaries.Count)
          return false;

        var loops = sketch.GetAllModelCurves();
        var plane = sketch.SketchPlane.GetPlane().ToPlane();

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
              polyCurve.DuplicateSegments() :
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

                if(!edge.GeometryCurve.IsAlmostEqualTo(curve))
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
            element = element.Document.GetElement(id) as DB.Floor;
        }
        else return false;
      }

      bool succeed = true;
      succeed &= element.get_Parameter(DB.BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL).Set(structural ? 1 : 0);
      succeed &= element.get_Parameter(DB.BuiltInParameter.LEVEL_PARAM).Set(level.Id);

      return succeed;
    }

    void ReconstructFloorByOutline
    (
      [Optional, NickName("DOC")]
      DB.Document document,

      [Description("New Floor")]
      ref DB.Floor floor,

      IList<Curve> boundary,
      Optional<DB.FloorType> type,
      Optional<DB.Level> level,
      [Optional] bool structural
    )
    {
      if (boundary.Count < 1) return;

      var normal = default(Vector3d); var maxArea = 0.0;
      var index = 0; var maxIndex = 0;
      foreach (var loop in boundary)
      {
        var plane = default(Plane);
        if
        (
          loop is null ||
          loop.IsShort(Revit.ShortCurveTolerance * Revit.ModelUnits) ||
          !loop.IsClosed ||
          !loop.TryGetPlane(out plane, Revit.VertexTolerance) ||
          plane.ZAxis.IsParallelTo(Vector3d.ZAxis, Revit.AngleTolerance) == 0
        )
          ThrowArgumentException(nameof(boundary), "Boundary loop curves should be a valid horizontal, planar and closed.");

        using (var properties = AreaMassProperties.Compute(loop))
        {
          if (properties.Area > maxArea)
          {
            normal = plane.Normal;
            maxArea = properties.Area;
            maxIndex = index;

            var orientation = loop.ClosedCurveOrientation(Plane.WorldXY);
            if (orientation == CurveOrientation.CounterClockwise)
              normal.Reverse();
          }
        }

        index++;
      }

#if !REVIT_2022
      if (boundary.Count > 1)
      {
        boundary = new Curve[] { boundary[maxIndex] };
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Multiple boundary curves are only supported on Revit 2022 or above.");
      }
#endif

      if (type.HasValue && type.Value.Document.IsEquivalent(document) == false)
        ThrowArgumentException(nameof(type));

      if (level.HasValue && level.Value.Document.IsEquivalent(document) == false)
        ThrowArgumentException(nameof(level));

      SolveOptionalType(document, ref type, DB.ElementTypeGroup.FloorType, nameof(type));

      SolveOptionalLevel(document, boundary, ref level, out var bbox);

      if (!Reuse(ref floor, boundary, type.Value, level.Value, structural))
      {
        var parametersMask = new DB.BuiltInParameter[]
        {
          DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_PARAM,
          DB.BuiltInParameter.ELEM_TYPE_PARAM,
          DB.BuiltInParameter.LEVEL_PARAM,
          DB.BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL
        };

#if REVIT_2022
        var curveLoops = boundary.ConvertAll(GeometryEncoder.ToCurveLoop);

        ReplaceElement(ref floor, DB.Floor.Create(document, curveLoops, type.Value.Id, level.Value.Id, structural, default, 0.0), parametersMask);
#else
        var curveArray = boundary[0].ToCurveArray();

        if (type.Value.IsFoundationSlab)
          ReplaceElement(ref floor, document.Create.NewFoundationSlab(curveArray, type.Value, level.Value, structural, DB.XYZ.BasisZ), parametersMask);
        else
          ReplaceElement(ref floor, document.Create.NewFloor(curveArray, type.Value, level.Value, structural, DB.XYZ.BasisZ), parametersMask);
#endif
      }

      if (floor is object)
      {
        var floorHeightabovelevel = bbox.Min.Z / Revit.ModelUnits - level.Value.GetHeight();
        if
        (
          floor.GetParameter(External.DB.Schemas.ParameterId.FloorHeightabovelevelParam) is DB.Parameter floorHeightabovelevelParam &&
          floorHeightabovelevelParam.AsDouble() != floorHeightabovelevel
        )
        {
          floorHeightabovelevelParam.Set(floorHeightabovelevel);
        }
      }
    }
  }
}

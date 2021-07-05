using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
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

    bool Reuse(ref DB.Floor element, Curve boundary, DB.FloorType type, DB.Level level, bool structural)
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
          var segments = profile.TryGetPolyCurve(out var polyCurve, Revit.AngleTolerance) ?
            polyCurve.DuplicateSegments() :
            new Curve[] { profile };

          var index = 0;
          var loops = sketch.GetAllModelCurves();
          foreach (var loop in loops)
          {
            if (segments.Length != loop.Count)
              return false;

            foreach (var edge in loop)
            {
              var segment = segments[(++index) % segments.Length];
              segment.Scale(1.0 / Revit.ModelUnits);

              var curve = default(DB.Curve);
              if
              (
                edge.GeometryCurve is DB.HermiteSpline &&
                segment.TryGetHermiteSpline(out var points, out var start, out var end, Revit.VertexTolerance)
              )
              {
                var xyz = new DB.XYZ[points.Count];
                for (int p = 0; p < xyz.Length; p++)
                  xyz[p] = points[p].ToXYZ(UnitConverter.NoScale);

                using (var tangents = new DB.HermiteSplineTangents() { StartTangent = start.ToXYZ(), EndTangent = end.ToXYZ() })
                  curve = DB.HermiteSpline.Create(xyz, segment.IsClosed, tangents);
              }
              else curve = segment.ToCurve(UnitConverter.NoScale);

              edge.SetGeometryCurve(curve, false);
            }
          }
        }
      }
      else return false;

      if (element.GetTypeId() != type.Id)
      {
        if (DB.Element.IsValidType(element.Document, new DB.ElementId[] { element.Id }, type.Id))
          element = element.Document.GetElement(element.ChangeTypeId(type.Id)) as DB.Floor;
        else
          return false;
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

      Curve boundary,
      Optional<DB.FloorType> type,
      Optional<DB.Level> level,
      [Optional] bool structural
    )
    {
      if
      (
        boundary.IsShort(Revit.ShortCurveTolerance * Revit.ModelUnits) ||
        !boundary.IsClosed ||
        !boundary.TryGetPlane(out var boundaryPlane, Revit.VertexTolerance * Revit.ModelUnits) ||
        boundaryPlane.ZAxis.IsParallelTo(Vector3d.ZAxis) == 0
      )
        ThrowArgumentException(nameof(boundary), "Boundary must be an horizontal planar closed curve.");

      if (type.HasValue && type.Value.Document.IsEquivalent(document) == false)
        ThrowArgumentException(nameof(type));

      if (level.HasValue && level.Value.Document.IsEquivalent(document) == false)
        ThrowArgumentException(nameof(level));

      SolveOptionalType(document, ref type, DB.ElementTypeGroup.FloorType, nameof(type));

      SolveOptionalLevel(document, boundary, ref level, out var bbox);

      var orientation = boundary.ClosedCurveOrientation(Plane.WorldXY);
      if (orientation == CurveOrientation.CounterClockwise)
        boundary.Reverse();

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
        var curveLoops = new DB.CurveLoop[] { boundary.ToCurveLoop() };

        ReplaceElement(ref floor, DB.Floor.Create(document, curveLoops, type.Value.Id, level.Value.Id, structural, default, 0.0), parametersMask);
#else
        var curveArray = boundary.ToCurveArray();

        if (type.Value.IsFoundationSlab)
          ReplaceElement(ref floor, document.Create.NewFoundationSlab(curveArray, type.Value, level.Value, structural, DB.XYZ.BasisZ), parametersMask);
        else
          ReplaceElement(ref floor, document.Create.NewFloor(curveArray, type.Value, level.Value, structural, DB.XYZ.BasisZ), parametersMask);
#endif
      }

      if (floor != null)
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

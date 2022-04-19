using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Families
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;
  using External.DB.Extensions;
  using Kernel.Attributes;

  public class FloorByOutline : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("DC8DAF4F-CC93-43E2-A871-3A01A920A722");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public FloorByOutline() : base
    (
      name: "Add Floor",
      nickname: "Floor",
      description: "Given its outline curve, it adds a Floor element to the active Revit document",
      category: "Revit",
      subCategory: "Build"
    )
    { }

    bool Reuse(ref ARDB.Floor element, IList<Curve> boundaries, ARDB.FloorType type, ARDB.Level level, bool structural)
    {
      if (element is null) return false;

      if (element.GetSketch() is ARDB.Sketch sketch)
      {
        var profiles = sketch.Profile.ToArray(GeometryDecoder.ToPolyCurve);
        if (profiles.Length != boundaries.Count)
          return false;

        var tol = GeometryObjectTolerance.Model;
        var loops = sketch.GetAllModelCurves();
        var plane = sketch.SketchPlane.GetPlane().ToPlane();

        var pi = 0;
        foreach (var boundary in boundaries)
        {
          var profile = Curve.ProjectToPlane(boundary, plane);

          if
          (
            !Curve.GetDistancesBetweenCurves(profiles[pi], profile, tol.VertexTolerance, out var max, out var _, out var _, out var _, out var _, out var _) ||
            max > tol.VertexTolerance
          )
          {
            var segments = profile.TryGetPolyCurve(out var polyCurve, tol.AngleTolerance) ?
              polyCurve.DuplicateSegments() :
              new Curve[] { profile };

            if (pi < loops.Count)
            {
              var loop = loops[pi];
              if (segments.Length != loop.Count)
                return false;

              var index = 0;
              foreach (var edge in loop)
              {
                var segment = segments[(++index) % segments.Length];

                var curve = default(ARDB.Curve);
                if (edge.GeometryCurve is ARDB.HermiteSpline)
                  curve = segment.ToHermiteSpline();
                else
                  curve = segment.ToCurve();

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
        if (ARDB.Element.IsValidType(element.Document, new ARDB.ElementId[] { element.Id }, type.Id))
        {
          if (element.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            element = element.Document.GetElement(id) as ARDB.Floor;
        }
        else return false;
      }

      bool succeed = true;
      succeed &= element.get_Parameter(ARDB.BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL).Update(structural ? 1 : 0);
      succeed &= element.get_Parameter(ARDB.BuiltInParameter.LEVEL_PARAM).Update(level.Id);

      return succeed;
    }

    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.LEVEL_PARAM,
      ARDB.BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM,
      ARDB.BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL
    };

    void ReconstructFloorByOutline
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New Floor")]
      ref ARDB.Floor floor,

      IList<Curve> boundary,
      Optional<ARDB.FloorType> type,
      Optional<ARDB.Level> level,
      [Optional] bool structural
    )
    {
      if (boundary is null) return;

      var tol = GeometryObjectTolerance.Model;
      var normal = default(Vector3d); var maxArea = 0.0;
      var index = 0; var maxIndex = 0;
      foreach (var loop in boundary)
      {
        if (loop is null) return;
        var plane = default(Plane);
        if
        (
          loop.IsShort(tol.ShortCurveTolerance) ||
          !loop.IsClosed ||
          !loop.TryGetPlane(out plane, tol.VertexTolerance) ||
          plane.ZAxis.IsParallelTo(Vector3d.ZAxis, tol.AngleTolerance) == 0
        )
          ThrowArgumentException(nameof(boundary), "Boundary loop curves should be a set of valid horizontal, coplanar and closed curves.");

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

      SolveOptionalType(document, ref type, ARDB.ElementTypeGroup.FloorType, nameof(type));

      SolveOptionalLevel(document, boundary, ref level, out var bbox);

      if (boundary.Count == 0)
      {
        floor = default;
      }
      else if (!Reuse(ref floor, boundary, type.Value, level.Value, structural))
      {
        var newFloor = default(ARDB.Floor);
#if REVIT_2022
        var curveLoops = boundary.ConvertAll(GeometryEncoder.ToCurveLoop);

        newFloor = ARDB.Floor.Create(document, curveLoops, type.Value.Id, level.Value.Id, isStructural: true, default, 0.0);
#else
        var curveArray = boundary[0].ToCurveArray();

        if (type.Value.IsFoundationSlab)
          newFloor = document.Create.NewFoundationSlab(curveArray, type.Value, level.Value, structural: true, ARDB.XYZ.BasisZ);
        else
          newFloor = document.Create.NewFloor(curveArray, type.Value, level.Value, structural: true, ARDB.XYZ.BasisZ);
#endif
        // We turn off analytical model off by default
        newFloor.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);

        ReplaceElement(ref floor, newFloor, ExcludeUniqueProperties);

        newFloor.get_Parameter(ARDB.BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL)?.Update(structural);
      }

      if (floor is object)
      {
        var heightAboveLevel = bbox.Min.Z / Revit.ModelUnits - level.Value.GetElevation();
        floor.get_Parameter(ARDB.BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)?.Update(heightAboveLevel);
      }
    }
  }
}

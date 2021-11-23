using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;
  using External.DB.Extensions;
  using Kernel.Attributes;

  [ComponentVersion(introduced: "1.3")]
  public class CeilingByOutline : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("A39BBDF2-78F2-4501-BB6E-F9CC3E83516E");

#if REVIT_2022
    public override GH_Exposure Exposure => GH_Exposure.secondary;
#else
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;
    public override bool SDKCompliancy(int exeVersion, int exeServiceRelease) => false;
#endif

   public CeilingByOutline() : base
    (
      name: "Add Ceiling",
      nickname: "Ceiling",
      description: "Given its outline curve, it adds a Ceiling element to the active Revit document",
      category: "Revit",
      subCategory: "Build"
    )
    { }

    bool Reuse(ref ARDB.Ceiling element, IList<Curve> boundaries, ARDB.CeilingType type, ARDB.Level level)
    {
      if (element is null) return false;

      if (element.GetSketch() is ARDB.Sketch sketch)
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

                var curve = default(ARDB.Curve);
                if
                (
                  edge.GeometryCurve is ARDB.HermiteSpline &&
                  segment.TryGetHermiteSpline(out var points, out var start, out var end, Revit.VertexTolerance * Revit.ModelUnits)
                )
                {
                  using (var tangents = new ARDB.HermiteSplineTangents() { StartTangent = start.ToXYZ(), EndTangent = end.ToXYZ() })
                  {
                    var xyz = points.ConvertAll(GeometryEncoder.ToXYZ);
                    curve = ARDB.HermiteSpline.Create(xyz, segment.IsClosed, tangents);
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
        if (ARDB.Element.IsValidType(element.Document, new ARDB.ElementId[] { element.Id }, type.Id))
        {
          if (element.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            element = element.Document.GetElement(id) as ARDB.Ceiling;
        }
        else return false;
      }

      bool succeed = true;
      succeed &= element.get_Parameter(ARDB.BuiltInParameter.LEVEL_PARAM).Update(level.Id);

      return succeed;
    }

    void ReconstructCeilingByOutline
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New Ceiling")]
      ref ARDB.Ceiling ceiling,

      IList<Curve> boundary,
      Optional<ARDB.CeilingType> type,
      Optional<ARDB.Level> level
    )
    {
      if (boundary is null) return;

      var normal = default(Vector3d); var maxArea = 0.0;
      var index = 0; var maxIndex = 0;
      foreach (var loop in boundary)
      {
        if (loop is null) return;
        var plane = default(Plane);
        if
        (
          loop.IsShort(Revit.ShortCurveTolerance * Revit.ModelUnits) ||
          !loop.IsClosed ||
          !loop.TryGetPlane(out plane, Revit.VertexTolerance) ||
          plane.ZAxis.IsParallelTo(Vector3d.ZAxis, Revit.AngleTolerance) == 0
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

      if (type.HasValue && type.Value.Document.IsEquivalent(document) == false)
        ThrowArgumentException(nameof(type));

      if (level.HasValue && level.Value.Document.IsEquivalent(document) == false)
        ThrowArgumentException(nameof(level));

      SolveOptionalType(document, ref type, ARDB.ElementTypeGroup.CeilingType, nameof(type));

      SolveOptionalLevel(document, boundary, ref level, out var bbox);

      if (boundary.Count == 0)
      {
        ceiling = default;
      }
      else if (!Reuse(ref ceiling, boundary, type.Value, level.Value))
      {
        var parametersMask = new ARDB.BuiltInParameter[]
        {
          ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
          ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
          ARDB.BuiltInParameter.LEVEL_PARAM,
          ARDB.BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM
        };

#if REVIT_2022
        var curveLoops = boundary.ConvertAll(GeometryEncoder.ToCurveLoop);

        ReplaceElement(ref ceiling, ARDB.Ceiling.Create(document, curveLoops, type.Value.Id, level.Value.Id, default, 0.0), parametersMask);
#else
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"'{Name}' component is only supported on Revit 2022 or above.");
#endif
      }

      if (ceiling is object)
      {
        var heightAboveLevel = bbox.Min.Z / Revit.ModelUnits - level.Value.GetHeight();
        ceiling.get_Parameter(ARDB.BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM)?.Update(heightAboveLevel);
      }
    }
  }
}

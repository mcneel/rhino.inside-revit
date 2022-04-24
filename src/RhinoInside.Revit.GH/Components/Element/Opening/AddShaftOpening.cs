using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.Openings
{
  [ComponentVersion(introduced: "1.6")]
  public class AddShaftOpening : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("657811B7-6662-4FCF-A67A-A65C34FA0651");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddShaftOpening() : base
    (
      name: "Add Shaft Opening",
      nickname: "Shaft",
      description: "Given its outline boundary, it adds a Shaft opening to the active Revit document",
      category: "Revit",
      subCategory: "Host"
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
          Description = "Boundary to create the shaft opening",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.LevelConstraint
        {
          Name = "Base",
          NickName = "BA",
          Description = $"Base of the opening.{Environment.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Curve'.",
          Optional = true,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.LevelConstraint
        {
          Name = "Top",
          NickName = "TO",
          Description = $"Top of the opening.{Environment.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Curve'",
          Optional = true,
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Opening()
        {
          Name = _Opening_,
          NickName = _Opening_.Substring(0, 1),
          Description = $"Output {_Opening_}",
        }
      )
    };

    const string _Opening_ = "Opening";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Opening>
      (
        doc.Value, _Opening_, (opening) =>
        {
          // Input
          if (!Params.GetDataList(DA, "Boundary", out IList<Curve> boundary) || boundary.Count == 0) return null;
          if (!Params.TryGetData(DA, "Base", out ERDB.ElevationElementReference? baseElevation)) return null;
          if (!Params.TryGetData(DA, "Top", out ERDB.ElevationElementReference? topElevation)) return null;

          var boundaryElevation = Interval.Unset;
          {
            var tol = GeometryObjectTolerance.Model;
            foreach (var loop in boundary)
            {
              if (loop is null) return null;
              if
              (
                loop.IsShort(tol.ShortCurveTolerance) ||
                !loop.IsClosed ||
                !loop.TryGetPlane(out var plane, tol.VertexTolerance) ||
                plane.ZAxis.IsParallelTo(Vector3d.ZAxis, tol.AngleTolerance) == 0
              )
              {
                boundaryElevation = Interval.Unset;
                break;
              }

              boundaryElevation = Interval.FromUnion(boundaryElevation, new Interval(plane.OriginZ, plane.OriginZ));
            }

            if (!boundaryElevation.IsValid || boundaryElevation.Length > tol.VertexTolerance)
              throw new Exceptions.RuntimeArgumentException("Boundary", "Boundary loop curves should be a set of valid horizontal, coplanar and closed curves.", boundary);
          }

          // Solve missing Base & Top
          ERDB.ElevationElementReference.SolveBaseAndTop
          (
            doc.Value, GeometryEncoder.ToInternalLength(boundaryElevation.Mid),
            0.0, 20.0,
            ref baseElevation, ref topElevation,
            -0.5, +0.5
          );

          // Compute
          opening = Reconstruct
          (
            opening, doc.Value,
            boundary,
            GeometryEncoder.ToInternalLength(boundaryElevation.Mid),
            baseElevation.Value,
            topElevation.Value
          );

          DA.SetData(_Opening_, opening);
          return opening;
        }
      );
    }

    bool Reuse(ARDB.Opening opening, IList<Curve> boundaries, double elevation)
    {
      if (opening is null) return false;

      if (opening.GetSketch() is ARDB.Sketch sketch)
      {
        var profiles = sketch.Profile.ToArray(GeometryDecoder.ToPolyCurve);
        if (profiles.Length != boundaries.Count)
          return false;

        var tol = GeometryObjectTolerance.Model;
        var hack = new ARDB.XYZ(1.0, 1.0, 0.0);
        var loops = sketch.GetAllModelCurves();
        var sketchPlane = sketch.SketchPlane.GetPlane();
        sketch.SketchPlane.Location.Move(ARDB.XYZ.BasisZ * (elevation - sketchPlane.Origin.Z));

        var pi = 0;
        foreach (var boundary in boundaries)
        {
          if
          (
            !Curve.GetDistancesBetweenCurves(profiles[pi], boundary, tol.VertexTolerance, out var max, out var _, out var _, out var _, out var _, out var _) ||
            max > tol.VertexTolerance
          )
          {
            var segments = boundary.TryGetPolyCurve(out var polyCurve, tol.AngleTolerance) ?
              polyCurve.DuplicateSegments() :
              new Curve[] { boundary };

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

                if (!edge.GeometryCurve.IsAlmostEqualTo(curve))
                {
                  // The following line allows SetGeometryCurve to work!!
                  edge.Location.Move(hack);
                  edge.SetGeometryCurve(curve, overrideJoins: true);
                }
              }
            }
          }

          pi++;
        }
      }
      else return false;

      return true;
    }

    ARDB.Opening Reconstruct
    (
      ARDB.Opening opening,
      ARDB.Document document,
      IList<Curve> boundaries, double elevation,
      ERDB.ElevationElementReference baseElevation,
      ERDB.ElevationElementReference topElevation
    )
    {
      // If there are no Levels!!
      if (!baseElevation.IsLevelConstraint(out var baseLevel, out var baseOffset))
        return default;

      if (!Reuse(opening, boundaries, elevation))
      {
        // We create a Level here to obtain an opening with a <not associated> `SketckPlane`
        var level = ARDB.Level.Create(document, elevation);
        opening = document.Create.NewOpening(level, default, boundaries.ToCurveArray());
        opening.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_CONSTRAINT).Update(baseLevel.Id);
        document.Delete(level.Id);
      }

      // TODO: Compute if needs to be updated or not
      opening.get_Parameter(ARDB.BuiltInParameter.WALL_HEIGHT_TYPE).Update(ARDB.ElementId.InvalidElementId);

      opening.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_CONSTRAINT).Update(baseLevel.Id);
      opening.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_OFFSET).Update(baseOffset.Value);

      if (topElevation.IsLevelConstraint(out var topLevel, out var topOffset))
      {
        opening.get_Parameter(ARDB.BuiltInParameter.WALL_HEIGHT_TYPE).Update(topLevel.Id);
        if (!opening.get_Parameter(ARDB.BuiltInParameter.WALL_TOP_OFFSET).Update(topOffset.Value))
          throw new Exceptions.RuntimeArgumentException("Top", $"The top of the Opening is lower than the bottom of the Opening or coincident with it. {{{opening.Id}}} ");
      }
      else
      {
        opening.get_Parameter(ARDB.BuiltInParameter.WALL_HEIGHT_TYPE).Update(ARDB.ElementId.InvalidElementId);
        if (!opening.get_Parameter(ARDB.BuiltInParameter.WALL_USER_HEIGHT_PARAM).Update(topElevation.Offset - baseOffset.Value))
          throw new Exceptions.RuntimeArgumentException("Top", $"The top of the Opening is lower than the bottom of the Opening or coincident with it. {{{opening.Id}}} ");
      }

      return opening;
    }
  }
}

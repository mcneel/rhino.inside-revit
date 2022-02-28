using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Openings
{
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
        new Parameters.Level
        {
          Name = "Base Constraint",
          NickName = "BC",
          Description = "Level to constraint the base of the opening",
        }
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = "Base Offset",
          NickName = "BO",
          Description = "Offset to the level of the base of the opening",
          Optional = true
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.Level
        {
          Name = "Top Constraint",
          NickName = "TC",
          Description = "Level to constraint the top of the opening",
        }
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = "Top Offset",
          NickName = "TO",
          Description = "Offset to the level of the top of the opening",
          Optional = true
        }, ParamRelevance.Occasional
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
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
          if (!Params.GetDataList(DA, "Boundary", out IList<Curve> boundary)) return null;
          if (!Params.GetData(DA, "Base Constraint", out ARDB.Level baseLevel)) return null;
          if (!Params.TryGetData(DA, "Base Offset", out double? baseOffset)) return null;
          if (!Params.GetData(DA, "Top Constraint", out ARDB.Level topLevel)) return null;
          if (!Params.TryGetData(DA, "Top Offset", out double? topOffset)) return null;

          var tol = GeometryObjectTolerance.Model;
          var normal = default(Vector3d); var maxArea = 0.0;
          var index = 0; var maxIndex = 0;
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
              throw new Exceptions.RuntimeArgumentException("Boundary", "Boundary loop curves should be a set of valid horizontal, coplanar and closed curves.", boundary);

            index++;
          }

          // Compute
          opening = Reconstruct(opening, doc.Value, boundary, baseLevel, baseOffset.HasValue ? baseOffset.Value : 0.0 , topLevel, topOffset.HasValue ? topOffset.Value : 0.0);

          DA.SetData(_Opening_, opening);
          return opening;
        }
      );
    }

    bool Reuse(ARDB.Opening opening, IList<Curve> boundaries)
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
                  segment.TryGetHermiteSpline(out var points, out var start, out var end, tol.VertexTolerance)
                )
                {
                  using (var tangents = new ARDB.HermiteSplineTangents() { StartTangent = start.ToXYZ(), EndTangent = end.ToXYZ() })
                  {
                    var xyz = points.ConvertAll(GeometryEncoder.ToXYZ);
                    curve = ARDB.HermiteSpline.Create(xyz, segment.IsClosed, tangents);
                  }
                }
                else curve = segment.ToCurve();

                if (!edge.GeometryCurve.IsAlmostEqualTo(curve))
                {
                  // The following line allows SetGeometryCurve to work!!
                  edge.Location.Move(hack);
                  edge.SetGeometryCurve(curve, false);
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

    ARDB.Opening Create(ARDB.Document doc, ARDB.Level baseLevel, ARDB.Level topLevel, IList<Curve> boundary)
    {
      return doc.Create.NewOpening(baseLevel, topLevel, boundary.ToCurveArray());
    }

    ARDB.Opening Reconstruct(ARDB.Opening opening, ARDB.Document doc, IList<Curve> boundary, ARDB.Level baseLevel, double baseOffset, ARDB.Level topLevel, double topOffset)
    {
      if (!Reuse(opening, boundary))
        opening = Create(doc, baseLevel, topLevel, boundary);

      opening.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_OFFSET).Update(baseOffset / Revit.ModelUnits);
      opening.get_Parameter(ARDB.BuiltInParameter.WALL_TOP_OFFSET).Update(topOffset / Revit.ModelUnits);

      return opening;
    }
  }
}

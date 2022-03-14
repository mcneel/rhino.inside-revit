using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Components.Element.Opening;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Openings
{
  public class AddFaceOpening : AddOpening
  {
    public override Guid ComponentGuid => new Guid("69A10E5D-5DF0-4227-95D3-2629529C1DEF");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddFaceOpening() : base
    (
      name: "Add Face Opening",
      nickname: "FaceOpen",
      description: "Given its outline boundary and a host element, it adds an opening to the active Revit document",
      category: "Revit",
      subCategory: "Host",
      isPerpendicular: true
    )
    { }
  }

  /* Previous
  public class AddFaceOpening : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("69A10E5D-5DF0-4227-95D3-2629529C1DEF");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddFaceOpening() : base
    (
      name: "Add Face Opening",
      nickname: "FaceOpen",
      description: "Given its outline boundary and a host element, it adds an opening to the active Revit document",
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
        new Parameters.HostObject()
        {
          Name = "Host Element",
          NickName = "HE",
          Description = "Host to add the opening",
        }
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
      )
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
          if (!Params.GetData(DA, "Host Element", out ARDB.HostObject host)) return null;
          if (!Params.GetDataList(DA, "Boundary", out IList<Curve> boundary)) return null;

          var tol = GeometryObjectTolerance.Model;
          foreach (var loop in boundary)
          {
            if (loop is null) return null;
            if
            (
              loop.IsShort(tol.ShortCurveTolerance) ||
              !loop.IsClosed ||
              !loop.TryGetPlane(out var plane, tol.VertexTolerance)
            )
              throw new Exceptions.RuntimeArgumentException("Boundary", "Boundary loop curves should be a set of valid horizontal, coplanar and closed curves.", boundary);
          }

          // Compute
          opening = Reconstruct(opening, doc.Value, host, boundary);

          DA.SetData(_Opening_, opening);
          return opening;
        }
      );
    }

    bool Reuse(ARDB.Opening opening, ARDB.HostObject host, IList<Curve> boundaries)
    {
      if (opening is null) return false;

      if (!opening.Host.IsEquivalent(host)) return false;

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

    ARDB.Opening Create(ARDB.Document doc, ARDB.HostObject hostElement, IList<Curve> boundary)
    {
      return doc.Create.NewOpening(hostElement, boundary.ToCurveArray(), true);
    }

    ARDB.Opening Reconstruct(ARDB.Opening opening, ARDB.Document doc, ARDB.HostObject host, IList<Curve> boundary)
    {
      if (!Reuse(opening, host, boundary))
        opening = Create(doc, host, boundary);

      return opening;
    }
  }
  */
}


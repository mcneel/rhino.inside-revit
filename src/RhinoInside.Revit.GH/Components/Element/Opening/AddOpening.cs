using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Openings
{
  using Convert.Geometry;
  using External.DB.Extensions;

  public abstract class AddOpening : ElementTrackerComponent
  {
    protected AddOpening(string name, string nickname, string description, string category, string subCategory) :
      base(name, nickname, description, category, subCategory)
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.HostObject()
        {
          Name = "Host",
          NickName = "H",
          Description = "Host to add the opening",
        }
      ),
      new ParamDefinition
       (
        new Param_Curve
        {
          Name = "Boundary",
          NickName = "B",
          Description = "Boundary to create the opening",
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
    protected virtual bool IsCutPerpendicularToFace { get; }

    bool IsSlopped(ARDB.HostObject host)
    {
      var elements = host.GetSketch().GetDependents<ARDB.CurveElement>();
      return elements.Any(x => x.get_Parameter(ARDB.BuiltInParameter.SPECIFY_SLOPE_OR_OFFSET)?.HasValue == true);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Host", out Types.HostObject host, x => x.IsValid)) return;

      ReconstructElement<ARDB.Opening>
      (
        host.Document, _Opening_, (opening) =>
        {
          // Input
          if (!Params.GetDataList(DA, "Boundary", out IList<Curve> boundary)) return null;

          switch (host.Value)
          {
            case ARDB.Floor _:
              if (IsCutPerpendicularToFace == false && IsSlopped(host.Value))
                throw new Exceptions.RuntimeArgumentException("Host", "Sloped floors are not supported. Use shafts to add vertical openings to floors", host);
              break;

            case ARDB.ExtrusionRoof _:
              if (IsCutPerpendicularToFace == false)
                throw new Exceptions.RuntimeArgumentException("Host", "Extrusion Roofs are not supported. Use shafts to add vertical openings to roofs", host);
              else
                throw new Exceptions.RuntimeArgumentException("Host", "Extrusion Roofs are not supported", host);

            case ARDB.RoofBase _:
              if (IsCutPerpendicularToFace == true && IsSlopped(host.Value))
                throw new Exceptions.RuntimeArgumentException("Host", "Sloped Roofs are not supported", host);
              break;
          }

          var tol = GeometryTolerance.Model;
          foreach (var loop in boundary)
          {
            if (loop is null) return null;
            if
            (
              loop.IsShort(tol.ShortCurveTolerance) ||
              !loop.IsClosed ||
              !loop.TryGetPlane(out var plane, tol.VertexTolerance)
            )
              throw new Exceptions.RuntimeArgumentException("Boundary", "Boundary loop curves should be a set of valid coplanar and closed curves.", boundary);
          }

          // Compute
          opening = Reconstruct(opening, host.Document, host.Value, boundary);
          host.InvalidateGraphics();

          DA.SetData(_Opening_, opening);
          return opening;
        }
      );
    }

    bool Reuse(ARDB.Opening opening, ARDB.HostObject host, IList<Curve> boundaries)
    {
      if (opening is null) return false;

      if (!opening.Host.IsEquivalent(host)) return false;

      if (!(opening.GetSketch() is ARDB.Sketch sketch && Types.Sketch.SetProfile(sketch, boundaries, Vector3d.ZAxis)))
        return false;

      return true;
    }

    ARDB.Opening Create(ARDB.Document doc, ARDB.HostObject host, IList<Curve> boundary)
    {
      return doc.Create.NewOpening(host, boundary.ToCurveArray(), IsCutPerpendicularToFace);
    }

    ARDB.Opening Reconstruct(ARDB.Opening opening, ARDB.Document doc, ARDB.HostObject host, IList<Curve> boundary)
    {
      if (IsCutPerpendicularToFace == false)
      {
        if (host.GetSketch() is ARDB.Sketch sketch)
        {
          var hostPlane = sketch.SketchPlane.GetPlane().ToPlane();
          boundary = boundary.Select(x => x.ProjectToPlane(hostPlane)).ToArray();
        }
      }

      if (!Reuse(opening, host, boundary))
        opening = Create(doc, host, boundary);

      return opening;
    }
  }
}

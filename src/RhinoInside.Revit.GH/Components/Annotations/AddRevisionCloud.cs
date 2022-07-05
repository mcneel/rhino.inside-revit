using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  [ComponentVersion(introduced: "1.8")]
  public class AddRevisionCloud : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("8FF70EEF-C599-476C-A76C-D7A9B8A1D54A");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public AddRevisionCloud() : base
    (
      name: "Add Revision Cloud",
      nickname: "RevisionCloud",
      description: "Given a profile, it adds a revision cloud to the given View",
      category: "Revit",
      subCategory: "Annotation"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to add a specific region",
        }
      ),
      new ParamDefinition
      (
        new Param_Surface
        {
          Name = "Boundary",
          NickName = "B",
          Description = "Boundary to create a specific region",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.Revision
        {
          Name = "Revision",
          NickName = "R",
          Description = "Revision associated with this revision cloud.",
          Optional = true,
        }, ParamRelevance.Primary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _Cloud_,
          NickName = _Cloud_.Substring(0, 1),
          Description = $"Output {_Cloud_}",
        }
      )
    };

    const string _Cloud_ = "Cloud";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view)) return;

      ReconstructElement<ARDB.RevisionCloud>
      (
        view.Document, _Cloud_, cloud =>
        {
          // Input
          if (!Params.GetDataList(DA, "Boundary", out IList<Brep> boundary) || boundary.Count == 0) return null;
          if (!Params.TryGetData(DA, "Revision", out ARDB.Revision revision, x => x.IsValid())) return null;

          if (view is Types.View3D || !view.Value.IsGraphicalView())
            throw new Exceptions.RuntimeArgumentException("View", $"View '{view.Nomen}' does not support detail items creation", view);

          var tol = GeometryTolerance.Model;
          var viewPlane = view.Location;
          var loops = boundary.SelectMany(x => x.Loops).Select(x => { var c = x.To3dCurve(); c.Reverse(); return c; }).ToArray();
          foreach (var loop in loops)
          {
            if (loop is null) return null;
            if
            (
              loop.IsShort(tol.ShortCurveTolerance) ||
              !loop.IsClosed ||
              !loop.IsParallelToPlane(viewPlane, tol.VertexTolerance, tol.AngleTolerance)
            )
              throw new Exceptions.RuntimeArgumentException("Boundary", "Curve should be a valid planar, closed curve and perperdicular to the input view.", loop);
          }

          loops = loops.Select(x => Curve.ProjectToPlane(x, viewPlane)).ToArray();

          if (revision is null)
          {
            var revisionId = ARDB.Revision.GetAllRevisionIds(view.Document).Last();
            revision = view.Document.GetElement(revisionId) as ARDB.Revision;
          }

          // Compute
          cloud = Reconstruct(cloud, view.Value, loops, revision);

          DA.SetData(_Cloud_, cloud);
          return cloud;
        }
      );
    }

    bool Reuse(ARDB.RevisionCloud cloud, ARDB.View view, IList<Curve> boundaries, ARDB.Revision revision)
    {
      if (cloud is null) return false;
      if (cloud.OwnerViewId != view.Id) return false;

      if (!(cloud.GetSketch() is ARDB.Sketch sketch && Types.Sketch.SetProfile(sketch, boundaries, view.ViewDirection.ToVector3d())))
        return false;
      
      if (cloud.RevisionId != revision.Id) cloud.RevisionId = revision.Id;
      return true;
    }

    ARDB.RevisionCloud Reconstruct
    (
      ARDB.RevisionCloud cloud,
      ARDB.View view,
      IList<Curve> boundaries,
      ARDB.Revision revision
    )
    {
      if (!Reuse(cloud, view, boundaries, revision))
      {
        var curves = boundaries.SelectMany(x => GeometryEncoder.ToCurveMany(x)).ToArray();
        cloud = ARDB.RevisionCloud.Create(view.Document, view, revision.Id, curves);
      }

      return cloud;
    }
  }
}

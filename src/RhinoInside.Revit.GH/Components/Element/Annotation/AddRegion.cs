using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotation
{
  [ComponentVersion(introduced: "1.8")]
  public class AddRegion : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("AD88CF11-1946-4429-8F4D-172E3F9B866F");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public AddRegion() : base
    (
      name: "Add Region",
      nickname: "Region",
      description: "Given a profile, it adds a region to the given View",
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
        new Param_Curve
        {
          Name = "Boundary",
          NickName = "B",
          Description = "Profile to create a specific region",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementType
        {
          Name = "Type",
          NickName = "T",
          Description = "Element type of the given region",
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_FilledRegion
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _Output_,
          NickName = _Output_.Substring(0, 1),
          Description = $"Output {_Output_}",
        }
      )
    };

    const string _Output_ = "Region";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;

      ReconstructElement<ARDB.FilledRegion>
      (
        view.Document, _Output_, (region) =>
        {
          // Input
          if (!Params.GetDataList(DA, "Boundary", out IList<Curve> boundary) || boundary.Count == 0) return null;
          if (!Params.GetData(DA, "Type", out ARDB.FilledRegionType type)) return null;

          if
          (
            view.ViewType is ARDB.ViewType.ThreeD ||
            view.ViewType is ARDB.ViewType.Schedule ||
            view.ViewType is ARDB.ViewType.ColumnSchedule ||
            view.ViewType is ARDB.ViewType.PanelSchedule
          )
            throw new Exceptions.RuntimeArgumentException("View", "This view does not support detail items creation", view);

          var tol = GeometryObjectTolerance.Model;
          foreach (var loop in boundary)
          {
            if (loop is null) return null;
            if
            (
            loop.IsShort(tol.ShortCurveTolerance) ||
            !loop.IsClosed ||
            !loop.TryGetPlane(out var plane, tol.VertexTolerance) ||
            plane.ZAxis.IsParallelTo(view.ViewDirection.ToVector3d(), tol.AngleTolerance) == 0
            )
              throw new Exceptions.RuntimeArgumentException("Curve", "Curve should be a valid planar, closed curve and perperdicular to the input view.", loop);
          }
          
          // Compute
          region = Reconstruct(region, view, type.Id, boundary);

          DA.SetData(_Output_, region);
          return region;
        }
      );
    }

    bool Reuse(ARDB.FilledRegion region, ARDB.View view, ARDB.ElementId typeId, IList<Curve> boundaries)
    {
      if (region is null) return false;

      if (region.OwnerViewId != view.Id) return false;
      if (region.GetTypeId() != typeId) return false;

      var pi = 0;
      var tol = GeometryObjectTolerance.Model;
      var profiles = region.GetBoundaries() as List<ARDB.CurveLoop>;
      if (profiles.Count != boundaries.Count)
        return false;

      foreach (var boundary in boundaries)
      {
        var profile = GeometryDecoder.ToCurve(profiles[pi]);
        if (!Curve.GetDistancesBetweenCurves(profile, boundary, tol.VertexTolerance, out var max, out var _, out var _, out var _, out var _, out var _) ||
            max > tol.VertexTolerance)
        {
          return false;
        }
        pi++;
      }
      
      return true;
    }

    ARDB.FilledRegion Create(ARDB.View view, ARDB.ElementId typeId, IList<Curve> boundary)
    {
      var plane = new Plane(view.Origin.ToPoint3d(), view.ViewDirection.ToVector3d());
      var loops = boundary.Select(x => Curve.ProjectToPlane(x, plane).ToCurveLoop()).ToList();
      return ARDB.FilledRegion.Create(view.Document, typeId, view.Id, loops);
    }

    ARDB.FilledRegion Reconstruct(ARDB.FilledRegion region, ARDB.View view, ARDB.ElementId typeId, IList<Curve> boundaries)
    {
      if (!Reuse(region, view, typeId, boundaries))
        region = Create(view, typeId, boundaries);

      return region;
    }
  }
}

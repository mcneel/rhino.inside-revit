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
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_FilledRegion
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
        view.Document, _Output_, region =>
        {
          // Input
          if (!Params.GetDataList(DA, "Boundary", out IList<Curve> boundary) || boundary.Count == 0) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.FilledRegionType type, Types.Document.FromValue(view.Document), ARDB.ElementTypeGroup.FilledRegionType)) return null;

          if
          (
            view.ViewType is ARDB.ViewType.ThreeD ||
            view.ViewType is ARDB.ViewType.Schedule ||
            view.ViewType is ARDB.ViewType.ColumnSchedule ||
            view.ViewType is ARDB.ViewType.PanelSchedule
          )
            throw new Exceptions.RuntimeArgumentException("View", "This view does not support detail items creation", view);

          var tol = GeometryTolerance.Model;
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

          var viewPlane = new Plane(view.Origin.ToPoint3d(), view.ViewDirection.ToVector3d());
          boundary = boundary.Select(x => Curve.ProjectToPlane(x, viewPlane)).ToList();

          // Compute
          region = Reconstruct(region, view, boundary.Select(GeometryEncoder.ToBoundedCurveLoop).ToArray(), type);

          DA.SetData(_Output_, region);
          return region;
        }
      );
    }

    public static IList<IList<ARDB.ModelCurve>> GetAllModelCurves(ARDB.FilledRegion region)
    {
      var boundaries = region.GetBoundaries();
      var modelCurves = new IList<ARDB.ModelCurve>[boundaries.Count];

      var loopIndex = 0;
      foreach (var boundary in boundaries)
      {
        modelCurves[loopIndex++] = boundary.Cast<ARDB.Curve>().
          Distinct(CurveEqualityComparer.Reference).
          Select(x => region.Document.GetElement(x.Reference.ElementId) as ARDB.ModelCurve).
          ToArray();
      }

      return modelCurves;
    }

    bool Reuse(ARDB.FilledRegion region, ARDB.View view, IList<ARDB.CurveLoop> boundaries, ARDB.FilledRegionType type)
    {
      if (region is null) return false;
      if (region.OwnerViewId != view.Id) return false;

      var sourceBoundaries = region.GetBoundaries();
      if (sourceBoundaries.Count != boundaries.Count)
        return false;

      var comparer = GeometryObjectEqualityComparer.Comparer(region.Document.Application.VertexTolerance);
      for(int l = 0; l < sourceBoundaries.Count; ++l)
      {
        var sourceBondary = sourceBoundaries[l];
        var targetBoundary = boundaries[l];
        if (sourceBondary.NumberOfCurves() != targetBoundary.NumberOfCurves())
          return false;

        foreach (var pair in sourceBondary.Zip(targetBoundary, (Source, Target) => (Source, Target)))
        {
          if (!comparer.Equals(pair.Source, pair.Target))
            return false;
        }
      }
      
      if (region.GetTypeId() != type.Id) region.ChangeTypeId(type.Id);
      return true;
    }

    ARDB.FilledRegion Create(ARDB.View view, IList<ARDB.CurveLoop> boundaries, ARDB.FilledRegionType type)
    {
      return ARDB.FilledRegion.Create(view.Document, type.Id, view.Id, boundaries);
    }

    ARDB.FilledRegion Reconstruct(ARDB.FilledRegion region, ARDB.View view, IList<ARDB.CurveLoop> boundaries, ARDB.FilledRegionType type)
    {
      if (!Reuse(region, view, boundaries, type))
        region = Create(view, boundaries, type);

      return region;
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Site
{
  [ComponentVersion(introduced: "1.8")]
  public class AddSiteSubRegion : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("0644989D-91B4-45F8-92BE-9FCDE38C5C76");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddSiteSubRegion() : base
    (
      name: "Add Topography Region",
      nickname: "TopoRegion",
      description: "Given a list of curves, it adds a topography region to the active Revit document",
      category: "Revit",
      subCategory: "Site"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.TopographySurface()
        {
          Name = "Topography",
          NickName = "T",
          Description = "Topography to add a specific site region"
        }
      ),
      new ParamDefinition
      (
        new Param_Curve
        {
          Name = "Boundary",
          NickName = "B",
          Description = "Curve to create a specific detail site region",
          Access = GH_ParamAccess.list
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.TopographySurface()
        {
          Name = _Region_,
          NickName = _Region_.Substring(0, 1),
          Description = $"Output {_Region_}"
        }
      )
    };

    const string _Region_ = "Region";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Topography", out ARDB.Architecture.TopographySurface topography)) return;

      ReconstructElement<ARDB.Architecture.TopographySurface>
      (
        topography.Document, _Region_, region =>
        {
          if (region?.IsSiteSubRegion == false)
            region = null;

          // Input
          if (!Params.GetDataList(DA, "Boundary", out IList<Curve> boundary)) return null;
          var curveLoops = boundary.Select(x => x.ToCurveLoop());

          // Compute
          var subRegion = Reconstruct(region?.AsSiteSubRegion(), topography, curveLoops.ToArray());

          DA.SetData(_Region_, subRegion.TopographySurface);
          return subRegion.TopographySurface;
        }
      );
    }

    bool Reuse
    (
      ARDB.Architecture.SiteSubRegion region,
      ARDB.Architecture.TopographySurface topography,
      IList<ARDB.CurveLoop> boundary
    )
    {
      if (region is null) return false;
      if (region.HostId != topography.Id) return false;

      region.SetBoundary(boundary);
      return true;
    }

    ARDB.Architecture.SiteSubRegion Create
    (
      ARDB.Architecture.TopographySurface topography,
      IList<ARDB.CurveLoop> boundary
    )
    {
      return ARDB.Architecture.SiteSubRegion.Create(topography.Document, boundary, topography.Id);
    }

    ARDB.Architecture.SiteSubRegion Reconstruct
    (
      ARDB.Architecture.SiteSubRegion region,
      ARDB.Architecture.TopographySurface topography,
      IList<ARDB.CurveLoop> boundary
    )
    {
      if (!Reuse(region, topography, boundary))
        region = Create(topography, boundary);

      return region;
    }
  }
}

using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Filters.Obsolete
{
  using Convert.Geometry;
  using External.DB;

  [Obsolete("Obsolete since 2020-10-15")]
  public class ElementBoundingBoxFilter : Filters.ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("F5A32842-B18E-470F-8BD3-BAE1373AD982");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.hidden;
    protected override string IconTag => "B";

    public ElementBoundingBoxFilter()
    : base("BoundingBox Filter", "BBoxFltr", "Filter used to match elements by their BoundingBox", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddPointParameter("Points", "C", "Points to query", GH_ParamAccess.list);
      manager.AddNumberParameter("Tolerance", "T", "Tolerance used to query", GH_ParamAccess.item, 0.0);
      manager.AddBooleanParameter("BoundingBox", "B", "Query as a BoundingBox", GH_ParamAccess.item, true);
      manager.AddBooleanParameter("Strict", "S", "True means element should be strictly contained", GH_ParamAccess.item, false);
      base.RegisterInputParams(manager);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      base.RegisterOutputParams(manager);
      manager.AddBoxParameter("Target", "T", string.Empty, GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var points = new List<Rhino.Geometry.Point3d>();
      if (!DA.GetDataList("Points", points))
        return;

      var tolerance = 0.0;
      if (!DA.GetData("Tolerance", ref tolerance))
        return;

      var boundingBox = true;
      if (!DA.GetData("BoundingBox", ref boundingBox))
        return;

      var strict = true;
      if (!DA.GetData("Strict", ref strict))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      var targets = new List<Rhino.Geometry.Box>();
      ARDB.ElementFilter filter = null;

      if (boundingBox)
      {
        var pointsBBox = new Rhino.Geometry.BoundingBox(points);
        {
          var box = new Rhino.Geometry.Box(pointsBBox);
          box.Inflate(tolerance);
          targets.Add(box);
        }

        if (strict)
          filter = new ARDB.BoundingBoxIsInsideFilter(pointsBBox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
        else
          filter = new ARDB.BoundingBoxIntersectsFilter(pointsBBox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
      }
      else
      {
        var filters = points.ConvertAll<ARDB.ElementFilter>
        (
          x =>
          {
            var pointsBBox = new Rhino.Geometry.BoundingBox(x, x);
            {
              var box = new Rhino.Geometry.Box(pointsBBox);
              box.Inflate(tolerance);
              targets.Add(box);
            }

            if (strict)
            {
              return new ARDB.BoundingBoxIsInsideFilter(pointsBBox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
            }
            else
            {
              return new ARDB.BoundingBoxContainsPointFilter(x.ToXYZ(), tolerance / Revit.ModelUnits, inverted);
            }
          }
        );

        filter = CompoundElementFilter.Union(filters);
      }

      DA.SetData("Filter", filter);
      DA.SetDataList("Target", targets);
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Obsolete
{
  [Obsolete("Obsolete since 2020-10-22")]
  public class ElementLogicalAndFilter : Filters.ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("754C40D7-5AE8-4027-921C-0210BBDFAB37");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.hidden;
    protected override string IconTag => "∧";

    public ElementLogicalAndFilter()
    : base("Logical And Filter", "AndFltr", "Filter used to combine a set of filters that pass when any pass", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementFilter(), "Filters", "F", "Filters to combine", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var filters = new List<DB.ElementFilter>();
      if (!DA.GetDataList("Filters", filters))
        return;

      DA.SetData("Filter", CompoundElementFilter.Intersect(filters));
    }
  }

  [Obsolete("Obsolete since 2020-10-22")]
  public class ElementLogicalOrFilter : Filters.ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("61F75DE1-EE65-4AA8-B9F8-40516BE46C8D");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.hidden;
    protected override string IconTag => "∨";

    public ElementLogicalOrFilter()
    : base("Logical Or Filter", "OrFltr", "Filter used to combine a set of filters that pass when any pass", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementFilter(), "Filters", "F", "Filters to combine", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var filters = new List<DB.ElementFilter>();
      if (!DA.GetDataList("Filters", filters))
        return;

      DA.SetData("Filter", CompoundElementFilter.Union(filters));
    }
  }

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
      DB.ElementFilter filter = null;

      if (boundingBox)
      {
        var pointsBBox = new Rhino.Geometry.BoundingBox(points);
        {
          var box = new Rhino.Geometry.Box(pointsBBox);
          box.Inflate(tolerance);
          targets.Add(box);
        }

        if (strict)
          filter = new DB.BoundingBoxIsInsideFilter(pointsBBox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
        else
          filter = new DB.BoundingBoxIntersectsFilter(pointsBBox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
      }
      else
      {
        var filters = points.ConvertAll<DB.ElementFilter>
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
              return new DB.BoundingBoxIsInsideFilter(pointsBBox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
            }
            else
            {
              return new DB.BoundingBoxContainsPointFilter(x.ToXYZ(), tolerance / Revit.ModelUnits, inverted);
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

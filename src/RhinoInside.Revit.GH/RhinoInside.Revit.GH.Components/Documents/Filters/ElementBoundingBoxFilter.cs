using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents.Filters
{
  public class ElementBoundingBoxFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("F5A32842-B18E-470F-8BD3-BAE1373AD982");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "B";

    public ElementBoundingBoxFilter()
    : base("Element.BoundingBoxFilter", "BoundingBox Filter", "Filter used to match elements by their BoundingBox", "Revit", "Filter")
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

      var scaleFactor = 1.0 / Revit.ModelUnits;

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

        pointsBBox = pointsBBox.ChangeUnits(scaleFactor);
        var outline = new DB.Outline(pointsBBox.Min.ToHost(), pointsBBox.Max.ToHost());

        if (strict)
          filter = new DB.BoundingBoxIsInsideFilter(outline, tolerance * scaleFactor, inverted);
        else
          filter = new DB.BoundingBoxIntersectsFilter(outline, tolerance * scaleFactor, inverted);
      }
      else
      {
        var filters = points.Select<Rhino.Geometry.Point3d, DB.ElementFilter>
                     (x =>
                     {
                       var pointsBBox = new Rhino.Geometry.BoundingBox(x, x);
                       {
                         var box = new Rhino.Geometry.Box(pointsBBox);
                         box.Inflate(tolerance);
                         targets.Add(box);
                       }

                       x = x.ChangeUnits(scaleFactor);

                       if (strict)
                       {
                         var outline = new DB.Outline(x.ToHost(), x.ToHost());
                         return new DB.BoundingBoxIsInsideFilter(outline, tolerance * scaleFactor, inverted);
                       }
                       else
                       {
                         return new DB.BoundingBoxContainsPointFilter(x.ToHost(), tolerance * scaleFactor, inverted);
                       }
                     });

        var filterList = filters.ToArray();
        filter = filterList.Length == 1 ?
                 filterList[0] :
                 new DB.LogicalOrFilter(filterList);
      }

      DA.SetData("Filter", filter);
      DA.SetDataList("Target", targets);
    }
  }
}

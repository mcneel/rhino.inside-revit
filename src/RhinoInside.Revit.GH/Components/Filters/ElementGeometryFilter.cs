using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Filters
{
  public class ElementBoundingBoxFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("3B8BE676-390B-4BE1-B6DA-C02FFA3234B6");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "B";

    public ElementBoundingBoxFilter()
    : base("Bounding Box Filter", "BBoxFltr", "Filter used to match elements by their BoundingBox", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddGeometryParameter("Bounding Box", "B", "World aligned bounding box to query", GH_ParamAccess.list);
      manager.AddBooleanParameter("Union", "U", "Target union of bounding boxes.", GH_ParamAccess.item, true);
      manager.AddBooleanParameter("Strict", "S", "True means element should be strictly contained", GH_ParamAccess.item, false);
      manager.AddNumberParameter("Tolerance", "T", "Tolerance used to query", GH_ParamAccess.item, 0.0);
      base.RegisterInputParams(manager);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      base.RegisterOutputParams(manager);
      manager.AddBoxParameter("Target", "T", string.Empty, GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var geometries = new List<IGH_GeometricGoo>();
      if (!DA.GetDataList("Bounding Box", geometries))
        return;

      var union = true;
      if (!DA.GetData("Union", ref union))
        return;

      var strict = true;
      if (!DA.GetData("Strict", ref strict))
        return;

      var tolerance = 0.0;
      if (!DA.GetData("Tolerance", ref tolerance))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      var targets = new List<Rhino.Geometry.Box>();
      DB.ElementFilter filter = null;

      var boundingBoxes = geometries.Select(x => x?.Boundingbox ?? Rhino.Geometry.BoundingBox.Empty).Where(x => x.IsDegenerate(0.0) < 4);
      if (boundingBoxes.Any())
      {
        if (union)
        {
          var bbox = Rhino.Geometry.BoundingBox.Empty;
          foreach (var boundingBox in boundingBoxes)
            bbox.Union(boundingBox);

          {
            var target = new Rhino.Geometry.Box(bbox);
            target.Inflate(tolerance);
            targets.Add(target);
          }

          if (bbox.IsDegenerate(0.0) == 3)
            filter = new DB.BoundingBoxContainsPointFilter(bbox.Center.ToXYZ(), Math.Abs(tolerance) / Revit.ModelUnits, inverted);
          else if (strict)
            filter = new DB.BoundingBoxIsInsideFilter(bbox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
          else
            filter = new DB.BoundingBoxIntersectsFilter(bbox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
        }
        else
        {
          var filters = boundingBoxes.Select<Rhino.Geometry.BoundingBox, DB.ElementFilter>
          (
            x =>
            {
              {
                var target = new Rhino.Geometry.Box(x);
                target.Inflate(tolerance);
                targets.Add(target);
              }

              var bbox = x;
              var degenerate = bbox.IsDegenerate(0.0);
              if (degenerate == 3)
                return new DB.BoundingBoxContainsPointFilter(bbox.Center.ToXYZ(), Math.Abs(tolerance) / Revit.ModelUnits, inverted);
              else if (strict)
                return new DB.BoundingBoxIsInsideFilter(bbox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
              else
                return new DB.BoundingBoxIntersectsFilter(bbox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
            }
          );

          var filterList = filters.ToArray();
          filter = filterList.Length == 1 ?
                   filterList[0] :
                   new DB.LogicalOrFilter(filterList);
        }
      }

      DA.SetData("Filter", filter);
      DA.SetDataList("Target", targets);
    }
  }

  public class ElementIntersectsElementFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("D1E4C98D-E550-4F25-991A-5061EF845C37");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "I";

    public ElementIntersectsElementFilter()
    : base("Intersects Element Filter", "ElemFltr", "Filter used to match elements that intersect to the given element", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", "Element to match", GH_ParamAccess.item);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Element element = null;
      if (!DA.GetData("Element", ref element))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new DB.ElementIntersectsElementFilter(element, inverted));
    }
  }

  public class ElementIntersectsBrepFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("A8889824-F607-4465-B84F-16DF79DD08AB");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "I";

    public ElementIntersectsBrepFilter()
    : base("Intersects Brep Filter", "BrepFltr", "Filter used to match elements that intersect to the given brep", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddBrepParameter("Brep", "B", "Brep to match", GH_ParamAccess.item);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Rhino.Geometry.Brep brep = null;
      if (!DA.GetData("Brep", ref brep))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      if (brep.ToSolid() is DB.Solid solid)
        DA.SetData("Filter", new DB.ElementIntersectsSolidFilter(solid, inverted));
      else
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to convert Brep");
    }
  }

  public class ElementIntersectsMeshFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("09F9E451-F6C9-42FB-90E3-85E9923998A2");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "I";

    public ElementIntersectsMeshFilter()
    : base("Intersects Mesh Filter", "MeshFltr", "Filter used to match elements that intersect to the given mesh", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddMeshParameter("Mesh", "B", "Mesh to match", GH_ParamAccess.item);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Rhino.Geometry.Mesh mesh = null;
      if (!DA.GetData("Mesh", ref mesh))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      if (mesh.ToSolid() is DB.Solid solid)
        DA.SetData("Filter", new DB.ElementIntersectsSolidFilter(solid, inverted));
      else
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to convert Mesh");
    }
  }
}

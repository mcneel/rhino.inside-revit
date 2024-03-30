using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Filters
{
  using Convert.Geometry;
  using External.DB;

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
      ARDB.ElementFilter filter = null;

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
            filter = new ARDB.BoundingBoxContainsPointFilter(bbox.Center.ToXYZ(), Math.Abs(tolerance) / Revit.ModelUnits, inverted);
          else if (strict)
            filter = new ARDB.BoundingBoxIsInsideFilter(bbox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
          else
            filter = new ARDB.BoundingBoxIntersectsFilter(bbox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
        }
        else
        {
          var filters = boundingBoxes.Select<Rhino.Geometry.BoundingBox, ARDB.ElementFilter>
          (
            x =>
            {
              {
                var target = new Rhino.Geometry.Box(x);
                target.Inflate(tolerance);
                targets.Add(target);
              }

              var bbox = x;
              var degenerate = bbox.IsDegenerate(GeometryTolerance.Model.VertexTolerance);
              if (degenerate == 3)
                return new ARDB.BoundingBoxContainsPointFilter(bbox.Center.ToXYZ(), Math.Abs(tolerance) / Revit.ModelUnits, inverted);
              else if (strict)
                return CompoundElementFilter.BoundingBoxIsInsideFilter(bbox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
              else
                return CompoundElementFilter.BoundingBoxIntersectsFilter(bbox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
            }
          );

          filter = inverted ? CompoundElementFilter.Intersect(filters.ToArray()) : CompoundElementFilter.Union(filters.ToArray());
        }

        if (inverted)
          filter = filter.Intersect(CompoundElementFilter.ElementHasBoundingBoxFilter);
      }

      DA.SetData("Filter", filter);
      DA.SetDataList("Target", targets);
    }
  }

  [ComponentVersion(introduced: "1.14")]
  public class ElementElevationFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("DDA08563-C19D-41FE-B492-E00C1111D91A");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "E";

    public ElementElevationFilter()
    : base("Elevation Filter", "ElevFltr", "Filter used to match elements located at specific elevation range", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager[manager.AddParameter(new Parameters.ProjectElevation(), "Base", "B", "Base elevation", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.ProjectElevation(), "Top", "T", "Top elevation", GH_ParamAccess.item)].Optional = true;
      manager.AddBooleanParameter("Strict", "S", "True means element should be strictly contained", GH_ParamAccess.item, false);
      manager.AddNumberParameter("Tolerance", "T", "Tolerance used to query", GH_ParamAccess.item, 0.0);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.TryGetData(DA, "Base", out Types.ProjectElevation baseElevation)) return;
      if (!Params.TryGetData(DA, "Top", out Types.ProjectElevation topElevation)) return;
      if (!Params.GetData(DA, "Strict", out bool? strict)) return;
      if (!Params.GetData(DA, "Tolerance", out double? tolerance)) return;
      if (!Params.GetData(DA, "Inverted", out bool? inverted)) return;

      var limits = CompoundElementFilter.BoundingBoxLimits;
      var zMin = baseElevation?.Value.IsElevation(out var be) is true ? be : -limits;
      var zMax = topElevation ?.Value.IsElevation(out var te) is true ? te : +limits;
      var min = new ARDB.XYZ(-limits, -limits, zMin);
      var max = new ARDB.XYZ(+limits, +limits, zMax);

      using (var outline = new ARDB.Outline(min, max))
      {
        switch (strict)
        {
          case true:
            DA.SetData("Filter", CompoundElementFilter.BoundingBoxIsInsideFilter(outline, GeometryEncoder.ToInternalLength(tolerance.Value), inverted.Value));
            break;

          case false:
            DA.SetData("Filter", CompoundElementFilter.BoundingBoxIntersectsFilter(outline, GeometryEncoder.ToInternalLength(tolerance.Value), inverted.Value));
            break;
        }
      }
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
      ARDB.Element element = null;
      if (!DA.GetData("Element", ref element))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new ARDB.ElementIntersectsElementFilter(element, inverted));
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

      if (brep.ToSolid() is ARDB.Solid solid)
        DA.SetData("Filter", new ARDB.ElementIntersectsSolidFilter(solid, inverted));
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

      if (mesh.ToSolid() is ARDB.Solid solid)
        DA.SetData("Filter", new ARDB.ElementIntersectsSolidFilter(solid, inverted));
      else
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to convert Mesh");
    }
  }
}

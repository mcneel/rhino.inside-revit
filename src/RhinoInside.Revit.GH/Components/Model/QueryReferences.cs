using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Geometry
{
  using Convert.Geometry;
  using External.DB;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.36")]
  public class QueryReferences : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("60F89F09-9391-4AE6-B9C6-75AB9F4879FE");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    static readonly ARDB.ElementFilter elementFilter = ElementFilters.ElementHasBoundingBoxFilter;
    protected override ARDB.ElementFilter ElementFilter => elementFilter;

    public QueryReferences() : base
    (
      name: "Query Geometry References",
      nickname: "QG-References",
      description: "Get the geometry references that intersect to the input ray.",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.View3D()
        {
          Name = "View",
          NickName = "V",
          Description = "View where perform the test.",
        }
      ),
      new ParamDefinition
      (
        new Param_Line()
        {
          Name = "Ray",
          NickName = "R",
          Description = "Ray to test with.",
        }
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Distance",
          NickName = "D",
          Description = "Max distance from ray start point to test with.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Integer()
        {
          Name = "Limit",
          NickName = "L",
          Description = "Max number of references to query for.",
          Optional= true
        }.SetDefaultVale(1)
        , ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.ElementFilter()
        {
          Name = "Filter",
          NickName = "F",
          Description = "Element Filter.",
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GeometryObject()
        {
          Name = "References",
          NickName = "R",
          Description = "Geometry references",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Plane()
        {
          Name = "Points",
          NickName = "X",
          Description = "Points of intersection",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Proximity",
          NickName = "P",
          Description = "Distance of intersection",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View3D view)) return;
      if (!Params.GetData(DA, "Ray", out Rhino.Geometry.Line? line)) return;
      if (Params.GetData(DA, "Distance", out double? distance) && double.IsNaN(distance.Value)) return;
      if (Params.GetData(DA, "Limit", out int? limit) && limit == 0) return;
      if (!Params.TryGetData(DA, "Filter", out Types.ElementFilter filter)) return;
      if (filter?.Value.IsEmpty() is true) return;

      limit ??= int.MaxValue;
      distance ??= double.PositiveInfinity;
      distance = GeometryEncoder.ToInternalLength(distance.Value);
      filter?.Value.AssertIsValidFiler(ExploreLinkedModels);

      using (var intersector = new ARDB.ReferenceIntersector
      (
        ElementFilters.ElementHasBoundingBoxFilter.Intersect(filter?.Value),
        ARDB.FindReferenceTarget.All,
        view.Value)
        { FindReferencesInRevitLinks = ExploreLinkedModels }
      )
      {
        IEnumerable<ARDB.ReferenceWithContext> result = Array.Empty<ARDB.ReferenceWithContext>();
        var origin = line.Value.From.ToXYZ();
        var direction = distance < 0.0 ? -line.Value.Direction.ToXYZ() : line.Value.Direction.ToXYZ();

        if (limit < 0)
        {
          result = intersector.Find(origin, direction).
              OrderByDescending(x => x.Proximity).
              SkipWhile(x => !double.IsInfinity(distance.Value) && Math.Abs(distance.Value) <= x.Proximity).
              ToArray();
        }
        else if (limit == 1)
        {
          if (intersector.FindNearest(origin, direction) is ARDB.ReferenceWithContext nearest)
          {
            if (Math.Abs(distance.Value) >= nearest.Proximity)
              result = new ARDB.ReferenceWithContext[] { nearest };
          }
        }
        else if (limit > 1)
        {
          result = intersector.Find(origin, direction).
              OrderBy(x => x.Proximity).
              TakeWhile(x => double.IsInfinity(distance.Value) || Math.Abs(distance.Value) >= x.Proximity).
              ToArray();
        }

        result = result.
          Where(x => x.GetReference().ElementReferenceType != ARDB.ElementReferenceType.REFERENCE_TYPE_NONE).
          Take(Math.Abs(limit.Value)).
          TakeWhileIsNotEscapeKeyDown(this);

        Params.TrySetDataList(DA, "References", () => result.Select(x => view.GetGeometryObjectFromReference<Types.GeometryObject>(x.GetReference())));
        Params.TrySetDataList(DA, "Points", () => result.Select(x => line.Value.PointAtLength(x.Proximity * Revit.ModelUnits)));
        Params.TrySetDataList(DA, "Proximity", () => result.Select(x => x.Proximity * Revit.ModelUnits));
      }
    }

    protected override void AfterSolveInstance()
    {
      base.AfterSolveInstance();
      Message = ExploreLinkedModels ? "Explore Links" : string.Empty;
    }

    public override bool NeedsToBeExpired(ARDB.Document document, ISet<ARDB.ElementId> added, ISet<ARDB.ElementId> deleted, ISet<ARDB.ElementId> modified)
    {
      // Check if the change is on a document this component is querying.
      if (!MayNeedToBeExpired(document))
        return false;

      // Check inputs with persistent data
      if (base.NeedsToBeExpired(document, added, deleted, modified))
        return true;

      foreach (var output in Params.Output.OfType<Kernel.IGH_ReferenceParam>())
      {
        if (output.NeedsToBeExpired(document, added, deleted, modified))
          return true;
      }

      if (Params.Input<Parameters.View3D>("View") is Parameters.View3D views)
      {
        var ids = new List<ARDB.ElementId>(added.Count + deleted.Count + modified.Count);
        ids.AddRange(added);
        ids.AddRange(deleted);
        ids.AddRange(modified);
        foreach (var view in views.VolatileData.AllData(true).OfType<Types.View>().Where(x => document.Equals(x.Document)))
        {
          using (var collector = new ARDB.FilteredElementCollector(view.Document, view.Id))
          {
            collector.WherePasses(ElementFilters.ExclusionFilter(ids, inverted: true));
            if (collector.Any())
              return true;
          }
        }
      }

      return false;
    }

    #region UI
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Explore linked models", IncludeLinkedElementsClicked, true, ExploreLinkedModels);
    }

    private bool ExploreLinkedModels
    {
      get => GetValue(nameof(ExploreLinkedModels), true);
      set
      {
        if (ExploreLinkedModels == value) return;
        SetValue(nameof(ExploreLinkedModels), value);
      }
    }

    void IncludeLinkedElementsClicked(object sender, EventArgs e)
    {
      RecordUndoEvent("Toggle explore linked models");
      ExploreLinkedModels = !ExploreLinkedModels;
      ExpireSolution(true);
    }
    #endregion
  }
}

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
  public class QueryReferences : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("60F89F09-9391-4AE6-B9C6-75AB9F4879FE");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    public QueryReferences() : base
    (
      name: "Query References",
      nickname: "Q-References",
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
          Name = "Radius",
          NickName = "R",
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
          Name = "Elements",
          NickName = "E",
          Description = "Element references",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GeometryFace()
        {
          Name = "Faces",
          NickName = "F",
          Description = "Element face references",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.GeometryCurve()
        {
          Name = "Edges",
          NickName = "E",
          Description = "Element edge references",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.GeometryObject()
        {
          Name = "Meshes",
          NickName = "M",
          Description = "Element mesh references",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.GeometryCurve()
        {
          Name = "Curves",
          NickName = "C",
          Description = "Element line references",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Secondary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View3D view)) return;
      if (!Params.GetData(DA, "Ray", out Rhino.Geometry.Line? line)) return;
      if (Params.GetData(DA, "Radius", out double? radius) && double.IsNaN(radius.Value)) return;
      if (Params.GetData(DA, "Limit", out int? limit) && limit == 0) return;
      if (!Params.TryGetData(DA, "Filter", out Types.ElementFilter filter)) return;

      limit ??= int.MaxValue;
      radius ??= double.PositiveInfinity;
      radius = GeometryEncoder.ToInternalLength(radius.Value);

      var referenceTarget = 0;
      if (Params.IndexOfOutputParam("Elements") >=0) referenceTarget |= (int) ARDB.FindReferenceTarget.Element;
      if (Params.IndexOfOutputParam("Faces") >= 0) referenceTarget |= (int) ARDB.FindReferenceTarget.Face;
      if (Params.IndexOfOutputParam("Edges") >= 0) referenceTarget |= (int) ARDB.FindReferenceTarget.Edge;
      if (Params.IndexOfOutputParam("Meshes") >= 0) referenceTarget |= (int) ARDB.FindReferenceTarget.Mesh;
      if (Params.IndexOfOutputParam("Curves") >= 0) referenceTarget |= (int) ARDB.FindReferenceTarget.Curve;
      if (referenceTarget == 0) return;
      if (!Enum.IsDefined(typeof(ARDB.FindReferenceTarget), referenceTarget)) referenceTarget = (int) ARDB.FindReferenceTarget.All;

      using (var intersector = new ARDB.ReferenceIntersector
      (
        CompoundElementFilter.ElementHasBoundingBoxFilter.Union(filter?.Value),
        (ARDB.FindReferenceTarget) referenceTarget,
        view.Value)
        { FindReferencesInRevitLinks = ExploreLinkedModels }
      )
      {
        IEnumerable<ARDB.Reference> result = Array.Empty<ARDB.Reference>();
        var origin = line.Value.From.ToXYZ();
        var direction = radius < 0.0 ? -line.Value.Direction.ToXYZ() : line.Value.Direction.ToXYZ();

        if (limit < 0)
        {
          result = intersector.Find(origin, direction).
              OrderByDescending(x => x.Proximity).
              SkipWhile(x => !double.IsInfinity(radius.Value) && Math.Abs(radius.Value) <= x.Proximity).
              Select(x => x.GetReference()).
              ToArray();
        }
        else if (limit == 1)
        {
          if (intersector.FindNearest(origin, direction) is ARDB.ReferenceWithContext nearest)
          {
            if (Math.Abs(radius.Value) >= nearest.Proximity)
              result = new ARDB.Reference[] { nearest.GetReference() };
          }
        }
        else if (limit > 1)
        {
          result = intersector.Find(origin, direction).
              OrderBy(x => x.Proximity).
              TakeWhile(x => double.IsInfinity(radius.Value) || Math.Abs(radius.Value) >= x.Proximity).
              Select(x => x.GetReference()).
              ToArray();
        }

        result = result.TakeWhileIsNotEscapeKeyDown(this);

        Params.TrySetDataList
        (
          DA, "Elements",
          () => result.
          Where(x => intersector.FindReferencesInRevitLinks || x.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_NONE).
          Select(view.GetGeometryElementFromReference).
          Distinct().
          Take(Math.Abs(limit.Value))
        );
        Params.TrySetDataList
        (
          DA, "Faces",
          () => result.
          Where(x => x.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_SURFACE).
          Take(Math.Abs(limit.Value)).
          Select(x => view.GetGeometryObjectFromReference<Types.GeometryFace>(x))
        );
        Params.TrySetDataList
        (
          DA, "Edges",
          () => result.
          Where(x => x.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_LINEAR).
          Where(x => (referenceTarget & (int)ARDB.FindReferenceTarget.Curve) == 0 || x.IsKindOf<ARDB.Edge>(view.ReferenceDocument)).
          Take(Math.Abs(limit.Value)).
          Select(x => view.GetGeometryObjectFromReference<Types.GeometryCurve>(x))
        );
        Params.TrySetDataList
        (
          DA, "Meshes",
          () => result.
          Where(x => x.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_MESH).
          Take(Math.Abs(limit.Value)).
          Select(x => view.GetGeometryObjectFromReference<Types.GeometryMesh>(x))
        );
        Params.TrySetDataList
        (
          DA, "Curves",
          () => result.
          Where(x => x.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_LINEAR).
          Where(x => (referenceTarget & (int) ARDB.FindReferenceTarget.Edge) == 0 || !x.IsKindOf<ARDB.Edge>(view.ReferenceDocument)).
          Take(Math.Abs(limit.Value)).
          Select(x => view.GetGeometryObjectFromReference<Types.GeometryCurve>(x))
        );
      }
    }

    protected override void AfterSolveInstance()
    {
      base.AfterSolveInstance();
      Message = ExploreLinkedModels ? "Explore Links" : string.Empty;
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

using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Geometry
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.14", updated: "1.15")]
  public class ElementGeometryReferences : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("BBD8187B-829A-4604-B6BC-DE896A9FF62B");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    public ElementGeometryReferences() : base
    (
      name: "Element References",
      nickname: "E-References",
      description: "Retrieves geometry references of given element.",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to query for references",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Invisble",
          NickName = "I",
          Description = "Include non visible geometry",
          Optional = true
        }.SetDefaultVale(false), ParamRelevance.Secondary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Deconstructed element",
          DataMapping = GH_DataMapping.Graft
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.GeometryFace()
        {
          Name = "Faces",
          NickName = "F",
          Description = "List of element face references",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GeometryCurve()
        {
          Name = "Edges",
          NickName = "E",
          Description = "List of element curve references",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GeometryCurve()
        {
          Name = "Curves",
          NickName = "C",
          Description = "List of element curve references",
          Access = GH_ParamAccess.list
        },ParamRelevance.Primary
      ),
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Output<IGH_Param>("Lines") is IGH_Param curves)
        curves.Name = "Curves";

      base.AddedToDocument(document);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.GraphicalElement element)) return;
      else Params.TrySetData(DA, "Element", () => element);

      if (!Params.TryGetData(DA, "Invisble", out bool? invisibles)) return;

      using (var options = new ARDB.Options() { ComputeReferences = true, IncludeNonVisibleObjects = invisibles ?? false })
      {
        if (element.Value.GetGeometry(options) is ARDB.GeometryElement geometry)
        {
          Params.TrySetDataList(DA, "Faces", () =>
            geometry.GetFaceReferences(element.Value).Select(element.GetGeometryObjectFromReference<Types.GeometryFace>));

          Params.TrySetDataList(DA, "Edges", () =>
            geometry.GetEdgeReferences(element.Value).Select(element.GetGeometryObjectFromReference<Types.GeometryCurve>));

          Params.TrySetDataList(DA, "Curves", () =>
            geometry.GetLineReferences(element.Value).Select(element.GetGeometryObjectFromReference<Types.GeometryCurve>));
        }
      }
    }
  }

  [ComponentVersion(introduced: "1.15")]
  public class CurvePointReferences : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("6388CFC0-E31E-4A16-8088-A7BBB9587442");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    public CurvePointReferences() : base
    (
      name: "Curve Point References",
      nickname: "CP-References",
      description: "Get point references of given curve.",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.GeometryCurve()
        {
          Name = "Curve",
          NickName = "C",
          Description = "Curve to extract points",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GeometryPoint()
        {
          Name = "Start",
          NickName = "S",
          Description = "Curve start point",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GeometryPoint()
        {
          Name = "End",
          NickName = "E",
          Description = "Curve end point",
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Curve", out Types.GeometryCurve curve)) return;

      Params.TrySetData(DA, "Start", () => curve.StartPoint);
      Params.TrySetData(DA, "End", () => curve.EndPoint);
    }
  }

  class EdgeFaceReferences : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("8F9DEA82-2A13-466E-9A43-9B79C023E896");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    public EdgeFaceReferences() : base
    (
      name: "Edge Face References",
      nickname: "EF-References",
      description: "Get face references of given edge.",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.GeometryCurve()
        {
          Name = "Edge",
          NickName = "E",
          Description = "Edge to extract adjacent faces",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GeometryFace()
        {
          Name = "Left",
          NickName = "L",
          Description = "Left face",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GeometryPoint()
        {
          Name = "Right",
          NickName = "R",
          Description = "Right face",
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Edge", out Types.GeometryCurve curve)) return;

      Params.TrySetData(DA, "Start", () => curve.LeftFace);
      Params.TrySetData(DA, "End", () => curve.RightFace);
    }
  }

  class FaceEdgeReferences : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("F653D38F-E692-4120-BE0E-F3F8DCEBD368");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    public FaceEdgeReferences() : base
    (
      name: "Face Edge References",
      nickname: "FE-References",
      description: "Get edge references of given face.",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.GeometryFace()
        {
          Name = "Face",
          NickName = "F",
          Description = "Face to extract edges",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GeometryCurve()
        {
          Name = "Edges",
          NickName = "E",
          Description = "Face edges",
          Access = GH_ParamAccess.tree
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Face", out Types.GeometryFace face)) return;

      Params.TrySetDataTree(DA, "Edges", () => face.EdgeLoops);
    }
  }

  class GeometryReferenceElements : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("3EC6A941-9C34-4687-82E1-D7D9FE48064C");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    public GeometryReferenceElements() : base
    (
      name: "Reference Elements",
      nickname: "R-Elements",
      description: "Get the elements that interact to generate the input geometry reference.",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.GeometryObject()
        {
          Name = "Reference",
          NickName = "R",
          Description = "Geometry reference to inspect.",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Generating elements",
          Access = GH_ParamAccess.list
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Reference", out Types.GeometryObject geometry)) return;

      Params.TrySetDataList(DA, "Elements", () =>
      {
        var element = Types.Element.FromReference(geometry.ReferenceDocument, geometry.GetReference());
        return element.Value.GetGeneratingElementIds(geometry.Value).Select(x => element.GetElement<Types.Element>(x));
      });
    }
  }

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
      if (Params.GetData(DA, "Radius", out double? radius) == double.IsNaN(radius.Value)) return;
      if (Params.GetData(DA, "Limit", out int? limit) && limit == 0) return;
      if (!Params.TryGetData(DA, "Filter", out Types.ElementFilter filter)) return;

      limit ??= int.MaxValue;
      radius ??= double.PositiveInfinity;

      var referenceTarget = 0;
      if (Params.IndexOfOutputParam("Elements") >=0) referenceTarget |= (int) ARDB.FindReferenceTarget.Element;
      if (Params.IndexOfOutputParam("Faces") >= 0) referenceTarget |= (int) ARDB.FindReferenceTarget.Face;
      if (Params.IndexOfOutputParam("Edges") >= 0) referenceTarget |= (int) ARDB.FindReferenceTarget.Edge;
      if (Params.IndexOfOutputParam("Meshes") >= 0) referenceTarget |= (int) ARDB.FindReferenceTarget.Mesh;
      if (Params.IndexOfOutputParam("Curves") >= 0) referenceTarget |= (int) ARDB.FindReferenceTarget.Curve;
      if (referenceTarget == 0) return;
      if (!Enum.IsDefined(typeof(ARDB.FindReferenceTarget), referenceTarget)) referenceTarget = (int) ARDB.FindReferenceTarget.All;

      using (var isector = new ARDB.ReferenceIntersector(filter?.Value ?? new ARDB.VisibleInViewFilter(view.Document, view.Id), (ARDB.FindReferenceTarget) referenceTarget, view.Value))
      {
        IEnumerable<ARDB.Reference> result = Array.Empty<ARDB.Reference>();

        if (limit < 0)
        {
          result = isector.Find(line.Value.From.ToXYZ(), radius < 0.0 ? -line.Value.Direction.ToXYZ() : line.Value.Direction.ToXYZ()).
              OrderByDescending(x => x.Proximity).
              SkipWhile(x => !double.IsInfinity(radius.Value) && Math.Abs(radius.Value) >= x.Proximity).
              Take(limit.Value).
              Select(x => x.GetReference()).
              ToArray();
        }
        else if (limit == 1)
        {
          if (isector.FindNearest(line.Value.From.ToXYZ(), line.Value.Direction.ToXYZ()) is ARDB.ReferenceWithContext nearest)
          {
            if (Math.Abs(radius.Value) >= nearest.Proximity)
              result = new ARDB.Reference[] { nearest.GetReference() };
          }
        }
        else if (limit > 1)
        {
          result = isector.Find(line.Value.From.ToXYZ(), line.Value.Direction.ToXYZ()).
              OrderBy(x => x.Proximity).
              TakeWhile(x => double.IsInfinity(radius.Value) || Math.Abs(radius.Value) >= x.Proximity).
              Take(limit.Value).
              Select(x => x.GetReference()).
              ToArray();
        }

        result = result.TakeWhileIsNotEscapeKeyDown(this);

        Params.TrySetDataList
        (
          DA, "Elements",
          () => result.
          Where(x => x.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_NONE).
          Select(x => view.GetGeometryObjectFromReference<Types.GeometryElement>(x))
        );
        Params.TrySetDataList
        (
          DA, "Faces",
          () => result.
          Where(x => x.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_SURFACE).
          Select(x => view.GetGeometryObjectFromReference<Types.GeometryFace>(x))
        );
        Params.TrySetDataList
        (
          DA, "Edges",
          () => result.
          Where(x => x.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_LINEAR).
          Where(x => (referenceTarget & (int)ARDB.FindReferenceTarget.Curve) == 0 || x.IsKindOf<ARDB.Edge>(view.ReferenceDocument)).
          Select(x => view.GetGeometryObjectFromReference<Types.GeometryCurve>(x))
        );
        Params.TrySetDataList
        (
          DA, "Meshes",
          () => result.
          Where(x => x.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_MESH).
          Select(x => view.GetGeometryObjectFromReference<Types.GeometryMesh>(x))
        );
        Params.TrySetDataList
        (
          DA, "Curves",
          () => result.
          Where(x => x.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_LINEAR).
          Where(x => (referenceTarget & (int) ARDB.FindReferenceTarget.Edge) == 0 || !x.IsKindOf<ARDB.Edge>(view.ReferenceDocument)).
          Select(x => view.GetGeometryObjectFromReference<Types.GeometryCurve>(x))
        );
      }
    }
  }
}

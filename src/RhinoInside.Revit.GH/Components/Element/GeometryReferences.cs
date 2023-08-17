using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Geometry
{
  using External.DB.Extensions;
  using Convert.Geometry;
  using External.DB;

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
          Name = "Lines",
          NickName = "L",
          Description = "List of element line references",
          Access = GH_ParamAccess.list
        },ParamRelevance.Primary
      ),
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Output<IGH_Param>("Curves") is IGH_Param curves)
        curves.Name = "Edges";

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

          Params.TrySetDataList(DA, "Lines", () =>
            geometry.GetLineReferences(element.Value).Select(element.GetGeometryObjectFromReference<Types.GeometryCurve>));
        }
      }
    }
  }

  [ComponentVersion(introduced: "1.15")]
  public class ElementPointReferences : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("6388CFC0-E31E-4A16-8088-A7BBB9587442");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    public ElementPointReferences() : base
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

  class ReferenceIntersector : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("60F89F09-9391-4AE6-B9C6-75AB9F4879FE");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    public ReferenceIntersector() : base
    (
      name: "Reference Intersector",
      nickname: "R-Intersector",
      description: "Get the elements that intersect to the input ray reference.",
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
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View3D view)) return;
      if (!Params.GetData(DA, "Ray", out Rhino.Geometry.Line? line)) return;
      if (!Params.TryGetData(DA, "Filter", out Types.ElementFilter filter)) return;

      using (var isector = new ARDB.ReferenceIntersector(filter?.Value ?? CompoundElementFilter.ElementHasBoundingBoxFilter, ARDB.FindReferenceTarget.Element, view.Value))
      {
        var result = isector.Find(line.Value.From.ToXYZ(), line.Value.Direction.ToXYZ());
        Params.TrySetDataList
        (
          DA, "Elements",
          () => result.OrderBy(x => x.Proximity).
          Select(x => view.GetGeometryObjectFromReference<Types.GeometryElement>(x.GetReference()))
        );
      }
    }
  }
}

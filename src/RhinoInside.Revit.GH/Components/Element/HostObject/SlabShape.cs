using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.Element.HostObject
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.9")]
  public class SlabShape : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("516B2771-0A9A-4F87-9DB1-E27FE0FA968B");

    public SlabShape() : base
    (
      name: "Host Sub Elements",
      nickname: "SubElems",
      description: "Manipulates points and edges on a slab, roof or floor.",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.HostObject()
        {
          Name = "Host",
          NickName = "H",
          Description = "Floor or Roof.",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.SlabShapeEditCurvedEdgeCondition>()
        {
          Name = "Curved Edge Condition",
          NickName = "CEC",
          Optional = true
        }.SetDefaultVale(ERDB.SlabShapeEditCurvedEdgeCondition.ProjectToSide), ParamRelevance.Tertiary
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Points",
          NickName = "P",
          Optional = true,
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Line()
        {
          Name = "Creases",
          NickName = "C",
          Optional = true,
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.HostObject()
        {
          Name = "Host",
          NickName = "H",
          Description = "Floor or Roof.",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.SlabShapeEditCurvedEdgeCondition>()
        {
          Name = "Curved Edge Condition",
          NickName = "CEC",
        }, ParamRelevance.Tertiary
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Corners",
          NickName = "C",
          Description = "Corner points",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Exteriors",
          NickName = "E",
          Description = "Boundary points",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Interiors",
          NickName = "I",
          Description = "Interior points",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Curve()
        {
          Name = "Boundary",
          NickName = "B",
          Description = "Boundary curves",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Tertiary
      ),
      new ParamDefinition
      (
        new Param_Curve()
        {
          Name = "Creases",
          NickName = "C",
          Description = "User drawn curves",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Curve()
        {
          Name = "Auto",
          NickName = "A",
          Description = "Automatic curves",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Occasional
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Host", out Types.HostObject host)) return;

      var shape = default(ARDB.SlabShapeEditor);
      switch (host)
      {
        case Types.Floor floor: shape = floor.Value.SlabShapeEditor; break;
        case Types.Roof roof:   shape = roof.Value.SlabShapeEditor; break;
      }

      if (shape is null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Only flat and horizontal slabs, floors or roofs are valid for '{Name}'.");
        return;
      }

      Params.TrySetData(DA, "Host", () => host);

      var curvedEdgeConditionParam = host.Value.get_Parameter(ARDB.BuiltInParameter.HOST_SSE_CURVED_EDGE_CONDITION_PARAM);
      var curvedEdgeConditionValue = curvedEdgeConditionParam?.AsInteger();

      var vertices = new Dictionary<Point3d, ARDB.SlabShapeVertex>();
      ARDB.SlabShapeVertex AddVertex(Point3d point)
      {
        if (vertices.TryGetValue(point, out var vertex)) return vertex;
        vertex = shape.DrawPoint(point.ToXYZ());

        if (vertex is null)
          AddGeometryRuntimeError(GH_RuntimeMessageLevel.Warning, "Failed to draw vertex", new Point(point));
        else
          vertices.Add(point, vertex);

        return vertex;
      }

      if
      (
        (Params.GetDataList(DA, "Points", out IList<Point3d> points)  && points is object) |
        (Params.GetDataList(DA, "Creases", out IList<Line> edges)     && edges is object)
      )
      {
        var tol = GeometryTolerance.Model;
        StartTransaction(host.Document);
        host.InvalidateGraphics();

        shape.Enable();
        shape.ResetSlabShape();

        if (points is object)
        {
          foreach (var point in points)
            AddVertex(point);
        }

        if (edges is object)
        {
          foreach (var edge in edges)
          {
            if (!edge.IsValid) continue;
            if (edge.Length < tol.VertexTolerance) continue;
            try
            {
              var from = AddVertex(edge.From);
              var to = AddVertex(edge.To);
              if (from is null && to is null) continue;

              shape.DrawSplitLine(from, to);
            }
            catch { AddGeometryRuntimeError(GH_RuntimeMessageLevel.Warning, "Failed to draw crease", new LineCurve(edge)); }
          }
        }
      }

      if (!Params.GetData(DA, "Curved Edge Condition", out int? curvedEdgeCondition))
        curvedEdgeCondition = curvedEdgeConditionValue;

      curvedEdgeConditionValue = curvedEdgeConditionParam?.AsInteger();
      if (curvedEdgeCondition != curvedEdgeConditionValue)
      {
        if (curvedEdgeConditionParam is object && !curvedEdgeConditionParam.IsReadOnly)
        {
          StartTransaction(host.Document);
          curvedEdgeConditionParam.Update(curvedEdgeCondition.Value);
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to update 'Curved Edge Condition'");
      }

      Params.TrySetData
      (
        DA, "Curved Edge Condition",
        () => curvedEdgeConditionParam?.AsInteger()
      );

      using (var shapeVertices = shape.SlabShapeVertices)
      {
        Params.TrySetDataList
        (
          DA, "Corners",
          () => shapeVertices.Cast<ARDB.SlabShapeVertex>().Where(x => x.VertexType == ARDB.SlabShapeVertexType.Corner).
          Select(x => x.Position.ToPoint3d())
        );
        Params.TrySetDataList
        (
          DA, "Exteriors",
          () => shapeVertices.Cast<ARDB.SlabShapeVertex>().Where(x => x.VertexType == ARDB.SlabShapeVertexType.Edge).
          Select(x => x.Position.ToPoint3d())
        );
        Params.TrySetDataList
        (
          DA, "Interiors",
          () => shapeVertices.Cast<ARDB.SlabShapeVertex>().Where(x => x.VertexType == ARDB.SlabShapeVertexType.Interior).
          Select(x => x.Position.ToPoint3d())
        );
      }

      using (var shapeCreases = shape.SlabShapeCreases)
      {
        Params.TrySetDataList
        (
          DA, "Boundary",
          () => shapeCreases.Cast<ARDB.SlabShapeCrease>().Where(x => x.CreaseType == ARDB.SlabShapeCreaseType.Boundary).
          Select
          (
            x =>
            {
              try { return x.Curve.ToCurve(); }
              catch { }
              return new LineCurve(x.EndPoints.get_Item(0).Position.ToPoint3d(), x.EndPoints.get_Item(x.EndPoints.Size - 1).Position.ToPoint3d());
            }
          )
        );
        Params.TrySetDataList
        (
          DA, "Creases",
          () => shapeCreases.Cast<ARDB.SlabShapeCrease>().Where(x => x.CreaseType == ARDB.SlabShapeCreaseType.UserDrawn).
          Select
          (
            x =>
            {
              try { return x.Curve.ToCurve(); }
              catch { }
              return new LineCurve(x.EndPoints.get_Item(0).Position.ToPoint3d(), x.EndPoints.get_Item(x.EndPoints.Size - 1).Position.ToPoint3d());
            }
          )
        );
        Params.TrySetDataList
        (
          DA, "Auto",
          () => shapeCreases.Cast<ARDB.SlabShapeCrease>().Where(x => x.CreaseType == ARDB.SlabShapeCreaseType.Auto).
          Select
          (
            x =>
            {
              try { return x.Curve.ToCurve(); }
              catch { }
              return new LineCurve(x.EndPoints.get_Item(0).Position.ToPoint3d(), x.EndPoints.get_Item(x.EndPoints.Size - 1).Position.ToPoint3d());
            }
          )
        );
      }
    }
  }
}

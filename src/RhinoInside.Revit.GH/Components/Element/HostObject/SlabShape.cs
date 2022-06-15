using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

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
      name: "Slab Shape",
      nickname: "SlabShape",
      description: "Given its outline curve, it adds a Floor element to the active Revit document",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Floor()
        {
          Name = "Floor",
          NickName = "F",
          Description = "Floor to update",
        }
      ),
      new ParamDefinition
      (
        new Param_Integer()
        {
          Name = "Curved Edge Condition",
          NickName = "CEC",
          Optional = true
        }.SetDefaultVale(2), ParamRelevance.Secondary
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
        new Parameters.Floor()
        {
          Name = "Floor",
          NickName = "F",
          Description = "Floor to update",
        }
      ),
      new ParamDefinition
      (
        new Param_Integer()
        {
          Name = "Curved Edge Condition",
          NickName = "CEC",
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Points",
          NickName = "P",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Line()
        {
          Name = "Creases",
          NickName = "C",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Floor", out Types.Floor floor)) return;
      else Params.TrySetData(DA, "Floor", () => floor);

      var shape = floor.Value.SlabShapeEditor;
      if (shape is null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Only flat and horizontal floors are valid for slab shape edit.");
        return;
      }

      var curvedEdgeConditionParam = floor.Value.get_Parameter(ARDB.BuiltInParameter.HOST_SSE_CURVED_EDGE_CONDITION_PARAM);
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
        var sketch = (floor as Types.ISketchAccess).Sketch;
        var sketchLocation = sketch.Location;
        var profiles = Curve.JoinCurves(sketch.Profiles);
        for (int c = 0; c < profiles.Length; c++)
          profiles[c] = Curve.ProjectToPlane(profiles[c], Plane.WorldXY);

        StartTransaction(floor.Document);
        (floor as IGH_PreviewMeshData).DestroyPreviewMeshes();

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
        if (curvedEdgeConditionParam is object)
        {
          StartTransaction(floor.Document);
          curvedEdgeConditionParam.Update(curvedEdgeCondition.Value);
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to update 'Curved Edge Condition'");
      }

      Params.TrySetData
      (
        DA, "Curved Edge Condition",
        () => curvedEdgeConditionParam?.AsInteger()
      );
      Params.TrySetDataList
      (
        DA, "Points",
        () => shape.SlabShapeVertices.Cast<ARDB.SlabShapeVertex>().
        Select(x => x.Position.ToPoint3d())
      );
      Params.TrySetDataList
      (
        DA, "Creases",
        () => shape.SlabShapeCreases.Cast<ARDB.SlabShapeCrease>().
        Select(x => new Line(x.EndPoints.get_Item(0).Position.ToPoint3d(), x.EndPoints.get_Item(x.EndPoints.Size-1).Position.ToPoint3d()))
      );
    }
  }
}

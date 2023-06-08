using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.15")]
  public class SketchLines : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("F9BC3F5E-7415-485E-B74C-5CB855B818B8");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public SketchLines() : base
    (
      name: "Sketch Lines",
      nickname: "S-Lines",
      description: "Get the model lines of the given sketch element",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Sketch()
        {
          Name = "Sketch",
          NickName = "S",
          Description = "Sketch to access model lines.",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Sketch()
        {
          Name = "Sketch",
          NickName = "S",
          Description = "Sketch element.",
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Owner",
          NickName = "O",
          Description = "Sketch owner element.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.SketchPlane()
        {
          Name = "Work Plane",
          NickName = "WP",
          Description = "Work plane element.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.CurveElement()
        {
          Name = "Model Lines",
          NickName = "ML",
          Description = "Sketch lines grouped by boundary-profile.",
          Access = GH_ParamAccess.tree
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Integer()
        {
          Name = "Boundary Condition",
          NickName = "BC",
          Description = "Sketch lines boundary condition grouped by boundary-profile.\n\n-1 = Internal loop\n 0 = Open loop\n+1= External loop",
          Access = GH_ParamAccess.tree
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.CurveElement()
        {
          Name = "Slope Arrow",
          NickName = "SA",
          Description = "Sketch Slope Arrow.",
          Access = GH_ParamAccess.item
        }, ParamRelevance.Secondary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Sketch", out Types.Sketch sketch, x => x.IsValid)) return;
      else Params.TrySetData(DA, "Sketch", () => sketch);

      Params.TrySetData(DA, "Owner", () => sketch?.Owner);
      Params.TrySetData(DA, "Work Plane", () => sketch?.SketchPlane);
      Params.TrySetDataTree
      (
        DA, "Model Lines", () =>
        sketch?.Value?.GetProfileCurveElements().
        Select(x => x.Select(Types.CurveElement.FromElement)).
        TakeWhileIsNotEscapeKeyDown(this)
      );

      Params.TrySetDataTree
      (
        DA, "Boundary Condition", () =>
        {
          var brep = sketch.TrimmedSurface;

          return sketch?.Value?.GetProfileCurveElements().
          Select
          (x =>
          {
            var segment = x[0].GeometryCurve;
            var point = segment.Evaluate(segment.GetRawParameter(0.5), normalized: false).ToPoint3d();
            var loop = brep.Loops.OrderBy(e => e.To3dCurve() is Curve loopCurve && loopCurve.ClosestPoint(point, out var t, GeometryTolerance.Model.VertexTolerance) ? loopCurve.PointAt(t).DistanceTo(point) : double.PositiveInfinity).FirstOrDefault();
            switch (loop?.LoopType)
            {
              case BrepLoopType.Outer: return Enumerable.Repeat(+1, x.Count);
              case BrepLoopType.Inner: return Enumerable.Repeat(-1, x.Count);
              default: return Enumerable.Repeat(0, x.Count);
            }
          }).
          TakeWhileIsNotEscapeKeyDown(this);
        }
      );

      Params.TrySetData(DA, "Slope Arrow", () => sketch?.SlopeArrow);
    }
  }
}

using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.SpatialElement
{
  public class AddSpaceSeparatorLine : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("DEA31165-A184-466F-9119-D726472B226E");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public AddSpaceSeparatorLine() : base
    (
      name: "Add Space Separation Lines",
      nickname: "SS",
      description: "Given the curve, it adds a Space separator line to the given Revit view",
      category: "Revit",
      subCategory: "Room & Area"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to add a specific a room separator line",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Curve
        {
          Name = "Curve",
          NickName = "C",
          Description = "Curves to create a specific space separator line",
          Access = GH_ParamAccess.item
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.CurveElement()
        {
          Name = _SpaceSeparator_,
          NickName = _SpaceSeparator_.Substring(0, 1),
          Description = $"Output {_SpaceSeparator_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _SpaceSeparator_ = "Space Separator";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.ModelCurve>
      (
        doc.Value, _SpaceSeparator_, (spaceSeparatorLine) =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve)) return null;
          if (!Params.GetData(DA, "View", out ARDB.View view)) return null;

          var tol = GeometryObjectTolerance.Model;
          if
          (
            curve.IsShort(tol.ShortCurveTolerance) ||
            curve.IsClosed ||
            !curve.TryGetPlane(out var plane, tol.VertexTolerance) ||
            plane.ZAxis.IsParallelTo(Vector3d.ZAxis, tol.AngleTolerance) == 0
          )
            throw new Exceptions.RuntimeArgumentException("Curve", "Curve should be a valid horizontal, coplanar and open curve.", curve);

          // Compute
          spaceSeparatorLine = Reconstruct(spaceSeparatorLine, doc.Value, curve, view);

          DA.SetData(_SpaceSeparator_, spaceSeparatorLine);
          return spaceSeparatorLine;
        }
      );
    }

    bool Reuse(ARDB.ModelCurve spaceSeparator, ARDB.Document doc, Curve curve, ARDB.View view)
    {
      if (spaceSeparator is null) return false;
      if (spaceSeparator.OwnerViewId != view.Id) return false;
      var projectedCurve = Curve.ProjectToPlane(curve, view.SketchPlane.GetPlane().ToPlane());
      if (!projectedCurve.ToCurve().IsAlmostEqualTo(spaceSeparator.GeometryCurve)) return false;
      return true;
    }

    ARDB.ModelCurve Create(ARDB.Document doc, Curve curve, ARDB.View view)
    {
      var curveArray = new ARDB.CurveArray();
      var projectedCurve = Curve.ProjectToPlane(curve, view.SketchPlane.GetPlane().ToPlane());
      curveArray.Append(projectedCurve.ToCurve());
      return doc.Create.NewRoomBoundaryLines(view.SketchPlane, curveArray, view).get_Item(0);
    }

    ARDB.ModelCurve Reconstruct(ARDB.ModelCurve spaceSeparator, ARDB.Document doc, Curve curve, ARDB.View view)
    {
      if (!Reuse(spaceSeparator, doc, curve, view))
        spaceSeparator = Create(doc, curve, view);

      return spaceSeparator;
    }
  }
}


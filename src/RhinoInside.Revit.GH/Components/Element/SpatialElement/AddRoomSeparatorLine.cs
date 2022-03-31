using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.SpatialElement
{
  [ComponentVersion(introduced: "1.7")]
  public class AddRoomSeparatorLine : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("34186815-AAF1-44C5-B400-8EE426B14AC8");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public AddRoomSeparatorLine() : base
    (
      name: "Add Room Separation Lines",
      nickname: "RS",
      description: "Given the curve, it adds a Room separator line to the given Revit view",
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
          Description = "Curves to create a specific room separator line",
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
          Name = _RoomSeparator_,
          NickName = _RoomSeparator_.Substring(0, 1),
          Description = $"Output {_RoomSeparator_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _RoomSeparator_ = "Room Separator";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.ModelCurve>
      (
        doc.Value, _RoomSeparator_, (roomSeparatorLine) =>
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
          roomSeparatorLine = Reconstruct(roomSeparatorLine, doc.Value, curve, view);

          DA.SetData(_RoomSeparator_, roomSeparatorLine);
          return roomSeparatorLine;
        }
      );
    }

    bool Reuse(ARDB.ModelCurve roomSeparator, Curve curve, ARDB.View view)
    {
      if (roomSeparator is null) return false;
      if (roomSeparator.OwnerViewId != view.Id) return false;
      var projectedCurve = Curve.ProjectToPlane(curve, view.SketchPlane.GetPlane().ToPlane());

      if (!projectedCurve.ToCurve().IsAlmostEqualTo(roomSeparator.GeometryCurve))
        roomSeparator.SetGeometryCurve(projectedCurve.ToCurve(), true);
      return true;
    }

    ARDB.ModelCurve Create(ARDB.Document doc, Curve curve, ARDB.View view)
    {
      var curveArray = new ARDB.CurveArray();
      var projectedCurve = Curve.ProjectToPlane(curve, view.SketchPlane.GetPlane().ToPlane());
      curveArray.Append(projectedCurve.ToCurve());
      return doc.Create.NewRoomBoundaryLines(view.SketchPlane, curveArray, view).get_Item(0);
    }

    ARDB.ModelCurve Reconstruct(ARDB.ModelCurve roomSeparator, ARDB.Document doc, Curve curve, ARDB.View view)
    {
      if (!Reuse(roomSeparator, curve, view))
        roomSeparator = Create(doc, curve, view);
      return roomSeparator;
    }
  }
}


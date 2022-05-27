using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Topology
{
  [ComponentVersion(introduced: "1.7")]
  public class AddRoomSeparatorLine : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("34186815-AAF1-44C5-B400-8EE426B14AC8");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddRoomSeparatorLine() : base
    (
      name: "Add Room Separation",
      nickname: "RoomSeparation",
      description: "Given the curve, it adds a Room separation line to the given Revit view",
      category: "Revit",
      subCategory: "Topology"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
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
          Description = "Curves to create a specific room separation line",
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
          Name = _RoomSeparation_,
          NickName = _RoomSeparation_.Substring(0, 1),
          Description = $"Output {_RoomSeparation_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _RoomSeparation_ = "Room Separation";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;
      if (!(view is ARDB.ViewPlan))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "View should be a plan view");
        return;
      }

      ReconstructElement<ARDB.ModelCurve>
      (
        view.Document, _RoomSeparation_, (roomSeparatorLine) =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve)) return null;

          var tol = GeometryObjectTolerance.Model;
          if
          (
            curve.IsShort(tol.ShortCurveTolerance) ||
            curve.IsClosed ||
            !curve.TryGetPlane(out var plane, tol.VertexTolerance) ||
            plane.ZAxis.IsParallelTo(Vector3d.ZAxis, tol.AngleTolerance) == 0
          )
            throw new Exceptions.RuntimeArgumentException("Curve", "Curve should be a valid horizontal, planar and open curve.", curve);

          // Compute
          roomSeparatorLine = Reconstruct(roomSeparatorLine, view as ARDB.ViewPlan, curve);

          DA.SetData(_RoomSeparation_, roomSeparatorLine);
          return roomSeparatorLine;
        }
      );
    }

    bool Reuse(ARDB.ModelCurve roomSeparator, ARDB.ViewPlan view, Curve curve)
    {
      if (roomSeparator is null) return false;

      var genLevel = view.GenLevel;
      if (roomSeparator.LevelId != genLevel?.Id) return false;

      var levelPlane = Plane.WorldXY;
      levelPlane.Translate(Vector3d.ZAxis * genLevel.GetElevation() * Revit.ModelUnits);

      using (var projectedCurve = Curve.ProjectToPlane(curve, levelPlane).ToCurve())
      {
        if (!projectedCurve.AlmostEquals(roomSeparator.GeometryCurve, GeometryObjectTolerance.Internal.VertexTolerance))
          roomSeparator.SetGeometryCurve(projectedCurve, overrideJoins: true);
      }

      return true;
    }

    ARDB.ModelCurve Create(ARDB.ViewPlan view, Curve curve)
    {
      if (view.GenLevel is ARDB.Level level)
      {
        var sketchPlane = level.GetSketchPlane(ensureSketchPlane: true);
        using (var projectedCurve = Curve.ProjectToPlane(curve, sketchPlane.GetPlane().ToPlane()))
        using (var curveArray = new ARDB.CurveArray())
        {
          curveArray.Append(projectedCurve.ToCurve());
          return view.Document.Create.NewRoomBoundaryLines(sketchPlane, curveArray, view).get_Item(0);
        }
      }

      return default;
    }

    ARDB.ModelCurve Reconstruct(ARDB.ModelCurve roomSeparator, ARDB.ViewPlan view, Curve curve)
    {
      if (!Reuse(roomSeparator, view, curve))
        roomSeparator = Create(view, curve);

      return roomSeparator;
    }
  }
}


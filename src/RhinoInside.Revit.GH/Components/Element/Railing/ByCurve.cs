using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Units;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Kernel.Attributes;

  public class TrackedByRailing : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("601ac666-e369-464e-ae6f-34e01b9dba3b");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public TrackedByRailing() : base
    (
      name: "Tracked Railing",
      nickname: "Tracked Rail",
      description: "Given a curve, it adds a Rail element to the active Revit document",
      category: "Revit",
      subCategory: "Custom"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Rail", "R", "New Rail", GH_ParamAccess.item);
    }


    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return null;
      }
    }

    void ReconstructTrackedByRailing
      (
        DB.Document doc,
        ref DB.Architecture.Railing element,
        Rhino.Geometry.Curve curve,
        DB.Element type,
        DB.Level level
      )
    {
#if REVIT_2020
      if
      (
        !(curve.IsLinear(Revit.VertexTolerance * Revit.ModelUnits) || curve.IsArc(Revit.VertexTolerance * Revit.ModelUnits) || curve.IsEllipse(Revit.VertexTolerance * Revit.ModelUnits)) ||
        !curve.TryGetPlane(out var axisPlane, Revit.VertexTolerance * Revit.ModelUnits) ||
        axisPlane.ZAxis.IsParallelTo(Rhino.Geometry.Vector3d.ZAxis) == 0
      )
        ThrowArgumentException(nameof(curve), "Curve must be a horizontal line, arc or ellipse curve.");
#else
      if
      (
        !(curve.IsLinear(Revit.VertexTolerance * Revit.ModelUnits) || curve.IsArc(Revit.VertexTolerance * Revit.ModelUnits)) ||
        !curve.TryGetPlane(out var axisPlane, Revit.VertexTolerance * Revit.ModelUnits) ||
        axisPlane.ZAxis.IsParallelTo(Rhino.Geometry.Vector3d.ZAxis) == 0
      )
        ThrowArgumentException(nameof(curve), "Curve must be a horizontal line or arc curve.");
#endif

      // Axis
      var levelPlane = new Rhino.Geometry.Plane(new Rhino.Geometry.Point3d(0.0, 0.0, level.Elevation * Revit.ModelUnits), Rhino.Geometry.Vector3d.ZAxis);
      curve = Rhino.Geometry.Curve.ProjectToPlane(curve, levelPlane);

      var railingCurve = curve.ToCurve();

      // Type
      ChangeElementTypeId(ref element, type.Id);

      DB.Architecture.Railing newRail = null;
      if (element is DB.Architecture.Railing previousRail && previousRail.Location is DB.LocationCurve locationCurve && railingCurve.IsSameKindAs(locationCurve.Curve))
      {
        newRail = previousRail;

        locationCurve.Curve = railingCurve;
      }
      else
      {
        DB.CurveLoop railingCurveAsLoop = curve.ToCurveLoop();

        newRail = DB.Architecture.Railing.Create
        (
          doc,
          railingCurveAsLoop,
          type.Id,
          level.Id
        );


        var parametersMask = new DB.BuiltInParameter[]
        {
          DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_PARAM,
          DB.BuiltInParameter.ELEM_TYPE_PARAM,
        };

        ReplaceElement(ref element, newRail, parametersMask);
      }
    }

  }
}


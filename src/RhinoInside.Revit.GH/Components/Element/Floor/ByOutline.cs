using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class FloorByOutline : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("DC8DAF4F-CC93-43E2-A871-3A01A920A722");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public FloorByOutline() : base
    (
      "Add Floor", "Floor",
      "Given its outline curve, it adds a Floor element to the active Revit document",
      "Revit", "Build"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Floor(), "Floor", "F", "New Floor", GH_ParamAccess.item);
    }

    bool Reuse(ref DB.Floor element, Rhino.Geometry.Curve boundary, DB.FloorType type, DB.Level level, bool structural)
    {
      if (element is null) return false;

      if (element.GetSketch() is DB.Sketch sketch)
      {
        var profiles = sketch.Profile.ToPolyCurves();
        if (profiles.Length != 1)
          return false;

        var plane = sketch.SketchPlane.GetPlane().ToPlane();
        var profile = Rhino.Geometry.Curve.ProjectToPlane(boundary, plane);

        if
        (
          !Rhino.Geometry.Curve.GetDistancesBetweenCurves(profiles[0], profile, Revit.VertexTolerance * Revit.ModelUnits * 0.1, out var max, out var _, out var _, out var _, out var _, out var _) ||
          max > Revit.VertexTolerance * Revit.ModelUnits
        )
          return false;
      }
      else return false;

      if (element.GetTypeId() != type.Id)
      {
        if (DB.Element.IsValidType(element.Document, new DB.ElementId[] { element.Id }, type.Id))
          element = element.Document.GetElement(element.ChangeTypeId(type.Id)) as DB.Floor;
        else
          return false;
      }

      bool succeed = true;
      succeed &= element.get_Parameter(DB.BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL).Set(structural ? 1 : 0);
      succeed &= element.get_Parameter(DB.BuiltInParameter.LEVEL_PARAM).Set(level.Id);

      return succeed;
    }

    void ReconstructFloorByOutline
    (
      DB.Document doc,
      ref DB.Floor element,

      Rhino.Geometry.Curve boundary,
      Optional<DB.FloorType> type,
      Optional<DB.Level> level,
      [Optional] bool structural
    )
    {
      if
      (
        !boundary.IsClosed ||
        !boundary.TryGetPlane(out var boundaryPlane, Revit.VertexTolerance) ||
        boundaryPlane.ZAxis.IsParallelTo(Rhino.Geometry.Vector3d.ZAxis) == 0
      )
        ThrowArgumentException(nameof(boundary), "Boundary must be an horizontal planar closed curve.");

      if (type.HasValue && type.Value.Document.IsEquivalent(doc) == false)
        ThrowArgumentException(nameof(type));

      if (level.HasValue && level.Value.Document.IsEquivalent(doc) == false)
        ThrowArgumentException(nameof(level));

      SolveOptionalType(doc, ref type, DB.ElementTypeGroup.FloorType, nameof(type));

      SolveOptionalLevel(doc, boundary, ref level, out var _);

      if (!Reuse(ref element, boundary, type.Value, level.Value, structural))
      {
        var curveArray = boundary.ToCurveArray();

        var parametersMask = new DB.BuiltInParameter[]
        {
          DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_PARAM,
          DB.BuiltInParameter.ELEM_TYPE_PARAM,
          DB.BuiltInParameter.LEVEL_PARAM,
          DB.BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL
        };

        if (type.Value.IsFoundationSlab)
          ReplaceElement(ref element, doc.Create.NewFoundationSlab(curveArray, type.Value, level.Value, structural, DB.XYZ.BasisZ), parametersMask);
        else
          ReplaceElement(ref element, doc.Create.NewFloor(curveArray, type.Value, level.Value, structural, DB.XYZ.BasisZ), parametersMask);
      }

      if (element != null)
      {
        var boundaryBBox = boundary.GetBoundingBox(true);
        var floorHeightabovelevel = boundaryBBox.Min.Z / Revit.ModelUnits - level.Value.GetHeight();
        if
        (
          element.GetParameter(External.DB.Schemas.ParameterId.FloorHeightabovelevelParam) is DB.Parameter floorHeightabovelevelParam &&
          floorHeightabovelevelParam.AsDouble() != floorHeightabovelevel
        )
        {
          floorHeightabovelevelParam.Set(floorHeightabovelevel);
        }
      }
    }
  }
}

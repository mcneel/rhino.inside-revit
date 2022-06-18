using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;
  using External.DB.Extensions;
  using Kernel.Attributes;

  public class RoofByOutline : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("198E152B-6636-4D90-9443-AE77B8B1475E");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public RoofByOutline() : base
    (
      name: "Add Roof",
      nickname: "Roof",
      description: "Given its outline curve, it adds a Roof element to the active Revit document",
      category: "Revit",
      subCategory: "Build"
    )
    { }

    bool Reuse(ref ARDB.FootPrintRoof element, Curve boundary, ARDB.RoofType type, ARDB.Level level)
    {
      if (element is null) return false;

      if (!(element.GetSketch() is ARDB.Sketch sketch && Types.Sketch.SetProfile(sketch, new Curve[] { boundary }, Vector3d.ZAxis)))
        return false;

      if (element.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(element.Document, new ARDB.ElementId[] { element.Id }, type.Id))
        {
          if (element.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            element = element.Document.GetElement(id) as ARDB.FootPrintRoof;
        }
        else return false;
      }

      bool succeed = true;
      succeed &= element.get_Parameter(ARDB.BuiltInParameter.ROOF_BASE_LEVEL_PARAM).Update(level.Id);

      return succeed;
    }

    void ReconstructRoofByOutline
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New Roof")]
      ref ARDB.FootPrintRoof roof,

      Curve boundary,
      Optional<ARDB.RoofType> type,
      Optional<ARDB.Level> level
    )
    {
      var tol = GeometryTolerance.Model;
      if
      (
        boundary is null ||
        boundary.IsShort(tol.ShortCurveTolerance) ||
        !boundary.IsClosed ||
        !boundary.TryGetPlane(out var boundaryPlane, tol.VertexTolerance) ||
        boundaryPlane.ZAxis.IsParallelTo(Vector3d.ZAxis) == 0
      )
        ThrowArgumentException(nameof(boundary), "Boundary curve should be a valid horizontal planar closed curve.");

      if (type.HasValue && type.Value.Document.IsEquivalent(document) == false)
        ThrowArgumentException(nameof(type));

      if (level.HasValue && level.Value.Document.IsEquivalent(document) == false)
        ThrowArgumentException(nameof(level));

      SolveOptionalType(document, ref type, ARDB.ElementTypeGroup.RoofType, nameof(type));

      SolveOptionalLevel(document, boundary, ref level, out var bbox);

      var orientation = boundary.ClosedCurveOrientation(Plane.WorldXY);
      if (orientation == CurveOrientation.CounterClockwise)
        boundary.Reverse();

      if (!Reuse(ref roof, boundary, type.Value, level.Value))
      {
        var parametersMask = new ARDB.BuiltInParameter[]
        {
          ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
          ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
          ARDB.BuiltInParameter.ROOF_BASE_LEVEL_PARAM,
          ARDB.BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM
        };

        using (var curveArray = boundary.ToCurveArray())
        {
          var footPrintToModelCurvesMapping = new ARDB.ModelCurveArray();
          ReplaceElement(ref roof, document.Create.NewFootPrintRoof(curveArray, level.Value, type.Value, out footPrintToModelCurvesMapping), parametersMask);
        }
      }

      if (roof is object)
      {
        var roofLevelOffset = bbox.Min.Z / Revit.ModelUnits - level.Value.GetElevation();
        roof.get_Parameter(ARDB.BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM)?.Update(roofLevelOffset);
      }
    }
  }
}

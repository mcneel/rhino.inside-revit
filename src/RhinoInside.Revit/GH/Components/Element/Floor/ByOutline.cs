using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class FloorByOutline : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("DC8DAF4F-CC93-43E2-A871-3A01A920A722");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public FloorByOutline() : base
    (
      "AddFloor.ByOutline", "ByOutline",
      "Given its outline curve, it adds a Floor element to the active Revit document",
      "Revit", "Build"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.HostObject(), "Floor", "F", "New Floor", GH_ParamAccess.item);
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
      var scaleFactor = 1.0 / Revit.ModelUnits;

      if
      (
        ((boundary = boundary.ChangeUnits(scaleFactor)) is null) ||
        !boundary.IsClosed ||
        !boundary.TryGetPlane(out var boundaryPlane, Revit.VertexTolerance) ||
        boundaryPlane.ZAxis.IsParallelTo(Rhino.Geometry.Vector3d.ZAxis) == 0
      )
        ThrowArgumentException(nameof(boundary), "Boundary must be an horizontal planar closed curve.");

      SolveOptionalType(ref type, doc, DB.ElementTypeGroup.FloorType, nameof(type));

      SolveOptionalLevel(ref level, doc, boundary, nameof(level));

      var curveArray = boundary.ToHostMultiple().ToCurveArray();

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

      if (element != null)
      {
        var boundaryBBox = boundary.GetBoundingBox(true);
        element.get_Parameter(DB.BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(boundaryBBox.Min.Z - level.Value.Elevation);
      }
    }
  }
}

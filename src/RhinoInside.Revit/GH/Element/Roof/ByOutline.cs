using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class RoofByOutline : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("198E152B-6636-4D90-9443-AE77B8B1475E");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public RoofByOutline() : base
    (
      "AddRoof.ByOutline", "ByOutline",
      "Given its outline curve, it adds a Roof element to the active Revit document",
      "Revit", "Build"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.HostObject(), "Roof", "R", "New Roof", GH_ParamAccess.item);
    }

    void ReconstructRoofByOutline
    (
      DB.Document doc,
      ref DB.FootPrintRoof element,

      Rhino.Geometry.Curve boundary,
      Optional<DB.RoofType> type,
      Optional<DB.Level> level
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;

      if
      (
        ((boundary = boundary.ChangeUnits(scaleFactor)) is null) ||
        boundary.IsShort(Revit.ShortCurveTolerance) ||
        !boundary.IsClosed ||
        !boundary.TryGetPlane(out var boundaryPlane, Revit.VertexTolerance) ||
        boundaryPlane.ZAxis.IsParallelTo(Rhino.Geometry.Vector3d.ZAxis) == 0
      )
        ThrowArgumentException(nameof(boundary), "Boundary should be an horizontal planar closed curve.");

      SolveOptionalType(ref type, doc, DB.ElementTypeGroup.RoofType, nameof(type));

      double minZ = boundary.GetBoundingBox(true).Min.Z;
      SolveOptionalLevel(ref level, doc, minZ);

      var parametersMask = new DB.BuiltInParameter[]
      {
        DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
        DB.BuiltInParameter.ELEM_FAMILY_PARAM,
        DB.BuiltInParameter.ELEM_TYPE_PARAM,
        DB.BuiltInParameter.LEVEL_PARAM,
        DB.BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM
      };

      using (var curveArray = boundary.ToHostMultiple().ToCurveArray())
      {
        var footPrintToModelCurvesMapping = new DB.ModelCurveArray();
        ReplaceElement(ref element, doc.Create.NewFootPrintRoof(curveArray, level.Value, type.Value, out footPrintToModelCurvesMapping), parametersMask);
      }

      if (element != null)
      {
        element.get_Parameter(DB.BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM).Set(minZ - level.Value.Elevation);
      }
    }
  }
}

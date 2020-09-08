using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class RailingByCurve : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("601AC666-E369-464E-AE6F-34E01B9DBA3B");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public RailingByCurve() : base
    (
      name: "Add Railing",
      nickname: "Railing",
      description: "Given a curve, it adds a Railing element to the active Revit document",
      category: "Revit",
      subCategory: "Build"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Railing", "R", "New Railing", GH_ParamAccess.item);
    }

    void ReconstructRailingByCurve
    (
      DB.Document doc,
      ref DB.Architecture.Railing element,
      Rhino.Geometry.Curve curve,
      Optional<DB.Architecture.RailingType> type,
      Optional<DB.Level> level
    )
    {
      SolveOptionalType(ref type, doc, DB.ElementTypeGroup.StairsRailingType, nameof(type));
      SolveOptionalLevel(doc, curve, ref level, out var bbox);

      // Axis
      var levelPlane = new Rhino.Geometry.Plane(new Rhino.Geometry.Point3d(0.0, 0.0, level.Value.Elevation * Revit.ModelUnits), Rhino.Geometry.Vector3d.ZAxis);
      curve = Rhino.Geometry.Curve.ProjectToPlane(curve, levelPlane);

      // Type
      ChangeElementTypeId(ref element, type.Value.Id);

      var newRail = DB.Architecture.Railing.Create
      (
        doc,
        curve.ToCurveLoop(),
        type.Value.Id,
        level.Value.Id
      );

      var parametersMask = new DB.BuiltInParameter[]
      {
        DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
        DB.BuiltInParameter.ELEM_FAMILY_PARAM,
        DB.BuiltInParameter.ELEM_TYPE_PARAM,
        DB.BuiltInParameter.STAIRS_RAILING_BASE_LEVEL_PARAM,
      };

      ReplaceElement(ref element, newRail, parametersMask);

      newRail?.get_Parameter(DB.BuiltInParameter.STAIRS_RAILING_HEIGHT_OFFSET).Set(bbox.Min.Z / Revit.ModelUnits - level.Value.Elevation);
    }
  }
}


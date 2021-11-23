using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using Kernel.Attributes;

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

    void ReconstructRailingByCurve
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New Railing"), ParamType(typeof(Parameters.GraphicalElement))]
      ref ARDB.Architecture.Railing railing,

      Rhino.Geometry.Curve curve,
      Optional<ARDB.Architecture.RailingType> type,
      Optional<ARDB.Level> level,
      [Optional] ARDB.Element host,
      [Optional] bool flipped
    )
    {
      SolveOptionalType(document, ref type, ARDB.ElementTypeGroup.StairsRailingType, nameof(type));
      SolveOptionalLevel(document, curve, ref level, out var bbox);

      // Axis
      var levelPlane = new Rhino.Geometry.Plane(new Rhino.Geometry.Point3d(0.0, 0.0, level.Value.GetHeight() * Revit.ModelUnits), Rhino.Geometry.Vector3d.ZAxis);
      curve = Rhino.Geometry.Curve.ProjectToPlane(curve, levelPlane);
      curve = curve.Simplify(Rhino.Geometry.CurveSimplifyOptions.All, Revit.VertexTolerance * Revit.ModelUnits, Revit.AngleTolerance) ?? curve;

      // Type
      ChangeElementTypeId(ref railing, type.Value.Id);

      ARDB.Architecture.Railing newRail = null;
      if (railing is ARDB.Architecture.Railing previousRail)
      {
        newRail = previousRail;

        newRail.SetPath(curve.ToCurveLoop());
      }
      else
      {
        newRail = ARDB.Architecture.Railing.Create
        (
          document,
          curve.ToCurveLoop(),
          type.Value.Id,
          level.Value.Id
        );

        var parametersMask = new ARDB.BuiltInParameter[]
        {
          ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
          ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
          ARDB.BuiltInParameter.STAIRS_RAILING_BASE_LEVEL_PARAM,
          ARDB.BuiltInParameter.STAIRS_RAILING_HEIGHT_OFFSET,
        };

        ReplaceElement(ref railing, newRail, parametersMask);
      }

      if (newRail is object)
      {
        using (var baseLevel = newRail.get_Parameter(ARDB.BuiltInParameter.STAIRS_RAILING_BASE_LEVEL_PARAM))
        {
          if (!baseLevel.IsReadOnly)
            baseLevel.Update(level.Value.Id);
        }
        using (var heightOffset = newRail.get_Parameter(ARDB.BuiltInParameter.STAIRS_RAILING_HEIGHT_OFFSET))
        {
          if (!heightOffset.IsReadOnly)
            heightOffset.Update(bbox.Min.Z / Revit.ModelUnits - level.Value.GetHeight());
        }

        newRail.HostId = host?.Id ?? ARDB.ElementId.InvalidElementId;

        if (newRail.Flipped != flipped)
          newRail.Flip();
      }
    }
  }
}

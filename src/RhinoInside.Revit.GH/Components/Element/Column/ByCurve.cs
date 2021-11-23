using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using GH.Kernel.Attributes;

  public class ColumnByCurve : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("47B560AC-1E1D-4576-9F17-BCCF612974D8");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public ColumnByCurve() : base
    (
      name: "Add Column",
      nickname: "Column",
      description: "Given its Axis, it adds a structural Column to the active Revit document",
      category: "Revit",
      subCategory: "Build"
    )
    { }

    void ReconstructColumnByCurve
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New Column")]
      ref ARDB.FamilyInstance column,

      Rhino.Geometry.Line curve,
      Optional<ARDB.FamilySymbol> type,
      Optional<ARDB.Level> level
    )
    {
      if (curve.FromZ > curve.ToZ)
        curve.Flip();

      SolveOptionalType(document, ref type, ARDB.BuiltInCategory.OST_StructuralColumns, nameof(type));

      if (!type.Value.IsActive)
        type.Value.Activate();

      SolveOptionalLevel(document, curve, ref level, out var _);

      // Type
      ChangeElementTypeId(ref column, type.Value.Id);

      if (column is object && column.Location is ARDB.LocationCurve locationCurve)
      {
        var centerLine = curve.ToLine();
        if (!locationCurve.Curve.IsAlmostEqualTo(centerLine))
          locationCurve.Curve = centerLine;

        return;
      }
      else
      {
        var newColumn = document.Create.NewFamilyInstance
        (
          curve.ToLine(),
          type.Value,
          level.Value,
          ARDB.Structure.StructuralType.Column
        );

        var parametersMask = new ARDB.BuiltInParameter[]
        {
          ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
          ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
          ARDB.BuiltInParameter.LEVEL_PARAM
        };

        ReplaceElement(ref column, newColumn, parametersMask);
      }
    }
  }
}

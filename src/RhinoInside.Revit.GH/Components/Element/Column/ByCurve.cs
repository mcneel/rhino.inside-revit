using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
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
      DB.Document doc,

      [Description("New Column")]
      ref DB.FamilyInstance column,

      Rhino.Geometry.Line curve,
      Optional<DB.FamilySymbol> type,
      Optional<DB.Level> level
    )
    {
      if (curve.FromZ > curve.ToZ)
        curve.Flip();

      SolveOptionalType(doc, ref type, DB.BuiltInCategory.OST_StructuralColumns, nameof(type));

      if (!type.Value.IsActive)
        type.Value.Activate();

      SolveOptionalLevel(doc, curve, ref level, out var _);

      // Type
      ChangeElementTypeId(ref column, type.Value.Id);

      if (column is object && column.Location is DB.LocationCurve locationCurve)
      {
        locationCurve.Curve = curve.ToLine();
      }
      else
      {
        var newColumn = doc.Create.NewFamilyInstance
        (
          curve.ToLine(),
          type.Value,
          level.Value,
          DB.Structure.StructuralType.Column
        );

        var parametersMask = new DB.BuiltInParameter[]
        {
          DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_PARAM,
          DB.BuiltInParameter.ELEM_TYPE_PARAM,
          DB.BuiltInParameter.LEVEL_PARAM
        };

        ReplaceElement(ref column, newColumn, parametersMask);
      }
    }
  }
}

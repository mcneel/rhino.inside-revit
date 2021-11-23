using System;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Grids
{
  using Convert.Geometry;
  using ElementTracking;
  using Kernel.Attributes;

  public class GridByCurve : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("CEC2B3DF-C6BA-414F-BECE-E3DAEE2A3F2C");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public GridByCurve() : base
    (
      name: "Add Grid",
      nickname: "Grid",
      description: "Given its Axis, it adds a Grid element to the active Revit document",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    public override void OnStarted(ARDB.Document document)
    {
      base.OnStarted(document);

      if (Params.Input<IGH_Param>("Name").DataType != GH_ParamData.@void)
      {
        // Rename all previous grids to avoid name conflicts
        var grids = Params.TrackedElements<ARDB.Grid>("Grid", document);
        var pinnedGrids = grids.Where(x => x.Pinned);

        foreach (var grid in pinnedGrids)
          grid.Name = Guid.NewGuid().ToString();
      }
    }

    void ReconstructGridByCurve
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New Grid")]
      ref ARDB.Grid grid,

      Curve curve,
      Optional<ARDB.GridType> type,
      Optional<string> name
    )
    {
      SolveOptionalType(document, ref type, ARDB.ElementTypeGroup.GridType, nameof(type));
      if (name.HasValue && name.Value == default) return;

      if
      (
        !(curve.IsLinear(Revit.VertexTolerance * Revit.ModelUnits) || curve.IsArc(Revit.VertexTolerance * Revit.ModelUnits)) ||
        !curve.TryGetPlane(out var axisPlane, Revit.VertexTolerance * Revit.ModelUnits) ||
        axisPlane.ZAxis.IsParallelTo(Vector3d.ZAxis, Revit.AngleTolerance) == 0
      )
        ThrowArgumentException(nameof(curve), "Curve must be a horizontal line or arc curve.");

      var newGrid = grid;
      if (curve.TryGetLine(out var line, Revit.VertexTolerance * Revit.ModelUnits))
      {
        var newLine = line.ToLine();
        if
        (
          !(grid?.Curve is ARDB.Line oldLine) ||
          !oldLine.GetEndPoint(0).IsAlmostEqualTo(newLine.GetEndPoint(0)) ||
          !oldLine.GetEndPoint(1).IsAlmostEqualTo(newLine.GetEndPoint(1))
        )
          newGrid = ARDB.Grid.Create(document, line.ToLine());
      }
      else if (curve.TryGetArc(out var arc, Revit.VertexTolerance * Revit.ModelUnits))
      {
        var newArc = arc.ToArc();
        if
        (
          !(grid?.Curve is ARDB.Arc oldArc) ||
          !oldArc.GetEndPoint(0).IsAlmostEqualTo(newArc.GetEndPoint(0)) ||
          !oldArc.GetEndPoint(1).IsAlmostEqualTo(newArc.GetEndPoint(1)) ||
          !oldArc.Evaluate(0.5, true).IsAlmostEqualTo(newArc.Evaluate(0.5, true))
        )
          newGrid = ARDB.Grid.Create(document, newArc);
      }
      else
      {
        ThrowArgumentException(nameof(curve), "Curve must be a horizontal line or arc curve.");
      }

      ChangeElementTypeId(ref newGrid, type.Value.Id);

      if (name.HasValue && newGrid.Name != name.Value)
        newGrid.Name = name.Value;

      var parametersMask = name.IsMissing ?
        new ARDB.BuiltInParameter[]
        {
          ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
          ARDB.BuiltInParameter.ELEM_TYPE_PARAM
        } :
        new ARDB.BuiltInParameter[]
        {
          ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
          ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
          ARDB.BuiltInParameter.DATUM_TEXT
        };

      ReplaceElement(ref grid, newGrid, parametersMask);
    }
  }
}

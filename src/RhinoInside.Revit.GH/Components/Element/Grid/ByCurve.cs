using System;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.GH.ElementTracking;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
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

    public override void OnStarted(DB.Document document)
    {
      base.OnStarted(document);

      if (Params.Input<IGH_Param>("Name").DataType != GH_ParamData.@void)
      {
        // Rename all previous grids to avoid name conflicts
        var grids = Params.TrackedElements<DB.Grid>("Grid", document);
        var pinnedGrids = grids.Where(x => x.Pinned);

        foreach (var grid in pinnedGrids)
          grid.Name = Guid.NewGuid().ToString();
      }
    }

    void ReconstructGridByCurve
    (
      [Optional, NickName("DOC")]
      DB.Document document,

      [Description("New Grid")]
      ref DB.Grid grid,

      Curve curve,
      Optional<DB.GridType> type,
      Optional<string> name
    )
    {
      SolveOptionalType(document, ref type, DB.ElementTypeGroup.GridType, nameof(type));
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
          !(grid?.Curve is DB.Line oldLine) ||
          !oldLine.GetEndPoint(0).IsAlmostEqualTo(newLine.GetEndPoint(0)) ||
          !oldLine.GetEndPoint(1).IsAlmostEqualTo(newLine.GetEndPoint(1))
        )
          newGrid = DB.Grid.Create(document, line.ToLine());
      }
      else if (curve.TryGetArc(out var arc, Revit.VertexTolerance * Revit.ModelUnits))
      {
        var newArc = arc.ToArc();
        if
        (
          !(grid?.Curve is DB.Arc oldArc) ||
          !oldArc.GetEndPoint(0).IsAlmostEqualTo(newArc.GetEndPoint(0)) ||
          !oldArc.GetEndPoint(1).IsAlmostEqualTo(newArc.GetEndPoint(1)) ||
          !oldArc.Evaluate(0.5, true).IsAlmostEqualTo(newArc.Evaluate(0.5, true))
        )
          newGrid = DB.Grid.Create(document, newArc);
      }
      else
      {
        ThrowArgumentException(nameof(curve), "Curve must be a horizontal line or arc curve.");
      }

      ChangeElementTypeId(ref newGrid, type.Value.Id);

      if (name.HasValue && newGrid.Name != name.Value)
        newGrid.Name = name.Value;

      var parametersMask = name.IsMissing ?
        new DB.BuiltInParameter[]
        {
          DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_PARAM,
          DB.BuiltInParameter.ELEM_TYPE_PARAM
        } :
        new DB.BuiltInParameter[]
        {
          DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_PARAM,
          DB.BuiltInParameter.ELEM_TYPE_PARAM,
          DB.BuiltInParameter.DATUM_TEXT
        };

      ReplaceElement(ref grid, newGrid, parametersMask);
    }
  }
}

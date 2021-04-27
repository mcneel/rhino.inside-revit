using System;
using Rhino.Geometry;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
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

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Grid(), "Grid", "G", "New Grid", GH_ParamAccess.item);
    }

    void ReconstructGridByCurve
    (
      DB.Document doc,
      ref DB.Element element,

      Curve curve,
      Optional<DB.GridType> type,
      Optional<string> name
    )
    {
      SolveOptionalType(doc, ref type, DB.ElementTypeGroup.GridType, nameof(type));

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
         DB. BuiltInParameter.DATUM_TEXT
        };

      if (curve.TryGetLine(out var line, Revit.VertexTolerance * Revit.ModelUnits))
      {
        ReplaceElement(ref element, DB.Grid.Create(doc, line.ToLine()), parametersMask);
        ChangeElementTypeId(ref element, type.Value.Id);
      }
      else if (curve.TryGetArc(out var arc, Revit.VertexTolerance * Revit.ModelUnits))
      {
        ReplaceElement(ref element, DB.Grid.Create(doc, arc.ToArc()), parametersMask);
        ChangeElementTypeId(ref element, type.Value.Id);
      }
      else
      {
        using (var curveLoop = new DB.CurveLoop())
        using (var polyline = curve.ToArcsAndLines(Revit.VertexTolerance * Revit.ModelUnits, Revit.AngleTolerance, Revit.ShortCurveTolerance * Revit.ModelUnits, double.PositiveInfinity))
        {
          int count = polyline.SegmentCount;
          for (int s = 0; s < count; ++s)
          {
            var segment = polyline.SegmentCurve(s);

            if (segment is LineCurve l)
              curveLoop.Append(l.ToCurve());
            else if (segment is ArcCurve a)
              curveLoop.Append(a.ToCurve());
            else
              ThrowArgumentException(nameof(curve), "Invalid curve type.");
          }

          curve.TryGetPlane(out var plane);
          var sketchPlane = DB.SketchPlane.Create(doc, plane.ToPlane());

          ReplaceElement(ref element, doc.GetElement(DB.MultiSegmentGrid.Create(doc, type.Value.Id, curveLoop, sketchPlane.Id)), parametersMask);
        }
      }

      if (name != Optional.Missing && element != null)
      {
        try { element.Name = name.Value; }
        catch (Autodesk.Revit.Exceptions.ArgumentException e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{e.Message.Replace($".{Environment.NewLine}", ". ")}");
        }
      }
    }
  }
}

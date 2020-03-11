using System;
using DB = Autodesk.Revit.DB;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Elements.Grid
{
  public class GridByCurve : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("CEC2B3DF-C6BA-414F-BECE-E3DAEE2A3F2C");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public GridByCurve() : base
    (
      "AddGrid.ByCurve", "ByCurve",
      "Given its Axis, it adds a Grid element to the active Revit document",
      "Revit", "Datum"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Grid", "G", "New Grid", GH_ParamAccess.item);
    }

    void ReconstructGridByCurve
    (
      DB.Document doc,
      ref Autodesk.Revit.DB.Element element,

      Rhino.Geometry.Curve curve,
      Optional<Autodesk.Revit.DB.GridType> type,
      Optional<string> name
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;
      curve = curve.ChangeUnits(scaleFactor);

      SolveOptionalType(ref type, doc, DB.ElementTypeGroup.GridType, nameof(type));

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

      if (curve.TryGetLine(out var line, Revit.VertexTolerance))
      {
        ReplaceElement(ref element, DB.Grid.Create(doc, line.ToHost()), parametersMask);
        ChangeElementTypeId(ref element, type.Value.Id);
      }
      else if (curve.TryGetArc(out var arc, Revit.VertexTolerance))
      {
        ReplaceElement(ref element, DB.Grid.Create(doc, arc.ToHost()), parametersMask);
        ChangeElementTypeId(ref element, type.Value.Id);
      }
      else
      {
        using (var curveLoop = new DB.CurveLoop())
        using (var polyline = curve.ToArcsAndLines(Revit.VertexTolerance, Revit.AngleTolerance, Revit.ShortCurveTolerance, double.PositiveInfinity))
        {
          int count = polyline.SegmentCount;
          for (int s = 0; s < count; ++s)
          {
            var segment = polyline.SegmentCurve(s);

            if (segment is Rhino.Geometry.LineCurve l)
              curveLoop.Append(l.ToHost());
            else if (segment is Rhino.Geometry.ArcCurve a)
              curveLoop.Append(a.ToHost());
            else
              ThrowArgumentException(nameof(curve), "Invalid curve type.");
          }

          curve.TryGetPlane(out var plane);
          var sketchPlane = DB.SketchPlane.Create(doc, plane.ToHost());

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

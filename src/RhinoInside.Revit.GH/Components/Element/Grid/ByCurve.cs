using System;
using Autodesk.Revit.DB;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Units;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Geometry.Extensions;

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
      manager.AddParameter(new Parameters.Element(), "Grid", "G", "New Grid", GH_ParamAccess.item);
    }

    void ReconstructGridByCurve
    (
      Document doc,
      ref Autodesk.Revit.DB.Element element,

      Rhino.Geometry.Curve curve,
      Optional<Autodesk.Revit.DB.GridType> type,
      Optional<string> name
    )
    {
      SolveOptionalType(ref type, doc, ElementTypeGroup.GridType, nameof(type));

      var parametersMask = name.IsMissing ?
        new BuiltInParameter[]
        {
          BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          BuiltInParameter.ELEM_FAMILY_PARAM,
          BuiltInParameter.ELEM_TYPE_PARAM
        } :
        new BuiltInParameter[]
        {
          BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          BuiltInParameter.ELEM_FAMILY_PARAM,
          BuiltInParameter.ELEM_TYPE_PARAM,
          BuiltInParameter.DATUM_TEXT
        };

      if (curve.TryGetLine(out var line, Revit.VertexTolerance * Revit.ModelUnits))
      {
        ReplaceElement(ref element, Grid.Create(doc, line.ToLine()), parametersMask);
        ChangeElementTypeId(ref element, type.Value.Id);
      }
      else if (curve.TryGetArc(out var arc, Revit.VertexTolerance * Revit.ModelUnits))
      {
        ReplaceElement(ref element, Grid.Create(doc, arc.ToArc()), parametersMask);
        ChangeElementTypeId(ref element, type.Value.Id);
      }
      else
      {
        using (var curveLoop = new CurveLoop())
        using (var polyline = curve.ToArcsAndLines(Revit.VertexTolerance * Revit.ModelUnits, Revit.AngleTolerance, Revit.ShortCurveTolerance * Revit.ModelUnits, double.PositiveInfinity))
        {
          int count = polyline.SegmentCount;
          for (int s = 0; s < count; ++s)
          {
            var segment = polyline.SegmentCurve(s);

            if (segment is Rhino.Geometry.LineCurve l)
              curveLoop.Append(l.ToCurve());
            else if (segment is Rhino.Geometry.ArcCurve a)
              curveLoop.Append(a.ToCurve());
            else
              ThrowArgumentException(nameof(curve), "Invalid curve type.");
          }

          curve.TryGetPlane(out var plane);
          var sketchPlane = SketchPlane.Create(doc, plane.ToPlane());

          ReplaceElement(ref element, doc.GetElement(MultiSegmentGrid.Create(doc, type.Value.Id, curveLoop, sketchPlane.Id)), parametersMask);
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

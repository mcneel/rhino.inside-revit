using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Structural Framing")]
  public class StructuralFraming : FamilyInstance
  {
    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      switch ((element as ARDB.FamilyInstance)?.StructuralType)
      {
        case ARDB.Structure.StructuralType.Beam:
        case ARDB.Structure.StructuralType.Brace:
          return element.Category?.Id.IntegerValue == (int) ARDB.BuiltInCategory.OST_StructuralFraming;
      }

      return false;
    }

    public StructuralFraming() { }
    public StructuralFraming(ARDB.FamilyInstance value) : base(value) { }

    public override void SetCurve(Curve curve, bool keepJoins = false)
    {
      if (curve is object && Value is ARDB.FamilyInstance instance && curve is object)
      {
        if (instance.StructuralType == ARDB.Structure.StructuralType.Beam)
        {
          if (instance.Location is ARDB.LocationCurve locationCurve)
          {
            var newCurve = curve.ToCurve();
            if (!locationCurve.Curve.IsAlmostEqualTo(newCurve))
            {
              if (instance.Host is ARDB.Level)
              {
                var startElevation = instance.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION);
                var endElevation = instance.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION);

                startElevation.Set(startElevation.AsDouble() + 1.0);
                endElevation.Set(endElevation.AsDouble() + 1.0);
              }

              using (!keepJoins ? ElementJoins.DisableJoinsScope(instance) : default)
                locationCurve.Curve = newCurve;

              InvalidateGraphics();
            }

            return;
          }
        }

        base.SetCurve(curve, keepJoins);
      }
    }
  }

  [Kernel.Attributes.Name("Structural Column")]
  public class StructuralColumn : FamilyInstance
  {
    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      
      switch ((element as ARDB.FamilyInstance)?.StructuralType)
      {
        case ARDB.Structure.StructuralType.Column:
          return element.Category?.Id.IntegerValue == (int) ARDB.BuiltInCategory.OST_StructuralColumns;
      }

      return false;
    }

    public StructuralColumn() { }
    public StructuralColumn(ARDB.FamilyInstance value) : base(value) { }

    public override void SetCurve(Curve curve, bool keepJoins = false)
    {
      if (curve is object && Value is ARDB.FamilyInstance instance && curve is object)
      {
        if (instance.StructuralType == ARDB.Structure.StructuralType.Column)
        {
          var columnStyleParam = instance.get_Parameter(ARDB.BuiltInParameter.SLANTED_COLUMN_TYPE_PARAM);
          var columnStyle = columnStyleParam.AsInteger();

          if (instance.Location is ARDB.LocationPoint)
            columnStyleParam.Update(2);

          if (instance.Location is ARDB.LocationCurve locationCurve)
          {
            var newCurve = curve.ToCurve();
            if (!locationCurve.Curve.IsAlmostEqualTo(newCurve))
            {
              using (!keepJoins ? ElementJoins.DisableJoinsScope(instance) : default)
                locationCurve.Curve = newCurve;

              InvalidateGraphics();
            }

            if (columnStyle != 2)
            {
              if (Vector3d.VectorAngle(curve.PointAtEnd - curve.PointAtStart, Vector3d.ZAxis) < GeometryObjectTolerance.Model.AngleTolerance)
              {
                instance.Document.Regenerate();
                columnStyleParam.Set(0);
              }
              else columnStyleParam.Set(1);
            }

            return;
          }
        }

        base.SetCurve(curve, keepJoins);
      }
    }
  }
}

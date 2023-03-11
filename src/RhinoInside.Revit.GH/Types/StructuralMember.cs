using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Structural Member")]
  public class StructuralMember : FamilyInstance
  {
    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      return ((element as ARDB.FamilyInstance)?.StructuralType) != ARDB.Structure.StructuralType.NonStructural;
    }

    public StructuralMember() { }
    public StructuralMember(ARDB.FamilyInstance value) : base(value) { }

    #region Joins
    public static bool IsStructuralFraming(ARDB.FamilyInstance frame) =>
      frame.Symbol.Family.FamilyPlacementType == ARDB.FamilyPlacementType.CurveDrivenStructural;

    public bool? IsJoinAllowedAtStart
    {
      get => Value is ARDB.FamilyInstance frame && IsStructuralFraming(frame) ?
        (bool?) ARDB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(frame, 0) :
        default;

      set
      {
        if (value is object && Value is ARDB.FamilyInstance frame && value != IsJoinAllowedAtStart)
        {
          if (!IsStructuralFraming(frame))
            throw new Exceptions.RuntimeErrorException("Join at start can not be set for this element.");

          InvalidateGraphics();

          if (value == true)
            ARDB.Structure.StructuralFramingUtils.AllowJoinAtEnd(frame, 0);
          else
            ARDB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(frame, 0);
        }
      }
    }

    public bool? IsJoinAllowedAtEnd
    {
      get => Value is ARDB.FamilyInstance frame && IsStructuralFraming(frame) ?
        (bool?) ARDB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(frame, 1) :
        default;

      set
      {
        if (value is object && Value is ARDB.FamilyInstance frame && value != IsJoinAllowedAtEnd)
        {
          if (!IsStructuralFraming(frame))
            throw new Exceptions.RuntimeErrorException("Join at end can not be set for this element.");

          InvalidateGraphics();

          if (value == true)
            ARDB.Structure.StructuralFramingUtils.AllowJoinAtEnd(frame, 1);
          else
            ARDB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(frame, 1);
        }
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Structural Beam")]
  public class StructuralBeam : StructuralMember
  {
    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      return ((element as ARDB.FamilyInstance)?.StructuralType) == ARDB.Structure.StructuralType.Beam;
    }

    public StructuralBeam() { }
    public StructuralBeam(ARDB.FamilyInstance value) : base(value) { }

    public override void SetCurve(Curve curve, bool keepJoins = false)
    {
      if (curve is object && Value is ARDB.FamilyInstance instance && curve is object)
      {
        if (instance.StructuralType == ARDB.Structure.StructuralType.Beam)
        {
          if (instance.Location is ARDB.LocationCurve locationCurve)
          {
            var newCurve = curve.ToCurve();
            if (!locationCurve.Curve.AlmostEquals(newCurve, GeometryTolerance.Internal.VertexTolerance))
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

  [Kernel.Attributes.Name("Structural Brace")]
  public class StructuralBrace : StructuralMember
  {
    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      return ((element as ARDB.FamilyInstance)?.StructuralType) == ARDB.Structure.StructuralType.Brace;
    }

    public StructuralBrace() { }
    public StructuralBrace(ARDB.FamilyInstance value) : base(value) { }
  }

  [Kernel.Attributes.Name("Structural Column")]
  public class StructuralColumn : StructuralMember
  {
    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      return ((element as ARDB.FamilyInstance)?.StructuralType) == ARDB.Structure.StructuralType.Column;
    }

    public StructuralColumn() { }
    public StructuralColumn(ARDB.FamilyInstance value) : base(value) { }

    public override void SetCurve(Curve curve, bool keepJoins = false)
    {
      if (curve is object && Value is ARDB.FamilyInstance instance && curve is object)
      {
        var columnStyleParam = instance.get_Parameter(ARDB.BuiltInParameter.SLANTED_COLUMN_TYPE_PARAM);
        var columnStyle = columnStyleParam.AsInteger();

        if (instance.Location is ARDB.LocationPoint)
          columnStyleParam.Update(2);

        if (instance.Location is ARDB.LocationCurve locationCurve)
        {
          var newCurve = curve.ToCurve();
          if (!locationCurve.Curve.AlmostEquals(newCurve))
          {
            using (!keepJoins ? ElementJoins.DisableJoinsScope(instance) : default)
              locationCurve.Curve = newCurve;

            InvalidateGraphics();
          }

          if (columnStyle == 0)
          {
            // Let's see if we can keep the Column Style as 'Vertical'.
            var axis = curve.PointAtEnd - curve.PointAtStart; axis.Unitize();
            if ((axis - Vector3d.ZAxis).Length <= GeometryTolerance.Model.DefaultTolerance)
            {
              instance.Document.Regenerate();
              columnStyleParam.Set(0);
            }
          }
        }
      }
    }
  }

  [Kernel.Attributes.Name("Structural Foundation")]
  public class StructuralFooting : StructuralMember
  {
    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      return ((element as ARDB.FamilyInstance)?.StructuralType) == ARDB.Structure.StructuralType.Footing;
    }

    public StructuralFooting() { }
    public StructuralFooting(ARDB.FamilyInstance value) : base(value) { }
  }

  [Kernel.Attributes.Name("Structural Framing")]
  public class StructuralFraming : StructuralMember
  {
    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      return ((element as ARDB.FamilyInstance)?.StructuralType) == ARDB.Structure.StructuralType.UnknownFraming;
    }

    public StructuralFraming() { }
    public StructuralFraming(ARDB.FamilyInstance value) : base(value) { }
  }
}

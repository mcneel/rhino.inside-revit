using System;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using Grasshopper.Kernel;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Dimension")]
  public class Dimension : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.Dimension);
    public new ARDB.Dimension Value => base.Value as ARDB.Dimension;

    public Dimension() { }
    public Dimension(ARDB.Dimension element) : base(element) { }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Dimension dimension)
        {
          if (dimension.Curve is ARDB.Curve curve)
          {
            try
            {
              if (!curve.IsBound)
              {
                var segments = dimension.Segments.Cast<ARDB.DimensionSegment>().Where(x => x.Value.HasValue).ToArray();
                if (segments.Length > 0)
                {
                  var first = segments.First();
                  var start = curve.Project(first.Origin);

                  var last = segments.Last();
                  var end = curve.Project(last.Origin);

                  curve.MakeBound(start.Parameter - first.Value.Value * 0.5, end.Parameter + last.Value.Value * 0.5);
                }
                else if (dimension.Value.HasValue)
                {
                  if (dimension.Curve.Project(dimension.Origin) is ARDB.IntersectionResult result)
                  {
                    var startParameter = dimension.Value.Value * -0.5;
                    var endParameter = dimension.Value.Value * +0.5;
                    curve.MakeBound(result.Parameter + startParameter, result.Parameter + endParameter);
                  }
                }
              }

              if (curve.TryGetLocation(out var origin, out var basisX, out var basisY))
              {
                origin = curve.Evaluate(0.5, normalized: true);
                return new Plane(origin.ToPoint3d(), basisX.ToVector3d(), basisY.ToVector3d());
              }
            }
            catch { }
          }

          return new Plane(dimension.Origin.ToPoint3d(), Vector3d.XAxis, Vector3d.YAxis);
        }

        return NaN.Plane;
      }
    }

    public override Curve Curve
    {
      get
      {
        if (Value is ARDB.Dimension dimension)
        {
          if (dimension.Curve is ARDB.Curve curve)
          {
            try
            {
              if (!curve.IsBound)
              {
                var segments = dimension.Segments.Cast<ARDB.DimensionSegment>().Where(x => x.Value.HasValue).ToArray();
                if (segments.Length > 0)
                {
                  var first = segments.First();
                  var start = curve.Project(first.Origin);

                  var last = segments.Last();
                  var end = curve.Project(last.Origin);

                  curve.MakeBound(start.Parameter - first.Value.Value * 0.5, end.Parameter + last.Value.Value * 0.5);
                }
                else if (dimension.Value.HasValue)
                {
                  if (dimension.Curve.Project(dimension.Origin) is ARDB.IntersectionResult result)
                  {
                    var startParameter = dimension.Value.Value * -0.5;
                    var endParameter = dimension.Value.Value * +0.5;
                    curve.MakeBound(result.Parameter + startParameter, result.Parameter + endParameter);
                  }
                }
              }
            }
            catch { }

            return curve.ToCurve();
          }
        }

        return default;
      }
    }

    public virtual bool? HasLeader
    {
#if REVIT_2021
      get => Value?.HasLeader;
      set { if (Value is ARDB.Dimension dimension && value is object && dimension.HasLeader != value) dimension.HasLeader = value.Value; }
#else
      get => Value?.get_Parameter(ARDB.BuiltInParameter.DIM_LEADER)?.AsInteger() != 0;
      set
      {
        if (Value?.get_Parameter(ARDB.BuiltInParameter.DIM_LEADER) is ARDB.Parameter hasLeader && value.HasValue)
          hasLeader.Update(value.Value);
      }
#endif
    }

    public virtual Curve Leader
    {
      get
      {
        if (Value is ARDB.Dimension dimension && HasLeader == true)
        {
          return new LineCurve
          (
            dimension.Origin.ToPoint3d(),
            dimension.LeaderEndPosition.ToPoint3d()
          );
        }

        return default;
      }
    }

    static string FormatValue(double value, ARDB.DimensionShape dimensionShape)
    {
      var provider = System.Globalization.CultureInfo.InvariantCulture;
      var precision = Rhino.RhinoDoc.ActiveDoc?.ModelDistanceDisplayPrecision ?? 3;

      switch (dimensionShape)
      {
        case ARDB.DimensionShape.Angular:
          return $"{Rhino.RhinoMath.ToDegrees(value):N1}°";

        case ARDB.DimensionShape.Radial:
          return $"R {GeometryDecoder.ToModelLength(value).ToString($"N{precision}", provider)} {GH_Format.RhinoUnitSymbol()}";

        case ARDB.DimensionShape.Diameter:
          return $"⌀ {GeometryDecoder.ToModelLength(value).ToString($"N{precision}", provider)} {GH_Format.RhinoUnitSymbol()}";
      }

      return $"{GeometryDecoder.ToModelLength(value).ToString($"N{precision}", provider)} {GH_Format.RhinoUnitSymbol()}";
    }

    protected static string FormatValue(ARDB.Dimension dimension, ARDB.DimensionStyleType style)
    {
      var provider = System.Globalization.CultureInfo.InvariantCulture;
      var precision = Rhino.RhinoDoc.ActiveDoc?.ModelDistanceDisplayPrecision ?? 3;

      switch (style)
      {
        case ARDB.DimensionStyleType.Angular:
          return $"{Rhino.RhinoMath.ToDegrees(dimension.Value.Value):N1}°";

        case ARDB.DimensionStyleType.Radial:
          return $"R {GeometryDecoder.ToModelLength(dimension.Value.Value).ToString($"N{precision}", provider)} {GH_Format.RhinoUnitSymbol()}";

        case ARDB.DimensionStyleType.Diameter:
          return $"⌀ {GeometryDecoder.ToModelLength(dimension.Value.Value).ToString($"N{precision}", provider)} {GH_Format.RhinoUnitSymbol()}";

        case ARDB.DimensionStyleType.SpotElevation:
          return $"{GeometryDecoder.ToModelLength(dimension.Origin.Z).ToString($"N{precision}", provider)} {GH_Format.RhinoUnitSymbol()}";

        case ARDB.DimensionStyleType.SpotCoordinate:
          return $"X {GeometryDecoder.ToModelLength(dimension.Origin.X).ToString($"N{precision}", provider)}{Environment.NewLine}Y {GeometryDecoder.ToModelLength(dimension.Origin.Y).ToString($"N{precision}", provider)}";

        case ARDB.DimensionStyleType.SpotSlope:
          return $"⌳ {Rhino.RhinoMath.ToDegrees(dimension.get_Parameter(ARDB.BuiltInParameter.DIM_VALUE_ANGLE).AsDouble()):N1}°";
      }

      return $"{GeometryDecoder.ToModelLength(dimension.Value.Value).ToString($"N{precision}", provider)} {GH_Format.RhinoUnitSymbol()}";
    }

    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.Dimension dimension)
      {
        try
        {
          if (dimension.NumberOfSegments > 0)
          {
            var segments = dimension.Segments.Cast<ARDB.DimensionSegment>().Where(x => x.Value.HasValue).ToArray();
            foreach (var segment in segments)
            {
              if (!segment.Value.HasValue) continue;

              var dimCurve = dimension.Curve;
              if (dimCurve.Project(segment.Origin) is ARDB.IntersectionResult result)
              {
                var startParameter = segment.Value.Value * -0.5;
                var endParameter   = segment.Value.Value * +0.5;
                dimCurve.MakeBound(result.Parameter + startParameter, result.Parameter + endParameter);

                var curve = dimCurve.ToCurve();
                args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
                args.Pipeline.DrawArrowHead(curve.PointAtStart, -curve.TangentAtStart, args.Color, 16, 0.0);
                args.Pipeline.DrawArrowHead(curve.PointAtEnd, curve.TangentAtEnd, args.Color, 16, 0.0);
              }

              var text = FormatValue(segment.Value.Value, dimension.DimensionShape);
              if (segment.TextPosition is ARDB.XYZ textPosition)
                args.Pipeline.DrawDot(textPosition.ToPoint3d(), text, args.Color, System.Drawing.Color.White);
            }
          }
          else 
          {
            if (Curve is Curve curve)
            {
              args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
              args.Pipeline.DrawArrowHead(curve.PointAtStart, -curve.TangentAtStart, args.Color, 16, 0.0);
              args.Pipeline.DrawArrowHead(curve.PointAtEnd, curve.TangentAtEnd, args.Color, 16, 0.0);
            }

            var text = FormatValue(dimension, dimension.DimensionType.StyleType);
            if (dimension.TextPosition is ARDB.XYZ textPosition)
              args.Pipeline.DrawDot(textPosition.ToPoint3d(), text, args.Color, System.Drawing.Color.White);
          }
        }
        catch { }
      }
    }
  }

  [Kernel.Attributes.Name("Dimension Type")]
  public class DimensionType : ElementType
  {
    protected override Type ValueType => typeof(ARDB.DimensionType);
    public new ARDB.DimensionType Value => base.Value as ARDB.DimensionType;

    public DimensionType() { }
    protected internal DimensionType(ARDB.DimensionType type) : base(type) { }
  }
}

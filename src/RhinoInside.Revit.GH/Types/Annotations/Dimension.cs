using System;
using System.Linq;
using Rhino.Display;
using Rhino.Geometry;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Numerical;
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Dimension")]
  public class Dimension : GraphicalElement, IGH_Annotation,
    IAnnotationReferencesAccess,
    IAnnotationLeadersAccess
  {
    protected override Type ValueType => typeof(ARDB.Dimension);
    public new ARDB.Dimension Value => base.Value as ARDB.Dimension;

    public Dimension() { }
    public Dimension(ARDB.Dimension element) : base(element) { }

    public new DimensionType Type => base.Type as DimensionType;

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
                return new Plane(origin.ToPoint3d(), basisX.Direction.ToVector3d(), basisY.Direction.ToVector3d());
              }
            }
            catch { }
          }

          return new Plane(dimension.Origin.ToPoint3d(), Vector3d.XAxis, Vector3d.YAxis);
        }

        return NaN.Plane;
      }
    }

    public override Curve Curve => Value?.GetBoundedCurve().ToCurve();

    #region IAnnotationReferencesAcces
    public GeometryObject[] References =>
      Value?.References.
      Cast<ARDB.Reference>().
      Select(GetGeometryObjectFromReference<GeometryObject>).
      ToArray();
    #endregion

    #region IAnnotationLeadersAcces
    public virtual bool? HasLeader
    {
      get => Value?.GetHasLeader();
      set
      {
        if (Value is ARDB.Dimension dimension && value is object && value != dimension.GetHasLeader())
        {
          dimension.SetHasLeader(value.Value);
          InvalidateGraphics();
        }
      }
    }

    public AnnotationLeader[] Leaders
    {
      get
      {
        if (Value is ARDB.Dimension dimension)
        {
          if (dimension.NumberOfSegments == 0)
            return new AnnotationLeader[] { new MonoLeader(this) };

          var leaders = new AnnotationLeader[dimension.NumberOfSegments];
          for (int r = 0; r < leaders.Length; ++r)
            leaders[r] = new MultiLeader(this, r);

          return leaders;
        }

        return null;
      }
    }

    abstract class DimensionLeader : AnnotationLeader
    {
      protected readonly Dimension dimension;
      protected DimensionLeader(Dimension d) => dimension = d;

      public abstract double? Value { get; }

      public abstract Curve SegmentCurve { get; }

      public override Curve LeaderCurve
      {
        get
        {
          if (dimension.Type.LeaderType == 1)
          {
            var curve = SegmentCurve;
            var start = HeadPosition;
            var end = EndPosition;
            var center = default(Point3d);

            switch (curve)
            {
              case null:
                return HasElbow ?
                  new PolylineCurve(new Point3d[] { dimension.Location.Origin, ElbowPosition, EndPosition }) :
                  new PolylineCurve(new Point3d[] { dimension.Location.Origin, EndPosition });

              case LineCurve lineCurve: center = lineCurve.Line.PointAt(lineCurve.Line.ClosestParameter(end)); break;
              default: curve.ClosestPoint(end, out var c); center = curve.PointAt(c); break;
            }

            var plane = new Plane(center, start, end);
            var ellipse = new Ellipse(plane, center.DistanceTo(start), center.DistanceTo(end));
            return ellipse.ToNurbsCurve(new Interval(0.0, Constant.Tau / 4));
          }

          return base.LeaderCurve;
        }
      }
    }

    class MonoLeader : DimensionLeader
    {
      public MonoLeader(Dimension d) : base(d) { }

      public override double? Value => dimension.Value.Value;

      public override Curve SegmentCurve
      {
        get
        {
          if (dimension.Value.Value is double dimValue)
          {
            using (var dimCurve = dimension.Value.Curve)
            {
              if (dimCurve?.Project(dimension.Value.Origin) is ARDB.IntersectionResult result)
              {
                dimCurve.MakeBound(result.Parameter - dimValue * 0.5, result.Parameter + dimValue * 0.5);
                return dimCurve.ToCurve();
              }
            }
          }

          return null;
        }
      }

      public override Point3d HeadPosition => dimension.Value.Origin.ToPoint3d();

      public override bool Visible
      {
        get => !dimension.Value.Origin.AlmostEqualPoints(dimension.Value.LeaderEndPosition, dimension.Document.Application.ShortCurveTolerance);
        set { }
      }

      public override bool HasElbow
      {
        get
        {
#if REVIT_2021
          if (dimension.Value is ARDB.SpotDimension spot)
            return spot.LeaderHasShoulder;
#endif

          return false;
        }
      }
      public override Point3d ElbowPosition
      {
        get
        {
#if REVIT_2021
          if (dimension.Value is ARDB.SpotDimension spot)
            return spot.LeaderShoulderPosition.ToPoint3d();
#endif

          return NaN.Point3d;
        }
        set
        {
#if REVIT_2021
          if (dimension.Value is ARDB.SpotDimension spot && spot.LeaderHasShoulder)
            spot.LeaderShoulderPosition = value.ToXYZ();
          else
#endif
          throw new InvalidOperationException($"Dimension '{dimension.Nomen}' do not have shoulder. {{{dimension.Id.ToString("D")}}}");
        }
      }

      public override Point3d EndPosition
      {
        get => dimension.Value.LeaderEndPosition.ToPoint3d();
        set => dimension.Value.LeaderEndPosition = value.ToXYZ();
      }

      public override bool IsTextPositionAdjustable => dimension.Value.IsTextPositionAdjustable();
      public override Point3d TextPosition
      {
        get => dimension.Value.TextPosition.ToPoint3d();
        set => dimension.Value.TextPosition = value.ToXYZ();
      }
    }

    class MultiLeader : DimensionLeader
    {
      readonly int index;
      public MultiLeader(Dimension d, int i) : base(d) => index = i;

      ARDB.DimensionSegment DimensionSegment => dimension.Value.Segments.get_Item(index);

      public override double? Value => DimensionSegment.Value;

      public override Curve SegmentCurve
      {
        get
        {
          if (DimensionSegment.Value is double dimValue)
          {
            using (var dimCurve = dimension.Value.Curve)
            {
              if (dimCurve?.Project(DimensionSegment.Origin) is ARDB.IntersectionResult result)
              {
                dimCurve.MakeBound(result.Parameter - dimValue * 0.5, result.Parameter + dimValue * 0.5);
                return dimCurve.ToCurve();
              }
            }
          }

          return null;
        }
      }

      public override Point3d HeadPosition => DimensionSegment.Origin.ToPoint3d();

      public override bool Visible
      {
        get => !DimensionSegment.Origin.AlmostEqualPoints(DimensionSegment.LeaderEndPosition, dimension.Document.Application.ShortCurveTolerance);
        set { }
      }

      public override bool HasElbow => false;
      public override Point3d ElbowPosition
      {
        get => NaN.Point3d;
        set => throw new InvalidOperationException();
      }

      public override Point3d EndPosition
      {
        get => DimensionSegment.LeaderEndPosition.ToPoint3d();
        set => DimensionSegment.LeaderEndPosition = value.ToXYZ();
      }

      public override bool IsTextPositionAdjustable => DimensionSegment.IsTextPositionAdjustable();
      public override Point3d TextPosition
      {
        get => DimensionSegment.TextPosition.ToPoint3d();
        set => DimensionSegment.TextPosition = value.ToXYZ();
      }
    }
#endregion

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

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.Dimension dimension && Type is DimensionType type)
      {
        try
        {
          var dpi = args.Pipeline.DpiScale;
          var tagSize = 0.5; // feet
          var dotPixels = 10.0 * dpi;

          var arrowSize = (int) Math.Round(2.0 * Grasshopper.CentralSettings.PreviewPointRadius * dpi);
          var tickMark = type.TickMark;
          var leaderTickMark = type.LeaderTickMark;
          foreach (var leader in Leaders.Cast<DimensionLeader>())
          {
            if (leader.SegmentCurve is Curve segmentCurve && segmentCurve.IsValid)
            {
              args.Pipeline.DrawCurve(segmentCurve, args.Color, args.Thickness);
              if (tickMark is object)
              {
                args.Pipeline.DrawArrowHead(segmentCurve.PointAtStart, -segmentCurve.TangentAtStart, args.Color, arrowSize, 0.0);
                args.Pipeline.DrawArrowHead(segmentCurve.PointAtEnd,    segmentCurve.TangentAtEnd,   args.Color, arrowSize, 0.0);
              }
            }

            if (HasLeader is true && leader.LeaderCurve is Curve leaderCurve)
            {
              args.Pipeline.DrawCurve(leaderCurve, args.Color, args.Thickness);
              if (leaderTickMark is object)
                args.Pipeline.DrawArrowHead(leaderCurve.PointAtStart, -leaderCurve.TangentAtStart, args.Color, arrowSize, 0.0);
            }

            var textPosition = leader.TextPosition;
            if (textPosition.IsValid)
            {
              var pixelSize = ((1.0 / args.Pipeline.Viewport.PixelsPerUnit(textPosition).X) / Revit.ModelUnits) / dpi;
              if (dotPixels * pixelSize > tagSize)
              {
                var color = System.Drawing.Color.White;
                var rotation = 0.0f;
                args.Pipeline.DrawPoint
                (
                  textPosition, PointStyle.Square,
                  args.Color,
                  color,
                  (float) (tagSize / pixelSize),
                  1.0f, 0.0f, rotation,
                  diameterIsInPixels: true,
                  autoScaleForDpi: false
                );
              }
              else if (leader.Value is double value)
              {
                var text = FormatValue(value, dimension.DimensionShape);
                args.Pipeline.DrawDot(textPosition, text, args.Color, System.Drawing.Color.White);
              }
            }
          }
        }
        catch { }
      }
    }
  }

  [Kernel.Attributes.Name("Dimension Type")]
  public class DimensionType : AnnotationType
  {
    protected override Type ValueType => typeof(ARDB.DimensionType);
    public new ARDB.DimensionType Value => base.Value as ARDB.DimensionType;

    public DimensionType() { }
    protected internal DimensionType(ARDB.DimensionType type) : base(type) { }

    internal ElementType TickMark => ElementType.FromElementId(Document, Value?.get_Parameter(ARDB.BuiltInParameter.DIM_LEADER_ARROWHEAD).AsElementId()) as ElementType;

    internal int? LeaderType => Value?.get_Parameter(ARDB.BuiltInParameter.DIM_LEADER_TYPE).AsInteger();
    internal ElementType LeaderTickMark => ElementType.FromElementId(Document, Value?.get_Parameter(ARDB.BuiltInParameter.DIM_STYLE_LEADER_TICK_MARK).AsElementId()) as ElementType;
  }
}

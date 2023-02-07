using System;
using Grasshopper.Kernel;
using Rhino.Display;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Spot Dimension")]
  public class SpotDimension : Dimension
  {
    protected override Type ValueType => typeof(ARDB.SpotDimension);
    public new ARDB.SpotDimension Value => base.Value as ARDB.SpotDimension;

    public SpotDimension() { }
    public SpotDimension(ARDB.SpotDimension spotDimension) : base(spotDimension) { }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.SpotDimension spot)
          return new Plane(spot.Origin.ToPoint3d(), Vector3d.XAxis, Vector3d.YAxis);

        return NaN.Plane;
      }
    }

#if !REVIT_2021
    public override bool? HasLeader
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.SPOT_DIM_LEADER)?.AsInteger() != 0;
      set
      {
        if (Value?.get_Parameter(ARDB.BuiltInParameter.SPOT_DIM_LEADER) is ARDB.Parameter hasLeader && value.HasValue)
          hasLeader.Update(value.Value);
      }
    }
#endif

    Curve LeaderCurve
    {
      get
      {
#if REVIT_2021
        if
        (
          Value is ARDB.SpotDimension spot &&
          spot.SpotDimensionType.StyleType != ARDB.DimensionStyleType.SpotSlope && 
          HasLeader == true
        )
        {
          if (spot.LeaderHasShoulder)
            return new PolylineCurve
            (
              new Point3d[]
              {
                spot.Origin.ToPoint3d(),
                spot.LeaderShoulderPosition.ToPoint3d(),
                spot.LeaderEndPosition.ToPoint3d()
              }
            );

          return new LineCurve
          (
            spot.Origin.ToPoint3d(),
            spot.LeaderEndPosition.ToPoint3d()
          );
        }
#endif

        return default;
      }
    }

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
#if REVIT_2021
      if (Value is ARDB.SpotDimension spot)
      {
        var dpi = args.Pipeline.DpiScale;
        var tagSize = 0.5; // feet
        var dotPixels = 10.0 * args.Pipeline.DpiScale;
        var arrowSize = (int) Math.Round(2.0 * Grasshopper.CentralSettings.PreviewPointRadius * dpi);

        if (LeaderCurve is Curve leader)
        {
          args.Pipeline.DrawCurve(leader, args.Color, args.Thickness);
          if ((spot.DimensionType.get_Parameter(ARDB.BuiltInParameter.SPOT_ELEV_LEADER_ARROWHEAD)?.AsElementId()).IsValid())
            args.Pipeline.DrawArrowHead(leader.PointAtStart, -leader.TangentAtStart, args.Color, arrowSize, 0.0);
        }

        var styleType = spot.DimensionType.StyleType;
        var position = (styleType == ARDB.DimensionStyleType.SpotSlope ? spot.Origin : spot.TextPosition).ToPoint3d();

        var pixelSize = ((1.0 / args.Pipeline.Viewport.PixelsPerUnit(position).X) / Revit.ModelUnits) / dpi;
        //if (dotPixels * pixelSize > tagSize)
        {
          args.Pipeline.DrawPoint
          (
            spot.LeaderEndPosition.ToPoint3d(),
            PointStyle.RoundActivePoint,
            args.Color,
            System.Drawing.Color.WhiteSmoke,
            (float) (tagSize / pixelSize),
            1.0f, 0.0f, 0.0f,
            diameterIsInPixels: true,
            autoScaleForDpi: false
          );
        }

        if (dotPixels * pixelSize < tagSize)
        {
          var text = FormatValue(spot, styleType);
          args.Pipeline.DrawDot(position, text, args.Color, System.Drawing.Color.White);
        }
      }
#endif
    }
  }
}

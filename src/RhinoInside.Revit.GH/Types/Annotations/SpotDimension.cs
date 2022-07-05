using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;
  using Grasshopper.Kernel;
  using Rhino.Geometry;
  using RhinoInside.Revit.Convert.Geometry;

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

    public override Curve Leader
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

    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
#if REVIT_2021
      if (Value is ARDB.SpotDimension spot)
      {
        if (Leader is Curve leader)
        {
          args.Pipeline.DrawCurve(leader, args.Color, args.Thickness);
          args.Pipeline.DrawArrowHead(leader.PointAtStart, -leader.TangentAtStart, args.Color, 16, 0.0);
        }

        {
          var text = FormatValue(spot, spot.DimensionType.StyleType);
          var position = spot.DimensionType.StyleType == ARDB.DimensionStyleType.SpotSlope ?
            spot.Origin : spot.TextPosition;

          args.Pipeline.DrawDot(position.ToPoint3d(), text, args.Color, System.Drawing.Color.White);
        }
      }
#endif
    }
  }
}

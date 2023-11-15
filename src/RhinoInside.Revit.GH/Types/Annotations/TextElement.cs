using System;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Text Element")]
  public class TextElement : GraphicalElement, IGH_Annotation,
    IAnnotationLeadersAccess
  {
    protected override Type ValueType => typeof(ARDB.TextElement);
    public new ARDB.TextElement Value => base.Value as ARDB.TextElement;

    public TextElement() { }
    public TextElement(ARDB.TextElement element) : base(element) { }

    public new TextElementType Type => base.Type as TextElementType;

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.TextElement text)
        {
          return new Plane
          (
            text.Coord.ToPoint3d(),
            text.BaseDirection.ToVector3d(),
            text.UpDirection.ToVector3d()
          );
        }

        return base.Location;
      }
    }

    public override BoundingBox GetBoundingBox(Transform xform) => Box.GetBoundingBox(xform);

    public override Box Box
    {
      get
      {
        if (Value is ARDB.TextElement text)
        {
          var scale = Revit.ModelUnits * OwnerView.Scale;
          var xSize = new Interval(0.5 * scale, 0.5 * scale);
          var ySize = new Interval(0.5 * scale, 0.5 * scale);
          var zSize = new Interval(-0.0, +0.0);

          switch (text.HorizontalAlignment)
          {
            case ARDB.HorizontalTextAlignment.Left:   xSize = new Interval(0.0,                          +text.Width * 2.0 * xSize.T1); break;
            case ARDB.HorizontalTextAlignment.Center: xSize = new Interval(-text.Width * xSize.T0,       +text.Width * xSize.T1); break;
            case ARDB.HorizontalTextAlignment.Right:  xSize = new Interval(-text.Width * 2.0 * xSize.T0, 0.0); break;
          }

          switch (text.VerticalAlignment)
          {
            case ARDB.VerticalTextAlignment.Top:    ySize = new Interval(-text.Height * 2.0 * ySize.T0, 0.0); break;
            case ARDB.VerticalTextAlignment.Middle: ySize = new Interval(-text.Height * ySize.T0, +text.Height * ySize.T1); break;
            case ARDB.VerticalTextAlignment.Bottom: ySize = new Interval(0.0, +text.Height * 2.0 * ySize.T1); break;
          }

          return new Box(Location, xSize, ySize, zSize);
        }

        return NaN.Box;
      }
    }

    #region IAnnotationLeadersAccess
    public bool? HasLeader
    {
      get => Value is ARDB.TextNote note && note.LeaderCount > 0;
      set
      {
        if (Value is ARDB.TextNote note && value is false)
          note.RemoveLeaders();
      }
    }

    public AnnotationLeader[] Leaders
    {
      get
      {
        if (Value is ARDB.TextNote text)
        {
          var leaders = new AnnotationLeader[text.LeaderCount];

          var ls = text.GetLeaders();
          for (int r = 0; r < Math.Min(leaders.Length, ls.Count); ++r)
            leaders[r] = new MultiLeader(this, ls[r]);

          return leaders;
        }

        return null;
      }
    }

    class MultiLeader : AnnotationLeader
    {
      protected readonly TextElement text;
      readonly ARDB.Leader Leader;
      public MultiLeader(TextElement t, ARDB.Leader l) { text = t; Leader = l; }

      public override Curve LeaderCurve
      {
        get
        {
          if (text.Value.get_Parameter(ARDB.BuiltInParameter.ARC_LEADER_PARAM)?.AsBoolean() is true)
          {
            var arc = new Arc(HeadPosition, ElbowPosition, EndPosition);
            return new ArcCurve(arc);
          }

          return base.LeaderCurve;
        }
      }

      public override Point3d HeadPosition => Leader.Anchor.ToPoint3d();

      public override bool Visible
      {
        get => true;
        set { }
      }

      public override bool HasElbow => true;
      public override Point3d ElbowPosition
      {
        get => Leader.Elbow.ToPoint3d();
        set => Leader.Elbow = value.ToXYZ();
      }

      public override Point3d EndPosition
      {
        get => Leader.End.ToPoint3d();
        set => Leader.End = value.ToXYZ();
      }

      public override bool IsTextPositionAdjustable => false;
      public override Point3d TextPosition
      {
        get => text.Position;
        set => throw new Exceptions.RuntimeArgumentException($"Text Position can't be adjusted.{{{text.Id.ToString("D")}}}");
      }
    }
    #endregion

    #region IGH_PreviewData
    protected override bool GetClippingBox(out BoundingBox clippingBox)
    {
      clippingBox = base.GetBoundingBox(Transform.Identity);

      foreach (var leader in Leaders)
      {
        if (leader.HasElbow) clippingBox.Union(leader.ElbowPosition);
        clippingBox.Union(leader.EndPosition);
      }

      return true;
    }

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.TextElement text && Type is TextElementType type)
      {
        if (!args.Pipeline.IsDynamicDisplay)
        {
          var viewScale = OwnerView?.Scale ?? 1.0;
          var horizontalAlignment = TextHorizontalAlignment.Center;
          switch (text.HorizontalAlignment)
          {
            case ARDB.HorizontalTextAlignment.Left: horizontalAlignment = Rhino.DocObjects.TextHorizontalAlignment.Left; break;
            case ARDB.HorizontalTextAlignment.Center: horizontalAlignment = Rhino.DocObjects.TextHorizontalAlignment.Center; break;
            case ARDB.HorizontalTextAlignment.Right: horizontalAlignment = Rhino.DocObjects.TextHorizontalAlignment.Right; break;
          }

          var verticalAlignment = TextVerticalAlignment.Middle;
          switch (text.VerticalAlignment)
          {
            case ARDB.VerticalTextAlignment.Top: verticalAlignment = Rhino.DocObjects.TextVerticalAlignment.Top; break;
            case ARDB.VerticalTextAlignment.Middle: verticalAlignment = Rhino.DocObjects.TextVerticalAlignment.Middle; break;
            case ARDB.VerticalTextAlignment.Bottom: verticalAlignment = Rhino.DocObjects.TextVerticalAlignment.Bottom; break;
          }

          var location = Location;
          var plainText = text.Text.Replace('\r', '\n');
          using (var textEntity = new Text3d(plainText)
          {
            HorizontalAlignment = horizontalAlignment,
            VerticalAlignment = verticalAlignment,
            TextPlane = Location,
            Height = (type.TextSize ?? 1.0) * viewScale * Revit.ModelUnits,
            FontFace = type.TextFont ?? "Arial",
            Bold = type.TextStyleBold ?? false,
            Italic = type.TextStyleItalic ?? false,
          })
          //using (var textEntity = new TextEntity()
          //{
          //  Font = new Font
          //  (
          //    type.get_Parameter(External.DB.Schemas.ParameterId.TextFont)?.AsString() ?? "Arial",
          //    weight: (type.get_Parameter(External.DB.Schemas.ParameterId.TextStyleBold)?.AsInteger() ?? 0) != 0 ? Font.FontWeight.Bold : Font.FontWeight.Normal,
          //    style: (type.get_Parameter(External.DB.Schemas.ParameterId.TextStyleItalic)?.AsInteger() ?? 0) != 0 ? Font.FontStyle.Italic : Font.FontStyle.Upright,
          //    underlined: (type.get_Parameter(External.DB.Schemas.ParameterId.TextStyleUnderline)?.AsInteger() ?? 0) != 0,
          //    strikethrough: false
          //  ),
          //  PlainText = plainText,
          //  Justification = TextJustification.Center,
          //  TextHorizontalAlignment = horizontalAlignment,
          //  TextVerticalAlignment = verticalAlignment,
          //  Plane = location,
          //  TextHeight = (type.get_Parameter(External.DB.Schemas.ParameterId.TextSize)?.AsDouble() ?? 1.0) * viewScale * Revit.ModelUnits,
          //})
          {
            var textWidthScale = type.TextWidthScale ?? 1.0;
            if (textWidthScale != 1.0) args.Pipeline.PushModelTransform(args.Pipeline.ModelTransform * Transform.Scale(location, textWidthScale, 1.0, 1.0));

            {
              double displacement = 0.0;
              switch (textEntity.VerticalAlignment)
              {
                case TextVerticalAlignment.Top:    displacement = 0.0; break;
                case TextVerticalAlignment.Middle: displacement = 0.5 * text.Height * Revit.ModelUnits * viewScale; break;
                case TextVerticalAlignment.Bottom: displacement = 1.0 * text.Height * Revit.ModelUnits * viewScale; break;
              }
              textEntity.VerticalAlignment = TextVerticalAlignment.Top;
              displacement -= textEntity.Height * 0.5;

              textEntity.TextPlane = new Plane
              (
                location.Origin + location.YAxis * displacement,
                location.XAxis,
                location.YAxis
              );

              args.Pipeline.Draw3dText(textEntity, args.Color);
            }

            //{
            //  args.Pipeline.DrawText(textEntity, args.Color);
            //}

            if (textWidthScale != 1.0) args.Pipeline.PopModelTransform();
          }
        }
        else
        {
          args.Pipeline.DrawPatternedPolyline(Box.GetCorners().Take(4), args.Color, 0x00003333, args.Thickness, close: true);
        }

        if (HasLeader is true)
        {
          var dpi = args.Pipeline.DpiScale;
          var arrowSize = (int) Math.Round(2.0 * Grasshopper.CentralSettings.PreviewPointRadius * dpi);
          var hasArrow = type.LeaderArrowhead is object;
          foreach (var leader in Leaders)
          {
            if (leader.LeaderCurve is Curve leaderCurve)
            {
              args.Pipeline.DrawCurve(leaderCurve, args.Color, args.Thickness);
              if (hasArrow)
                args.Pipeline.DrawArrowHead(leaderCurve.PointAtEnd, leaderCurve.TangentAtEnd, args.Color, arrowSize, 0.0);
            }
          }
        }
      }
      else base.DrawViewportWires(args);
    }
    #endregion
  }

  [Kernel.Attributes.Name("Text Element Type")]
  public class TextElementType : LineAndTextAttrSymbol
  {
    protected override Type ValueType => typeof(ARDB.TextElementType);
    public new ARDB.TextElementType Value => base.Value as ARDB.TextElementType;

    public TextElementType() { }
    protected internal TextElementType(ARDB.TextElementType type) : base(type) { }

    internal ElementType LeaderArrowhead => ElementType.FromElementId(Document, Value?.get_Parameter(ARDB.BuiltInParameter.LEADER_ARROWHEAD).AsElementId()) as ElementType;
  }
}

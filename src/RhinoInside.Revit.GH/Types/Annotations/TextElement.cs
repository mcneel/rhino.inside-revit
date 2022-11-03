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

  [Kernel.Attributes.Name("Text Element")]
  public class TextElement : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.TextElement);
    public new ARDB.TextElement Value => base.Value as ARDB.TextElement;

    public TextElement() { }
    public TextElement(ARDB.TextElement element) : base(element) { }

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

    public override BoundingBox ClippingBox => base.BoundingBox;

    public override BoundingBox GetBoundingBox(Transform xform) => Box.GetBoundingBox(xform);

    public override Box Box
    {
      get
      {
        if (Value is ARDB.TextElement text)
        {
          var scale = Revit.ModelUnits * (text.Document.GetElement(text.OwnerViewId) as ARDB.View)?.Scale ?? 1.0;
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

          var box = new Box(Location, xSize, ySize, zSize);
          return box;
        }

        return NaN.Box;
      }
    }

    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.TextElement text)
      {
        if (!args.Pipeline.IsDynamicDisplay)
        {
          var viewScale = (text.Document.GetElement(text.OwnerViewId) as ARDB.View)?.Scale ?? 1.0;
          using (var type = text.Symbol)
          {
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
              Height = (type.get_Parameter(External.DB.Schemas.ParameterId.TextSize)?.AsDouble() ?? 1.0) * viewScale * Revit.ModelUnits,
              FontFace = type.get_Parameter(External.DB.Schemas.ParameterId.TextFont)?.AsString() ?? "Arial",
              Bold = (type.get_Parameter(External.DB.Schemas.ParameterId.TextStyleBold)?.AsInteger() ?? 0) != 0,
              Italic = (type.get_Parameter(External.DB.Schemas.ParameterId.TextStyleItalic)?.AsInteger() ?? 0) != 0,
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
              var textWidthScale = type.get_Parameter(External.DB.Schemas.ParameterId.TextWidthScale)?.AsDouble() ?? 1.0;
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
        }
        else
        {
          args.Pipeline.DrawPatternedPolyline(Box.GetCorners().Take(4), args.Color, 0x00003333, args.Thickness, close: true);
        }
      }
      else base.DrawViewportWires(args);
    }
  }
}

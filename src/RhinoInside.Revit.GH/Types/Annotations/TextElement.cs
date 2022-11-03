using System;
using Grasshopper.Kernel;
using Rhino.Display;
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

    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (!args.Pipeline.IsDynamicDisplay && Value is ARDB.TextElement text)
      {
        var viewScale = (text.Document.GetElement(text.OwnerViewId) as ARDB.View)?.Scale ?? 1.0;
        using (var type = text.Symbol)
        {
          var horizontalAlignment = Rhino.DocObjects.TextHorizontalAlignment.Center;
          switch (text.HorizontalAlignment)
          {
            case ARDB.HorizontalTextAlignment.Left:   horizontalAlignment = Rhino.DocObjects.TextHorizontalAlignment.Left; break;
            case ARDB.HorizontalTextAlignment.Center: horizontalAlignment = Rhino.DocObjects.TextHorizontalAlignment.Center; break;
            case ARDB.HorizontalTextAlignment.Right:  horizontalAlignment = Rhino.DocObjects.TextHorizontalAlignment.Right; break;
          }

          var verticalAlignment = Rhino.DocObjects.TextVerticalAlignment.Middle;
          switch (text.VerticalAlignment)
          {
            case ARDB.VerticalTextAlignment.Top:    verticalAlignment = Rhino.DocObjects.TextVerticalAlignment.Top; break;
            case ARDB.VerticalTextAlignment.Middle: verticalAlignment = Rhino.DocObjects.TextVerticalAlignment.Middle; break;
            case ARDB.VerticalTextAlignment.Bottom: verticalAlignment = Rhino.DocObjects.TextVerticalAlignment.Bottom; break;
          }

          using (var textEntity = new Text3d(text.Text)
          {
            HorizontalAlignment = horizontalAlignment,
            VerticalAlignment   = verticalAlignment,
            TextPlane           = Location,
            Height              = (type.get_Parameter(External.DB.Schemas.ParameterId.TextSize)?.AsDouble() ?? 1.0) * viewScale * Revit.ModelUnits,
            FontFace            = type.get_Parameter(External.DB.Schemas.ParameterId.TextFont)?.AsString() ?? "Arial",
            Bold                = (type.get_Parameter(External.DB.Schemas.ParameterId.TextStyleBold)?.AsInteger() ?? 0) != 0,
            Italic              = (type.get_Parameter(External.DB.Schemas.ParameterId.TextStyleItalic)?.AsInteger() ?? 0) != 0,
          })
          {
            var textWidthScale = type.get_Parameter(External.DB.Schemas.ParameterId.TextWidthScale)?.AsDouble() ?? 1.0;
            if (textWidthScale != 1.0)
            {
              args.Pipeline.PushModelTransform(Transform.Scale(textEntity.TextPlane, textWidthScale, 1.0, 1.0));
              args.Pipeline.Draw3dText(textEntity, args.Color);
              args.Pipeline.PopModelTransform();
            }
            else
            {
              args.Pipeline.Draw3dText(textEntity, args.Color);
            }
          }
        }
      }
      else base.DrawViewportWires(args);
    }
  }
}

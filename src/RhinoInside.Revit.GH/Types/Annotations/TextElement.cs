using System;
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
  }
}

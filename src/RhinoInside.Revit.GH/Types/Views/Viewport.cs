using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Viewport")]
  public class Viewport : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.Viewport);
    public new ARDB.Viewport Value => base.Value as ARDB.Viewport;

    public Viewport() { }
    public Viewport(ARDB.Viewport element) : base(element) { }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Viewport viewport)
        {
          return new Plane
          (
            viewport.GetBoxCenter().ToPoint3d(),
            Vector3d.XAxis,
            Vector3d.YAxis
          );
        }

        return base.Location;
      }
    }
  }
}

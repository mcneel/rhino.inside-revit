using System;
using System.Linq;
using Rhino;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Space")]
  public class SpaceElement : SpatialElement
  {
    protected override Type ValueType => typeof(ARDB.Mechanical.Space);
    public new ARDB.Mechanical.Space Value => base.Value as ARDB.Mechanical.Space;

    public SpaceElement() { }
    public SpaceElement(ARDB.Mechanical.Space element) : base(element) { }

    #region Location
    public override Brep PolySurface
    {
      get
      {
        if (Value is ARDB.Mechanical.Space space)
        {
          var solids = space.ClosedShell.OfType<ARDB.Solid>().Where(x => x.Faces.Size > 0);
          return Brep.MergeBreps(solids.Select(x => x.ToBrep()), RhinoMath.UnsetValue);
        }

        return null;
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Zone")]
  public class ZoneElement : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.Mechanical.Zone);
    public new ARDB.Mechanical.Zone Value => base.Value as ARDB.Mechanical.Zone;

    public ZoneElement() { }
    public ZoneElement(ARDB.Mechanical.Zone element) : base(element) { }

    #region Casting
    public override bool ConvertTo<Q>(out Q target)
    {
      target = default;

      if (typeof(Q).IsAssignableFrom(typeof(Phase)))
      {
        target = (Q) (object) GetElement<Phase>(Value?.Phase);
        return true;
      }

      return base.ConvertTo(out target);
    }
    #endregion
  }
}

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
}

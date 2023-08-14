using System;
using System.Linq;
using Rhino;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Room")]
  public class RoomElement : SpatialElement
  {
    protected override Type ValueType => typeof(ARDB.Architecture.Room);
    public new ARDB.Architecture.Room Value => base.Value as ARDB.Architecture.Room;

    public RoomElement() { }
    public RoomElement(ARDB.Architecture.Room element) : base(element) { }

    #region Location
    public override Brep PolySurface
    {
      get
      {
        if (Value is ARDB.Architecture.Room room)
        {
          var solids = room.ClosedShell.OfType<ARDB.Solid>().Where(x => x.Faces.Size > 0);
          return Brep.MergeBreps(solids.Select(x => x.ToBrep()), RhinoMath.UnsetValue);
        }

        return null;
      }
    }
    #endregion
  }
}

using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Wall Foundation")]
  public class WallFoundation : HostObject, IHostElementAccess
  {
    protected override Type ValueType => typeof(ARDB.WallFoundation);
    public new ARDB.WallFoundation Value => base.Value as ARDB.WallFoundation;

    public WallFoundation() { }
    public WallFoundation(ARDB.WallFoundation wallFoundation) : base(wallFoundation) { }

    #region IHostElementAccess
    public override GraphicalElement HostElement => Value is ARDB.WallFoundation wallFoundation ?
      wallFoundation.WallId.IsValid() ?
      GetElement<GraphicalElement>(wallFoundation.WallId) :
      base.HostElement :
      default;
    #endregion
  }
}

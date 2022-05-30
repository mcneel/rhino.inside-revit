using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Spot Dimension")]
  public class SpotDimension : Dimension
  {
    protected override Type ValueType => typeof(ARDB.SpotDimension);
    public new ARDB.SpotDimension Value => base.Value as ARDB.SpotDimension;

    public SpotDimension() { }
    public SpotDimension(ARDB.SpotDimension spotDimension) : base(spotDimension) { }
  }
}

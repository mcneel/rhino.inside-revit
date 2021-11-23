using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Spatial Element")]
  public class SpatialElement : InstanceElement
  {
    protected override Type ValueType => typeof(ARDB.SpatialElement);
    public static explicit operator ARDB.SpatialElement(SpatialElement value) => value?.Value;
    public new ARDB.SpatialElement Value => base.Value as ARDB.SpatialElement;

    public SpatialElement() { }
    public SpatialElement(ARDB.SpatialElement element) : base(element) { }
  }
}

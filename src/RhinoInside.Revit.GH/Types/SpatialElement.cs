using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Spatial Element")]
  public class SpatialElement : InstanceElement
  {
    protected override Type ValueType => typeof(DB.SpatialElement);
    public static explicit operator DB.SpatialElement(SpatialElement value) => value?.Value;
    public new DB.SpatialElement Value => base.Value as DB.SpatialElement;

    public SpatialElement() { }
    public SpatialElement(DB.SpatialElement element) : base(element) { }
  }
}

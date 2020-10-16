using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class SpatialElement : InstanceElement
  {
    public override string TypeDescription => "Represents a Revit Spatial Element";
    protected override Type ScriptVariableType => typeof(DB.SpatialElement);
    public static explicit operator DB.SpatialElement(SpatialElement value) => value?.Value;
    public new DB.SpatialElement Value => base.Value as DB.SpatialElement;

    public SpatialElement() { }
    public SpatialElement(DB.SpatialElement element) : base(element) { }
  }
}

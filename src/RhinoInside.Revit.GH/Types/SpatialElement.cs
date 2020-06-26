using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class SpatialElement : GeometricElement
  {
    public override string TypeDescription => "Represents a Revit Spatial Element";
    protected override Type ScriptVariableType => typeof(DB.SpatialElement);
    public static explicit operator DB.SpatialElement(SpatialElement value) =>
      value.IsValid ? value.Document?.GetElement(value) as DB.SpatialElement : default;

    public SpatialElement() { }
    public SpatialElement(DB.SpatialElement element) : base(element) { }
  }
}

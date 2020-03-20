using System;

namespace RhinoInside.Revit.GH.Types
{
  public class HostObject : GeometricElement
  {
    public override string TypeDescription => "Represents a Revit host element";
    protected override Type ScriptVariableType => typeof(Autodesk.Revit.DB.HostObject);
    public static explicit operator Autodesk.Revit.DB.HostObject(HostObject self) =>
      self.Document?.GetElement(self) as Autodesk.Revit.DB.HostObject;

    public HostObject() { }
    public HostObject(Autodesk.Revit.DB.HostObject host) : base(host) { }
  }
}

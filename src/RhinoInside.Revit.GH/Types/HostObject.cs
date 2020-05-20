using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class HostObject : GeometricElement
  {
    public override string TypeDescription => "Represents a Revit host element";
    protected override Type ScriptVariableType => typeof(DB.HostObject);
    public static explicit operator DB.HostObject(HostObject self) =>
      self.Document?.GetElement(self) as DB.HostObject;

    public HostObject() { }
    public HostObject(DB.HostObject host) : base(host) { }
  }
}

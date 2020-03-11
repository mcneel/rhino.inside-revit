using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class DatumPlane : GeometricElement
  {
    public override string TypeName => "Revit DatumPlane";
    public override string TypeDescription => "Represents a Revit DatumPlane";
    protected override Type ScriptVariableType => typeof(DB.DatumPlane);
    public static explicit operator DB.DatumPlane(DatumPlane self) =>
      self.Document?.GetElement(self) as DB.DatumPlane;

    public DatumPlane() { }
    public DatumPlane(DB.DatumPlane plane) : base(plane) { }

    public override string DisplayName
    {
      get
      {
        var element = (DB.DatumPlane) this;
        if (element is object)
          return element.Name;

        return base.DisplayName;
      }
    }
  }
}

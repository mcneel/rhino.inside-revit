using System;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class FamilyInstance : GraphicalElement
  {
    public override string TypeDescription => "Represents a Revit Family Instance";
    protected override Type ScriptVariableType => typeof(DB.FamilyInstance);
    public static explicit operator DB.FamilyInstance(FamilyInstance value) =>
      value.Document?.GetElement(value) as DB.FamilyInstance;

    public FamilyInstance() { }
    public FamilyInstance(DB.FamilyInstance value) : base(value) { }

    public override Rhino.Geometry.Vector3d Orientation
    {
      get
      {
        var instance = (DB.FamilyInstance) this;
        return instance?.FacingOrientation.ToVector3d() ?? base.Orientation;
      }
    }
  }
}

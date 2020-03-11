using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types.Elements.Level
{
  public class Level : DatumPlane
  {
    public override string TypeName => "Revit Level";
    public override string TypeDescription => "Represents a Revit level";
    protected override Type ScriptVariableType => typeof(DB.Level);
    public static explicit operator DB.Level(Level self) =>
      self.Document?.GetElement(self) as DB.Level;

    public Level() { }
    public Level(DB.Level level) : base(level) { }

    public override Rhino.Geometry.Point3d Location
    {
      get
      {
        var level = (DB.Level) this;
        if (level is object)
        {
          var p = new Rhino.Geometry.Point3d(0.0, 0.0, level.Elevation);
          return p.ChangeUnits(Revit.ModelUnits);
        }

        return new Rhino.Geometry.Point3d(double.NaN, double.NaN, double.NaN);
      }
    }
  }
}

using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types.Elements.Grid
{
  public class Grid : DatumPlane
  {
    public override string TypeName => "Revit Level";
    public override string TypeDescription => "Represents a Revit level";
    protected override Type ScriptVariableType => typeof(DB.Grid);
    public static explicit operator DB.Grid(Grid self) =>
      self.Document?.GetElement(self) as DB.Grid;

    public Grid() { }
    public Grid(DB.Grid grid) : base(grid) { }

    public override Rhino.Geometry.Curve Axis
    {
      get
      {
        var grid = (DB.Grid) this;
        if (grid is object)
          return grid.Curve.ToRhino().ChangeUnits(Revit.ModelUnits);

        return null;
      }
    }
  }
}

using System;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class DatumPlane : GraphicalElement
  {
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

  public class Level : DatumPlane
  {
    public override string TypeDescription => "Represents a Revit level";
    protected override Type ScriptVariableType => typeof(DB.Level);
    public static explicit operator DB.Level(Level self) =>
      self.Document?.GetElement(self) as DB.Level;

    public Level() { }
    public Level(DB.Level level) : base(level) { }

    public override bool CastFrom(object source)
    {
      var value = source;

      if (source is IGH_Goo goo)
        value = goo.ScriptVariable();

      if (value is DB.View view)
        return view.GenLevel is null ? false : SetValue(view.GenLevel);

      return base.CastFrom(source);
    }

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

  public class Grid : DatumPlane
  {

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
        if(grid is object)
          return grid.Curve.ToRhino().ChangeUnits(Revit.ModelUnits);

        return null;
      }
    }
  }
}

using System;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class CurtainGridLine : HostObject
  {
    public override string TypeDescription => "Represents a Revit Curtain Grid Line Element";
    protected override Type ScriptVariableType => typeof(DB.CurtainGridLine);
    public static explicit operator DB.CurtainGridLine(CurtainGridLine self) =>
      self.Document?.GetElement(self) as DB.CurtainGridLine;

    public CurtainGridLine() { }
    public CurtainGridLine(DB.CurtainGridLine gridLine) : base(gridLine) { }

    public override Rhino.Geometry.Curve Curve
    {
      get
      {
        var gridLine = (DB.CurtainGridLine) this;
        var axisCurve = gridLine?.FullCurve?.ToCurve();

        return axisCurve;
      }
    }
  }
}

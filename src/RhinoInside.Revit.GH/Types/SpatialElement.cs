using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class SpatialElement : GraphicalElement
  {
    public override string TypeDescription => "Represents a Revit Spatial Element";
    protected override Type ScriptVariableType => typeof(DB.SpatialElement);
    public static explicit operator DB.SpatialElement(SpatialElement self) =>
      self.Document?.GetElement(self) as DB.SpatialElement;

    public SpatialElement() { }
    public SpatialElement(DB.SpatialElement gridLine) : base(gridLine) { }
  }
}

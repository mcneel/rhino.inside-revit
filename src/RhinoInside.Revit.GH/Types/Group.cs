using System;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Group : GraphicalElement
  {
    public override string TypeDescription => "Represents a Revit group element";
    protected override Type ScriptVariableType => typeof(DB.Group);
    public static explicit operator DB.Group(Group value) =>
      value.IsValid ? value.Document?.GetElement(value) as DB.Group : default;

    public Group() { }
    public Group(DB.Group value) : base(value) { }

    public override Level Level
    {
      get
      {
        var element = (DB.Group) this;
        return element is null ? null : FromElement(element.GetParameterValue<DB.Level>(DB.BuiltInParameter.GROUP_LEVEL)) as Level;
      }
    }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var bbox = Boundingbox;
      if (!bbox.IsValid)
        return;

      foreach (var edge in bbox.GetEdges() ?? Enumerable.Empty<Line>())
        args.Pipeline.DrawPatternedLine(edge.From, edge.To, args.Color, 0x00003333, args.Thickness);
    }
    public override void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion
  }
}

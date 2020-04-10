using System;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Group : GraphicalElement
  {
    public override string TypeDescription => "Represents a Revit group element";
    protected override Type ScriptVariableType => typeof(Autodesk.Revit.DB.HostObject);
    public static explicit operator Autodesk.Revit.DB.Group(Group self) =>
      self.Document?.GetElement(self) as Autodesk.Revit.DB.Group;

    public Group() { }
    public Group(Autodesk.Revit.DB.Group host) : base(host) { }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var bbox = Boundingbox;
      if (!bbox.IsValid)
        return;

      foreach (var edge in bbox.GetEdges() ?? Enumerable.Empty<Line>())
        args.Pipeline.DrawPatternedLine(edge.From, edge.To, args.Color, 0x00003333, 1 /*args.Thickness*/);
    }
    public override void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion
  }
}

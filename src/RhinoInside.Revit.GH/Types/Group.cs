using System;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Group")]
  public class Group : GraphicalElement
  {
    protected override Type ScriptVariableType => typeof(DB.Group);
    public new DB.Group Value => base.Value as DB.Group;
    public static explicit operator DB.Group(Group value) => value?.Value;

    public Group() { }
    public Group(DB.Group value) : base(value) { }

    public override Level Level
    {
      get
      {
        if(Value is DB.Group group)
          return Types.Level.FromElement(group.GetParameterValue<DB.Level>(DB.BuiltInParameter.GROUP_LEVEL)) as Level;

        return default;
      }
    }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var bbox = ClippingBox;
      if (!bbox.IsValid)
        return;

      foreach (var edge in bbox.GetEdges() ?? Enumerable.Empty<Line>())
        args.Pipeline.DrawPatternedLine(edge.From, edge.To, args.Color, 0x00003333, args.Thickness);
    }
    #endregion
  }
}

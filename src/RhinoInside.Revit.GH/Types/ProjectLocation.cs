using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Shared Site")]
  public class ProjectLocation : Instance
  {
    protected override Type ScriptVariableType => typeof(DB.ProjectLocation);
    public new DB.ProjectLocation Value => base.Value as DB.ProjectLocation;

    public ProjectLocation() { }
    public ProjectLocation(DB.ProjectLocation instance) : base(instance) { }

    #region IGH_PreviewData
    public override BoundingBox BoundingBox
    {
      get
      {
        var location = Location;
        if (location.IsValid)
          return new BoundingBox(location.Origin, location.Origin);

        return BoundingBox.Unset;
      }
    }

    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var location = Location;
      if (location.IsValid)
      {
        var strokeColor = (System.Drawing.Color) Rhino.Display.ColorRGBA.ApplyGamma(new Rhino.Display.ColorRGBA(args.Color), 2.0);
        args.Pipeline.DrawPoint(location.Origin, Rhino.Display.PointStyle.Pin, strokeColor, args.Color, 12.0f, 2.0f, 7.0f, 0.0f, true, true);
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Site Location")]
  public class SiteLocation : ElementType
  {
    protected override Type ScriptVariableType => typeof(DB.SiteLocation);
    public new DB.SiteLocation Value => base.Value as DB.SiteLocation;

    public SiteLocation() { }
    public SiteLocation(DB.SiteLocation value) : base(value) { }

    public override string DisplayName => Value?.PlaceName;
  }
}

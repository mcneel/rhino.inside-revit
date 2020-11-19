using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Base Point")]
  class BasePoint : GraphicalElement
  {
    protected override Type ScriptVariableType => typeof(DB.BasePoint);
    public new DB.BasePoint Value => base.Value as DB.BasePoint;

    public BasePoint() { }
    public BasePoint(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public BasePoint(DB.BasePoint point) : base(point) { }

    public override string DisplayName
    {
      get
      {
        if (Value is DB.BasePoint point)
          return point.Category.Name;

        return base.DisplayName;
      }
    }

    #region IGH_PreviewData
    public override BoundingBox ClippingBox
    {
      get
      {
        if (Value is DB.BasePoint point)
        {
          return new BoundingBox
          (
            new Point3d[]
            {
              point.GetPosition().ToPoint3d(),
              (point.GetPosition() - point.GetSharedPosition()).ToPoint3d()
            }
          );
        }

        return BoundingBox.Unset;
      }
    }

    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is DB.BasePoint point)
      {
        var location = Location;
        point.Category.Id.TryGetBuiltInCategory(out var builtInCategory);
        var pointStyle = default(Rhino.Display.PointStyle);
        var angle = default(float);
        var radius = 6.0f;
        var secondarySize = 3.5f;
        switch (builtInCategory)
        {
          case DB.BuiltInCategory.OST_IOS_GeoSite:
            pointStyle = Rhino.Display.PointStyle.ActivePoint;
            break;
          case DB.BuiltInCategory.OST_ProjectBasePoint:
            pointStyle = Rhino.Display.PointStyle.RoundActivePoint;
            angle = (float) Rhino.RhinoMath.ToRadians(45);
            break;
          case DB.BuiltInCategory.OST_SharedBasePoint:
            pointStyle = Rhino.Display.PointStyle.Triangle;
            radius = 12.0f;
            secondarySize = 7.0f;
            break;
        }

        var strokeColor = (System.Drawing.Color) Rhino.Display.ColorRGBA.ApplyGamma(new Rhino.Display.ColorRGBA(args.Color), 2.0);
        args.Pipeline.DrawPoint(location.Origin, pointStyle, strokeColor, args.Color, radius, 2.0f, secondarySize, angle, true, true);
      }
    }
    #endregion

    #region Properties
    public override Plane Location
    {
      get
      {
        if (Value is DB.BasePoint point)
        {
          return new Plane
          (
            point.GetPosition().ToPoint3d(),
            Vector3d.XAxis,
            Vector3d.YAxis
          );
        }

        return base.Location;
      }
    }
    #endregion
  }
}

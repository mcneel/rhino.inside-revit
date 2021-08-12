using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Base Point")]
  public class BasePoint : GraphicalElement
  {
    protected override Type ScriptVariableType => typeof(DB.BasePoint);
    public new DB.BasePoint Value => base.Value as DB.BasePoint;

    protected override bool SetValue(DB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(DB.Element element)
    {
      return element is DB.BasePoint &&
             element.Category.Id.IntegerValue != (int) DB.BuiltInCategory.OST_IOS_GeoSite;
    }

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

        return NaN.BoundingBox;
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
          var origin = point.GetPosition().ToPoint3d();
          var axisX = Vector3d.XAxis;
          var axisY = Vector3d.YAxis;

          if (point.IsShared)
          {
            point.Document.ActiveProjectLocation.GetLocation(out var _, out var basisX, out var basisY);
            axisX = basisX.ToVector3d();
            axisY = basisY.ToVector3d();
          }
          return new Plane(origin, axisX, axisY);
        }

        return base.Location;
      }
    }
    #endregion
  }
}

namespace RhinoInside.Revit.GH.Types
{
// TODO : Upgrade Revit 2021 nuget package to 2021.0.1 and change the if below to REVIT_2021
#if REVIT_2022
  using DBInternalOrigin = Autodesk.Revit.DB.InternalOrigin;
#else
  using DBInternalOrigin = Autodesk.Revit.DB.BasePoint;
#endif

  [Kernel.Attributes.Name("Internal Origin")]
  public class InternalOrigin : GraphicalElement
  {
    protected override Type ScriptVariableType => typeof(DBInternalOrigin);
    public new DBInternalOrigin Value => base.Value as DBInternalOrigin;

    protected override bool SetValue(DB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(DB.Element element)
    {
      return element is DBInternalOrigin &&
             element.Category.Id.IntegerValue == (int) DB.BuiltInCategory.OST_IOS_GeoSite;
    }

    public InternalOrigin() { }
    public InternalOrigin(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public InternalOrigin(DBInternalOrigin point) : base(point) { }

    public override string DisplayName
    {
      get
      {
        if (Value is DBInternalOrigin point)
          return point.Category.Name;

        return base.DisplayName;
      }
    }

#region IGH_PreviewData
    public override BoundingBox ClippingBox
    {
      get
      {
        if (Value is DBInternalOrigin point)
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

        return NaN.BoundingBox;
      }
    }

    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is DBInternalOrigin point)
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
        if (Value is DBInternalOrigin point)
        {
          var origin = point.GetPosition().ToPoint3d();
          var axisX = Vector3d.XAxis;
          var axisY = Vector3d.YAxis;

          if (true /*point.IsShared*/)
          {
            point.Document.ActiveProjectLocation.GetLocation(out var _, out var basisX, out var basisY);
            axisX = basisX.ToVector3d();
            axisY = basisY.ToVector3d();
          }
          return new Plane(origin, axisX, axisY);
        }

        return base.Location;
      }
    }
#endregion
  }
}

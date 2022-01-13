using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Base Point")]
  public class BasePoint : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.BasePoint);
    public new ARDB.BasePoint Value => base.Value as ARDB.BasePoint;

    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      return element is ARDB.BasePoint &&
             element.Category.Id.IntegerValue != (int) ARDB.BuiltInCategory.OST_IOS_GeoSite;
    }

    public BasePoint() { }
    public BasePoint(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public BasePoint(ARDB.BasePoint point) : base(point) { }

    public override string DisplayName
    {
      get
      {
        if (Value is ARDB.BasePoint point)
          return point.Category.Name;

        return base.DisplayName;
      }
    }

    #region IGH_PreviewData
    public override BoundingBox ClippingBox
    {
      get
      {
        if (Value is ARDB.BasePoint point)
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
      if (Value is ARDB.BasePoint point)
      {
        var location = Location;
        point.Category.Id.TryGetBuiltInCategory(out var builtInCategory);
        var pointStyle = default(Rhino.Display.PointStyle);
        var angle = default(float);
        var radius = 6.0f;
        var secondarySize = 3.5f;
        switch (builtInCategory)
        {
          case ARDB.BuiltInCategory.OST_IOS_GeoSite:
            pointStyle = Rhino.Display.PointStyle.ActivePoint;
            break;
          case ARDB.BuiltInCategory.OST_ProjectBasePoint:
            pointStyle = Rhino.Display.PointStyle.RoundActivePoint;
            angle = (float) Rhino.RhinoMath.ToRadians(45);
            break;
          case ARDB.BuiltInCategory.OST_SharedBasePoint:
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
        if (Value is ARDB.BasePoint point)
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
#if REVIT_2021
  using DBInternalOrigin = Autodesk.Revit.DB.InternalOrigin;
#elif REVIT_2020
  using DBInternalOrigin = Autodesk.Revit.DB.Element;
#else
  using DBInternalOrigin = Autodesk.Revit.DB.BasePoint;
#endif

  [Kernel.Attributes.Name("Internal Origin")]
  public class InternalOrigin : GraphicalElement
  {
    protected override Type ValueType => typeof(DBInternalOrigin);
    public new DBInternalOrigin Value => base.Value as DBInternalOrigin;

    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      return element is DBInternalOrigin &&
             element.Category?.Id.IntegerValue == (int) ARDB.BuiltInCategory.OST_IOS_GeoSite;
    }

    public InternalOrigin() { }
    public InternalOrigin(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public InternalOrigin(DBInternalOrigin point) : base(point)
    {
#if !REVIT_2021
      if (!IsValidElement(point))
        throw new ArgumentException("Invalid Element", nameof(point));
#endif
    }

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
    public override BoundingBox ClippingBox => Value is DBInternalOrigin ?
      new BoundingBox(Point3d.Origin, Point3d.Origin):
      NaN.BoundingBox;

    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is DBInternalOrigin)
      {
        var location = Location;
        var pointStyle = Rhino.Display.PointStyle.ActivePoint;
        var angle = default(float);
        var radius = 6.0f;
        var secondarySize = 3.5f;

        var strokeColor = (System.Drawing.Color) Rhino.Display.ColorRGBA.ApplyGamma(new Rhino.Display.ColorRGBA(args.Color), 2.0);
        args.Pipeline.DrawPoint(location.Origin, pointStyle, strokeColor, args.Color, radius, 2.0f, secondarySize, angle, true, true);
      }
    }
    #endregion

    #region Properties
    public override Plane Location => Value is DBInternalOrigin ?
      Plane.WorldXY : base.Location;
    #endregion
  }
}

using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.Geometry.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  /// <summary>
  /// Base class for any <see cref="DB.Element"/> that has a Graphical representation in Revit
  /// </summary>
  public class GraphicalElement :
    Element,
    IGH_GeometricGoo,
    IGH_PreviewData
  {
    public GraphicalElement() { }
    public GraphicalElement(DB.Element element) : base(element) { }

    protected override bool SetValue(DB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static bool IsValidElement(DB.Element element)
    {
      if (element is DB.ElementType)
        return false;

      if (element is DB.View)
        return false;

      if (element.Location is object)
        return true;

      return
      (
        element is DB.DirectShape ||
        element is DB.CurveElement ||
        element is DB.CombinableElement ||
        element is DB.Architecture.TopographySurface ||
        element is DB.Opening ||
        InstanceElement.IsValidElement(element)
      );
    }

    #region IGH_GeometricGoo
    public BoundingBox Boundingbox => ClippingBox;
    Guid IGH_GeometricGoo.ReferenceID
    {
      get => Guid.Empty;
      set { if (value != Guid.Empty) throw new InvalidOperationException(); }
    }
    bool IGH_GeometricGoo.IsReferencedGeometry => IsReferencedElement;
    bool IGH_GeometricGoo.IsGeometryLoaded => IsElementLoaded;

    void IGH_GeometricGoo.ClearCaches() => UnloadElement();
    IGH_GeometricGoo IGH_GeometricGoo.DuplicateGeometry() => (IGH_GeometricGoo) MemberwiseClone();
    public virtual BoundingBox GetBoundingBox(Transform xform) => ClippingBox;
    bool IGH_GeometricGoo.LoadGeometry() => IsElementLoaded || LoadElement();
    bool IGH_GeometricGoo.LoadGeometry(Rhino.RhinoDoc doc) => IsElementLoaded || LoadElement();
    IGH_GeometricGoo IGH_GeometricGoo.Transform(Transform xform) => null;
    IGH_GeometricGoo IGH_GeometricGoo.Morph(SpaceMorph xmorph) => null;
    #endregion

    #region IGH_PreviewData
    public virtual void DrawViewportWires(GH_PreviewWireArgs args) { }
    public virtual void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion

    public override bool CastTo<Q>(ref Q target)
    {
      if (base.CastTo<Q>(ref target))
        return true;

      if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
      {
        try
        {
          var plane = Location;
          if (!plane.IsValid || !plane.Origin.IsValid)
            return false;

          target = (Q) (object) new GH_Plane(plane);
          return true;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Point)))
      {
        var location = Location.Origin;
        if (!location.IsValid)
          return false;

        target = (Q) (object) new GH_Point(location);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Line)))
      {
        var curve = Curve;
        if (curve?.IsValid != true)
          return false;

        if (!curve.TryGetLine(out var line, Revit.VertexTolerance * Revit.ModelUnits))
          return false;

        target = (Q) (object) new GH_Line(line);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Vector)))
      {
        var orientation = Orientation;
        if (!orientation.IsValid || orientation.IsZero)
          return false;

        target = (Q) (object) new GH_Vector(orientation);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Transform)))
      {
        var plane = Location;
        if (!plane.IsValid || !plane.Origin.IsValid)
          return false;

        target = (Q) (object) new GH_Transform(Transform.PlaneToPlane(Plane.WorldXY, plane));
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Box)))
      {
        var box = Box;
        if (!box.IsValid)
          return false;

        target = (Q) (object) new GH_Box(box);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
      {
        var axis = Curve;
        if (axis is null)
          return false;

        target = (Q) (object) new GH_Curve(axis);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Surface)))
      {
        var surface = Surface;
        if (surface is null)
          return false;

        target = (Q) (object) new GH_Surface(surface);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
      {
        var surface = Surface;
        if (surface is null)
          return false;

        target = (Q) (object) new GH_Brep(surface);
        return true;
      }

      return false;
    }

    #region Location
    public virtual Box Box
    {
      get
      {
        if ((DB.Element) this is DB.Element element)
          return element.get_BoundingBox(null).ToBox();

        return new Box(ClippingBox);
      }
    }

    public virtual Level Level => default;

    public virtual Plane Location
    {
      get
      {
        if (!ClippingBox.IsValid) return new Plane
        (
          new Point3d(double.NaN, double.NaN, double.NaN),
          new Vector3d(double.NaN, double.NaN, double.NaN),
          new Vector3d(double.NaN, double.NaN, double.NaN)
        );

        var origin = ClippingBox.Center;
        var axis = Vector3d.XAxis;
        var perp = Vector3d.YAxis;

        var element = (DB.Element) this;
        if (element is object)
        {
          switch (element.Location)
          {
            case DB.LocationPoint pointLocation:
              origin = pointLocation.Point.ToPoint3d();
              try
              {
                axis.Rotate(pointLocation.Rotation, Vector3d.ZAxis);
                perp.Rotate(pointLocation.Rotation, Vector3d.ZAxis);
              }
              catch { }

              break;
            case DB.LocationCurve curveLocation:
              var curve = curveLocation.Curve;
              if (curve.IsBound)
              {
                var start = curve.Evaluate(0.0, normalized: true).ToPoint3d();
                var end = curve.Evaluate(1.0, normalized: true).ToPoint3d();
                axis = end - start;
                origin = start + (axis * 0.5);
                perp = axis.PerpVector();
              }
              else if(curve is DB.Arc || curve is DB.Ellipse)
              {
                var start = curve.Evaluate(0.0, normalized: false).ToPoint3d();
                var end = curve.Evaluate(Math.PI, normalized: false).ToPoint3d();
                axis = end - start;
                origin = start + (axis * 0.5);
                perp = axis.PerpVector();
              }

              break;
          }
        }

        return new Plane(origin, axis, perp);
      }
    }

    public virtual Vector3d Orientation => Location.YAxis;

    public virtual Vector3d Handing => Location.XAxis;

    public virtual Curve Curve
    {
      get
      {
        var element = (DB.Element) this;

        if (element is DB.ModelCurve modelCurve)
        {
          return modelCurve.GeometryCurve.ToCurve();
        }

        if (element.Location is DB.LocationPoint location)
        {
          if (element is DB.FamilyInstance instance)
          {
            if (instance.Symbol.Family.FamilyPlacementType == DB.FamilyPlacementType.TwoLevelsBased)
            {
              var baseLevel = element.get_Parameter(DB.BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).AsElementId();
              var topLevel = element.get_Parameter(DB.BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).AsElementId();
              var baseLevelOffset = element.get_Parameter(DB.BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).AsDouble();
              var topLevelOffset = element.get_Parameter(DB.BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).AsDouble();

              var baseElevation = ((element.Document.GetElement(baseLevel) as DB.Level).Elevation + baseLevelOffset) * Revit.ModelUnits;
              var topElevation = ((element.Document.GetElement(topLevel) as DB.Level).Elevation + topLevelOffset) * Revit.ModelUnits;

              var origin = location.Point.ToPoint3d();
              return new LineCurve
              (
                new Line
                (
                  origin + Vector3d.ZAxis * baseElevation,
                  origin + Vector3d.ZAxis * topElevation
                )
                ,
                baseElevation,
                topElevation
              );
            }
          }
        }

        return element?.Location is DB.LocationCurve curveLocation ?
          curveLocation.Curve.ToCurve() :
          null;
      }
    }

    public virtual Brep Surface => null;
    #endregion
  }
}

using System;
using System.Reflection;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  /// <summary>
  /// Interface that represents any <see cref="DB.Element"/> that has a Graphical representation in Revit
  /// </summary>
  [Kernel.Attributes.Name("Graphical Element")]
  public interface IGH_GraphicalElement : IGH_Element, IGH_QuickCast
  {
    bool? ViewSpecific { get; }
    View OwnerView { get; }
  }

  [Kernel.Attributes.Name("Graphical Element")]
  public class GraphicalElement :
    Element,
    IGH_GraphicalElement,
    IGH_GeometricGoo,
    IGH_PreviewData
  {
    public GraphicalElement() { }
    public GraphicalElement(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public GraphicalElement(DB.Element element) : base(element) { }

    protected override bool SetValue(DB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static bool IsValidElement(DB.Element element)
    {
      if (element is DB.ElementType)
        return false;

      if (element is DB.View)
        return false;

      using (var location = element.Location)
      {
        if (location is object) return true;
      }

      using (var bbox = element.get_BoundingBox(null))
      {
        return bbox is object;
      }
    }

    protected override void SubInvalidateGraphics()
    {
      clippingBox = default;

      base.SubInvalidateGraphics();
    }

    #region IGH_GraphicalElement
    public bool? ViewSpecific => Value?.ViewSpecific;
    public View OwnerView => View.FromElementId(Document, Value?.OwnerViewId) as View;
    #endregion

    #region IGH_GeometricGoo
    BoundingBox IGH_GeometricGoo.Boundingbox => BoundingBox;
    Guid IGH_GeometricGoo.ReferenceID
    {
      get => Guid.Empty;
      set { if (value != Guid.Empty) throw new InvalidOperationException(); }
    }
    bool IGH_GeometricGoo.IsReferencedGeometry => IsReferencedElement;
    bool IGH_GeometricGoo.IsGeometryLoaded => IsElementLoaded;

    void IGH_GeometricGoo.ClearCaches() => UnloadElement();
    IGH_GeometricGoo IGH_GeometricGoo.DuplicateGeometry() => (IGH_GeometricGoo) MemberwiseClone();
    public virtual BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is DB.Element)
      {
        var bbox = BoundingBox;

        return xform.IsIdentity ? bbox : new BoundingBox(new Point3d[] { bbox.Min, bbox.Max }, xform);
      }

      return BoundingBox.Unset;
    }

    bool IGH_GeometricGoo.LoadGeometry() => IsElementLoaded || LoadElement();
    bool IGH_GeometricGoo.LoadGeometry(Rhino.RhinoDoc doc) => IsElementLoaded || LoadElement();
    IGH_GeometricGoo IGH_GeometricGoo.Transform(Transform xform) => null;
    IGH_GeometricGoo IGH_GeometricGoo.Morph(SpaceMorph xmorph) => null;
    #endregion

    #region IGH_QuickCast
    Point3d IGH_QuickCast.QC_Pt()
    {
      var location = Location;
      if (location.IsValid)
        return location.Origin;

      throw new InvalidCastException();
    }
    Vector3d IGH_QuickCast.QC_Vec()
    {
      var orientation = FacingOrientation;
      if (orientation.IsValid)
        return orientation;

      throw new InvalidCastException();
    }
    Interval IGH_QuickCast.QC_Interval()
    {
      var bbox = BoundingBox;
      if (bbox.IsValid)
        return new Interval(bbox.Min.Z, bbox.Max.Z);

      throw new InvalidCastException();
    }
    #endregion

    #region IGH_PreviewData
    private BoundingBox? clippingBox;
    BoundingBox IGH_PreviewData.ClippingBox
    {
      get
      {
        if (!clippingBox.HasValue)
          clippingBox = ClippingBox;

        return clippingBox.Value;
      }
    }

    /// <summary>
    /// Not necessarily accurate axis aligned <see cref="Rhino.Geometry.BoundingBox"/> for display.
    /// </summary>
    public virtual BoundingBox ClippingBox => BoundingBox;

    public virtual void DrawViewportWires(GH_PreviewWireArgs args) { }
    public virtual void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo<Q>(out target))
        return true;

      if (typeof(Q).IsAssignableFrom(typeof(GH_Interval)))
      {
        var domain = Domain;
        if (!domain.IsValid)
          return false;

        target = (Q) (object) new GH_Interval(domain);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Interval2D)))
      {
        var domain = DomainUV;
        if (!domain.IsValid)
          return false;

        target = (Q) (object) new GH_Interval2D(domain);
        return true;
      }

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

      if (typeof(Q).IsAssignableFrom(typeof(GH_Vector)))
      {
        var orientation = FacingOrientation;
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

      if (typeof(Q).IsAssignableFrom(typeof(GH_Line)))
      {
        var curve = Curve;
        if (curve?.IsValid != true)
          return false;

        target = (Q) (object) new GH_Line(new Line(curve.PointAtStart, curve.PointAtEnd));
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
    public virtual Level Level => default;

    /// <summary>
    /// Accurate axis aligned <see cref="Rhino.Geometry.BoundingBox"/> for computation.
    /// </summary>
    public virtual BoundingBox BoundingBox => Value is DB.Element element ?
      element.get_BoundingBox(null).ToBoundingBox() :
      BoundingBox.Unset;

    /// <summary>
    /// Box aligned to <see cref="Location"/>
    /// </summary>
    public virtual Box Box
    {
      get
      {
        if (Value is DB.Element element)
        {
          var plane = Location;
          if (!Location.IsValid)
            return element.get_BoundingBox(null).ToBox();

          var xform = Transform.ChangeBasis(Plane.WorldXY, plane);
          var bbox = GetBoundingBox(xform);

          return new Box
          (
            plane,
            new Interval(bbox.Min.X, bbox.Max.X),
            new Interval(bbox.Min.Y, bbox.Max.Y),
            new Interval(bbox.Min.Z, bbox.Max.Z)
          );
        }

        return Box.Unset;
      }
    }

    public virtual Interval Domain
    {
      get
      {
        var box = BoundingBox;
        if (!box.IsValid)
          return Interval.Unset;

        return new Interval(box.Min.Z, box.Max.Z);
      }
    }

    public virtual UVInterval DomainUV
    {
      get
      {
        var box = BoundingBox;
        if (!box.IsValid)
          return new UVInterval(Interval.Unset, Interval.Unset);

        var u = new Interval(box.Min.X, box.Max.X);
        var v = new Interval(box.Min.Y, box.Max.Y);
        return new UVInterval(u, v);
      }
    }

    /// <summary>
    /// <see cref="Rhino.Geometry.Plane"/> where this element is located.
    /// </summary>
    public virtual Plane Location
    {
      get
      {
        var origin = new Point3d(double.NaN, double.NaN, double.NaN);
        var axis = new Vector3d(double.NaN, double.NaN, double.NaN);
        var perp = new Vector3d(double.NaN, double.NaN, double.NaN);

        if (Value is DB.Element element)
        {
          switch (element.Location)
          {
            case DB.LocationPoint pointLocation:
              origin = pointLocation.Point.ToPoint3d();
              axis = Vector3d.XAxis;
              perp = Vector3d.YAxis;
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
              else if (curve is DB.Arc || curve is DB.Ellipse)
              {
                var start = curve.Evaluate(0.0, normalized: false).ToPoint3d();
                var end = curve.Evaluate(Math.PI, normalized: false).ToPoint3d();
                axis = end - start;
                origin = start + (axis * 0.5);
                perp = axis.PerpVector();
              }

              break;
            default:
              var bbox = BoundingBox;
              if (bbox.IsValid)
              {
                origin = BoundingBox.Center;
                axis = Vector3d.XAxis;
                perp = Vector3d.YAxis;
              }
              break;
          }
        }

        return new Plane(origin, axis, perp);
      }
      set
      {
        var plane = value.ToPlane();
        SetLocation(plane.Origin, plane.XVec, plane.YVec);
      }
    }

    void GetLocation(out DB.XYZ origin, out DB.XYZ basisX, out DB.XYZ basisY)
    {
      var plane = Location.ToPlane();
      origin = plane.Origin;
      basisX = plane.XVec;
      basisY = plane.YVec;
    }

    void SetLocation(DB.XYZ newOrigin, DB.XYZ newBasisX, DB.XYZ newBasisY)
    {
      if (Value is DB.Element element)
      {
        InvalidateGraphics();

        GetLocation(out var origin, out var basisX, out var basisY);
        var basisZ = basisX.CrossProduct(basisY);

        var newBasisZ = newBasisX.CrossProduct(newBasisY);
        {
          if (!basisZ.IsParallelTo(newBasisZ))
          {
            var axisDirection = basisZ.CrossProduct(newBasisZ);
            double angle = basisZ.AngleTo(newBasisZ);

            using (var axis = DB.Line.CreateUnbound(origin, axisDirection))
              DB.ElementTransformUtils.RotateElement(element.Document, element.Id, axis, angle);

            GetLocation(out origin, out basisX, out basisY);
            basisZ = basisX.CrossProduct(basisY);
          }

          if (!basisX.IsAlmostEqualTo(newBasisX))
          {
            double angle = basisX.AngleOnPlaneTo(newBasisX, newBasisZ);
            using (var axis = DB.Line.CreateUnbound(origin, newBasisZ))
              DB.ElementTransformUtils.RotateElement(element.Document, element.Id, axis, angle);
          }

          {
            var trans = newOrigin - origin;
            if (!trans.IsZeroLength())
              DB.ElementTransformUtils.MoveElement(element.Document, element.Id, trans);
          }
        }
      }
    }

    protected static Rhino.DocObjects.ConstructionPlane CreateConstructionPlane(string name, Plane location, Rhino.RhinoDoc rhinoDoc)
    {
      bool imperial = rhinoDoc.ModelUnitSystem == Rhino.UnitSystem.Feet || rhinoDoc.ModelUnitSystem == Rhino.UnitSystem.Inches;

      return new Rhino.DocObjects.ConstructionPlane()
      {
        Plane = location,
        GridSpacing = imperial ?
        1.0 * Rhino.RhinoMath.UnitScale(Rhino.UnitSystem.Yards, rhinoDoc.ModelUnitSystem) :
        1.0 * Rhino.RhinoMath.UnitScale(Rhino.UnitSystem.Meters, rhinoDoc.ModelUnitSystem),

        SnapSpacing = imperial ?
        1 / 16.0 * Rhino.RhinoMath.UnitScale(Rhino.UnitSystem.Inches, rhinoDoc.ModelUnitSystem) :
        1.0 * Rhino.RhinoMath.UnitScale(Rhino.UnitSystem.Millimeters, rhinoDoc.ModelUnitSystem),

        GridLineCount = 70,
        ThickLineFrequency = imperial ? 6 : 5,
        DepthBuffered = true,
        Name = name
      };
    }

    public virtual Vector3d FacingOrientation => Location.YAxis;

    public virtual Vector3d HandOrientation => Location.XAxis;

    public virtual Curve Curve
    {
      get => Value?.Location is DB.LocationCurve curveLocation ?
          curveLocation.Curve.ToCurve() :
          default;
      set
      {
        if (value is object && Value is DB.Element element)
        {
          if (element.Location is DB.LocationCurve locationCurve)
          {
            InvalidateGraphics();
            locationCurve.Curve = value.ToCurve();
          }
          else throw new InvalidOperationException("Curve can not be set for this element.");
        }
      }
    }

    public virtual Brep Surface => null;
    #endregion

    #region Flip
    public virtual bool CanFlipFacing
    {
      get
      {
        return Value?.GetType() is Type type &&
          type.GetMethod("Flip") is MethodInfo &&
          type.GetProperty("Flipped") is PropertyInfo;
      }
    }
    public virtual bool? FacingFlipped
    {
      get
      {
        return Value is DB.Element element && element.GetType().GetProperty("Flipped") is PropertyInfo Flipped ?
          (bool?) Flipped.GetValue(element) :
          default;
      }
      set
      {
        if (value.HasValue && Value is DB.Element element)
        {
          var Flip = element.GetType().GetMethod("Flip");
          var Flipped = element.GetType().GetProperty("Flipped");

          if (Flip is null || Flipped is null)
            throw new InvalidOperationException("Facing can not be flipped for this element.");

          if ((bool) Flipped.GetValue(element) != value)
          {
            InvalidateGraphics();
            Flip.Invoke(element, new object[] { });
          }
        }
      }
    }

    public virtual bool CanFlipHand => false;
    public virtual bool? HandFlipped
    {
      get => default;
      set
      {
        if (value.HasValue && Value is DB.Element element)
        {
          if (!CanFlipHand)
            throw new InvalidOperationException("Hand can not be flipped for this element.");

          if (HandFlipped != value)
            throw new MissingMemberException(element.GetType().FullName, nameof(HandFlipped));
        }
      }
    }

    public virtual bool CanFlipWorkPlane => false;
    public virtual bool? WorkPlaneFlipped
    {
      get => default;
      set
      {
        if (value.HasValue && Value is DB.Element element)
        {
          if (!CanFlipWorkPlane)
            throw new InvalidOperationException("Work Plane can not be flipped for this element.");

          if (WorkPlaneFlipped != value)
            throw new MissingMemberException(element.GetType().FullName, nameof(WorkPlaneFlipped));
        }
      }
    }
    #endregion
  }
}

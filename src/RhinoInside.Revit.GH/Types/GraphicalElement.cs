using System;
using System.Reflection;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  /// <summary>
  /// Interface that represents any <see cref="ARDB.Element"/> that has a Graphical representation in Revit
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
    public GraphicalElement(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public GraphicalElement(ARDB.Element element) : base(element) { }

    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static bool IsValidElement(ARDB.Element element)
    {
      if (!element.IsValid())
        return false;

      if (element is ARDB.ElementType)
        return false;

      if (element is ARDB.View)
        return false;

      using (var location = element.Location)
      {
        if (location is object) return true;
      }

      using (var bbox = element.GetBoundingBoxXYZ())
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
    bool IGH_GeometricGoo.IsReferencedGeometry => IsReferencedData;
    bool IGH_GeometricGoo.IsGeometryLoaded => IsReferencedDataLoaded;

    void IGH_GeometricGoo.ClearCaches() => UnloadReferencedData();
    IGH_GeometricGoo IGH_GeometricGoo.DuplicateGeometry() => (IGH_GeometricGoo) MemberwiseClone();
    public virtual BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is ARDB.Element)
      {
        var bbox = BoundingBox;
        if (bbox.Transform(xform))
          return bbox;
      }

      return NaN.BoundingBox;
    }

    bool IGH_GeometricGoo.LoadGeometry() => IsReferencedDataLoaded || LoadReferencedData();
    bool IGH_GeometricGoo.LoadGeometry(Rhino.RhinoDoc doc) => IsReferencedDataLoaded || LoadReferencedData();
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
        var surface = TrimmedSurface;
        if (surface is null)
          return false;

        target = (Q) (object) new GH_Surface(surface);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
      {
        var brep = PolySurface;
        if (brep is null)
          return false;

        target = (Q) (object) new GH_Brep(brep);
        return true;
      }

      return false;
    }

    #region Location
    public virtual Level Level => default;

    /// <summary>
    /// Accurate axis aligned <see cref="Rhino.Geometry.BoundingBox"/> for computation.
    /// </summary>
    public virtual BoundingBox BoundingBox => Value is ARDB.Element element ?
      element.GetBoundingBoxXYZ().ToBoundingBox() :
      NaN.BoundingBox;

    /// <summary>
    /// Box aligned to <see cref="Location"/>
    /// </summary>
    public virtual Box Box
    {
      get
      {
        if (Value is ARDB.Element element)
        {
          var plane = Location;
          if (!Location.IsValid)
            return element.GetBoundingBoxXYZ().ToBox();

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

        return NaN.Box;
      }
    }

    public virtual Interval Domain
    {
      get
      {
        var box = BoundingBox;
        if (!box.IsValid)
          return NaN.Interval;

        return new Interval(box.Min.Z, box.Max.Z);
      }
    }

    public virtual UVInterval DomainUV
    {
      get
      {
        var box = BoundingBox;
        if (!box.IsValid)
          return new UVInterval(NaN.Interval, NaN.Interval);

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
        var origin = NaN.Point3d;
        var axis = NaN.Vector3d;
        var perp = NaN.Vector3d;

        if (Value is ARDB.Element element)
        {
          switch (element.Location)
          {
            case ARDB.LocationPoint pointLocation:
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
            case ARDB.LocationCurve curveLocation:
              if(curveLocation.Curve.TryGetLocation(out var cO, out var cX, out var cY))
                return new Plane(cO.ToPoint3d(), cX.ToVector3d(), cY.ToVector3d());

              break;
            default:
              // Try with the first non empty geometry object.
              using (var options = new ARDB.Options { DetailLevel = ARDB.ViewDetailLevel.Undefined })
              {
                if (element.get_Geometry(options).TryGetLocation(out var gO, out var gX, out var gY))
                  return new Plane(gO.ToPoint3d(), gX.ToVector3d(), gY.ToVector3d());
              }

              var bbox = BoundingBox;
              if (bbox.IsValid)
              {
                // If we have nothing better, the center of the BoundingBox will do the job.
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

    void GetLocation(out ARDB.XYZ origin, out ARDB.XYZ basisX, out ARDB.XYZ basisY)
    {
      var plane = Location.ToPlane();
      origin = plane.Origin;
      basisX = plane.XVec;
      basisY = plane.YVec;
    }

    void SetLocation(ARDB.XYZ newOrigin, ARDB.XYZ newBasisX, ARDB.XYZ newBasisY)
    {
      if (Value is ARDB.Element element)
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

            using (var axis = ARDB.Line.CreateUnbound(origin, axisDirection))
              ARDB.ElementTransformUtils.RotateElement(element.Document, element.Id, axis, angle);

            GetLocation(out origin, out basisX, out basisY);
            basisZ = basisX.CrossProduct(basisY);
          }

          if (!basisX.IsAlmostEqualTo(newBasisX))
          {
            double angle = basisX.AngleOnPlaneTo(newBasisX, newBasisZ);
            using (var axis = ARDB.Line.CreateUnbound(origin, newBasisZ))
              ARDB.ElementTransformUtils.RotateElement(element.Document, element.Id, axis, angle);
          }

          {
            var trans = newOrigin - origin;
            if (!trans.IsZeroLength())
              ARDB.ElementTransformUtils.MoveElement(element.Document, element.Id, trans);
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
        UnitConverter.Convert(1.0, Rhino.UnitSystem.Yards, rhinoDoc.ModelUnitSystem) :
        UnitConverter.Convert(1.0, Rhino.UnitSystem.Meters, rhinoDoc.ModelUnitSystem),

        SnapSpacing = imperial ?
        UnitConverter.Convert(1.0, Rhino.UnitSystem.Yards, rhinoDoc.ModelUnitSystem) :
        UnitConverter.Convert(1.0, Rhino.UnitSystem.Meters, rhinoDoc.ModelUnitSystem),

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
      get => Value?.Location is ARDB.LocationCurve curveLocation ?
          curveLocation.Curve.ToCurve() :
          default;
      set
      {
        if (value is object && Value is ARDB.Element element)
        {
          if (element.Location is ARDB.LocationCurve locationCurve)
          {
            var curve = value.ToCurve();
            if (!locationCurve.Curve.IsAlmostEqualTo(curve))
            {
              InvalidateGraphics();
              locationCurve.Curve = curve;
            }
          }
          else throw new InvalidOperationException("Curve can not be set for this element.");
        }
      }
    }

    public virtual Surface Surface => null;
    public virtual Brep TrimmedSurface => Brep.CreateFromSurface(Surface);
    public virtual Brep PolySurface => TrimmedSurface;
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
        return Value is ARDB.Element element && element.GetType().GetProperty("Flipped") is PropertyInfo Flipped ?
          (bool?) Flipped.GetValue(element) :
          default;
      }
      set
      {
        if (value.HasValue && Value is ARDB.Element element)
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
        if (value.HasValue && Value is ARDB.Element element)
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
        if (value.HasValue && Value is ARDB.Element element)
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

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Display;
  using Convert.Geometry;
  using External.DB;
  using External.DB.Extensions;
  using GH.Kernel.Attributes;

  [Name("Geometry")]
  public interface IGH_GeometryObject : IGH_Reference { }

  [Name("Geometry")]
  public abstract class GeometryObject : Reference,
    IGH_GeometryObject,
    IGH_GeometricGoo,
    IGH_PreviewData,
    IGH_PreviewMeshData
  {
    #region System.Object
#if DEBUG
    public override string ToString()
    {
      try   { return GetReference()?.ConvertToStableRepresentation(ReferenceDocument) ?? base.ToString(); }
      catch { return base.ToString(); }
    }
#endif
    #endregion

    #region IGH_Goo
    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo<Q>(out target)) return true;

      if (typeof(Q).IsAssignableFrom(typeof(ARDB.GeometryObject)))
      {
        target = (Q) (object) Value;
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(ARDB.Reference)))
      {
        target = (Q) (object) GetReference();
        return true;
      }
      else if (typeof(IGH_Element).IsAssignableFrom(typeof(Q)))
      {
        if (GetReference() is ARDB.Reference reference)
          target = (Q) (object) (Element.FromReference(ReferenceDocument, reference) is Q element ? element : default);
        else if (Id == ElementIdExtension.InvalidElementId)
          target = (Q) (object) new Element();
        else
          target = default;

        return true;
      }

      return false;
    }
    #endregion

    #region DocumentObject
    public override string DisplayName => GetType().GetCustomAttribute<NameAttribute>().Name;

    public override object ScriptVariable() => Value;

    public new ARDB.GeometryObject Value
    {
      get
      {
        var geometryObject = base.Value as ARDB.GeometryObject;
        switch (geometryObject?.IsValidObject())
        {
          case false:
            Debug.WriteLine("GeometryObject is not valid.");
            ResetValue();
            return base.Value as ARDB.GeometryObject;

          case true: return geometryObject;
          default: return null;
        }
      }
    }

    protected override object FetchValue()
    {
      LoadReferencedData();

      _GeometryToWorldTransform = default;
      _WorldToGeometryTransform = default;

      if (_ReferenceDocument is object && _Reference is object)
      {
        try
        {
          if (_ReferenceDocument.GetElement(_Reference) is ARDB.Element element)
          {
            var geometryReference = _Reference;
            if (element is ARDB.RevitLinkInstance link && _Reference.LinkedElementId.IsValid())
            {
              _GeometryToWorldTransform = link.GetTransform().ToTransform();
              element = link.GetLinkDocument()?.GetElement(_Reference.LinkedElementId);
              geometryReference = _Reference.CreateReferenceInLink(link);
            }

            if (geometryReference.ElementReferenceType != ARDB.ElementReferenceType.REFERENCE_TYPE_NONE && element is ARDB.Instance instance)
              _GeometryToWorldTransform = _GeometryToWorldTransform.HasValue ? _GeometryToWorldTransform.Value * instance.GetTransform().ToTransform() : instance.GetTransform().ToTransform();

            Document = element?.Document;
            return element?.GetGeometryObjectFromReference(geometryReference);
          }
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { }
      }

      return default;
    }

    protected override void ResetValue()
    {
      (this as IGH_PreviewMeshData).DestroyPreviewMeshes();
      base.ResetValue();
    }

    protected void SetValue(ARDB.Document document, ARDB.Reference reference)
    {
      ResetValue();

      if (reference is null)
        document = null;

      if (document is object)
      {
        ReferenceUniqueId = reference.ConvertToPersistentRepresentation(document);
        ReferenceDocumentId = document.GetFingerprintGUID();

        _ReferenceDocument = document;
        Document = reference.LinkedElementId == ARDB.ElementId.InvalidElementId ? _ReferenceDocument :
          _ReferenceDocument.GetElement<ARDB.RevitLinkInstance>(reference.ElementId)?.GetLinkDocument();

        _Reference = reference;
      }
    }
    #endregion

    #region ReferenceObject
    public override bool? IsEditable => Value?.IsReadOnly;
    #endregion

    #region Reference
    public override ARDB.ElementId Id => ReferenceUniqueId == string.Empty ?
      ElementIdExtension.InvalidElementId :
      _Reference is null ? null :
      _Reference.LinkedElementId != ARDB.ElementId.InvalidElementId ?
      _Reference.LinkedElementId :
      _Reference.ElementId;

    private ARDB.Document _ReferenceDocument;
    public override ARDB.Document ReferenceDocument => _ReferenceDocument?.IsValidObject is true ? _ReferenceDocument : null;

    private ARDB.Reference _Reference;
    public override ARDB.Reference GetReference() => _Reference;

    public override ARDB.ElementId ReferenceId => _Reference?.ElementId;

    public override bool IsReferencedDataLoaded => _ReferenceDocument is object && _Reference is object;

    public override bool LoadReferencedData()
    {
      if (IsReferencedData && !IsReferencedDataLoaded)
      {
        UnloadReferencedData();

        if (Types.Document.TryGetDocument(ReferenceDocumentId, out _ReferenceDocument))
        {
          try
          {
            _Reference = ReferenceExtension.ParseFromPersistentRepresentation(_ReferenceDocument, ReferenceUniqueId);

            if (_Reference.LinkedElementId == ARDB.ElementId.InvalidElementId)
            {
              Document = _ReferenceDocument;
              return true;
            }

            if (_ReferenceDocument.GetElement(_Reference.ElementId) is ARDB.RevitLinkInstance link && link.GetLinkDocument() is ARDB.Document linkDocument)
            {
              Document = linkDocument;
              return true;
            }
          }
          catch { }

          _ReferenceDocument = null;
          _Reference = null;
          Document = null;
        }
      }

      return IsReferencedDataLoaded;
    }

    public override void UnloadReferencedData()
    {
      if (IsReferencedData)
      {
        _ReferenceDocument = default;
        _Reference = default;
      }

      base.UnloadReferencedData();
    }
    #endregion

    #region IGH_GeometricGoo
    BoundingBox IGH_GeometricGoo.Boundingbox => GetBoundingBox(Transform.Identity);
    Guid IGH_GeometricGoo.ReferenceID
    {
      get => Guid.Empty;
      set { if (value != Guid.Empty) throw new InvalidOperationException(); }
    }
    bool IGH_GeometricGoo.IsReferencedGeometry => IsReferencedData;
    bool IGH_GeometricGoo.IsGeometryLoaded => IsReferencedDataLoaded;

    void IGH_GeometricGoo.ClearCaches() => UnloadReferencedData();
    IGH_GeometricGoo IGH_GeometricGoo.DuplicateGeometry() => (IGH_GeometricGoo) MemberwiseClone();
    public abstract BoundingBox GetBoundingBox(Transform xform);
    bool IGH_GeometricGoo.LoadGeometry() => IsReferencedDataLoaded || LoadReferencedData();
    bool IGH_GeometricGoo.LoadGeometry(Rhino.RhinoDoc doc) => IsReferencedDataLoaded || LoadReferencedData();
    IGH_GeometricGoo IGH_GeometricGoo.Transform(Transform xform) => null;
    IGH_GeometricGoo IGH_GeometricGoo.Morph(SpaceMorph xmorph) => null;
    #endregion

    #region IGH_PreviewData
    private BoundingBox? _ClippingBox;

    /// <summary>
    /// Not necessarily accurate axis aligned <see cref="Rhino.Geometry.BoundingBox"/> used for display.
    /// </summary>
    /// <returns>
    /// A finite axis aligned bounding box.
    /// </returns>
    public BoundingBox ClippingBox => _ClippingBox ?? (_ClippingBox = BoundingBox).Value;

    public virtual void DrawViewportWires(GH_PreviewWireArgs args) { }
    public virtual void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion

    #region IGH_PreviewMeshData
    protected Point _Point = null;
    protected Curve[] _Wires = null;
    protected Mesh[] _Meshes = null;
    protected double _LevelOfDetail = double.NaN;

    Transform? _GeometryToWorldTransform = default;
    public Transform GeometryToWorldTransform => _GeometryToWorldTransform ?? Transform.Identity;

    Transform? _WorldToGeometryTransform = default;
    public Transform WorldToGeometryTransform => _WorldToGeometryTransform ??
    (
      !_GeometryToWorldTransform.HasValue ? Transform.Identity :
      (
        _GeometryToWorldTransform.Value.TryGetInverse(out var inverse) ?
        (_WorldToGeometryTransform = inverse).Value :
        throw new InvalidOperationException("Transform is not invertile")
      )
    );

    protected bool HasTransform => _GeometryToWorldTransform.HasValue;

    void IGH_PreviewMeshData.DestroyPreviewMeshes()
    {
      _ClippingBox = null;
      _GeometryToWorldTransform = null;
      _WorldToGeometryTransform = null;
      _LevelOfDetail = double.NaN;

      _Point = null;
      _Wires = null;
      _Meshes = null;
    }

    Mesh[] IGH_PreviewMeshData.GetPreviewMeshes() => _Meshes;
    #endregion

    protected GeometryObject() { }
    protected GeometryObject(ARDB.Document document, ARDB.GeometryObject geometryObject) : base(document, geometryObject) { }
    protected GeometryObject(ARDB.Document document, ARDB.Reference reference)
    {
      if (reference is null) document = null;

      ReferenceDocumentId = document.GetFingerprintGUID();
      ReferenceUniqueId = reference?.ConvertToPersistentRepresentation(document) ?? string.Empty;

      _ReferenceDocument = document;
      _Reference = reference;

      Document = reference is null ? document :
        reference.LinkedElementId == ARDB.ElementId.InvalidElementId ?
        _ReferenceDocument :
        _ReferenceDocument.GetElement<ARDB.RevitLinkInstance>(reference.ElementId)?.GetLinkDocument();
    }

    public static GeometryObject FromReference(ARDB.Document document, ARDB.Reference reference)
    {
      switch (reference?.ElementReferenceType)
      {
        case ARDB.ElementReferenceType.REFERENCE_TYPE_NONE:
          return new GeometryElement(document, reference);

        case ARDB.ElementReferenceType.REFERENCE_TYPE_LINEAR:
        {
          var stable = reference.ConvertToStableRepresentation(document);
          return (stable.EndsWith("/0") || stable.EndsWith("/1")) ?
            (GeometryObject) new GeometryPoint(document, reference) :
            (GeometryObject) new GeometryCurve(document, reference);
        }

        case ARDB.ElementReferenceType.REFERENCE_TYPE_SURFACE:
          return new GeometryFace(document, reference);
      }

      return null;
    }

    public static GeometryObject FromElementId(ARDB.Document document, ARDB.ElementId id)
    {
      if (document.GetElement(id) is ARDB.Element element)
        return new GeometryElement(document, ARDB.Reference.ParseFromStableRepresentation(document, element.UniqueId));

      return null;
    }

    public static GeometryObject FromLinkElementId(ARDB.Document document, ARDB.LinkElementId id)
    {
      if (id.HostElementId != ARDB.ElementId.InvalidElementId)
        return FromElementId(document, id.HostElementId);

      if
      (
        document.GetElement(id.LinkInstanceId) is ARDB.RevitLinkInstance link &&
        link.GetLinkDocument() is ARDB.Document linkedDocument &&
        linkedDocument.GetElement(id.LinkedElementId) is ARDB.Element linkedElement
      )
      {
        using (var linkedElementReference = ARDB.Reference.ParseFromStableRepresentation(linkedElement.Document, linkedElement.UniqueId))
          return new GeometryElement(document, linkedElementReference.CreateLinkReference(link));
      }

      return default;
    }

    /// <summary>
    /// Accurate axis aligned <see cref="Rhino.Geometry.BoundingBox"/> for computation.
    /// </summary>
    public virtual BoundingBox BoundingBox => GetBoundingBox(Transform.Identity);

    public virtual ARDB.Reference GetDefaultReference() => _Reference;
  }

  [Name("Element")]
  public class GeometryElement : GeometryObject, IGH_PreviewData
  {
    public override object ScriptVariable() => base.Value;

    public new ARDB.GeometryElement Value => base.Value as ARDB.GeometryElement;
    public override ARDB.Reference GetDefaultReference()
    {
      return GetAbsoluteReference(Document?.GetElement(Id)?.GetDefaultReference());
    }

    public GeometryElement() { }
    public GeometryElement(ARDB.Document doc, ARDB.Reference reference) : base(doc, reference) { }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value?.GetBoundingBox()?.ToBox() is Box box)
      {
        if (HasTransform) box.Transform(GeometryToWorldTransform);
        return xform == Transform.Identity ?
          box.BoundingBox :
          box.GetBoundingBox(xform);
      }

      return NaN.BoundingBox;
    }

    #region IGH_PreviewData
    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (!IsValid) return;

      var bbox = ClippingBox;
      if (!bbox.IsValid)
        return;

      args.Pipeline.DrawBoxCorners(bbox, args.Color);
    }
    #endregion

    #region Properties
    public override string DisplayName
    {
      get
      {
        if (Id == ElementIdExtension.InvalidElementId) return "<None>";
        switch (Element.FromReference(ReferenceDocument, GetReference()))
        {
          case null: return $"Null {base.DisplayName}";
          case Element element: return element.DisplayName;
        }
      }
    }
    #endregion

    #region Casting
    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo(out target)) return true;

      if (typeof(Q).IsAssignableFrom(typeof(ARDB.GeometryElement)))
      {
        target = (Q) (object) (IsValid ? Value : null);
        return true;
      }

      return false;
    }

    public override bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      switch (source)
      {
        case ARDB.Element element:
          if (element.GetDefaultReference() is ARDB.Reference reference && reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_NONE)
          {
            SetValue(element.Document, reference);
            return true;
          }
          break;
      }

      return base.CastFrom(source);
    }
    #endregion
  }

  [Name("Point")]
  public class GeometryPoint : GeometryObject, IGH_PreviewData
  {
    public override object ScriptVariable() => Value;

    public new ARDB.Point Value
    {
      get
      {
        if (GetReference() is ARDB.Reference reference && reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_LINEAR)
        {
          var uniqueId = reference.ConvertToStableRepresentation(ReferenceDocument);
          int end = -1;
          if (uniqueId.EndsWith("/0")) end = 0;
          else if (uniqueId.EndsWith("/1")) end = 1;

          if (end == 0 || end == 1)
          {
            var curve = default(ARDB.Curve);
            switch (base.Value)
            {
              case ARDB.Edge e: curve = e.AsCurve(); break;
              case ARDB.Curve c: curve = c; break;
            }

            if (curve is object && curve.IsBound)
              return ARDB.Point.Create(curve.GetEndPoint(end));
          }
        }

        return base.Value as ARDB.Point;
      }
    }

    public GeometryPoint() { }
    public GeometryPoint(ARDB.Document document, ARDB.XYZ xyz) : base(document, ARDB.Point.Create(xyz)) { }
    public GeometryPoint(ARDB.Document doc, ARDB.Reference reference) : base(doc, reference) { }

    public sealed override string ToString()
    {
      return IsReferencedData ? base.ToString() :
        Point is Point point ? GH_Format.FormatPoint(point.Location) :
        "Invalid Point";
    }

    public static new GeometryPoint FromReference(ARDB.Document document, ARDB.Reference reference)
    {
      var stable = reference.ConvertToStableRepresentation(document);
      if (reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_LINEAR && stable.EndsWith("/0") || stable.EndsWith("/1"))
      {
        return new GeometryPoint(document, reference);
      }
      else if (document.GetGeometryObjectFromReference(reference, out var transform) is ARDB.GeometryObject geometry)
      {
        using (geometry)
        {
          if (reference.GlobalPoint is object)
          {
            switch (geometry)
            {
              case ARDB.Edge edge:
              {
                using (var worldCurve = edge.AsCurve().CreateTransformed(transform))
                {
                  var result = worldCurve.Project(reference.GlobalPoint);
                  var points = new ARDB.XYZ[] { worldCurve.GetEndPoint(0), worldCurve.GetEndPoint(1) };
                  int end = result.XYZPoint.DistanceTo(points[0]) < result.XYZPoint.DistanceTo(points[1]) ? 0 : 1;

                  stable = reference.ConvertToStableRepresentation(document);
                  reference = ARDB.Reference.ParseFromStableRepresentation(document, $"{stable}/{end}");
                  return new GeometryPoint(document, reference);
                }
              }

              case ARDB.Curve curve:
              {
                using (var worldCurve = curve.CreateTransformed(transform))
                {
                  var result = worldCurve.Project(reference.GlobalPoint);
                  var points = new ARDB.XYZ[] { worldCurve.GetEndPoint(0), worldCurve.GetEndPoint(1) };
                  int end = result.XYZPoint.DistanceTo(points[0]) < result.XYZPoint.DistanceTo(points[1]) ? 0 : 1;

                  reference = ARDB.Reference.ParseFromStableRepresentation(document, $"{stable}/{end}");
                  return new GeometryPoint(document, reference);
                }
              }
            }

            return new GeometryPoint(document, reference.GlobalPoint);
          }
          else if (geometry.TryGetLocation(out var location, out var _, out var _))
          {
            return new GeometryPoint(document, location);
          }
        }
      }

      return null;
    }

    public Point Point
    {
      get
      {
        if (_Point is null && Value is ARDB.Point point)
        {
          _Point = new Point(point.Coord.ToPoint3d());

          if (HasTransform)
            _Point.Transform(GeometryToWorldTransform);
        }

        return _Point;
      }
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      return Point is Point point ?
      (
        xform == Transform.Identity ?
        point.GetBoundingBox(true) :
        point.GetBoundingBox(xform)
      ) : NaN.BoundingBox;
    }

    #region IGH_PreviewData
    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Point is Point point)
        args.Pipeline.DrawPoint(point.Location, CentralSettings.PreviewPointStyle, CentralSettings.PreviewPointRadius, args.Color);
    }
    #endregion

    #region Casting
    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo(out target)) return true;

      if (typeof(Q).IsAssignableFrom(typeof(ARDB.Point)))
      {
        target = (Q) (object) (base.IsValid ? base.Value : null);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Point)))
      {
        if (Point is Point point)
        {
          target = (Q) (object) new GH_Point(point.Location);
          return true;
        }

        target = default;
        return false;
      }

      return false;
    }
    #endregion
  }

  [Name("Curve")]
  public class GeometryCurve : GeometryObject, IGH_PreviewData, IGH_Goo
  {
    public override object ScriptVariable() => base.Value;

    public new ARDB.Curve Value
    {
      get
      {
        switch (base.Value)
        {
          case ARDB.Curve c: return c;
          case ARDB.Edge e: return e.AsCurve();
        }
        return default;
      }
    }

    public GeometryCurve() { }
    public GeometryCurve(ARDB.Document doc, ARDB.Reference reference) : base(doc, reference) { }

    public static new GeometryCurve FromReference(ARDB.Document document, ARDB.Reference reference)
    {
      return reference?.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_LINEAR ?
        new GeometryCurve(document, reference) : null;
    }

    public Curve Curve
    {
      get
      {
        if (Value is ARDB.Curve curve && _Wires is null)
        {
          _Wires = new Curve[] { curve.ToCurve() };

          if (HasTransform)
            _Wires[0].Transform(GeometryToWorldTransform);
        }

        return _Wires?.FirstOrDefault();
      }
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      return Curve is Curve curve ?
      (
        xform == Transform.Identity ?
        curve.GetBoundingBox(true) :
        curve.GetBoundingBox(xform)
      ) : NaN.BoundingBox;
    }

    #region IGH_PreviewData
    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Curve is Curve curve)
        args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
    }
    #endregion

    #region Properties
    public override string DisplayName
    {
      get
      {
        var value = base.Value;
        string visibility;
        switch (value?.Visibility)
        {
          case null:                    visibility = string.Empty; break;
          case ARDB.Visibility.Visible: visibility = string.Empty; break;
          default:                      visibility = $"{value.Visibility} "; break;
        }

        var typeName = base.DisplayName;
        if (value is ARDB.Edge edge)
        {
          typeName = "Edge";
          value = edge.AsCurve();
        }

        switch (value)
        {
          case null:                    return $"Null {typeName}";
          case ARDB.Arc _:              return $"{visibility}Arc {typeName}";
          case ARDB.CylindricalHelix _: return $"{visibility}Helix {typeName}";
          case ARDB.Ellipse _:          return $"{visibility}Ellipse {typeName}";
          case ARDB.HermiteSpline _:    return $"{visibility}Hermite {typeName}";
          case ARDB.Line _:             return $"{visibility}Line {typeName}";
          case ARDB.NurbSpline _:       return $"{visibility}NURBS {typeName}";
          case ARDB.Curve _:            return $"{visibility}Unknown {typeName}";
          default:                      return "Curve";
        }
      }
    }
    #endregion

    #region Casting
    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo(out target)) return true;

      if (typeof(Q).IsAssignableFrom(typeof(ARDB.Curve)))
      {
        target = (Q) (object) (base.Value as ARDB.Curve);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(ARDB.Edge)))
      {
        target = (Q) (object) (base.Value as ARDB.Edge);
        return true;
      }
      if (Curve is Curve curve)
      {
        var tol = GeometryTolerance.Model;
        if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
        {
          target = curve.TryGetPlane(out var plane, tol.VertexTolerance) ? (Q) (object) new GH_Plane(plane) : default;
          return target is object;
        }
        else if (typeof(Q).IsAssignableFrom(typeof(GH_Line)))
        {
          target = curve.TryGetLine(out var line, tol.VertexTolerance) ? (Q) (object) new GH_Line(line) : default;
          return target is object;
        }
        else if (typeof(Q).IsAssignableFrom(typeof(GH_Arc)))
        {
          target = curve.TryGetArc(out var arc, tol.VertexTolerance) ? (Q) (object) new GH_Arc(arc) : default;
          return target is object;
        }
        else if (typeof(Q).IsAssignableFrom(typeof(GH_Circle)))
        {
          target = curve.TryGetCircle(out var circle, tol.VertexTolerance) ? (Q) (object) new GH_Circle(new Circle(circle.Plane, circle.Radius)) : default;
          return target is object;
        }
        else if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
        {
          target = (Q) (object) new GH_Curve(curve);
          return true;
        }
      }

      return false;
    }

    public override bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      switch (source)
      {
        case ARDB.Element element:
          if (element.GetDefaultReference() is ARDB.Reference reference && reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_LINEAR)
          {
            SetValue(element.Document, reference);
            return true;
          }
          break;
      }

      return base.CastFrom(source);
    }
    #endregion
  }

  [Name("Face")]
  public class GeometryFace : GeometryObject, IGH_PreviewData
  {
    public override object ScriptVariable() => base.Value;

    public new ARDB.Face Value => base.Value as ARDB.Face;

    public GeometryFace() { }
    public GeometryFace(ARDB.Document doc, ARDB.Reference reference) : base(doc, reference) { }

    public static new GeometryFace FromReference(ARDB.Document document, ARDB.Reference reference)
    {
      return reference?.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_SURFACE ?
        new GeometryFace(document, reference) : null;
    }

    public Brep PolySurface
    {
      get
      {
        if (Value?.ToBrep() is Brep brep)
        {
          if (HasTransform) brep.Transform(GeometryToWorldTransform);
          return brep;
        }

        return null;
      }
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      return PolySurface is Brep brep ?
      (
        xform == Transform.Identity ?
        brep.GetBoundingBox(true) :
        brep.GetBoundingBox(xform)
      ) : NaN.BoundingBox;
    }

    #region IGH_PreviewData
    protected Curve[] Edges
    {
      get
      {
        if (ClippingBox.IsValid && Value is ARDB.Face face && _Wires is null)
        {
          _Wires = face.GetEdgesAsCurveLoops().SelectMany(x => x.GetPreviewWires()).ToArray();

          if (HasTransform)
          {
            foreach (var wire in _Wires)
              wire.Transform(GeometryToWorldTransform);
          }
        }

        return _Wires;
      }
    }

    protected Mesh[] GetPreviewMeshes(MeshingParameters meshingParameters)
    {
      if (meshingParameters.LevelOfDetail() != _LevelOfDetail)
      {
        _LevelOfDetail = meshingParameters.LevelOfDetail();
        if (ClippingBox.IsValid && Value is ARDB.Face face)
        {
          _Meshes = Enumerable.Repeat(face, 1).GetPreviewMeshes(Document, meshingParameters).ToArray();

          var transform = GeometryToWorldTransform;
          foreach (var mesh in _Meshes)
          {
            if (HasTransform) mesh.Transform(transform);
            mesh.Normals.ComputeNormals();
          }
        }
      }

      return _Meshes;
    }

    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Edges is Curve[] curves)
      {
        foreach (var curve in curves)
          args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
      }
    }

    void IGH_PreviewData.DrawViewportMeshes(GH_PreviewMeshArgs args)
    {
      if (GetPreviewMeshes(args.MeshingParameters) is Mesh[] meshes)
      {
        foreach (var mesh in meshes)
          args.Pipeline.DrawMeshShaded(mesh, args.Material);
      }
    }
    #endregion

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo(out target)) return true;

      if (typeof(Q).IsAssignableFrom(typeof(ARDB.Reference)))
      {
        target = (Q) (object) (IsValid ? GetReference() : null);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(ARDB.Face)))
      {
        target = (Q) (object) (IsValid ? Value : null);
        return true;
      }
      else if (Value is ARDB.Face face)
      {
        if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
        {
          if (face is ARDB.PlanarFace planarFace)
          {
            var plane = new Plane(planarFace.Origin.ToPoint3d(), planarFace.XVector.ToVector3d(), planarFace.YVector.ToVector3d());
            if (HasTransform) plane.Transform(GeometryToWorldTransform);
            target = (Q) (object) new GH_Plane(plane);
          }
          else target = default;
          return true;
        }
        else if (typeof(Q).IsAssignableFrom(typeof(GH_Surface)))
        {
          target = PolySurface is Brep brep && brep.Surfaces.Count == 1 ? (Q) (object) new GH_Surface(brep.Surfaces.FirstOrDefault()) : default;
          return target is object;
        }
        else if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
        {
          target = PolySurface is Brep brep && brep.Surfaces.Count > 0 ? (Q) (object) new GH_Brep(brep) : default;
          return target is object;
        }
        else if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
        {
          if (_Meshes is object)
          {
            var m = new Mesh(); m.Append(_Meshes);
            target = (Q) (object) new GH_Mesh(m);
          }
          else target = default;
          return true;
        }
      }
      else if (ReferenceDocument is ARDB.Document referenceDocument && GetReference() is ARDB.Reference planeReference)
      {
        if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
        {
          using (referenceDocument.RollBackScope())
          using (referenceDocument.RollBackScope())
          {
            var sketchPlane = ARDB.SketchPlane.Create(referenceDocument, planeReference);
            var plane = sketchPlane.GetPlane().ToPlane();
            // The `ARDB.SketchPlane` is already in WCS
            // if (HasTransform) plane.Transform(GeometryToWorldTransform);
            target = (Q) (object) new GH_Plane(plane);
            return true;
          }
        }
      }

      return false;
    }

    public override bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      switch (source)
      {
        case ARDB.Element element:
          if (element.GetDefaultReference() is ARDB.Reference reference && reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_SURFACE)
          {
            SetValue(element.Document, reference);
            return true;
          }
          break;
      }

      return base.CastFrom(source);
    }

    #region Properties
    public override string DisplayName
    {
      get
      {
        var value = base.Value;
        string visibility;
        switch (value?.Visibility)
        {
          case null:                    visibility = string.Empty; break;
          case ARDB.Visibility.Visible: visibility = string.Empty; break;
          default:                      visibility = $"{value.Visibility} "; break;
        }

        switch (value)
        {
          case null:                    return "Null Face";
          case ARDB.ConicalFace _:      return $"{visibility}Conical Face";
          case ARDB.CylindricalFace _:  return $"{visibility}Cylindrical Face";
          case ARDB.HermiteFace _:      return $"{visibility}Hermite Face";
          case ARDB.PlanarFace _:       return $"{visibility}Planar Face";
          case ARDB.RevolvedFace _:     return $"{visibility}Revolved Face";
          case ARDB.RuledFace _:        return $"{visibility}Ruled Face";
          case ARDB.Face face:

#if REVIT_2021
          using(var surface = face.GetSurface())
          if (surface is ARDB.OffsetSurface) return $"{visibility}Offset Face";
#endif
                                        return $"{visibility}Unknown Face";
          default:                      return "Face";
        }
      }
    }
    #endregion
  }
}

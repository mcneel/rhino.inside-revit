using System;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Display;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public abstract class GeometryObject<X> :
    ElementId,
    IEquatable<GeometryObject<X>>,
    IGH_GeometricGoo,
    IGH_PreviewData,
    IGH_PreviewMeshData
    where X : DB.GeometryObject
  {
    #region System.Object
    public bool Equals(GeometryObject<X> other) => other is object &&
      other.DocumentGUID == DocumentGUID && other.UniqueID == UniqueID;
    public override bool Equals(object obj) => (obj is GeometryObject<X> id) ? Equals(id) : base.Equals(obj);
    public override int GetHashCode() => DocumentGUID.GetHashCode() ^ UniqueID.GetHashCode();

    public sealed override string ToString()
    {
      string typeName = ((IGH_Goo) this).TypeName;
      if (!IsValid)
        return "Null " + typeName;

      try
      {
        if (Document?.GetElement(Reference) is DB.Element element)
        {
          typeName = "Referenced ";
          switch (Reference.ElementReferenceType)
          {
            case DB.ElementReferenceType.REFERENCE_TYPE_NONE: typeName += "geometry"; break;
            case DB.ElementReferenceType.REFERENCE_TYPE_LINEAR: typeName += "edge"; break;
            case DB.ElementReferenceType.REFERENCE_TYPE_SURFACE: typeName += "face"; break;
            case DB.ElementReferenceType.REFERENCE_TYPE_FOREIGN: typeName += "external geometry"; break;
            case DB.ElementReferenceType.REFERENCE_TYPE_INSTANCE: typeName += "instance"; break;
            case DB.ElementReferenceType.REFERENCE_TYPE_CUT_EDGE: typeName += "trim"; break;
            case DB.ElementReferenceType.REFERENCE_TYPE_MESH: typeName += "mesh"; break;
#if REVIT_2018
            case DB.ElementReferenceType.REFERENCE_TYPE_SUBELEMENT: typeName += "subelement"; break;
#endif
          }

          typeName += " at Revit " + element.GetType().Name + " \"" + element.Name + "\"";
        }

#if DEBUG
        typeName += " (" + Reference.ConvertToStableRepresentation(Document) + ")";
#endif
        return typeName;
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException)
      {
        return "Invalid" + typeName;
      }
    }
    #endregion

    #region IGH_Goo
    public override bool IsValid => Value is X;
    #endregion

    #region ReferenceObject
    public override DB.ElementId Id => reference?.ElementId;
    #endregion

    #region IGH_ElementId
    DB.Reference reference;
    public override DB.Reference Reference => reference;

    public override bool IsReferencedDataLoaded => Document is object && Reference is object;
    public override bool LoadReferencedData()
    {
      if (IsReferencedData && !IsReferencedDataLoaded)
      {
        UnloadReferencedData();

        if (Types.Document.TryGetDocument(DocumentGUID, out var document))
        {
          try
          {
            reference = DB.Reference.ParseFromStableRepresentation(document, UniqueID);
            Document = document;
          }
          catch { }
        }
      }

      return IsReferencedDataLoaded;
    }

    public override void UnloadReferencedData()
    {
      if (IsReferencedData)
        reference = default;

      base.UnloadReferencedData();
    }

    protected override object FetchValue()
    {
      if (Document is DB.Document doc && Reference is DB.Reference reference)
        try { return doc.GetElement(reference)?.GetGeometryObjectFromReference(reference); }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { }

      return default;
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
    bool IGH_GeometricGoo.LoadGeometry(                  ) => IsReferencedDataLoaded || LoadReferencedData();
    bool IGH_GeometricGoo.LoadGeometry(Rhino.RhinoDoc doc) => IsReferencedDataLoaded || LoadReferencedData();
    IGH_GeometricGoo IGH_GeometricGoo.Transform(Transform xform) => null;
    IGH_GeometricGoo IGH_GeometricGoo.Morph(SpaceMorph xmorph) => null;
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

    public virtual void DrawViewportWires(GH_PreviewWireArgs args) { }
    public virtual void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion

    #region IGH_PreviewMeshData
    protected Point   point = null;
    protected Curve[] wires = null;
    protected Mesh[]  meshes = null;

    void IGH_PreviewMeshData.DestroyPreviewMeshes()
    {
      point = null;
      wires = null;
      meshes = null;
    }

    Mesh[] IGH_PreviewMeshData.GetPreviewMeshes() => meshes;
    #endregion

    protected GeometryObject() { }
    protected GeometryObject(DB.Document doc, DB.Reference reference)
    {
      try
      {
        Document = doc;
        DocumentGUID = doc.GetFingerprintGUID();

        this.reference = reference;
        UniqueID = reference.ConvertToStableRepresentation(doc);
      }
      catch (Autodesk.Revit.Exceptions.InvalidObjectException) { }
    }

    public new X Value => base.Value as X;

    /// <summary>
    /// Accurate axis aligned <see cref="Rhino.Geometry.BoundingBox"/> for computation.
    /// </summary>
    public virtual BoundingBox BoundingBox => GetBoundingBox(Transform.Identity);

    /// <summary>
    /// Not necessarily accurate axis aligned <see cref="Rhino.Geometry.BoundingBox"/> for display.
    /// </summary>
    public virtual BoundingBox ClippingBox => BoundingBox;
  }

  [Name("Revit Vertex")]
  public class Vertex : GeometryObject<DB.Point>, IGH_PreviewData
  {
    readonly int VertexIndex = -1;
    protected override object FetchValue()
    {
      if (Document is DB.Document doc && Reference is DB.Reference reference)
        try
        {
          if (doc.GetElement(reference)?.GetGeometryObjectFromReference(reference) is DB.Edge edge)
          {
            var curve = edge.AsCurve();
            var points = new DB.XYZ[] { curve.GetEndPoint(0), curve.GetEndPoint(1) };
            return DB.Point.Create(points[VertexIndex]);
          }
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { }

      return default;
    }

    public Vertex() { }
    public Vertex(DB.Document doc, DB.Reference reference, int index) : base(doc, reference) { VertexIndex = index; }

    Point Point
    {
      get
      {
        if (point is null && IsValid)
        {
          point = new Point(Value.Coord.ToPoint3d());

          if(/*Value.IsElementGeometry && */Document?.GetElement(Reference) is DB.Instance instance)
          {
            var xform = instance.GetTransform().ToTransform();
            point.Transform(xform);
          }
        }

        return point;
      }
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(DB.Reference)))
      {
        target = (Q) (object) (IsValid ? Reference : null);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(DB.Point)))
      {
        target = (Q) (object) (IsValid ? Value : null);
        return true;
      }
      else if (Value is object)
      {
        if (typeof(Q).IsAssignableFrom(typeof(GH_Point)))
        {
          target = (Q) (object) new GH_Point(Point.Location);
          return true;
        }
        else if (Reference is object && typeof(Q).IsAssignableFrom(typeof(Element)))
        {
          target = (Q) (object) Element.FromElementId(Document, Id);
          return true;
        }
      }

      return base.CastTo(out target);
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is null)
        return BoundingBox.Empty;

      return
      (
        xform == Transform.Identity ?
        Point?.GetBoundingBox(true) :
        Point?.GetBoundingBox(xform)
      ) ?? BoundingBox.Empty;
    }

    #region IGH_PreviewData
    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (!IsValid)
        return;

      if (Point is Point point)
        args.Pipeline.DrawPoint(point.Location, CentralSettings.PreviewPointStyle, CentralSettings.PreviewPointRadius, args.Color);
    }
    #endregion

    #region Properties
    public override string DisplayName => "Vertex";
    #endregion
  }

  [Name("Edge")]
  public class Edge : GeometryObject<DB.Edge>, IGH_PreviewData
  {
    public Edge() { }
    public Edge(DB.Document doc, DB.Reference reference) : base(doc, reference) { }

    Curve Curve
    {
      get
      {
        if (wires is null && IsValid)
        {
          wires = Enumerable.Repeat(Value, 1).GetPreviewWires().ToArray();

          if (Value.IsElementGeometry && Document?.GetElement(Reference) is DB.Instance instance)
          {
            var xform = instance.GetTransform().ToTransform();
            foreach (var wire in wires)
              wire.Transform(xform);
          }
        }

        return wires.FirstOrDefault();
      }
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(DB.Reference)))
      {
        target = (Q) (object) (IsValid ? Reference : null);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(DB.Edge)))
      {
        target = (Q) (object) (IsValid ? Value : null);
        return true;
      }
      else if (Value is object)
      {
        if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
        {
          target = (Q) (object) new GH_Curve(Curve);
          return true;
        }
        else if (Reference is object && typeof(Q).IsAssignableFrom(typeof(Element)))
        {
          target = (Q) (object) Element.FromElementId(Document, Id);
          return true;
        }
      }

      return base.CastTo(out target);
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is null)
        return BoundingBox.Empty;

      return
      (
        xform == Transform.Identity ?
        Curve?.GetBoundingBox(true) :
        Curve?.GetBoundingBox(xform)
      ) ?? BoundingBox.Empty;
    }

    #region IGH_PreviewData
    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (!IsValid)
        return;

      if(Curve is Curve curve)
        args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
    }
    #endregion

    #region Properties
    public override string DisplayName
    {
      get
      {
        var edgeType = "Invalid Edge";
        using (var curve = Value?.AsCurve())
        {
          switch (curve)
          {
            case DB.Arc _:              edgeType = "Arc Edge"; break;
            case DB.CylindricalHelix _: edgeType = "Helix Edge"; break;
            case DB.Ellipse _:          edgeType = "Ellipse Edge"; break;
            case DB.HermiteSpline _:    edgeType = "Hermite Edge"; break;
            case DB.Line _:             edgeType = "Line Edge"; break;
            case DB.NurbSpline _:       edgeType = "NURB Edge"; break;
            default:                    edgeType = "Unknown Edge"; break;
          }
        }

        return edgeType;
      }
    }
    #endregion
  }

  [Name("Face")]
  public class Face : GeometryObject<DB.Face>, IGH_PreviewData
  {
    public Face() { }
    public Face(DB.Document doc, DB.Reference reference) : base(doc, reference) { }

    Curve[] Curves
    {
      get
      {
        if (wires is null && IsValid)
        {
          wires = Value.GetEdgesAsCurveLoops().SelectMany(x => x.GetPreviewWires()).ToArray();

          if (Value.IsElementGeometry && Document?.GetElement(Reference) is DB.Instance instance)
          {
            var xform = instance.GetTransform().ToTransform();
            foreach (var wire in wires)
              wire.Transform(xform);
          }
        }

        return wires;
      }
    }

    Mesh[] Meshes(MeshingParameters meshingParameters)
    {
      if (meshes is null && IsValid)
      {
        meshes = Enumerable.Repeat(Value, 1).GetPreviewMeshes(Document, meshingParameters).ToArray();

        if (Value.IsElementGeometry && Document?.GetElement(Reference) is DB.Instance instance)
        {
          var xform = instance.GetTransform().ToTransform();
          foreach (var mesh in meshes)
            mesh.Transform(xform);
        }

        foreach (var mesh in meshes)
          mesh.Normals.ComputeNormals();
      }

      return meshes;
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(DB.Reference)))
      {
        target = (Q) (object) (IsValid ? Reference : null);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(DB.Face)))
      {
        target = (Q) (object) (IsValid ? Value : null);
        return true;
      }
      else if (Value is object)
      {
        var element = Reference is object ? Document?.GetElement(Reference) : null;

        if (typeof(Q).IsAssignableFrom(typeof(GH_Surface)))
        {
          if (Value.ToBrep() is Brep brep)
          {
            if (element is DB.Instance instance)
              brep.Transform(instance.GetTransform().ToTransform());

            target = (Q) (object) new GH_Surface(brep);
          }
          else target = default;
          return true;
        }
        else if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
        {
          if (Value.ToBrep() is Brep brep)
          {
            if (element is DB.Instance instance)
              brep.Transform(instance.GetTransform().ToTransform());

            target = (Q) (object) new GH_Brep(brep);
          }
          else target = default;
          return true;
        }
        if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
        {
          if (Value.Triangulate()?.ToMesh() is Mesh mesh)
          {
            if (element is DB.Instance instance)
              mesh.Transform(instance.GetTransform().ToTransform());

            mesh.Normals.ComputeNormals();

            target = (Q) (object) new GH_Mesh(mesh);
          }
          else target = default;
          return true;
        }
        else if (element is object && typeof(Q).IsAssignableFrom(typeof(Element)))
        {
          target = (Q) (object) Element.FromElement(element);
          return true;
        }
      }

      return base.CastTo(out target);
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is null)
        return BoundingBox.Empty;

      var bbox = BoundingBox.Empty;
      if (Curves is Curve[] curves)
      {
        if (xform == Transform.Identity)
        {
          foreach (var curve in curves)
            bbox.Union(curve.GetBoundingBox(true));
        }
        else
        {
          foreach (var curve in curves)
            bbox.Union(curve.GetBoundingBox(xform));
        }
      }

      return bbox;
    }

    #region IGH_PreviewData
    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (!IsValid)
        return;

      foreach (var curve in Curves)
        args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
    }

    void IGH_PreviewData.DrawViewportMeshes(GH_PreviewMeshArgs args)
    {
      if (!IsValid)
        return;

      foreach (var mesh in Meshes(args.MeshingParameters))
        args.Pipeline.DrawMeshShaded(mesh, args.Material);
    }
    #endregion

    #region Properties
    public override string DisplayName
    {
      get
      {
        var faceType = "Invalid Face";
        using (var surface = Value?.GetSurface())
        {
          switch (surface)
          {
            case DB.ConicalSurface _:     faceType = "Conical Face"; break;
            case DB.CylindricalSurface _: faceType = "Cylindrical Face"; break;
            case DB.HermiteSurface _:     faceType = "Hermite Face"; break;
            case DB.Plane _:              faceType = "Planar Face"; break;
#if REVIT_2021
            case DB.OffsetSurface _:      faceType = "Offset Face"; break;
#endif
            case DB.RevolvedSurface _:    faceType = "Revolved Face"; break;
            case DB.RuledSurface _:       faceType = "Ruled Face"; break;
            default:                      faceType = "Unknown Face"; break;
          }
        }

        return faceType;
      }
    }
    #endregion
  }
}

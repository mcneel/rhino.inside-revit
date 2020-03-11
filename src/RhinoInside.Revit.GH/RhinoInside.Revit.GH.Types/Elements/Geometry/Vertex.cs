using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types.Elements.Geometry
{
  public class Vertex : GeometryObject<DB.Point>, IGH_PreviewData
  {
    public override string TypeName => "Revit Vertex";
    public override string TypeDescription => "Represents a Revit Vertex";

    readonly int VertexIndex = -1;
    public override bool LoadElement()
    {
      Document = default;
      Value = default;

      if (Revit.ActiveUIApplication.TryGetDocument(DocumentGUID, out var doc))
      {
        Document = doc;

        try
        {
          Reference = DB.Reference.ParseFromStableRepresentation(doc, UniqueID);
          var element = doc.GetElement(Reference);
          var geometry = element?.GetGeometryObjectFromReference(Reference);
          if (geometry is DB.Edge edge)
          {
            var curve = edge.AsCurve();
            var points = new DB.XYZ[] { curve.GetEndPoint(0), curve.GetEndPoint(1) };
            Value = DB.Point.Create(points[VertexIndex]);
          }
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { }
      }

      return IsValid;
    }

    public Vertex() { }
    public Vertex(DB.Point data) : base(data) { }
    public Vertex(DB.Document doc, DB.Reference reference, int index) : base(doc, reference) { VertexIndex = index; }

    Point Point
    {
      get
      {
        if (point is null && IsValid)
        {
          point = new Point(Value.Coord.ToRhino().ChangeUnits(Revit.ModelUnits));

          if (/*Value.IsElementGeometry && */Document?.GetElement(Reference) is DB.Instance instance)
          {
            var xform = instance.GetTransform().ToRhino().ChangeUnits(Revit.ModelUnits);
            point.Transform(xform);
          }
        }

        return point;
      }
    }

    public override bool CastFrom(object source)
    {
      if (source is GH_Point point)
      {
        Value = DB.Point.Create(point.Value.ChangeUnits(1.0 / Revit.ModelUnits).ToHost());
        UniqueID = string.Empty;
        return true;
      }

      return false;
    }

    public override bool CastTo<Q>(ref Q target)
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

      return base.CastTo(ref target);
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is null)
        return BoundingBox.Empty;

      return xform == Transform.Identity ?
        Point.GetBoundingBox(true) :
        Point.GetBoundingBox(xform);
    }

    #region IGH_PreviewData
    BoundingBox IGH_PreviewData.ClippingBox => GetBoundingBox(Transform.Identity);

    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (!IsValid)
        return;

      if (Point is Point point)
        args.Pipeline.DrawPoint(point.Location, CentralSettings.PreviewPointStyle, CentralSettings.PreviewPointRadius, args.Color);
    }

    void IGH_PreviewData.DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion
  }
}

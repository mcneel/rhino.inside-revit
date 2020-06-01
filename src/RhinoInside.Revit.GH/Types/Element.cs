using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Convert.Display;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Element : ElementId
  {
    public override string TypeName
    {
      get
      {
        var element = (DB.Element) this;
        if (element is object)
          return $"Revit {element.GetType().Name}";

        return $"Revit {GetType().Name}";
      }
    }
    public override string TypeDescription => "Represents a Revit element";
    override public object ScriptVariable() => (DB.Element) this;
    protected override Type ScriptVariableType => typeof(DB.Element);
    public static explicit operator DB.Element(Element self) => self.IsValid ? self.Document.GetElement(self) : null;

    public static Element FromValue(object data)
    {
      switch (data)
      {
        case DB.Category category: return Category.FromCategory(category);
        case DB.Element  element:  return Element.FromElement(element);
      }

      return null;
    }

    public static readonly Dictionary<Type, Func<DB.Element, Element>> ActivatorDictionary = new Dictionary<Type, Func<DB.Element, Element>>()
    {
      { typeof(DB.View),              (element)=> new View          (element as DB.View)              },
      { typeof(DB.Family),            (element)=> new Family        (element as DB.Family)            },
      { typeof(DB.ElementType),       (element)=> new ElementType   (element as DB.ElementType)       },
      { typeof(DB.HostObjAttributes), (element)=> new HostObjectType(element as DB.HostObjAttributes) },
      { typeof(DB.ParameterElement),  (element)=> new ParameterKey  (element as DB.ParameterElement)  },
      { typeof(DB.Material),          (element)=> new Material      (element as DB.Material)          },
      { typeof(DB.GraphicsStyle),     (element)=> new GraphicsStyle (element as DB.GraphicsStyle)     },

      { typeof(DB.Sketch),            (element)=> new Sketch        (element as DB.Sketch)            },
      { typeof(DB.SketchPlane),       (element)=> new SketchPlane   (element as DB.SketchPlane)       },
      { typeof(DB.DatumPlane),        (element)=> new DatumPlane    (element as DB.DatumPlane)        },
      { typeof(DB.Level),             (element)=> new Level         (element as DB.Level)             },
      { typeof(DB.Grid),              (element)=> new Grid          (element as DB.Grid)              },
      { typeof(DB.Group),             (element)=> new Group         (element as DB.Group)             },
      { typeof(DB.HostObject),        (element)=> new HostObject    (element as DB.HostObject)        },
      { typeof(DB.Panel),             (element)=> new Panel         (element as DB.Panel)             },
      { typeof(DB.Mullion),           (element)=> new Mullion       (element as DB.Mullion)           },
    };

    public static Element FromElement(DB.Element element)
    {
      if (element is null)
        return null;

      if (element.GetType() == typeof(DB.Element))
      {
        try
        {
          if (DB.Category.GetCategory(element.Document, element.Id) is DB.Category category)
            return new Category(category);
        }
        catch (Autodesk.Revit.Exceptions.InternalException) { }
      }
      else
      {
        for (var type = element.GetType(); type != typeof(DB.Element); type = type.BaseType)
        {
          if (ActivatorDictionary.TryGetValue(type, out var activator))
            return activator(element);
        }
      }

      if (GeometricElement.IsValidElement(element))
        return new GeometricElement(element);

      return new Element(element);
    }

    new public static Element FromElementId(DB.Document doc, DB.ElementId Id)
    {
      if (doc.GetElement(Id) is DB.Element value)
        return FromElement(value);

      return null;
    }

    public static Element FromReference(DB.Document doc, DB.Reference reference)
    {
      if (doc.GetElement(reference) is DB.Element value)
      {
        if (value is DB.RevitLinkInstance link)
        {
          if (reference.LinkedElementId != DB.ElementId.InvalidElementId)
          {
            var linkedDoc = link.GetLinkDocument();
            return FromValue(linkedDoc?.GetElement(reference.LinkedElementId));
          }
        }

        return FromElement(value);
      }

      return null;
    }

    protected virtual bool SetValue(DB.Element element)
    {
      if (ScriptVariableType.IsInstanceOfType(element))
      {
        Document     = element.Document;
        DocumentGUID = Document.GetFingerprintGUID();
        Value        = element.Id;
        UniqueID     = element.UniqueId;
        return true;
      }

      return false;
    }

    public Element() : base() { }
    internal Element(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    protected Element(DB.Element element)               : base(element.Document, element.Id) { }

    public override bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      var element = default(DB.Element);
      switch (source)
      {
        case DB.Element e:    element = e; break;
        case DB.ElementId id: element = Revit.ActiveDBDocument.GetElement(id); break;
        case int integer:     element = Revit.ActiveDBDocument.GetElement(new DB.ElementId(integer)); break;
        default:              return false;
      }

      return SetValue(element);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (base.CastTo<Q>(ref target))
        return true;

      var element = (DB.Element) this;
      if (typeof(DB.Element).IsAssignableFrom(typeof(Q)))
      {
        if (element is null)
        {
          if (IsValid)
            return false;
        }
        else if (!typeof(Q).IsAssignableFrom(element.GetType()))
          return false;

        target = (Q) (object) element;
        return true;
      }

      if (element is null)
        return false;

      if (element.Category?.HasMaterialQuantities ?? false)
      {
        if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
        {
          using (var options = new DB.Options() { DetailLevel = DB.ViewDetailLevel.Fine })
          using (var geometry = element.GetGeometry(options))
          {
            if (geometry is object)
            {
              var mesh = new Rhino.Geometry.Mesh();
              mesh.Append(geometry.GetPreviewMeshes(null).Where(x => x is object));
              mesh.Normals.ComputeNormals();
              if (mesh.Faces.Count > 0)
              {
                target = (Q) (object) new GH_Mesh(mesh);
                return true;
              }
            }
          }
        }
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
      {
        try
        {
          var plane = Plane;
          if (!plane.IsValid || !plane.Origin.IsValid)
            return false;

          target = (Q) (object) new GH_Plane(plane);
          return true;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Point)))
      {
        var location = Origin;
        if (!location.IsValid)
          return false;

        target = (Q) (object) new GH_Point(location);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Vector)))
      {
        var normal = ZAxis;
        if (!normal.IsValid)
          return false;

        target = (Q) (object) new GH_Vector(normal);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Transform)))
      {
        var plane = Plane;
        if (!plane.IsValid || !plane.Origin.IsValid)
          return false;

        target = (Q) (object) new GH_Transform(Rhino.Geometry.Transform.PlaneToPlane(Rhino.Geometry.Plane.WorldXY, plane));
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

    new class Proxy : ElementId.Proxy
    {
      public Proxy(Element e) : base(e) { (this as IGH_GooProxy).UserString = FormatInstance(); }

      public override bool IsParsable() => true;
      public override string FormatInstance() => owner.IsValid ? $"{owner.Value.IntegerValue}:{element?.Name ?? string.Empty}" : "-1";
      public override bool FromString(string str)
      {
        int index = str.IndexOf(':');
        if(index >= 0)
          str = str.Substring(0, index);

        str = str.Trim();
        if (int.TryParse(str, out int elementId))
        {
          owner.SetValue(owner.Document ?? Revit.ActiveUIDocument.Document, new DB.ElementId(elementId));
          return true;
        }

        return false;
      }

      DB.Element element => owner.IsElementLoaded ? owner.Document?.GetElement(owner.Id) : null;
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);

    public override string DisplayName
    {
      get
      {
        var element = (DB.Element) this;
        if (element is object && !string.IsNullOrEmpty(element.Name))
          return element.Name;

        return base.DisplayName;
      }
    }

    #region Location
    protected Rhino.Geometry.BoundingBox clippingBox = Rhino.Geometry.BoundingBox.Empty;
    public Rhino.Geometry.BoundingBox ClippingBox
    {
      get
      {
        if (!clippingBox.IsValid)
        {
          var element = (DB.Element) this;
          if (element is object)
            clippingBox = element.get_BoundingBox(null).ToBoundingBox();
        }

        return clippingBox;
      }
    }

    public Rhino.Geometry.Box Box
    {
      get
      {
        var b = Rhino.Geometry.Box.Empty;

        var element = (DB.Element) this;
        if (element?.get_BoundingBox(null) is DB.BoundingBoxXYZ bbox)
        {
          b = new Rhino.Geometry.Box(new Rhino.Geometry.BoundingBox(bbox.Min.ToPoint3d(), bbox.Max.ToPoint3d()));
          if (!b.Transform(Rhino.Geometry.Transform.Scale(Rhino.Geometry.Point3d.Origin, Revit.ModelUnits) * bbox.Transform.ToTransform()))
            b = new Rhino.Geometry.Box(ClippingBox);
        }

        return b;
      }
    }

    public virtual Rhino.Geometry.Point3d Origin
    {
      get
      {
        var p = new Rhino.Geometry.Point3d(double.NaN, double.NaN, double.NaN);

        var element = (DB.Element) this;
        if (element is object)
        {
          if (element is DB.Instance instance)
          {
            p = instance.GetTransform().Origin.ToPoint3d();
          }
          else switch (element.Location)
          {
            case DB.LocationPoint pointLocation: p = pointLocation.Point.ToPoint3d(); break;
            case DB.LocationCurve curveLocation: p = curveLocation.Curve.Evaluate(0.0, curveLocation.Curve.IsBound).ToPoint3d(); break;
            default:
              if (element.get_BoundingBox(null) is DB.BoundingBoxXYZ bbox)
                p = bbox.Min.ToPoint3d();
              break;
          }
        }

        return p;
      }
    }

    public virtual Rhino.Geometry.Vector3d XAxis
    {
      get
      {
        var x = Rhino.Geometry.Vector3d.Zero;

        var element = (DB.Element) this;
        if (element is object)
        {
          if (element is DB.Instance instance)
            x = (Rhino.Geometry.Vector3d) instance.GetTransform().BasisX.ToPoint3d();
          else if (element.Location is DB.LocationCurve curveLocation)
          {
            var c = curveLocation.Curve.ToCurve();
            x = c.TangentAt(c.Domain.Min);
          }
          else if (element.Location is DB.LocationPoint pointLocation)
          {
            x = Rhino.Geometry.Vector3d.XAxis;
            x.Rotate(pointLocation.Rotation, Rhino.Geometry.Vector3d.ZAxis);
          }

          if (x.IsZero || !x.Unitize())
            x = Rhino.Geometry.Vector3d.XAxis;
        }

        return x;
      }
    }

    public virtual Rhino.Geometry.Vector3d YAxis
    {
      get
      {
        var y = Rhino.Geometry.Vector3d.Zero;

        var element = (DB.Element) this;
        if (element is object)
        {
          if (element is DB.Instance instance)
            y = (Rhino.Geometry.Vector3d) instance.GetTransform().BasisY.ToPoint3d();
          else if (element.Location is DB.LocationCurve curveLocation)
          {
            var c = curveLocation.Curve.ToCurve();
            y = c.CurvatureAt(c.Domain.Min);
          }
          else if (element.Location is DB.LocationPoint pointLocation)
          {
            y = Rhino.Geometry.Vector3d.YAxis;
            y.Rotate(pointLocation.Rotation, Rhino.Geometry.Vector3d.ZAxis);
          }

          if (y.IsZero || !y.Unitize())
          {
            var axis = XAxis;
            if (new Rhino.Geometry.Vector3d(axis.X, axis.Y, 0.0).IsZero)
              y = new Rhino.Geometry.Vector3d(axis.Z, 0.0, -axis.X);
            else
              y = new Rhino.Geometry.Vector3d(-axis.Y, axis.X, 0.0);
          }

          if (y.IsZero || !y.Unitize())
            y = Rhino.Geometry.Vector3d.YAxis;
        }

        return y;
      }
    }

    public virtual Rhino.Geometry.Vector3d ZAxis
    {
      get
      {
        var z = Rhino.Geometry.Vector3d.Zero;

        var element = (DB.Element) this;
        if (element is object)
        {
          if (element is DB.Instance instance)
            z = (Rhino.Geometry.Vector3d) instance.GetTransform().BasisZ.ToPoint3d();
          else if (element.Location is DB.LocationCurve curveLocation)
          {
            var c = curveLocation.Curve.ToCurve();
            z = Rhino.Geometry.Vector3d.CrossProduct(c.TangentAt(c.Domain.Min), c.CurvatureAt(c.Domain.Min));
          }
          else if (element.Location is DB.LocationPoint pointLocation)
          {
            z = Rhino.Geometry.Vector3d.ZAxis;
          }

          if (z.IsZero || !z.Unitize())
            z = Rhino.Geometry.Vector3d.CrossProduct(XAxis, YAxis);

          if (z.IsZero || !z.Unitize())
            z = Rhino.Geometry.Vector3d.ZAxis;
        }

        return z;
      }
    }

    public virtual Rhino.Geometry.Plane Plane => new Rhino.Geometry.Plane(Origin, XAxis, YAxis);

    public virtual Rhino.Geometry.Curve Curve
    {
      get
      {
        var element = (DB.Element) this;

        return element?.Location is DB.LocationCurve curveLocation ?
          curveLocation.Curve.ToCurve() :
          null;
      }
    }

    public virtual Rhino.Geometry.Brep Surface => null;
    #endregion
  }
}

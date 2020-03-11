using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.GH.Types.Documents;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Element : ElementId
  {
    public override string TypeName => "Revit Element";
    public override string TypeDescription => "Represents a Revit element";
    override public object ScriptVariable() => (DB.Element) this;
    protected override Type ScriptVariableType => typeof(DB.Element);
    public static explicit operator DB.Element(Element self) => self.IsValid ? self.Document.GetElement(self) : null;

    public static Element FromValue(object data)
    {
      switch (data)
      {
        case DB.Category category: return Documents.Categories.Category.FromCategory(category);
        case DB.Element  element:  return Element.FromElement(element);
      }

      return null;
    }

    public static readonly Dictionary<Type, Func<DB.Element, Element>> ActivatorDictionary = new Dictionary<Type, Func<DB.Element, Element>>()
    {
      { typeof(DB.Family),            (element)=> new Documents.Families.Family        (element as DB.Family)            },
      { typeof(DB.ElementType),       (element)=> new Documents.ElementTypes.ElementType   (element as DB.ElementType)       },
      { typeof(DB.ParameterElement),  (element)=> new Documents.Params.ParameterKey  (element as DB.ParameterElement)  },
      { typeof(DB.Material),          (element)=> new Elements.Material.Material      (element as DB.Material)          },
      { typeof(DB.GraphicsStyle),     (element)=> new Documents.Styles.GraphicsStyle (element as DB.GraphicsStyle)     },

      { typeof(DB.View),              (element)=> new Elements.View.View          (element as DB.View)              },
      { typeof(DB.SketchPlane),       (element)=> new Elements.SketchPlane.SketchPlane   (element as DB.SketchPlane)       },
      { typeof(DB.DatumPlane),        (element)=> new DatumPlane    (element as DB.DatumPlane)        },
      { typeof(DB.HostObject),        (element)=> new Elements.HostObject.HostObject    (element as DB.HostObject)        },
      { typeof(DB.Level),             (element)=> new Elements.Level.Level         (element as DB.Level)             },
      { typeof(DB.Grid),              (element)=> new Elements.Grid.Grid          (element as DB.Grid)              },
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
            return new Documents.Categories.Category(category);
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
    protected Element(DB.Document doc, DB.ElementId id) : base(doc, id) { }
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
          DB.Options options = null;
          using (var geometry = element.GetGeometry(DB.ViewDetailLevel.Fine, out options)) using (options)
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

      if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
      {
        var axis = Axis;
        if (axis is null)
          return false;

        target = (Q) (object) new GH_Curve(axis);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
      {
        var plane = Plane;
        if (!plane.IsValid || !plane.Origin.IsValid)
          return false;

        target = (Q) (object) new GH_Plane(plane);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Point)))
      {
        var location = Location;
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
            clippingBox = element.get_BoundingBox(null).ToRhino().ChangeUnits(Revit.ModelUnits);
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
          b = new Rhino.Geometry.Box(new Rhino.Geometry.BoundingBox(bbox.Min.ToRhino(), bbox.Max.ToRhino()));
          if (!b.Transform(Rhino.Geometry.Transform.Scale(Rhino.Geometry.Point3d.Origin, Revit.ModelUnits) * bbox.Transform.ToRhino()))
            b = new Rhino.Geometry.Box(ClippingBox);
        }

        return b;
      }
    }

    public virtual Rhino.Geometry.Point3d Location
    {
      get
      {
        var p = new Rhino.Geometry.Point3d(double.NaN, double.NaN, double.NaN);

        var element = (DB.Element) this;
        if (element is object)
        {
          if (element is DB.Instance instance)
            p = instance.GetTransform().Origin.ToRhino();
          else switch (element.Location)
          {
            case DB.LocationPoint pointLocation: p = pointLocation.Point.ToRhino(); break;
            case DB.LocationCurve curveLocation: p = curveLocation.Curve.Evaluate(0.0, curveLocation.Curve.IsBound).ToRhino(); break;
            default:
                var bbox = element.get_BoundingBox(null);
                if(bbox is object)
                  p = bbox.Min.ToRhino(); break;
          }

          if (p.IsValid)
            return p.ChangeUnits(Revit.ModelUnits);
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
            x = (Rhino.Geometry.Vector3d) instance.GetTransform().BasisX.ToRhino();
          else if (element.Location is DB.LocationCurve curveLocation)
          {
            var c = curveLocation.Curve.ToRhino();
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
            y = (Rhino.Geometry.Vector3d) instance.GetTransform().BasisY.ToRhino();
          else if (element.Location is DB.LocationCurve curveLocation)
          {
            var c = curveLocation.Curve.ToRhino();
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
            z = (Rhino.Geometry.Vector3d) instance.GetTransform().BasisZ.ToRhino();
          else if (element.Location is DB.LocationCurve curveLocation)
          {
            var c = curveLocation.Curve.ToRhino();
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

    public virtual Rhino.Geometry.Plane Plane => new Rhino.Geometry.Plane(Location, XAxis, YAxis);

    public virtual Rhino.Geometry.Curve Axis
    {
      get
      {
        var element = (DB.Element) this;
        Rhino.Geometry.Curve c = null;

        if(element?.Location is DB.LocationCurve curveLocation)
          c = curveLocation.Curve.ToRhino();

        return c?.ChangeUnits(Revit.ModelUnits);
      }
    }
    #endregion
  }
}

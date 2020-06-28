using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
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
    public static explicit operator DB.Element(Element value) =>
      value.IsValid ? value.Document.GetElement(value) : default;

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
      { typeof(DB.View),                    (element)=> new View          (element as DB.View)              },
      { typeof(DB.Family),                  (element)=> new Family        (element as DB.Family)            },
      { typeof(DB.ElementType),             (element)=> new ElementType   (element as DB.ElementType)       },
      { typeof(DB.HostObjAttributes),       (element)=> new HostObjectType(element as DB.HostObjAttributes) },
      { typeof(DB.ParameterElement),        (element)=> new ParameterKey  (element as DB.ParameterElement)  },
      { typeof(DB.Material),                (element)=> new Material      (element as DB.Material)          },
      { typeof(DB.GraphicsStyle),           (element)=> new GraphicsStyle (element as DB.GraphicsStyle)     },

      { typeof(DB.Sketch),                  (element)=> new Sketch        (element as DB.Sketch)            },
      { typeof(DB.SketchPlane),             (element)=> new SketchPlane   (element as DB.SketchPlane)       },
      { typeof(DB.DatumPlane),              (element)=> new DatumPlane    (element as DB.DatumPlane)        },
      { typeof(DB.Level),                   (element)=> new Level         (element as DB.Level)             },
      { typeof(DB.Grid),                    (element)=> new Grid          (element as DB.Grid)              },
      { typeof(DB.SpatialElement),          (element)=> new SpatialElement(element as DB.SpatialElement)    },
      { typeof(DB.Group),                   (element)=> new Group         (element as DB.Group)             },
      { typeof(DB.HostObject),              (element)=> new HostObject    (element as DB.HostObject)        },
      { typeof(DB.CurtainGridLine),         (element)=> new CurtainGridLine(element as DB.CurtainGridLine)  },
      { typeof(DB.Floor),                   (element)=> new Floor         (element as DB.Floor)             },
      { typeof(DB.Architecture.BuildingPad),(element)=> new BuildingPad   (element as DB.Architecture.BuildingPad)             },
      { typeof(DB.Ceiling),                 (element)=> new Ceiling       (element as DB.Ceiling)           },
      { typeof(DB.RoofBase),                (element)=> new Roof          (element as DB.RoofBase)          },
      { typeof(DB.Wall),                    (element)=> new Wall          (element as DB.Wall)              },
      { typeof(DB.Instance),                (element)=> new Instance      (element as DB.Instance)          },
      { typeof(DB.FamilyInstance),          (element)=> new FamilyInstance(element as DB.FamilyInstance)    },
      { typeof(DB.Panel),                   (element)=> new Panel         (element as DB.Panel)             },
      { typeof(DB.Mullion),                 (element)=> new Mullion       (element as DB.Mullion)           },
      { typeof(DB.Dimension),               (element)=> new Dimension     (element as DB.Dimension)         },
      { typeof(DB.CurveElement),            (element)=> new CurveElement  (element as DB.CurveElement)      },
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

      if (GraphicalElement.IsValidElement(element))
      {
        if (InstanceElement.IsValidElement(element))
        {
          if (Panel.IsValidElement(element))
            return new Panel(element as DB.FamilyInstance);

          return new InstanceElement(element);
        }

        return GeometricElement.IsValidElement(element) ?
          new GeometricElement(element) :
          new GraphicalElement(element);
      }

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
              var mesh = new Mesh();
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
    protected BoundingBox clippingBox = BoundingBox.Unset;
    public virtual BoundingBox ClippingBox
    {
      get
      {
        if (!clippingBox.IsValid)
        {
          if ((DB.Element) this is DB.Element element)
            clippingBox = element.get_BoundingBox(null).ToBoundingBox();
        }

        return clippingBox;
      }
    }
    #endregion
  }
}

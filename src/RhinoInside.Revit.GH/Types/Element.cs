using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Display;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.External.UI.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Kernel.Attributes;

  [Name("Element")]
  public interface IGH_Element : IGH_ElementId
  {
    ElementType Type { get; set; }
  }

  [Name("Element")]
  public class Element : ElementId, IGH_Element
  {
    #region IGH_Goo
    public override bool IsValid => base.IsValid && Value is object;
    public override string IsValidWhyNot
    {
      get
      {
        if (base.IsValidWhyNot is string log) return log;

        if (Id.IsBuiltInId())
        {
          if (!IsValid) return $"Referenced built-in {((IGH_Goo) this).TypeName} is not valid. {{{Id.IntegerValue}}}";
        }
        else if (Value is null) return $"Referenced {((IGH_Goo) this).TypeName} was deleted or undone. {{{Id.IntegerValue}}}";

        return default;
      }
    }

    protected virtual Type ScriptVariableType => typeof(DB.Element);
    #endregion

    #region DocumentObject
    DB.Element value => base.Value as DB.Element;
    public new DB.Element Value
    {
      get
      {
        if (value?.IsValidObject == false)
          ResetValue();

        return value;
      }
    }
    #endregion

    #region ReferenceObject
    DB.ElementId id = DB.ElementId.InvalidElementId;
    public override DB.ElementId Id => id;
    #endregion

    #region IGH_ElementId
    public override DB.Reference Reference
    {
      get
      {
        try { return DB.Reference.ParseFromStableRepresentation(Document, UniqueID); }
        catch (Autodesk.Revit.Exceptions.ArgumentNullException) { return null; }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { return null; }
      }
    }

    public override bool IsReferencedDataLoaded => Document is object && Id is object;
    public sealed override bool LoadReferencedData()
    {
      if (IsReferencedData)
      {
        UnloadReferencedData();

        if (Types.Document.TryGetDocument(DocumentGUID, out var document))
        {
          if (document.TryGetElementId(UniqueID, out id))
            Document = document;
        }
      }

      return IsReferencedDataLoaded;
    }

    public override void UnloadReferencedData()
    {
      if (IsReferencedData)
        id = default;

      base.UnloadReferencedData();
    }

    protected override object FetchValue() => Document.GetElement(Id);
    #endregion

    protected void InvalidateGraphics()
    {
      Debug.Assert(Document.IsModifiable);

      SubInvalidateGraphics();
    }

    protected virtual void SubInvalidateGraphics() { }

    public static Element FromValue(object data)
    {
      switch (data)
      {
        case DB.Category category: return new Category(category);
        case DB.Element  element:  return Element.FromElement(element);
      }

      return null;
    }

    public static readonly Dictionary<Type, Func<DB.Element, Element>> ActivatorDictionary = new Dictionary<Type, Func<DB.Element, Element>>()
    {
      { typeof(DB.BasePoint),               (element)=> new BasePoint             (element as DB.BasePoint)         },
      { typeof(DB.DesignOption),            (element)=> new DesignOption          (element as DB.DesignOption)      },
      { typeof(DB.View),                    (element)=> new View                  (element as DB.View)              },
      { typeof(DB.Family),                  (element)=> new Family                (element as DB.Family)            },
      { typeof(DB.ElementType),             (element)=> new ElementType           (element as DB.ElementType)       },
      { typeof(DB.FamilySymbol),            (element)=> new FamilySymbol          (element as DB.FamilySymbol)      },
      { typeof(DB.HostObjAttributes),       (element)=> new HostObjectType        (element as DB.HostObjAttributes) },
      { typeof(DB.ParameterElement),        (element)=> new ParameterKey          (element as DB.ParameterElement)  },
      { typeof(DB.Material),                (element)=> new Material              (element as DB.Material)          },
      { typeof(DB.GraphicsStyle),           (element)=> new GraphicsStyle         (element as DB.GraphicsStyle)     },
      { typeof(DB.LinePatternElement),      (element)=> new LinePatternElement    (element as DB.LinePatternElement)},
      { typeof(DB.FillPatternElement),      (element)=> new FillPatternElement    (element as DB.FillPatternElement)},
      { typeof(DB.AppearanceAssetElement),  (element)=> new AppearanceAssetElement(element as DB.AppearanceAssetElement)},
      
      { typeof(DB.Instance),                (element)=> new Instance              (element as DB.Instance)          },
      { typeof(DB.ProjectLocation),         (element)=> new ProjectLocation       (element as DB.ProjectLocation)   },
      { typeof(DB.SiteLocation),            (element)=> new SiteLocation          (element as DB.SiteLocation)      },
      { typeof(DB.RevitLinkInstance),       (element)=> new RevitLinkInstance     (element as DB.RevitLinkInstance) },
      { typeof(DB.ImportInstance),          (element)=> new ImportInstance        (element as DB.ImportInstance)    },
      { typeof(DB.PointCloudInstance),      (element)=> new PointCloudInstance    (element as DB.PointCloudInstance)},

      { typeof(DB.DirectShape),             (element)=> new DirectShape           (element as DB.DirectShape)       },
      { typeof(DB.DirectShapeType),         (element)=> new DirectShapeType       (element as DB.DirectShapeType)   },

      { typeof(DB.Sketch),                  (element)=> new Sketch                (element as DB.Sketch)            },
      { typeof(DB.SketchPlane),             (element)=> new SketchPlane           (element as DB.SketchPlane)       },
      { typeof(DB.DatumPlane),              (element)=> new DatumPlane            (element as DB.DatumPlane)        },
      { typeof(DB.Level),                   (element)=> new Level                 (element as DB.Level)             },
      { typeof(DB.Grid),                    (element)=> new Grid                  (element as DB.Grid)              },
      { typeof(DB.ReferencePlane),          (element)=> new ReferencePlane        (element as DB.ReferencePlane)    },
      { typeof(DB.SpatialElement),          (element)=> new SpatialElement        (element as DB.SpatialElement)    },
      { typeof(DB.Group),                   (element)=> new Group                 (element as DB.Group)             },
      { typeof(DB.HostObject),              (element)=> new HostObject            (element as DB.HostObject)        },
      { typeof(DB.CurtainSystem),           (element)=> new CurtainSystem         (element as DB.CurtainSystem)     },
      { typeof(DB.CurtainGridLine),         (element)=> new CurtainGridLine       (element as DB.CurtainGridLine)   },
      { typeof(DB.Floor),                   (element)=> new Floor                 (element as DB.Floor)             },
      { typeof(DB.Architecture.BuildingPad),(element)=> new BuildingPad           (element as DB.Architecture.BuildingPad) },
      { typeof(DB.Ceiling),                 (element)=> new Ceiling               (element as DB.Ceiling)           },
      { typeof(DB.RoofBase),                (element)=> new Roof                  (element as DB.RoofBase)          },
      { typeof(DB.Wall),                    (element)=> new Wall                  (element as DB.Wall)              },
      { typeof(DB.FamilyInstance),          (element)=> new FamilyInstance        (element as DB.FamilyInstance)    },
      { typeof(DB.Panel),                   (element)=> new Panel                 (element as DB.Panel)             },
      { typeof(DB.Mullion),                 (element)=> new Mullion               (element as DB.Mullion)           },
      { typeof(DB.Dimension),               (element)=> new Dimension             (element as DB.Dimension)         },
      { typeof(DB.CurveElement),            (element)=> new CurveElement          (element as DB.CurveElement)      },
    };

    public static Element FromElement(DB.Element element)
    {
      if (!element.IsValid())
        return null;

      for (var type = element.GetType(); type != typeof(DB.Element); type = type.BaseType)
      {
        if (ActivatorDictionary.TryGetValue(type, out var activator))
          return activator(element);
      }

      if (DocumentExtension.AsCategory(element) is DB.Category category)
        return new Category(category);

      if (element is DB.PropertySetElement pset)
      {
        if (StructuralAssetElement.IsValidElement(element))
          return new StructuralAssetElement(pset);
        else if (ThermalAssetElement.IsValidElement(element))
          return new ThermalAssetElement(pset);
      }
      else if (GraphicalElement.IsValidElement(element))
      {
        if (InstanceElement.IsValidElement(element))
        {
          if (Panel.IsValidElement(element))
            return new Panel(element as DB.FamilyInstance);

          return new InstanceElement(element);
        }

        if (GeometricElement.IsValidElement(element))
          return new GeometricElement(element);

        return new GraphicalElement(element);
      }
      else
      {
        if (DesignOptionSet.IsValidElement(element))
          return new DesignOptionSet(element);
      }

      return new Element(element);
    }

    public static Element FromElementId(DB.Document doc, DB.ElementId id)
    {
      if (doc is null || id is null)
        return default;

      if (Category.FromElementId(doc, id) is Category c)
        return c;

      if (ParameterKey.FromElementId(doc, id) is ParameterKey p)
        return p;

      if (LinePatternElement.FromElementId(doc, id) is LinePatternElement l)
        return l;

      if (FromElement(doc.GetElement(id)) is Element e)
        return e;

      return new Element(doc, id);
    }

    public static T FromElementId<T>(DB.Document doc, DB.ElementId id) where T : Element, new ()
    {
      if (doc is null || id is null) return default;
      if (id == DB.ElementId.InvalidElementId) return new T();

      return FromElementId(doc, id) as T;
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

    protected internal void SetValue(DB.Document doc, DB.ElementId id)
    {
      if (id == DB.ElementId.InvalidElementId)
        doc = null;

      Document = doc;
      DocumentGUID = doc.GetFingerprintGUID();

      this.id = id;
      UniqueID = doc?.GetElement(id)?.UniqueId ??
      (
        id.IntegerValue < DB.ElementId.InvalidElementId.IntegerValue ?
          UniqueId.Format(Guid.Empty, id.IntegerValue) :
          string.Empty
      );
    }

    protected virtual bool SetValue(DB.Element element)
    {
      if (ScriptVariableType.IsInstanceOfType(element))
      {
        Document     = element.Document;
        DocumentGUID = Document.GetFingerprintGUID();
        id           = element.Id;
        UniqueID     = element.UniqueId;
        base.Value   = element;
        return true;
      }

      return false;
    }

    protected override void ResetValue()
    {
      SubInvalidateGraphics();

      base.ResetValue();
    }

    public Element() { }
    internal Element(DB.Document doc, DB.ElementId id) => SetValue(doc, id);
    protected Element(DB.Element element) : base(element?.Document, element)
    {
      DocumentGUID = Document.GetFingerprintGUID();

      if (element is object)
      {
        UniqueID = element.UniqueId;
        id = element.Id;
      }
    }

    public override bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
      {
        if (source is IGH_Element element)
        {
          var document = element.Document;
          var id = element.Id;

          if (id == DB.ElementId.InvalidElementId)
          {
            SetValue(document, id);
            return true;
          }
          else source = document?.GetElement(id);
        }
        else source = goo.ScriptVariable();
      }

      if (source is string uniqueid)
      {
        if (FullUniqueId.TryParse(uniqueid, out var documentId, out var uniqueId))
        {
          if (Types.Document.TryGetDocument(documentId, out var doc))
          {
            try { source = doc.GetElement(uniqueId); }
            catch { }
          }
        }
      }

      if (source is ValueTuple<DB.Document, DB.ElementId> tuple)
      {
        try { source = tuple.Item1.GetElement(tuple.Item2); }
        catch { }
      }

      return SetValue(source as DB.Element);
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo<Q>(out target))
        return true;

      var element = Value;
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
              mesh.Append(geometry.GetPreviewMeshes(element.Document, null));
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

    protected new class Proxy : ElementId.Proxy
    {
      protected new Element owner => base.owner as Element;

      public Proxy(Element e) : base(e) { (this as IGH_GooProxy).UserString = FormatInstance(); }

      public override string FormatInstance()
      {
        return owner.DisplayName;
      }

      [System.ComponentModel.Description("The element identifier in this session.")]
      [System.ComponentModel.RefreshProperties(System.ComponentModel.RefreshProperties.All)]
      public virtual int? Id
      {
        get => owner.Id?.IntegerValue;
        set
        {
          if (!value.HasValue || value == DB.ElementId.InvalidElementId.IntegerValue)
            owner.SetValue(default, DB.ElementId.InvalidElementId);
          else
          {
            var doc = owner.Document ?? Revit.ActiveDBDocument;
            var id = new DB.ElementId(value.Value);

            if (IsValidId(doc, id)) owner.SetValue(doc, id);
          }
        }
      }

      [System.ComponentModel.Description("A human readable name for the Element.")]
      public string Name => owner.Name;
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);

    public override string DisplayName
    {
      get
      {
        if (Name is string name && name != string.Empty)
          return name;

        return base.DisplayName;
      }
    }

    #region Properties
    public DB.WorksetId WorksetId
    {
      get => Document?.GetWorksetId(Id);
      set => Value?.get_Parameter(DB.BuiltInParameter.ELEM_PARTITION_PARAM)?.Set(value.IntegerValue);
    }

    public bool CanDelete => IsValid && DB.DocumentValidation.CanDeleteElement(Document, Id);

    public bool? Pinned
    {
      get => Value?.Pinned;
      set
      {
        if (value.HasValue && Value is DB.Element element && element.Pinned != value.Value)
          element.Pinned = value.Value;
      }
    }

    public virtual bool CanBeRenamed() => Value.CanBeRenamed();

    static string GetUniqueNameRoot(string name)
    {
      if (name?.EndsWith(")") == true)
      {
        var start = name.LastIndexOf('(');
        if (start >= 0)
        {
          var number = name.Substring(start + 1, name.Length - start - 2);
          if (int.TryParse(number, out var _))
            return name.Substring(0, start - 1);
        }
      }

      return name;
    }

    public bool SetUniqueName(string name)
    {
      if (!DB.NamingUtils.IsValidName(name))
        throw new ArgumentException("Element name contains prohibited characters and is invalid.", nameof(name));

      if (Value is DB.Element element)
      {
        name = GetUniqueNameRoot(name);

        int index = 0;
        while (true)
        {
          try
          {
            if (index == 0) { index++; element.Name = name; }
            else element.Name = $"{name} ({index++})";
            return true;
          }
          catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
          catch (Autodesk.Revit.Exceptions.ArgumentException) { }
        }
      }

      return false;
    }

    public virtual string Name
    {
      get => Value?.Name;
      set
      {
        if (value is object)
        {
          if (Id.IsBuiltInId())
          {
            if (value == Name) return;
            throw new InvalidOperationException($"BuiltIn {((IGH_Goo) this).TypeName.ToLowerInvariant()} '{DisplayName}' does not support assignment of a user-specified name.");
          }
          else if (Value is DB.Element element && element.Name != value)
          {
            element.Name = value;
          }
        }
      }
    }

    public Category Category
    {
      get => Category.FromCategory(Value?.Category);
    }

    public virtual ElementType Type
    {
      get => ElementType.FromElementId(Document, Value?.GetTypeId()) as ElementType;
      set
      {
        if (value is object && Value is DB.Element element)
        {
          AssertValidDocument(value.Document, nameof(Type));
          InvalidateGraphics();

          element.ChangeTypeId(value.Id);
        }
      }
    }
    #endregion

    #region Identity Data
    public string Description
    {
      get => Value?.get_Parameter(DB.BuiltInParameter.ALL_MODEL_DESCRIPTION)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(DB.BuiltInParameter.ALL_MODEL_DESCRIPTION)?.Set(value);
      }
    }

    public string Comments
    {
      get => Value?.get_Parameter(DB.BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(DB.BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(value);
      }
    }

    public string Manufacturer
    {
      get => Value?.get_Parameter(DB.BuiltInParameter.ALL_MODEL_MANUFACTURER)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(DB.BuiltInParameter.ALL_MODEL_MANUFACTURER)?.Set(value);
      }
    }

    public string Model
    {
      get => Value?.get_Parameter(DB.BuiltInParameter.ALL_MODEL_MODEL)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(DB.BuiltInParameter.ALL_MODEL_MODEL)?.Set(value);
      }
    }

    public double? Cost
    {
      get => Value?.get_Parameter(DB.BuiltInParameter.ALL_MODEL_COST)?.AsDouble();
      set
      {
        if (value is object)
          Value?.get_Parameter(DB.BuiltInParameter.ALL_MODEL_COST)?.Set(value.Value);
      }
    }

    public string Url
    {
      get => Value?.get_Parameter(DB.BuiltInParameter.ALL_MODEL_URL)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(DB.BuiltInParameter.ALL_MODEL_URL)?.Set(value);
      }
    }

    public string Keynote
    {
      get => Value?.get_Parameter(DB.BuiltInParameter.KEYNOTE_PARAM)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(DB.BuiltInParameter.KEYNOTE_PARAM)?.Set(value);
      }
    }

    public virtual string Mark
    {
      get => Value?.get_Parameter(DB.BuiltInParameter.ALL_MODEL_MARK) is DB.Parameter parameter &&
        parameter.HasValue ?
        parameter.AsString() :
        default;

      set
      {
        if (value is object)
          Value?.get_Parameter(DB.BuiltInParameter.ALL_MODEL_MARK)?.Set(value);
      }
    }
    #endregion
  }
}

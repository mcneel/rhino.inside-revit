using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Display;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Element")]
  public interface IGH_Element : IGH_Reference
  {
    Category Category { get; }
    ElementType Type { get; set; }
  }

  [Kernel.Attributes.Name("Element")]
  public class Element : Reference, IGH_Element
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
          if (!IsValid) return $"Referenced built-in {((IGH_Goo) this).TypeName} is not valid. {{{Id.ToValue()}}}";
        }
        else if (Value is null) return $"Referenced {((IGH_Goo) this).TypeName} was deleted or undone. {{{Id.ToValue()}}}";

        return default;
      }
    }

    protected virtual Type ValueType => typeof(ARDB.Element);
    #endregion

    #region DocumentObject
    public new ARDB.Element Value
    {
      get
      {
        var element = base.Value as ARDB.Element;
        switch (element?.IsValidObject)
        {
          case false:
            Debug.WriteLine("Element is not valid.");
            ResetValue();
            return base.Value as ARDB.Element;

          case true:  return element;
          default:    return null;
        }
      }
    }

    protected override object FetchValue() => Document?.GetElement(Id);

    protected override void ResetValue()
    {
      SubInvalidateGraphics();

      base.ResetValue();
    }

    public override string DisplayName => Nomen ?? (IsReferencedData ? string.Empty : "<None>");
    #endregion

    #region ReferenceObject
    public override bool? IsEditable => IsValid ? !Document.IsLinked && CanDelete : default(bool?);
    #endregion

    #region IGH_Reference
    ARDB.ElementId _Id = ARDB.ElementId.InvalidElementId;
    public override ARDB.ElementId Id => _Id;

    ARDB.Document _ReferenceDocument;
    public override ARDB.Document ReferenceDocument => _ReferenceDocument;

    public override ARDB.Reference GetReference()
    {
      try { return ARDB.Reference.ParseFromStableRepresentation(ReferenceDocument, UniqueID); }
      catch (Autodesk.Revit.Exceptions.ArgumentNullException) { return null; }
      catch (Autodesk.Revit.Exceptions.ArgumentException) { return null; }
    }

    ARDB.ElementId _ReferenceId = ARDB.ElementId.InvalidElementId;
    public override ARDB.ElementId ReferenceId => _ReferenceId;
    #endregion

    #region IGH_ReferencedData
    public override bool IsReferencedDataLoaded => ReferenceDocument is object && Id is object;
    public sealed override bool LoadReferencedData()
    {
      if (IsReferencedData)
      {
        UnloadReferencedData();

        if (Types.Document.TryGetDocument(DocumentGUID, out _ReferenceDocument))
        {
          if (_ReferenceDocument.TryGetLinkElementId(UniqueID, out var linkElementId))
          {
            _ReferenceId = _Id = linkElementId.HostElementId;
            if (_ReferenceId == ARDB.ElementId.InvalidElementId)
            {
              _ReferenceId = linkElementId.LinkInstanceId;
              _Id = linkElementId.LinkedElementId;
              Document = (_ReferenceDocument.GetElement(_ReferenceId) as ARDB.RevitLinkInstance).GetLinkDocument();
            }
            else Document = _ReferenceDocument;
          }
          else _ReferenceDocument = null;
        }
      }

      return IsReferencedDataLoaded;
    }

    public override void UnloadReferencedData()
    {
      if (IsReferencedData)
      {
        _ReferenceDocument = default;
        _ReferenceId = default;
        _Id = default;
      }

      base.UnloadReferencedData();
    }
    #endregion

    protected internal void InvalidateGraphics()
    {
      Debug.Assert(Document.IsModifiable);

      SubInvalidateGraphics();
    }

    protected virtual void SubInvalidateGraphics() { }

    public static readonly Dictionary<Type, Func<ARDB.Element, Element>> ActivatorDictionary = new Dictionary<Type, Func<ARDB.Element, Element>>()
    {
#if REVIT_2021
      { typeof(ARDB.InternalOrigin),                  (element)=> new InternalOrigin        (element as ARDB.InternalOrigin)    },
      { typeof(ARDB.BasePoint),                       (element)=> new BasePoint             (element as ARDB.BasePoint)         },
#endif
      { typeof(ARDB.DesignOption),                    (element)=> new DesignOption          (element as ARDB.DesignOption)      },
      { typeof(ARDB.Phase),                           (element)=> new Phase                 (element as ARDB.Phase)             },
      { typeof(ARDB.SelectionFilterElement),          (element)=> new SelectionFilterElement(element as ARDB.SelectionFilterElement)},
      { typeof(ARDB.ParameterFilterElement),          (element)=> new ParameterFilterElement(element as ARDB.ParameterFilterElement)},
      { typeof(ARDB.Family),                          (element)=> new Family                (element as ARDB.Family)            },
      { typeof(ARDB.ElementType),                     (element)=> new ElementType           (element as ARDB.ElementType)       },
      { typeof(ARDB.FamilySymbol),                    (element)=> new FamilySymbol          (element as ARDB.FamilySymbol)      },
      { typeof(ARDB.HostObjAttributes),               (element)=> new HostObjectType        (element as ARDB.HostObjAttributes) },
      { typeof(ARDB.MEPCurveType),                    (element)=> new MEPCurveType          (element as ARDB.MEPCurveType)      },
      { typeof(ARDB.ParameterElement),                (element)=> new ParameterKey          (element as ARDB.ParameterElement)  },
      { typeof(ARDB.Material),                        (element)=> new Material              (element as ARDB.Material)          },
      { typeof(ARDB.GraphicsStyle),                   (element)=> new GraphicsStyle         (element as ARDB.GraphicsStyle)     },
      { typeof(ARDB.LinePatternElement),              (element)=> new LinePatternElement    (element as ARDB.LinePatternElement)},
      { typeof(ARDB.FillPatternElement),              (element)=> new FillPatternElement    (element as ARDB.FillPatternElement)},
      { typeof(ARDB.AppearanceAssetElement),          (element)=> new AppearanceAssetElement(element as ARDB.AppearanceAssetElement)},

      { typeof(ARDB.ViewFamilyType),                  (element)=> new ViewFamilyType        (element as ARDB.ViewFamilyType)    },
      { typeof(ARDB.View),                            (element)=> new View                  (element as ARDB.View)              },
      { typeof(ARDB.Viewport),                        (element)=> new Viewport              (element as ARDB.Viewport)          },
      { typeof(ARDB.ViewSheet),                       (element)=> new ViewSheet             (element as ARDB.ViewSheet)         },
      { typeof(ARDB.View3D),                          (element)=> new View3D                (element as ARDB.View3D)            },
      { typeof(ARDB.ViewPlan),                        (element)=> new ViewPlan              (element as ARDB.ViewPlan)          },
      { typeof(ARDB.ViewSection),                     (element)=> new ViewSection           (element as ARDB.ViewSection)       },
      { typeof(ARDB.ViewDrafting),                    (element)=> new ViewDrafting          (element as ARDB.ViewDrafting)      },

      { typeof(ARDB.Instance),                        (element)=> new Instance              (element as ARDB.Instance)          },
      { typeof(ARDB.ProjectLocation),                 (element)=> new ProjectLocation       (element as ARDB.ProjectLocation)   },
      { typeof(ARDB.SiteLocation),                    (element)=> new SiteLocation          (element as ARDB.SiteLocation)      },
      { typeof(ARDB.RevitLinkInstance),               (element)=> new RevitLinkInstance     (element as ARDB.RevitLinkInstance) },
      { typeof(ARDB.ImportInstance),                  (element)=> new ImportInstance        (element as ARDB.ImportInstance)    },
      { typeof(ARDB.PointCloudInstance),              (element)=> new PointCloudInstance    (element as ARDB.PointCloudInstance)},

      { typeof(ARDB.DirectShape),                     (element)=> new DirectShape           (element as ARDB.DirectShape)       },
      { typeof(ARDB.DirectShapeType),                 (element)=> new DirectShapeType       (element as ARDB.DirectShapeType)   },

      { typeof(ARDB.Sketch),                          (element)=> new Sketch                (element as ARDB.Sketch)            },
      { typeof(ARDB.SketchPlane),                     (element)=> new SketchPlane           (element as ARDB.SketchPlane)       },
      { typeof(ARDB.CurveElement),                    (element)=> new CurveElement          (element as ARDB.CurveElement)      },
      { typeof(ARDB.CombinableElement),               (element)=> new CombinableElement     (element as ARDB.CombinableElement) },

      { typeof(ARDB.DatumPlane),                      (element)=> new DatumPlane            (element as ARDB.DatumPlane)        },
      { typeof(ARDB.Level),                           (element)=> new Level                 (element as ARDB.Level)             },
      { typeof(ARDB.Grid),                            (element)=> new Grid                  (element as ARDB.Grid)              },
      { typeof(ARDB.ReferencePlane),                  (element)=> new ReferencePlane        (element as ARDB.ReferencePlane)    },
      { typeof(ARDB.ReferencePoint),                  (element)=> new ReferencePoint        (element as ARDB.ReferencePoint)    },
      { typeof(ARDB.Group),                           (element)=> new Group                 (element as ARDB.Group)             },
      { typeof(ARDB.Opening),                         (element)=> new Opening               (element as ARDB.Opening)           },
      { typeof(ARDB.HostObject),                      (element)=> new HostObject            (element as ARDB.HostObject)        },
      { typeof(ARDB.MEPCurve),                        (element)=> new MEPCurve              (element as ARDB.MEPCurve)          },
      { typeof(ARDB.CurtainSystem),                   (element)=> new CurtainSystem         (element as ARDB.CurtainSystem)     },
      { typeof(ARDB.CurtainGridLine),                 (element)=> new CurtainGridLine       (element as ARDB.CurtainGridLine)   },
      { typeof(ARDB.Floor),                           (element)=> new Floor                 (element as ARDB.Floor)             },
      { typeof(ARDB.Ceiling),                         (element)=> new Ceiling               (element as ARDB.Ceiling)           },
      { typeof(ARDB.RoofBase),                        (element)=> new Roof                  (element as ARDB.RoofBase)          },
      { typeof(ARDB.Wall),                            (element)=> new Wall                  (element as ARDB.Wall)              },
      { typeof(ARDB.FamilyInstance),                  (element)=> new FamilyInstance        (element as ARDB.FamilyInstance)    },
      { typeof(ARDB.Panel),                           (element)=> new Panel                 (element as ARDB.Panel)             },
      { typeof(ARDB.Mullion),                         (element)=> new Mullion               (element as ARDB.Mullion)           },

      { typeof(ARDB.TextElement),                     (element)=> new TextElement           (element as ARDB.TextElement)       },
      { typeof(ARDB.Dimension),                       (element)=> new Dimension             (element as ARDB.Dimension)         },
      { typeof(ARDB.DimensionType),                   (element)=> new DimensionType         (element as ARDB.DimensionType)     },
      { typeof(ARDB.SpotDimension),                   (element)=> new SpotDimension         (element as ARDB.SpotDimension)     },
      { typeof(ARDB.FilledRegion),                    (element)=> new FilledRegion          (element as ARDB.FilledRegion)      },
      { typeof(ARDB.Revision),                        (element)=> new Revision              (element as ARDB.Revision)          },
      { typeof(ARDB.RevisionCloud),                   (element)=> new RevisionCloud         (element as ARDB.RevisionCloud)     },
      { typeof(ARDB.AnnotationSymbol),                (element)=> new AnnotationSymbol      (element as ARDB.AnnotationSymbol)  },
      { typeof(ARDB.IndependentTag),                  (element)=> new IndependentTag        (element as ARDB.IndependentTag)    },
      
      { typeof(ARDB.AssemblyInstance),                (element)=> new AssemblyInstance      (element as ARDB.AssemblyInstance)  },

      { typeof(ARDB.SpatialElement),                  (element)=> new SpatialElement        (element as ARDB.SpatialElement)    },
      { typeof(ARDB.Area),                            (element)=> new AreaElement           (element as ARDB.Area)              },
      { typeof(ARDB.Architecture.Room),               (element)=> new RoomElement           (element as ARDB.Architecture.Room) },
      { typeof(ARDB.Mechanical.Space),                (element)=> new SpaceElement          (element as ARDB.Mechanical.Space)  },

      { typeof(ARDB.SpatialElementTag),               (element)=> new SpatialElementTag     (element as ARDB.SpatialElementTag)    },
      { typeof(ARDB.AreaTag),                         (element)=> new AreaElementTag        (element as ARDB.AreaTag)              },
      { typeof(ARDB.Architecture.RoomTag),            (element)=> new RoomElementTag        (element as ARDB.Architecture.RoomTag) },
      { typeof(ARDB.Mechanical.SpaceTag),             (element)=> new SpaceElementTag       (element as ARDB.Mechanical.SpaceTag)  },

      { typeof(ARDB.Architecture.TopographySurface),  (element)=> new TopographySurface     (element as ARDB.Architecture.TopographySurface) },
      { typeof(ARDB.Architecture.BuildingPad),        (element)=> new BuildingPad           (element as ARDB.Architecture.BuildingPad) },
      { typeof(ARDB.Architecture.Railing),            (element)=> new Railing               (element as ARDB.Architecture.Railing) },
    };

    public static Element FromValue(ARDB.Document doc, object data)
    {
      try
      {
        switch (data)
        {
          case string s:

            if (ERDB.FullUniqueId.TryParse(s, out var documentId, out var stableId))
            {
              if (documentId != doc.GetFingerprintGUID()) return null;
              s = stableId;
            }

            if (doc.TryGetLinkElementId(s, out var linkElementId))
              return FromLinkElementId(doc, linkElementId);

            return default;

          case ARDB.BuiltInCategory c:    return FromElementId(doc, new ARDB.ElementId(c));
          case ARDB.BuiltInParameter p:   return FromElementId(doc, new ARDB.ElementId(p));
          case ARDB.ElementId id:         return FromElementId(doc, id);
          case ARDB.LinkElementId id:     return FromLinkElementId(doc, id);
          case ARDB.Reference r:          return FromReference(doc, r);
          case ARDB.Element element:      return doc.IsEquivalent(element.Document) ? FromElement(element) : null;
          case ARDB.Category category:    return doc.IsEquivalent(category.Document()) ? new Category(category) : null;
#if REVIT_2024
          case Int64 id:                  return FromElementId(doc, new ARDB.ElementId(id));
          case IConvertible convertible:  return FromElementId(doc, new ARDB.ElementId(System.Convert.ToInt64(convertible)));
#else
          case Int32 id:                  return FromElementId(doc, new ARDB.ElementId(id));
          case IConvertible convertible:  return FromElementId(doc, new ARDB.ElementId(System.Convert.ToInt32(convertible)));
#endif
        }
      }
      catch { }

      return null;
    }

    public static Element FromValue(object data)
    {
      switch (data)
      {
        case ARDB.Category category: return new Category(category);
        case ARDB.Element element: return Element.FromElement(element);
      }

      return null;
    }

    public static Element FromElement(ARDB.Element element)
    {
      if (!element.IsValid())
        return null;

      // Special FamilyInstance
      if (element is ARDB.FamilyInstance familyInstance)
      {
        if (StructuralBeam.IsValidElement(familyInstance)) return new StructuralBeam(familyInstance);
        if (StructuralBrace.IsValidElement(familyInstance)) return new StructuralBrace(familyInstance);
        if (StructuralColumn.IsValidElement(familyInstance)) return new StructuralColumn(familyInstance);
        if (Panel.IsValidElement(element)) return new Panel(familyInstance);
      }

      if (element is ARDB.View view)
      {
        switch (view.ViewType)
        {
          case ARDB.ViewType.FloorPlan:       return new FloorPlan(view as ARDB.ViewPlan);
          case ARDB.ViewType.CeilingPlan:     return new CeilingPlan(view as ARDB.ViewPlan);
          case ARDB.ViewType.AreaPlan:        return new AreaPlan(view as ARDB.ViewPlan);
          case ARDB.ViewType.EngineeringPlan: return new StructuralPlan(view as ARDB.ViewPlan);

          case ARDB.ViewType.Section:         return new SectionView(view as ARDB.ViewSection);
          case ARDB.ViewType.Elevation:       return new ElevationView(view as ARDB.ViewSection);
          case ARDB.ViewType.Detail:          return new DetailView(view as ARDB.ViewSection);
        }
      }

      // By type
      for (var type = element.GetType(); type != typeof(ARDB.Element); type = type.BaseType)
      {
        if (ActivatorDictionary.TryGetValue(type, out var activator))
          return activator(element);
      }

      // By Category
      if (element.Category is null)
      {
        if (DocumentExtension.AsCategory(element) is ARDB.Category category)
          return new Category(category);
      }
      else if (element.Category.Id.TryGetBuiltInCategory(out var bic))
      {
        switch (bic)
        {
          case ARDB.BuiltInCategory.OST_DesignOptionSets:
            if (DesignOptionSet.IsValidElement(element)) return new DesignOptionSet(element);
            break;
#if !REVIT_2021
          case ARDB.BuiltInCategory.OST_IOS_GeoSite:
            if (InternalOrigin.IsValidElement(element)) return new InternalOrigin(element);
            if (BasePoint.IsValidElement(element)) return new BasePoint(element as ARDB.BasePoint);
            break;
#endif
          case ARDB.BuiltInCategory.OST_VolumeOfInterest:
            if (ScopeBox.IsValidElement(element)) return new ScopeBox(element);
            break;

          case ARDB.BuiltInCategory.OST_SectionBox:
            if (SectionBox.IsValidElement(element)) return new SectionBox(element);
            break;

          case ARDB.BuiltInCategory.OST_PropertySet:
            if (element is ARDB.PropertySetElement pset)
            {
              if (StructuralAssetElement.IsValidElement(element)) return new StructuralAssetElement(pset);
              else if (ThermalAssetElement.IsValidElement(element)) return new ThermalAssetElement(pset);
            }
            break;
        }
      }

      // By Features
      if (GraphicalElement.IsValidElement(element))
      {
        if (InstanceElement.IsValidElement(element))
          return new InstanceElement(element);

        if (GeometricElement.IsValidElement(element))
          return new GeometricElement(element);

        return new GraphicalElement(element);
      }

      return new Element(element);
    }

    public static Element FromElementId(ARDB.Document doc, ARDB.ElementId id)
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

    public static Element FromLinkElementId(ARDB.Document doc, ARDB.LinkElementId id)
    {
      if (id.HostElementId != ARDB.ElementId.InvalidElementId)
        return FromElementId(doc, id.HostElementId);

      if (doc.GetElement(id.LinkInstanceId) is ARDB.RevitLinkInstance link)
      {
        if (FromElement(link.GetLinkDocument()?.GetElement(id.LinkedElementId)) is Element element)
        {
          using (var linkedElementReference = ARDB.Reference.ParseFromStableRepresentation(element.Document, element.UniqueID))
          {
            using (var elementReference = linkedElementReference.CreateLinkReference(link))
            {
              element.DocumentGUID = doc.GetFingerprintGUID();
              element.UniqueID = elementReference.ConvertToStableRepresentation(doc);
              element._ReferenceDocument = doc;
              element._ReferenceId = link.Id;
              return element;
            }
          }
        }
      }

      return default;
    }

    public static Element FromReference(ARDB.Document doc, ARDB.Reference reference)
    {
      // We call FromLinkElementId to truncate the geometry part of the reference and create a stable UniqueId if is linked.
      return FromLinkElementId
      (
          doc,
          reference.LinkedElementId != ARDB.ElementId.InvalidElementId ?
          new ARDB.LinkElementId(reference.ElementId, reference.LinkedElementId) :
          new ARDB.LinkElementId(reference.ElementId)
      );
    }

    protected internal void SetValue(ARDB.Document doc, ARDB.ElementId id)
    {
      if (id == ARDB.ElementId.InvalidElementId)
        doc = null;

      Document = _ReferenceDocument = doc;
      DocumentGUID = doc.GetFingerprintGUID();

      _Id = _ReferenceId = id;
      if (doc is object && id is object)
      {
        UniqueID = id.IsBuiltInId() ?
          ERDB.UniqueId.Format(ARDB.ExportUtils.GetGBXMLDocumentId(doc), id.IntegerValue) :
          doc.GetElement(id)?.UniqueId ?? string.Empty;
      }
      else UniqueID = string.Empty;
    }

    protected virtual bool SetValue(ARDB.Element element)
    {
      if (ValueType.IsInstanceOfType(element))
      {
        DocumentGUID = Document.GetFingerprintGUID();
        UniqueID = element.UniqueId;
        Document = _ReferenceDocument = element.Document;
        _Id = _ReferenceId = element.Id;
        base.Value = element;
        return true;
      }

      return false;
    }

    public Element() { }
    internal Element(ARDB.Document doc, ARDB.ElementId id) => SetValue(doc, id);
    protected Element(ARDB.Element element) : base(element?.Document, element)
    {
      DocumentGUID = Document.GetFingerprintGUID();

      if (element is object)
      {
        UniqueID = element.UniqueId;
        _ReferenceDocument = element.Document;
        _ReferenceId = _Id = element.Id;
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

          if (id.IsBuiltInId())
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
        if (ERDB.FullUniqueId.TryParse(uniqueid, out var documentId, out var uniqueId))
        {
          if (Types.Document.TryGetDocument(documentId, out var doc))
          {
            try { source = doc.GetElement(uniqueId); }
            catch { }
          }
        }
      }

      if (source is ARDB.Element value)
        return SetValue(value);

      return false;
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo(out target))
        return true;

      var element = Value;
      if (typeof(ARDB.Element).IsAssignableFrom(typeof(Q)))
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

      if (typeof(Q).IsAssignableFrom(typeof(IGH_ElementType)))
      {
        target = (Q) (object) Type;
        return true;
      }

      if (element is null)
        return false;

      if (element.Category?.HasMaterialQuantities ?? false)
      {
        if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
        {
          using (var options = new ARDB.Options() { DetailLevel = ARDB.ViewDetailLevel.Fine })
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

    [TypeConverter(typeof(Proxy.ObjectConverter))]
    protected new class Proxy : Reference.Proxy
    {
      protected new Element owner => base.owner as Element;

      public Proxy(Element e) : base(e) { (this as IGH_GooProxy).UserString = FormatInstance(); }

      public override string FormatInstance()
      {
        return owner.DisplayName;
      }

      protected virtual bool IsValidId(ARDB.Document doc, ARDB.ElementId id) =>
        owner.GetType() == Element.FromElementId(doc, id).GetType();

      [Description("Element is built in Revit.")]
      public bool IsBuiltIn => owner.IsReferencedData && owner.Id.IsBuiltInId();

      [Description("The element identifier in this session.")]
      [RefreshProperties(RefreshProperties.All)]
      public virtual int? Id
      {
        get => owner.Id?.ToValue();
      }

      [Description("A human readable name for the Element.")]
      public string Name => owner.Nomen;

      class ObjectConverter : ExpandableObjectConverter
      {
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
          var properties = base.GetProperties(context, value, attributes);
          if (value is Proxy proxy && proxy.Valid)
          {
            var element = proxy.owner.Document?.GetElement(proxy.owner.Id);
            if (element is object)
            {
              var parameters = element.GetParameters(External.DB.ParameterClass.Any).
                Select(p => new ParameterPropertyDescriptor(p)).
                ToArray();

              var descriptors = new PropertyDescriptor[properties.Count + parameters.Length];
              properties.CopyTo(descriptors, 0);
              parameters.CopyTo(descriptors, properties.Count);

              return new PropertyDescriptorCollection(descriptors, true);
            }
          }

          return properties;
        }
      }

      private class ParameterPropertyDescriptor : PropertyDescriptor
      {
        readonly ARDB.Parameter parameter;
        public ParameterPropertyDescriptor(ARDB.Parameter p) : base(p.Definition?.Name ?? p.Id.ToValue().ToString(), null) { parameter = p; }
        public override Type ComponentType => typeof(Proxy);
        public override bool IsReadOnly => true;
        public override string Name => parameter.Definition?.Name ?? string.Empty;
        public override string Category => parameter.Definition is null ? string.Empty : ARDB.LabelUtils.GetLabelFor(parameter.Definition.ParameterGroup);
        public override string Description
        {
          get
          {
            var description = string.Empty;
            if (parameter.Definition is ARDB.Definition definition)
            {
              External.DB.Schemas.DataType dataType = definition.GetDataType();
              description = dataType.Label.ToLower();

              if (string.IsNullOrEmpty(description))
                description = parameter.StorageType.ToString();
            }

            string parameterClass = "Unknown";
            if (parameter.Id.IsBuiltInId()) parameterClass = "Built-in";
            else if (parameter.IsShared) parameterClass = "Shared";
            else parameterClass = "Project";

            if (parameter.IsReadOnly)
              description = "read only " + description;

            description = $"{parameterClass} {description} parameter.{Environment.NewLine}{Environment.NewLine}";
            description += $"ParameterId : {((External.DB.Schemas.ParameterId) parameter.GetTypeId()).FullName}";

            if (parameter.Id.TryGetBuiltInParameter(out var builtInParameter))
              description += $"{Environment.NewLine}BuiltInParameter : {builtInParameter.ToStringGeneric()}";

            return description;
          }
        }
        public override bool Equals(object obj)
        {
          if (obj is ParameterPropertyDescriptor other)
            return other.parameter.Id == parameter.Id;

          return false;
        }
        public override int GetHashCode() => parameter.Id.GetHashCode();
        public override bool ShouldSerializeValue(object component) { return false; }
        public override void ResetValue(object component) { }
        public override bool CanResetValue(object component) { return false; }
        public override void SetValue(object component, object value) { }
        public override Type PropertyType => typeof(string);
        public override object GetValue(object component)
        {
          if (parameter.Element is object && parameter.Definition is object)
          {
            if (parameter.StorageType == ARDB.StorageType.String)
              return parameter.AsString();

            return parameter.Element.GetParameterFormatOptions(parameter.Id) is ARDB.FormatOptions options ?
              parameter.AsValueString(options) :
              parameter.AsValueString();
          }

          return null;
        }
      }
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);

    #region Version
    public Guid? ExportID => Document?.GetExportID(Id);

    public (Guid? Created, Guid? Updated) Version
    {
      get
      {
        var created = default(Guid?);
        var updated = default(Guid?);

        if (Value is ARDB.Element element)
        {
          if (ERDB.UniqueId.TryParse(element.UniqueId, out var episode, out var _) && episode != default)
            created = episode;

#if REVIT_2021
          updated = element.VersionGuid;
#endif
        }

        return (created, updated);
      }
    }
    #endregion

    #region Properties
    public bool CanDelete => IsValid && ARDB.DocumentValidation.CanDeleteElement(Document, Id);

    public bool? Pinned
    {
      get => Value?.Pinned;
      set
      {
        if (value.HasValue && Value is ARDB.Element element && element.Pinned != value.Value)
          element.Pinned = value.Value;
      }
    }

    public virtual bool CanBeRenominated() => Value.CanBeRenominated();

    public virtual string NextIncrementalNomen(string prefix)
    {
      if (Value is ARDB.Element element)
      {
        var categoryId = element.Category is ARDB.Category category &&
          category.Id.TryGetBuiltInCategory(out var builtInCategory) ?
          builtInCategory : default(ARDB.BuiltInCategory?);

        var nextName = element.Document.NextIncrementalNomen
        (
          prefix,
          element.GetType(),
          element is ARDB.ElementType type ? type.GetFamilyName() : default,
          categoryId
        );

        return nextName;
      }

      return default;
    }

    public bool SetIncrementalNomen(string prefix)
    {
      if (!ARDB.NamingUtils.IsValidName(prefix))
        throw new Exceptions.RuntimeArgumentException(nameof(prefix), "Element name contains prohibited characters and is invalid.");

      if (Value is ARDB.Element)
      {
        var prefixed = DocumentExtension.TryParseNomenId(Nomen, out var elementPrefix, out var _);
        if (!prefixed || prefix != elementPrefix)
        {
          if (NextIncrementalNomen(prefix) is string next)
          {
            Nomen = next;
            return true;
          }
        }
      }

      return false;
    }

    public bool SetUniqueNomen(string name)
    {
      if (!ARDB.NamingUtils.IsValidName(name))
        throw new Exceptions.RuntimeArgumentException(nameof(name), "Element name contains prohibited characters and is invalid.");

      if (Value is ARDB.Element element)
      {
        if (name?.EndsWith(")") == true)
        {
          var start = name.LastIndexOf('(');
          if (start >= 0)
          {
            var uniqueId = name.Substring(start + 1, name.Length - start - 2);
            if (ERDB.UniqueId.TryParse(uniqueId, out var _, out var _))
              name = name.Substring(0, Math.Max(0, start - 1));
          }
        }

        element.SetElementNomen
        (
          string.IsNullOrEmpty(name) ? $"({element.UniqueId})" : $"{name} ({element.UniqueId})"
        );
        return true;
      }

      return false;
    }

    public virtual string Nomen
    {
      get => Rhinoceros.InvokeInHostContext(() => Value?.GetElementNomen());
      set
      {
        if (value is object && value != Nomen)
        {
          if (Id.IsBuiltInId())
          {
            throw new Exceptions.RuntimeErrorException($"BuiltIn {((IGH_Goo) this).TypeName.ToLowerInvariant()} '{DisplayName}' does not support assignment of a user-specified name.");
          }
          else if (Value is ARDB.Element element)
          {
            element.SetElementNomen(value);
          }
        }
      }
    }

    #region Category
    public virtual Category Category
    {
      get => Value is object ?
        Value.Category is ARDB.Category category ?
        Category.FromCategory(category) :
        new Category() :
        default;

      set => throw new Exceptions.RuntimeErrorException($"{((IGH_Goo) this).TypeName} '{DisplayName}' does not support assignment of a Category.");
    }
    #endregion

    public virtual ElementType Type
    {
      get => ElementType.FromElementId(Document, Value?.GetTypeId()) as ElementType;
      set
      {
        if (value is object && Value is ARDB.Element element)
        {
          AssertValidDocument(value, nameof(Type));
          InvalidateGraphics();

          element.ChangeTypeId(value.Id);
        }
      }
    }

    public Workset Workset
    {
      get => new Workset(Document, Document?.GetWorksetId(Id) ?? ARDB.WorksetId.InvalidWorksetId);
      set
      {
        if (value is object && Value is ARDB.Element element)
        {
          AssertValidDocument(value, nameof(Workset));
          element.get_Parameter(ARDB.BuiltInParameter.ELEM_PARTITION_PARAM).Update(value.Id.IntegerValue);
        }
      }
    }

    public Phase CreatedPhase
    {
      get => Value is ARDB.Element element && element.HasPhases() ? new Phase(element.Document, element.CreatedPhaseId) : default;
      set
      {
        if (value is object && Value is ARDB.Element element && element.HasPhases())
        {
          AssertValidDocument(value, nameof(CreatedPhase));
          if (element.CreatedPhaseId != value.Id)
            element.CreatedPhaseId = value.Id;
        }
      }
    }

    public Phase DemolishedPhase
    {
      get => Value is ARDB.Element element && element.HasPhases() ? new Phase(element.Document, element.DemolishedPhaseId) : default;
      set
      {
        if (value is object && Value is ARDB.Element element && element.HasPhases())
        {
          AssertValidDocument(value, nameof(DemolishedPhase));
          if (element.DemolishedPhaseId != value.Id)
            element.DemolishedPhaseId = value.Id;
        }
      }
    }
    #endregion

    #region Identity Data
    public virtual string Description
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_DESCRIPTION)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_DESCRIPTION)?.Update(value);
      }
    }

    public string Comments
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Update(value);
      }
    }

    public string Manufacturer
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MANUFACTURER)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MANUFACTURER)?.Update(value);
      }
    }

    public string Model
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MODEL)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MODEL)?.Update(value);
      }
    }

    public double? Cost
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_COST)?.AsDouble();
      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_COST)?.Update(value.Value);
      }
    }

    public string Url
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_URL)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_URL)?.Update(value);
      }
    }

    public string Keynote
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.KEYNOTE_PARAM)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.KEYNOTE_PARAM)?.Update(value);
      }
    }

    public virtual string Mark
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MARK) is ARDB.Parameter parameter &&
        parameter.HasValue ?
        parameter.AsString() :
        default;

      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MARK)?.Update(value);
      }
    }
    #endregion
  }
}

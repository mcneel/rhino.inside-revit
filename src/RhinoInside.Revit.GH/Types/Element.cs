using System;
using System.Collections.Generic;
using System.Diagnostics;
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
  public partial class Element : Reference, IGH_Element
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
    public override string DisplayName => Nomen ?? (IsReferencedData ? string.Empty : "<None>");

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

    protected override void ResetValue()
    {
      SubInvalidateGraphics();
      base.ResetValue();
    }

    protected override object FetchValue()
    {
      LoadReferencedData();
      return Document?.GetElement(Id);
    }

    protected void SetValue(ARDB.Document doc, ARDB.ElementId id)
    {
      ResetValue();

      if (!id.IsValid())
        doc = null;

      _ReferenceDocument = Document = doc;
      _ReferenceId = _Id = id ?? ARDB.ElementId.InvalidElementId;

      if (Document is object)
      {
        ReferenceDocumentId = _ReferenceDocument.GetFingerprintGUID();

        if (_Id.IsBuiltInId())
        {
          ReferenceUniqueId = ERDB.UniqueId.Format(ARDB.ExportUtils.GetGBXMLDocumentId(Document), _Id.ToValue());
          base.Value = null;
        }
        else
        {
          var element = Document.GetElement(_Id);
          ReferenceUniqueId = element?.UniqueId ?? string.Empty;
          base.Value = element;
        }
      }
    }

    protected virtual bool SetValue(ARDB.Element element)
    {
      if (ValueType.IsInstanceOfType(element))
      {
        _ReferenceDocument = Document = element.Document;
        _ReferenceId = _Id = element.Id;

        ReferenceDocumentId = _ReferenceDocument.GetFingerprintGUID();
        ReferenceUniqueId = element.UniqueId;

        base.Value = element;
        return true;
      }

      return false;
    }
    #endregion

    #region ReferenceObject
    public override bool? IsEditable => IsValid ? !Document.IsLinked && CanDelete : default(bool?);
    #endregion

    #region Reference
    ARDB.ElementId _Id = ARDB.ElementId.InvalidElementId;
    public override ARDB.ElementId Id => _Id;

    ARDB.Document _ReferenceDocument;
    public override ARDB.Document ReferenceDocument => _ReferenceDocument?.IsValidObject == true ? _ReferenceDocument : null;

    public override ARDB.Reference GetReference()
    {
      if (ReferenceDocument is object)
      {
        try { return ReferenceExtension.ParseFromPersistentRepresentation(ReferenceDocument, ReferenceUniqueId); }
        catch (FormatException) { return null; }
      }

      Debug.Assert(string.IsNullOrEmpty(ReferenceUniqueId));
      return null;
    }

    static readonly ARDB.Transform IdentityTransform = ARDB.Transform.Identity;
    internal ARDB.Transform GetReferenceTransform()
    {
      // TODO : Keep the transform for preview and other purposes.
      return IsLinked ?
        (ReferenceDocument.GetElement(ReferenceId) as ARDB.RevitLinkInstance).GetTransform() :
        IdentityTransform;
    }

    ARDB.ElementId _ReferenceId = ARDB.ElementId.InvalidElementId;
    public override ARDB.ElementId ReferenceId => _ReferenceId;

    public string UniqueId =>
      Document is ARDB.Document document && ERDB.ReferenceId.TryParse(ReferenceUniqueId, out var referenceId, ReferenceDocument) ?
      referenceId.Element.ToString(document) : default;
    #endregion

    #region IGH_ReferencedData
    public override bool IsReferencedDataLoaded => _ReferenceDocument is object && _ReferenceId is object && _Id is object;

    public sealed override bool LoadReferencedData()
    {
      if (IsReferencedData)
      {
        if (!IsReferencedDataLoaded)
        {
          UnloadReferencedData();

          if (Types.Document.TryGetDocument(ReferenceDocumentId, out _ReferenceDocument))
          {
            if (_ReferenceDocument.TryGetLinkElementId(ReferenceUniqueId, out var linkElementId))
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

        return _Id.IsValid();
      }

      return false;
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
#if REVIT_2020
      { typeof(ARDB.ImageInstance),                   (element)=> new ImageInstance         (element as ARDB.ImageInstance)     },
#endif
      { typeof(ARDB.ImageType),                       (element)=> new ImageType             (element as ARDB.ImageType)         },

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
      { typeof(ARDB.ElevationMarker),                 (element)=> new ElevationMarker       (element as ARDB.ElevationMarker)   },

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
      { typeof(ARDB.WallSweep),                       (element)=> new WallSweep             (element as ARDB.WallSweep)         },
      { typeof(ARDB.WallFoundation),                  (element)=> new WallFoundation        (element as ARDB.WallFoundation)    },
      { typeof(ARDB.FamilyInstance),                  (element)=> new FamilyInstance        (element as ARDB.FamilyInstance)    },
      { typeof(ARDB.Panel),                           (element)=> new Panel                 (element as ARDB.Panel)             },
      { typeof(ARDB.PanelType),                       (element)=> new PanelType             (element as ARDB.PanelType)         },
      { typeof(ARDB.Mullion),                         (element)=> new Mullion               (element as ARDB.Mullion)           },
      { typeof(ARDB.MullionType),                     (element)=> new MullionType           (element as ARDB.MullionType)       },

      { typeof(ARDB.TextElement),                     (element)=> new TextElement           (element as ARDB.TextElement)       },
      { typeof(ARDB.TextElementType),                 (element)=> new TextElementType       (element as ARDB.TextElementType)   },
      { typeof(ARDB.Dimension),                       (element)=> new Dimension             (element as ARDB.Dimension)         },
      { typeof(ARDB.DimensionType),                   (element)=> new DimensionType         (element as ARDB.DimensionType)     },
      { typeof(ARDB.SpotDimension),                   (element)=> new SpotDimension         (element as ARDB.SpotDimension)     },
      { typeof(ARDB.FilledRegion),                    (element)=> new FilledRegion          (element as ARDB.FilledRegion)      },
      { typeof(ARDB.FilledRegionType),                (element)=> new FilledRegionType      (element as ARDB.FilledRegionType)  },
      { typeof(ARDB.Revision),                        (element)=> new Revision              (element as ARDB.Revision)          },
      { typeof(ARDB.RevisionCloud),                   (element)=> new RevisionCloud         (element as ARDB.RevisionCloud)     },
      { typeof(ARDB.AnnotationSymbol),                (element)=> new AnnotationSymbol      (element as ARDB.AnnotationSymbol)  },
      { typeof(ARDB.IndependentTag),                  (element)=> new IndependentTag        (element as ARDB.IndependentTag)    },
      
      { typeof(ARDB.AssemblyInstance),                (element)=> new AssemblyInstance      (element as ARDB.AssemblyInstance)  },

      { typeof(ARDB.SpatialElement),                  (element)=> new SpatialElement        (element as ARDB.SpatialElement)    },
      { typeof(ARDB.Area),                            (element)=> new AreaElement           (element as ARDB.Area)              },
      { typeof(ARDB.Architecture.Room),               (element)=> new RoomElement           (element as ARDB.Architecture.Room) },
      { typeof(ARDB.Mechanical.Space),                (element)=> new SpaceElement          (element as ARDB.Mechanical.Space)  },

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

      // Overrides
      switch(element)
      {
        case ARDB.FamilyInstance familyInstance:
          switch(familyInstance.StructuralType)
          {
            case ARDB.Structure.StructuralType.Beam: return new StructuralBeam(familyInstance);
            case ARDB.Structure.StructuralType.Brace: return new StructuralBrace(familyInstance);
            case ARDB.Structure.StructuralType.Column: return new StructuralColumn(familyInstance);
            case ARDB.Structure.StructuralType.Footing: return new StructuralFooting(familyInstance);
            case ARDB.Structure.StructuralType.UnknownFraming: return new StructuralFraming(familyInstance);
          }
          if (Panel.IsValidElement(element)) return new Panel(familyInstance);
          break;

        case ARDB.FamilySymbol familySymbol:
          if (PanelType.IsValidElement(element)) return new PanelType(familySymbol);
          if (ProfileType.IsValidElement(element)) return new ProfileType(familySymbol);
          break;

        case ARDB.ElementType elementType:
          if (ArrowheadType.IsValidElement(elementType)) return new ArrowheadType(elementType);
          break;
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

      // By Type
      for (var type = element.GetType(); type != typeof(ARDB.Element); type = type.BaseType)
      {
        if (ActivatorDictionary.TryGetValue(type, out var activator))
          return activator(element);
      }

      // By Category
      if (element.Category is null)
      {
        if (Viewer.IsValidElement(element))                                   return new Viewer(element);
        if (DocumentExtension.AsCategory(element) is ARDB.Category category)  return new Category(category);
      }
      else if (element.Category.Id.TryGetBuiltInCategory(out var bic))
      {
        switch (bic)
        {
          case ARDB.BuiltInCategory.OST_DesignOptionSets:
            if (DesignOptionSet.IsValidElement(element)) return new DesignOptionSet(element);
            break;

          case ARDB.BuiltInCategory.OST_PropertySet:
            if (element is ARDB.PropertySetElement pset)
            {
              if (StructuralAssetElement.IsValidElement(element)) return new StructuralAssetElement(pset);
              if (ThermalAssetElement.IsValidElement(element)) return new ThermalAssetElement(pset);
            }
            break;

          case ARDB.BuiltInCategory.OST_VolumeOfInterest:
            if (ScopeBox.IsValidElement(element)) return new ScopeBox(element);
            break;

          case ARDB.BuiltInCategory.OST_SectionBox:
            if (SectionBox.IsValidElement(element)) return new SectionBox(element);
            break;

          case ARDB.BuiltInCategory.OST_Viewers:
          case ARDB.BuiltInCategory.OST_Cameras:
            if (Viewer.IsValidElement(element)) return new Viewer(element);
            break;

#if !REVIT_2021
          case ARDB.BuiltInCategory.OST_IOS_GeoSite:
            if (InternalOrigin.IsValidElement(element)) return new InternalOrigin(element);
            if (BasePoint.IsValidElement(element)) return new BasePoint(element as ARDB.BasePoint);
            break;
#endif

#if !REVIT_2020
          case ARDB.BuiltInCategory.OST_RasterImages:
            if (ImageInstance.IsValidElement(element)) return new ImageInstance(element);
            break;
#endif
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
          using (var linkedElementReference = ARDB.Reference.ParseFromStableRepresentation(element.Document, element.ReferenceUniqueId))
          {
            using (var elementReference = linkedElementReference.CreateLinkReference(link))
            {
              element.ReferenceDocumentId = doc.GetFingerprintGUID();
              element.ReferenceUniqueId = elementReference.ConvertToPersistentRepresentation(doc);
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
      // We call FromLinkElementId to truncate the geometry part of the reference and create a persistent UniqueId if is linked.
      return FromLinkElementId
      (
          doc,
          reference.LinkedElementId != ARDB.ElementId.InvalidElementId ?
          new ARDB.LinkElementId(reference.ElementId, reference.LinkedElementId) :
          new ARDB.LinkElementId(reference.ElementId)
      );
    }

    protected internal T GetElement<T>(ARDB.ElementId elementId) where T : Element
    {
      if (elementId.IsValid())
      {
        return (T)
          (IsLinked?
          Element.FromLinkElementId(ReferenceDocument, new ARDB.LinkElementId(ReferenceId, elementId)) :
          Element.FromElementId(Document, elementId));
      }

      return null;
    }

    protected internal T GetElement<T>(ARDB.Element element) where T : Element
    {
      if (element.IsValid())
      {
        if (!Document.IsEquivalent(element.Document))
          throw new Exceptions.RuntimeArgumentException($"Invalid {typeof(T)} Document", nameof(element));

        return (T)
          (IsLinked ?
          Element.FromLinkElementId(ReferenceDocument, new ARDB.LinkElementId(ReferenceId, element.Id)) :
          Element.FromElement(element));
      }

      return null;
    }

    protected T SetElement<T>(Element element) where T : ARDB.Element
    {
      if (element?.IsValid is true)
      {
        if (!Document.IsEquivalent(element.Document))
          throw new Exceptions.RuntimeArgumentException($"Invalid {typeof(T)} Document", nameof(element));

        return (T) element.Value;
      }

      return null;
    }

    public Element() { }
    internal Element(ARDB.Document doc, ARDB.ElementId id) => SetValue(doc, id);
    protected Element(ARDB.Element element) : base(element?.Document, element)
    {
      if (element is object)
      {
        _ReferenceDocument  = Document;
        _ReferenceId        = _Id       = element.Id;

        ReferenceDocumentId = Document.GetFingerprintGUID();
        ReferenceUniqueId   = element.UniqueId;

        Debug.Assert(ReferenceEquals(base.Value, element));
      }
    }

    public override bool CastFrom(object source)
    {
      // Hack to force `ParameterValue.CastTo` to be called.
      if (source is ParameterValue)
        return false;

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

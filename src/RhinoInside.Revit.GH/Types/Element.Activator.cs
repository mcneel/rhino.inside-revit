using System;
using System.Collections.Generic;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  public partial class Element
  {
    public static Element FromValue(ARDB.Document doc, object data)
    {
      try
      {
        switch (data)
        {
          case string s:

            if (ERDB.FullUniqueId.TryParse(s, out var documentId, out var stableId))
            {
              if (documentId != doc.GetPersistentGUID()) return null;
              s = stableId;
            }

            if (doc.TryGetLinkElementId(s, out var linkElementId))
              return FromLinkElementId(doc, linkElementId);

            return default;

          case ARDB.BuiltInCategory c: return FromElementId(doc, new ARDB.ElementId(c));
          case ARDB.BuiltInParameter p: return FromElementId(doc, new ARDB.ElementId(p));
          case ARDB.ElementId id: return FromElementId(doc, id);
          case ARDB.LinkElementId id: return FromLinkElementId(doc, id);
          case ARDB.Reference r: return FromReference(doc, r);
          case ARDB.Element element: return doc.IsEquivalent(element.Document) ? FromElement(element) : null;
          case ARDB.Category category: return doc.IsEquivalent(category.Document()) ? new Category(category) : null;
#if REVIT_2024
          case Int64 id: return FromElementId(doc, new ARDB.ElementId(id));
          case IConvertible convertible: return FromElementId(doc, new ARDB.ElementId(System.Convert.ToInt64(convertible)));
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
      switch (element)
      {
        case ARDB.FamilyInstance familyInstance:
          switch (familyInstance.StructuralType)
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

        case ARDB.View view:
          switch (view.ViewType)
          {
            case ARDB.ViewType.FloorPlan: return new FloorPlan(view as ARDB.ViewPlan);
            case ARDB.ViewType.CeilingPlan: return new CeilingPlan(view as ARDB.ViewPlan);
            case ARDB.ViewType.AreaPlan: return new AreaPlan(view as ARDB.ViewPlan);
            case ARDB.ViewType.EngineeringPlan: return new StructuralPlan(view as ARDB.ViewPlan);

            case ARDB.ViewType.Section: return new SectionView(view as ARDB.ViewSection);
            case ARDB.ViewType.Elevation: return new ElevationView(view as ARDB.ViewSection);
            case ARDB.ViewType.Detail: return new DetailView(view as ARDB.ViewSection);
          }

          break;
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
        if (Viewer.IsValidElement(element)) return new Viewer(element);
        if (DocumentExtension.AsCategory(element) is ARDB.Category category) return new Category(category);
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
              element.ReferenceDocumentId = doc.GetPersistentGUID();
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

    static readonly Dictionary<Type, Func<ARDB.Element, Element>> ActivatorDictionary = new Dictionary<Type, Func<ARDB.Element, Element>>()
    {
#if REVIT_2024
      { typeof(ARDB.Toposolid),                       (element)=> new Toposolid             (element as ARDB.Toposolid)         },
#endif
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
      { typeof(ARDB.GeomCombination),                 (element)=> new GeomCombination       (element as ARDB.GeomCombination)   },
      { typeof(ARDB.GenericForm),                     (element)=> new GenericForm           (element as ARDB.GenericForm)       },

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
      { typeof(ARDB.AreaScheme),                      (element)=> new AreaScheme            (element as ARDB.AreaScheme)        },
      { typeof(ARDB.Architecture.Room),               (element)=> new RoomElement           (element as ARDB.Architecture.Room) },
      { typeof(ARDB.Mechanical.Space),                (element)=> new SpaceElement          (element as ARDB.Mechanical.Space)  },

      { typeof(ARDB.AreaTag),                         (element)=> new AreaElementTag        (element as ARDB.AreaTag)              },
      { typeof(ARDB.Architecture.RoomTag),            (element)=> new RoomElementTag        (element as ARDB.Architecture.RoomTag) },
      { typeof(ARDB.Mechanical.SpaceTag),             (element)=> new SpaceElementTag       (element as ARDB.Mechanical.SpaceTag)  },

      { typeof(ARDB.Architecture.TopographySurface),  (element)=> new TopographySurface     (element as ARDB.Architecture.TopographySurface) },
      { typeof(ARDB.Architecture.BuildingPad),        (element)=> new BuildingPad           (element as ARDB.Architecture.BuildingPad) },
      { typeof(ARDB.Architecture.Railing),            (element)=> new Railing               (element as ARDB.Architecture.Railing) },
    };
  }
}

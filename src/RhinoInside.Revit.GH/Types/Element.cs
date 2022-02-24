using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Display;
  using External.DB;
  using External.DB.Extensions;
  using External.UI.Extensions;

  [Kernel.Attributes.Name("Element")]
  public interface IGH_Element : IGH_ElementId
  {
    ElementType Type { get; set; }
  }

  [Kernel.Attributes.Name("Element")]
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

    protected virtual Type ValueType => typeof(ARDB.Element);
    #endregion

    #region DocumentObject
    ARDB.Element value => base.Value as ARDB.Element;
    public new ARDB.Element Value
    {
      get
      {
        if (value?.IsValidObject == false)
          ResetValue();

        return value;
      }
    }

    public override string DisplayName => Nomen ?? (IsReferencedData ? string.Empty : "<None>");
    #endregion

    #region ReferenceObject
    ARDB.ElementId id = ARDB.ElementId.InvalidElementId;
    public override ARDB.ElementId Id => id;
    #endregion

    #region IGH_ElementId
    public override ARDB.Reference Reference
    {
      get
      {
        try { return ARDB.Reference.ParseFromStableRepresentation(Document, UniqueID); }
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
        case ARDB.Category category: return new Category(category);
        case ARDB.Element element: return Element.FromElement(element);
      }

      return null;
    }

    public static readonly Dictionary<Type, Func<ARDB.Element, Element>> ActivatorDictionary = new Dictionary<Type, Func<ARDB.Element, Element>>()
    {
#if REVIT_2021
      { typeof(ARDB.InternalOrigin),          (element)=> new InternalOrigin        (element as ARDB.InternalOrigin)    },
      { typeof(ARDB.BasePoint),               (element)=> new BasePoint             (element as ARDB.BasePoint)         },
#endif
      { typeof(ARDB.DesignOption),            (element)=> new DesignOption          (element as ARDB.DesignOption)      },
      { typeof(ARDB.Phase),                   (element)=> new Phase                 (element as ARDB.Phase)             },
      { typeof(ARDB.SelectionFilterElement),  (element)=> new SelectionFilterElement(element as ARDB.SelectionFilterElement)},
      { typeof(ARDB.ParameterFilterElement),  (element)=> new ParameterFilterElement(element as ARDB.ParameterFilterElement)},
      { typeof(ARDB.Family),                  (element)=> new Family                (element as ARDB.Family)            },
      { typeof(ARDB.ElementType),             (element)=> new ElementType           (element as ARDB.ElementType)       },
      { typeof(ARDB.FamilySymbol),            (element)=> new FamilySymbol          (element as ARDB.FamilySymbol)      },
      { typeof(ARDB.HostObjAttributes),       (element)=> new HostObjectType        (element as ARDB.HostObjAttributes) },
      { typeof(ARDB.MEPCurveType),            (element)=> new MEPCurveType          (element as ARDB.MEPCurveType)      },
      { typeof(ARDB.ParameterElement),        (element)=> new ParameterKey          (element as ARDB.ParameterElement)  },
      { typeof(ARDB.Material),                (element)=> new Material              (element as ARDB.Material)          },
      { typeof(ARDB.GraphicsStyle),           (element)=> new GraphicsStyle         (element as ARDB.GraphicsStyle)     },
      { typeof(ARDB.LinePatternElement),      (element)=> new LinePatternElement    (element as ARDB.LinePatternElement)},
      { typeof(ARDB.FillPatternElement),      (element)=> new FillPatternElement    (element as ARDB.FillPatternElement)},
      { typeof(ARDB.AppearanceAssetElement),  (element)=> new AppearanceAssetElement(element as ARDB.AppearanceAssetElement)},

      { typeof(ARDB.View),                    (element)=> new View                  (element as ARDB.View)              },
      { typeof(ARDB.ViewFamilyType),          (element)=> new ViewFamilyType        (element as ARDB.ViewFamilyType)    },
      { typeof(ARDB.ViewSheet),               (element)=> new ViewSheet             (element as ARDB.ViewSheet)         },

      { typeof(ARDB.Instance),                (element)=> new Instance              (element as ARDB.Instance)          },
      { typeof(ARDB.ProjectLocation),         (element)=> new ProjectLocation       (element as ARDB.ProjectLocation)   },
      { typeof(ARDB.SiteLocation),            (element)=> new SiteLocation          (element as ARDB.SiteLocation)      },
      { typeof(ARDB.RevitLinkInstance),       (element)=> new RevitLinkInstance     (element as ARDB.RevitLinkInstance) },
      { typeof(ARDB.ImportInstance),          (element)=> new ImportInstance        (element as ARDB.ImportInstance)    },
      { typeof(ARDB.PointCloudInstance),      (element)=> new PointCloudInstance    (element as ARDB.PointCloudInstance)},

      { typeof(ARDB.DirectShape),             (element)=> new DirectShape           (element as ARDB.DirectShape)       },
      { typeof(ARDB.DirectShapeType),         (element)=> new DirectShapeType       (element as ARDB.DirectShapeType)   },

      { typeof(ARDB.Sketch),                  (element)=> new Sketch                (element as ARDB.Sketch)            },
      { typeof(ARDB.SketchPlane),             (element)=> new SketchPlane           (element as ARDB.SketchPlane)       },
      { typeof(ARDB.DatumPlane),              (element)=> new DatumPlane            (element as ARDB.DatumPlane)        },
      { typeof(ARDB.Level),                   (element)=> new Level                 (element as ARDB.Level)             },
      { typeof(ARDB.Grid),                    (element)=> new Grid                  (element as ARDB.Grid)              },
      { typeof(ARDB.ReferencePlane),          (element)=> new ReferencePlane        (element as ARDB.ReferencePlane)    },
      { typeof(ARDB.SpatialElement),          (element)=> new SpatialElement        (element as ARDB.SpatialElement)    },
      { typeof(ARDB.Group),                   (element)=> new Group                 (element as ARDB.Group)             },
      { typeof(ARDB.Opening),                 (element)=> new Opening               (element as ARDB.Opening)           },
      { typeof(ARDB.HostObject),              (element)=> new HostObject            (element as ARDB.HostObject)        },
      { typeof(ARDB.MEPCurve),                (element)=> new MEPCurve              (element as ARDB.MEPCurve)          },
      { typeof(ARDB.CurtainSystem),           (element)=> new CurtainSystem         (element as ARDB.CurtainSystem)     },
      { typeof(ARDB.CurtainGridLine),         (element)=> new CurtainGridLine       (element as ARDB.CurtainGridLine)   },
      { typeof(ARDB.Floor),                   (element)=> new Floor                 (element as ARDB.Floor)             },
      { typeof(ARDB.Architecture.BuildingPad),(element)=> new BuildingPad           (element as ARDB.Architecture.BuildingPad) },
      { typeof(ARDB.Ceiling),                 (element)=> new Ceiling               (element as ARDB.Ceiling)           },
      { typeof(ARDB.RoofBase),                (element)=> new Roof                  (element as ARDB.RoofBase)          },
      { typeof(ARDB.Wall),                    (element)=> new Wall                  (element as ARDB.Wall)              },
      { typeof(ARDB.FamilyInstance),          (element)=> new FamilyInstance        (element as ARDB.FamilyInstance)    },
      { typeof(ARDB.Panel),                   (element)=> new Panel                 (element as ARDB.Panel)             },
      { typeof(ARDB.Mullion),                 (element)=> new Mullion               (element as ARDB.Mullion)           },
      { typeof(ARDB.Dimension),               (element)=> new Dimension             (element as ARDB.Dimension)         },
      { typeof(ARDB.CurveElement),            (element)=> new CurveElement          (element as ARDB.CurveElement)      },

      { typeof(ARDB.AssemblyInstance),        (element)=> new AssemblyInstance      (element as ARDB.AssemblyInstance)  },
    };

    public static Element FromElement(ARDB.Element element)
    {
      if (!element.IsValid())
        return null;

      for (var type = element.GetType(); type != typeof(ARDB.Element); type = type.BaseType)
      {
        if (ActivatorDictionary.TryGetValue(type, out var activator))
          return activator(element);
      }

      if (element.Category is null)
      {
        if (DocumentExtension.AsCategory(element) is ARDB.Category category)
          return new Category(category);
      }
      else if (element.Category.Id.TryGetBuiltInCategory(out var bic))
      {
        switch (bic)
        {
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

      if (GraphicalElement.IsValidElement(element))
      {
        if (InstanceElement.IsValidElement(element))
        {
          if (Panel.IsValidElement(element)) return new Panel(element as ARDB.FamilyInstance);

          return new InstanceElement(element);
        }
        if (GeometricElement.IsValidElement(element))
          return new GeometricElement(element);

        return new GraphicalElement(element);
      }
      else
      {
        if (DesignOptionSet.IsValidElement(element)) return new DesignOptionSet(element);
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

    public static T FromElementId<T>(ARDB.Document doc, ARDB.ElementId id) where T : Element, new()
    {
      if (doc is null || id is null) return default;
      if (id == ARDB.ElementId.InvalidElementId) return new T();

      return FromElementId(doc, id) as T;
    }

    public static Element FromReference(ARDB.Document doc, ARDB.Reference reference)
    {
      if (doc.GetElement(reference) is ARDB.Element value)
      {
        if (value is ARDB.RevitLinkInstance link)
        {
          if (reference.LinkedElementId != ARDB.ElementId.InvalidElementId)
          {
            var linkedDoc = link.GetLinkDocument();
            return FromValue(linkedDoc?.GetElement(reference.LinkedElementId));
          }
        }

        return FromElement(value);
      }

      return null;
    }

    protected internal void SetValue(ARDB.Document doc, ARDB.ElementId id)
    {
      if (id == ARDB.ElementId.InvalidElementId)
        doc = null;

      Document = doc;
      DocumentGUID = doc.GetFingerprintGUID();

      this.id = id;
      UniqueID = doc?.GetElement(id)?.UniqueId ??
      (
        id.IntegerValue < ARDB.ElementId.InvalidElementId.IntegerValue ?
          UniqueId.Format(Guid.Empty, id.IntegerValue) :
          string.Empty
      );
    }

    protected virtual bool SetValue(ARDB.Element element)
    {
      if (ValueType.IsInstanceOfType(element))
      {
        Document = element.Document;
        DocumentGUID = Document.GetFingerprintGUID();
        id = element.Id;
        UniqueID = element.UniqueId;
        base.Value = element;
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
    internal Element(ARDB.Document doc, ARDB.ElementId id) => SetValue(doc, id);
    protected Element(ARDB.Element element) : base(element?.Document, element)
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

          if (id == ARDB.ElementId.InvalidElementId)
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

      if (source is ARDB.Element value)
        return SetValue(value);

      return false;
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo<Q>(out target))
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
      }

      [System.ComponentModel.Description("A human readable name for the Element.")]
      public string Name => owner.Nomen;
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);

    #region Version
    public Guid? ExportID => Document?.GetExportID(Id);

    public override bool? IsEditable => IsValid ?
      !Id.IsBuiltInId() && (Document?.IsLinked == false) : default(bool?);

    public (Guid? Created, Guid? Updated) Version
    {
      get
      {
        var created = default(Guid?);
        if (UniqueId.TryParse(UniqueID, out var episode, out var _) && episode != default)
          created = episode;

        var updated = default(Guid?);
#if REVIT_2021
        if (Value is ARDB.Element element)
          updated = element.VersionGuid;
#endif

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
            if (UniqueId.TryParse(uniqueId, out var _, out var _))
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

    public Category Category
    {
      get => Value is object ?
        Value.Category is ARDB.Category category ?
        Category.FromCategory(category) :
        new Category() :
        default;
    }

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

    public ARDB.WorksetId WorksetId
    {
      get => Document?.GetWorksetId(Id);
      set => Value?.get_Parameter(ARDB.BuiltInParameter.ELEM_PARTITION_PARAM)?.Update(value.IntegerValue);
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

using System;
using System.Diagnostics;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Display;
  using Convert.Geometry;
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
        ReferenceDocumentId = _ReferenceDocument.GetPersistentGUID();
        ReferenceUniqueId = _Id.ToUniqueId(Document, out var value);

        base.Value = value;
      }
    }

    protected virtual bool SetValue(ARDB.Element element)
    {
      if (ValueType.IsInstanceOfType(element))
      {
        _ReferenceDocument = Document = element.Document;
        _ReferenceId = _Id = element.Id;

        ReferenceDocumentId = _ReferenceDocument.GetPersistentGUID();
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
                using (var link = _ReferenceDocument.GetElement(_ReferenceId) as ARDB.RevitLinkInstance)
                {
                  ReferenceTransform = link.GetTransform().ToTransform();
                  Document = link.GetLinkDocument();
                }
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
        ResetReferenceTransform();
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

        ReferenceDocumentId = Document.GetPersistentGUID();
        ReferenceUniqueId   = element.UniqueId;

        Debug.Assert(ReferenceEquals(base.Value, element));
      }
    }

    public override bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

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

      return SetValue(source as ARDB.Element);
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

    internal ARDB.BuiltInCategory? BuiltInCategory => Value?.Category?.ToBuiltInCategory();

    public virtual Category Category
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

    public bool? HasPhases => Value?.HasPhases();

    public Phase CreatedPhase
    {
      get => Value is ARDB.Element element && element.HasPhases() ? new Phase(element.Document, element.CreatedPhaseId) : default;
      set
      {
        if (value is object && Value is ARDB.Element element && element.HasPhases())
        {
          AssertValidDocument(value, nameof(CreatedPhase));
          if (element.CreatedPhaseId != value.Id)
          {
            if (!element.ArePhasesModifiable())
              throw new Exceptions.RuntimeErrorException($"The element does not allow setting the property '{CreatedPhase}'.");

            element.CreatedPhaseId = value.Id;
          }
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
          {
            if (!element.ArePhasesModifiable())
              throw new Exceptions.RuntimeErrorException($"The element does not allow setting the property '{DemolishedPhase}'.");

            element.DemolishedPhaseId = value.Id;
          }
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

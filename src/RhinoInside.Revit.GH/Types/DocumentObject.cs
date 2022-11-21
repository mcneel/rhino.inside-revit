using System;
using System.ComponentModel;
using System.Reflection;
using Rhino.Geometry;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  /// <summary>
  /// Interface to wrap document related types.
  /// </summary>
  /// <remarks>
  /// For example: <see cref="ARDB.Document"/>
  /// </remarks>
  public interface IGH_DocumentObject : IGH_Goo
  {
    ARDB.Document Document { get; }
    object Value { get; }
  }

  public abstract class DocumentObject : IGH_DocumentObject, IEquatable<DocumentObject>, ICloneable
  {
    #region System.Object
    public bool Equals(DocumentObject other) => other is object &&
      Equals(Document, other.Document) && Equals(Value, other.Value);
    public override bool Equals(object obj) => (obj is DocumentObject id) ? Equals(id) : base.Equals(obj);
    public override int GetHashCode() => Document.GetHashCode() ^ Value.GetHashCode();
    public override string ToString()
    {
      string Invalid = IsValid ? string.Empty : "Invalid ";
      string TypeName = ((IGH_Goo) this).TypeName;
      string InstanceName = DisplayName is string displayName ? $" : {displayName}" : string.Empty;

      return Invalid + TypeName + InstanceName;
    }

    object ICloneable.Clone() => MemberwiseClone();
    #endregion

    #region IGH_Goo
    string IGH_Goo.TypeName
    {
      get
      {
        var type = GetType();
        var name = type.GetTypeInfo().GetCustomAttribute(typeof(Kernel.Attributes.NameAttribute)) as Kernel.Attributes.NameAttribute;
        return $"Revit {name?.Name ?? type.Name}";
      }
    }
    string IGH_Goo.TypeDescription => $"Represents a {((IGH_Goo) this).TypeName.ToLowerInvariant()}";
    public virtual bool IsValid => _Document.IsValid();
    public virtual string IsValidWhyNot => _Document.IsValidWithLog(out var log) ? default : log;
    IGH_Goo IGH_Goo.Duplicate() => (IGH_Goo) (this as ICloneable)?.Clone();
    object IGH_Goo.ScriptVariable() => ScriptVariable();
    public virtual object ScriptVariable() => Value;

    IGH_GooProxy IGH_Goo.EmitProxy() => default;
    public virtual bool CastFrom(object source) => false;
    public virtual bool CastTo<Q>(out Q target)
    {
      if (this is IConvertible)
      {
        if (typeof(Q).IsGenericSubclassOf(typeof(GH_Goo<>), out var genericType))
        {
          var targetType = genericType.GenericTypeArguments[0];

          try
          {
            var value = System.Convert.ChangeType(this, targetType, default);
            if (Activator.CreateInstance<Q>() is IGH_Goo goo && goo.CastFrom(value))
            {
              target = (Q) goo;
              return true;
            }
          }
          catch (InvalidCastException) { }
        }
        else
        {
          try
          {
            target = (Q) System.Convert.ChangeType(this, typeof(Q), default);
            return true;
          }
          catch (InvalidCastException) { }
        }
      }

      target = default;
      return false;
    }

    bool GH_IO.GH_ISerializable.Write(GH_IWriter writer) => Write(writer);
    bool GH_IO.GH_ISerializable.Read(GH_IReader reader) => Read(reader);
    protected virtual bool Write(GH_IWriter writer) => false;
    protected virtual bool Read(GH_IReader reader) => false;
    #endregion

    protected DocumentObject() { }
    protected DocumentObject(ARDB.Document doc, object val) { _Document = doc; _Value = val; }

    ARDB.Document _Document;
    public ARDB.Document Document
    {
      get => _Document?.IsValidObject == true ? _Document : null;
      protected set => _Document = value;
    }

    protected internal bool AssertValidDocument(DocumentObject other, string paramName)
    {
      if (other?.Document is null) return false;
      if (other.Document.Equals(Document)) return true;

      throw new Exceptions.RuntimeArgumentException(paramName, "Invalid Document");
    }

    object _Value;
    public virtual object Value
    {
      get => _Value;
      protected set => _Value = value;
    }

    protected virtual void ResetValue()
    {
      _Document = default;
      _Value = default;
    }

    public abstract string DisplayName { get; }
  }

  /// <summary>
  /// Interface to wrap document related types that can be created-duplicated-updated-deleted without starting a Revit Transaction.
  /// </summary>
  /// <remarks>
  /// For example: <see cref="ARDB.CompoundStructureLayer"/>
  /// </remarks>
  public interface IGH_ValueObject : IGH_DocumentObject
  {

  }

  public abstract class ValueObject : DocumentObject, IGH_ValueObject, IEquatable<ValueObject>
  {
    #region System.Object
    public bool Equals(ValueObject other) => other is object &&
      Equals(Document, other.Document) && Equals(Value, other.Value);
    public override bool Equals(object obj) => (obj is ValueObject id) ? Equals(id) : base.Equals(obj);
    public override int GetHashCode() => Document.GetHashCode() ^ Value.GetHashCode();
    #endregion

    #region IGH_Goo
    public override bool IsValid => base.IsValid && Value is object;
    public override string IsValidWhyNot
    {
      get
      {
        if (base.IsValidWhyNot is string log) return log;

        if (Value is null) return $"Referenced {((IGH_Goo) this).TypeName} was deleted or undone.";

        return default;
      }
    }
    #endregion

    protected ValueObject() { }

    protected ValueObject(ARDB.Document doc, object val) : base(doc, val) { }
  }

  /// <summary>
  /// Interface to wrap document related types that can NOT be created-duplicated-updated-deleted without starting a Revit Transaction.
  /// </summary>
  /// <remarks>
  /// For example: <see cref="ARDB.Element"/>, <see cref="ARDB.Category"/> or, <see cref="ARDB.Workset"/>
  /// </remarks>
  public interface IGH_ReferenceObject : IGH_DocumentObject
  {
    Guid ReferenceDocumentId { get; }
    string ReferenceUniqueId { get; }
  }

  public abstract class ReferenceObject : DocumentObject,
    IEquatable<ReferenceObject>,
    IGH_ReferenceObject,
    IGH_ReferenceData,
    IGH_QuickCast
  {
    protected ReferenceObject() { }

    protected ReferenceObject(ARDB.Document doc, object val) : base(doc, val) { }

    public abstract bool? IsEditable { get; }

    #region System.Object
    public bool Equals(ReferenceObject other) => other is object &&
      Equals(ReferenceDocumentId, other.ReferenceDocumentId) &&
      Equals(ReferenceUniqueId, other.ReferenceUniqueId) &&
      Equals(GetType(), other.GetType());
    public override bool Equals(object obj) => (obj is ReferenceObject id) ? Equals(id) : base.Equals(obj);
    public override int GetHashCode() => ReferenceDocumentId.GetHashCode() ^ ReferenceUniqueId.GetHashCode();
    #endregion

    #region DocumentObject
    public override object Value
    {
      get
      {
        if (base.Value is null && IsReferencedDataLoaded)
          base.Value = FetchValue();

        return base.Value;
      }
      protected set => base.Value = value;
    }

    protected abstract object FetchValue();
    #endregion

    #region GH_ISerializable
    protected override bool Read(GH_IReader reader)
    {
      UnloadReferencedData();

      var documentGUID = Guid.Empty;
      reader.TryGetGuid("DocumentGUID", ref documentGUID);
      ReferenceDocumentId = documentGUID;

      string uniqueID = string.Empty;
      reader.TryGetString("UniqueID", ref uniqueID);
      ReferenceUniqueId = uniqueID;

      return true;
    }

    protected override bool Write(GH_IWriter writer)
    {
      if (ReferenceDocumentId != Guid.Empty)
        writer.SetGuid("DocumentGUID", ReferenceDocumentId);

      if (!string.IsNullOrEmpty(ReferenceUniqueId))
        writer.SetString("UniqueID", ReferenceUniqueId);

      return true;
    }
    #endregion

    #region IGH_ReferenceObject
    public Guid ReferenceDocumentId { get; protected set; } = Guid.Empty;
    public string ReferenceUniqueId { get; protected set; } = string.Empty;
    #endregion

    #region IGH_ReferencedData
    public virtual bool IsReferencedData => ReferenceDocumentId != Guid.Empty;
    public abstract bool IsReferencedDataLoaded { get; }
    public abstract bool LoadReferencedData();
    public virtual void UnloadReferencedData()
    {
      if (!IsReferencedData) return;

      ResetValue();
    }
    #endregion

    #region IGH_QuickCast
    GH_QuickCastType IGH_QuickCast.QC_Type => GH_QuickCastType.text;
    private string QC_Value => External.DB.FullUniqueId.Format(ReferenceDocumentId, ReferenceUniqueId);

    int IGH_QuickCast.QC_Hash() => QC_Value.GetHashCode();

    double IGH_QuickCast.QC_Distance(IGH_QuickCast other) => (this as IGH_QuickCast).QC_CompareTo(other) == 0 ? 0.0 : double.NaN;

    int IGH_QuickCast.QC_CompareTo(IGH_QuickCast other)
    {
      return GetType() == other.GetType() && other is IGH_ReferenceObject otherId ?
        QC_Value.CompareTo(External.DB.FullUniqueId.Format(otherId.ReferenceDocumentId, otherId.ReferenceUniqueId)) :
        -1;
    }

    bool IGH_QuickCast.QC_Bool() => throw new InvalidCastException();
    int IGH_QuickCast.QC_Int() => throw new InvalidCastException();
    double IGH_QuickCast.QC_Num() => throw new InvalidCastException();
    string IGH_QuickCast.QC_Text() => QC_Value;
    System.Drawing.Color IGH_QuickCast.QC_Col() => throw new InvalidCastException();
    Point3d IGH_QuickCast.QC_Pt() => throw new InvalidCastException();
    Vector3d IGH_QuickCast.QC_Vec() => throw new InvalidCastException();
    Complex IGH_QuickCast.QC_Complex() => throw new InvalidCastException();
    Matrix IGH_QuickCast.QC_Matrix() => throw new InvalidCastException();
    Interval IGH_QuickCast.QC_Interval() => throw new InvalidCastException();
    #endregion

    #region Proxy
    protected class Proxy : IGH_GooProxy
    {
      protected readonly ReferenceObject owner;
      public Proxy(ReferenceObject o) { owner = o; ((IGH_GooProxy) this).UserString = FormatInstance(); }
      public override string ToString() => owner.DisplayName;

      IGH_Goo IGH_GooProxy.ProxyOwner => owner;
      string IGH_GooProxy.UserString { get; set; }
      bool IGH_GooProxy.IsParsable => IsParsable();
      string IGH_GooProxy.MutateString(string str) => str.Trim();
      bool IGH_GooProxy.Valid => Valid;
      protected bool Valid => owner.IsValid;

      public virtual void Construct() { }
      public virtual bool IsParsable() => false;
      public virtual string FormatInstance() => owner.DisplayName;
      public virtual bool FromString(string str) => throw new NotImplementedException();

      [DisplayName("Class"), Description("API Object Type."), Category("Object")]
      public virtual Type ObjectType => owner.Value?.GetType();

      [DisplayName("Model"), Description("The document this element belongs to."), Category("Object")]
      public string Document => owner.Document?.GetTitle();

      [DisplayName("Document ID"), Description("The Guid of document that references this element."), Category("Reference")]
      public Guid ReferenceDocumentId => owner.ReferenceDocumentId;

      [DisplayName("Unique ID"), Description("A stable unique identifier for an element within the document."), Category("Reference")]
      public string ReferenceUniqueId => owner.ReferenceUniqueId;
    }

    public virtual IGH_GooProxy EmitProxy() => new Proxy(this);
    #endregion
  }
}

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
    public virtual bool IsValid => Document.IsValid();
    public virtual string IsValidWhyNot => document.IsValidWithLog(out var log) ? default : log;
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
    protected DocumentObject(ARDB.Document doc, object val) { document = doc; value = val; }

    ARDB.Document document;
    public ARDB.Document Document
    {
      get => document?.IsValidObject == true ? document : null;
      protected set => document = value;
    }

    protected internal bool AssertValidDocument(DocumentObject other, string paramName)
    {
      if (other?.Document is null) return false;
      if (other.Document.Equals(Document)) return true;

      throw new Exceptions.RuntimeArgumentException(paramName, "Invalid Document");
    }

    object value;
    public virtual object Value
    {
      get => value;
      protected set => this.value = value;
    }

    protected virtual void ResetValue()
    {
      document = default;
      value = default;
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
    Guid DocumentGUID { get; }
    string UniqueID { get; }
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
      Equals(Document, other.Document) && Equals(UniqueID, other.UniqueID);
    public override bool Equals(object obj) => (obj is ReferenceObject id) ? Equals(id) : base.Equals(obj);
    public override int GetHashCode() => Document.GetHashCode() ^ UniqueID.GetHashCode();
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
    #endregion

    #region GH_ISerializable
    protected override bool Read(GH_IReader reader)
    {
      UnloadReferencedData();

      var documentGUID = Guid.Empty;
      reader.TryGetGuid("DocumentGUID", ref documentGUID);
      DocumentGUID = documentGUID;

      string uniqueID = string.Empty;
      reader.TryGetString("UniqueID", ref uniqueID);
      UniqueID = uniqueID;

      return true;
    }

    protected override bool Write(GH_IWriter writer)
    {
      if (DocumentGUID != Guid.Empty)
        writer.SetGuid("DocumentGUID", DocumentGUID);

      if (!string.IsNullOrEmpty(UniqueID))
        writer.SetString("UniqueID", UniqueID);

      return true;
    }
    #endregion

    #region IGH_ReferenceObject
    public Guid DocumentGUID { get; protected set; } = Guid.Empty;
    public string UniqueID { get; protected set; } = string.Empty;
    #endregion

    #region IGH_ReferencedData
    public virtual bool IsReferencedData => DocumentGUID != Guid.Empty;
    public abstract bool IsReferencedDataLoaded { get; }

    public abstract bool LoadReferencedData();
    protected abstract object FetchValue();
    public virtual void UnloadReferencedData()
    {
      if (!IsReferencedData) return;

      ResetValue();
    }
    #endregion

    #region IGH_QuickCast
    GH_QuickCastType IGH_QuickCast.QC_Type => GH_QuickCastType.text;
    private string QC_Value => External.DB.FullUniqueId.Format(DocumentGUID, UniqueID);

    int IGH_QuickCast.QC_Hash() => QC_Value.GetHashCode();

    double IGH_QuickCast.QC_Distance(IGH_QuickCast other) => (this as IGH_QuickCast).QC_CompareTo(other) == 0 ? 0.0 : double.NaN;

    int IGH_QuickCast.QC_CompareTo(IGH_QuickCast other)
    {
      return GetType() == other.GetType() && other is IGH_ReferenceObject otherId ?
        QC_Value.CompareTo(External.DB.FullUniqueId.Format(otherId.DocumentGUID, otherId.UniqueID)) :
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

      public virtual void Construct() { }
      public virtual bool IsParsable() => false;
      public virtual string FormatInstance() => owner.DisplayName;
      public virtual bool FromString(string str) => throw new NotImplementedException();

      public bool Valid => owner.IsValid;

      [Description("The document this element belongs to.")]
      public string Document => owner.Document?.GetTitle();

      [Description("The Guid of document this element belongs to.")]
      public Guid DocumentGUID => owner.DocumentGUID;

      [Description("A stable unique identifier for an element within the document.")]
      public string UniqueID => owner.UniqueID;
      [Description("API Object Type.")]
      public virtual Type ObjectType => owner.Value?.GetType();
    }

    public virtual IGH_GooProxy EmitProxy() => new Proxy(this);
    #endregion
  }
}

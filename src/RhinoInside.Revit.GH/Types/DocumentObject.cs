using System;
using System.Reflection;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using Grasshopper.Special;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  /// <summary>
  /// Interface to wrap classes that are defined into <see cref="Autodesk.Revit.DB"/> namespace.
  /// For example: <see cref="DB.Document"/>
  /// </summary>
  public interface IGH_DocumentObject : IGH_Goo
  {
    DB.Document Document { get; }
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
    string IGH_Goo.TypeDescription => $"Represents a Revit {((IGH_Goo) this).TypeName.ToLowerInvariant()}";
    public virtual bool IsValid => Document.IsValid();
    public virtual string IsValidWhyNot => document.IsValidWithLog(out var log) ? default : log;
    IGH_Goo IGH_Goo.Duplicate() => (IGH_Goo) (this as ICloneable)?.Clone();
    object IGH_Goo.ScriptVariable() => Value;

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
    protected DocumentObject(DB.Document doc, object val) { document = doc; value = val; }

    DB.Document document = default;
    public DB.Document Document
    {
      get => document?.IsValidObject != true ? null : document;
      protected set
      {
        // Please don't Dispose 'document' here, same reference may be in use in other places.
        //if (value is null) document?.Dispose();

        document = value;
        ResetValue();
      }
    }

    protected internal bool AssertValidDocument(DocumentObject other, string paramName)
    {
      if (other.Document is null) return false;
      if (other.Document.Equals(Document)) return true;

      throw new System.ArgumentException("Invalid Document", paramName);
    }

    object value;
    public virtual object Value
    {
      get => value;
      protected set => this.value = value;
    }

    protected virtual void ResetValue()
    {
      // Please don't Dispose 'value' here, same reference may be in use in other places.
      //if (value is IDisposable disposable)
      //  disposable.Dispose();

      value = default;
    }

    public abstract string DisplayName { get; }
  }

  /// <summary>
  /// Interface to wrap classes that can be created-duplicated-updated-deleted without starting a Revit Transaction.
  /// For example: <see cref="DB.CompoundStructureLayer"/>
  /// </summary>
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

    protected ValueObject(DB.Document doc, object val) : base(doc, val) { }
  }

  /// <summary>
  /// Interface to wrap classes that can not be created-duplicated-updated-deleted without starting a Revit Transaction.
  /// For example: <see cref="DB.CurtainGrid"/>, <see cref="DB.Parameter"/>
  /// </summary>
  public interface IGH_ReferenceObject : IGH_DocumentObject
  {
    DB.ElementId Id { get; }
  }

  public abstract class ReferenceObject : DocumentObject,
    IEquatable<ReferenceObject>,
    IGH_ReferenceObject,
    IGH_ItemDescription
  {
    #region System.Object
    public bool Equals(ReferenceObject other) => other is object &&
      Equals(Document, other.Document) && Equals(Id, other.Id);
    public override bool Equals(object obj) => (obj is ReferenceObject id) ? Equals(id) : base.Equals(obj);
    public override int GetHashCode() => Document.GetHashCode() ^ Id.IntegerValue;
    #endregion

    protected ReferenceObject() { }

    protected ReferenceObject(DB.Document doc, object val) : base(doc, val) { }

    public abstract DB.ElementId Id { get; }

    #region IGH_ItemDescription
    System.Drawing.Bitmap IGH_ItemDescription.GetImage(System.Drawing.Size size) => default;
    string IGH_ItemDescription.Name => DisplayName;
    string IGH_ItemDescription.NickName => $"{{{Id?.IntegerValue}}}";
    string IGH_ItemDescription.Description => Document?.GetFileName();
    #endregion
  }
}

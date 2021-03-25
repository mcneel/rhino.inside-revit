using System;
using System.Reflection;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  /// <summary>
  /// Interface to implement into classes that are defined into <see cref="Autodesk.Revit.DB"/> namespace.
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
    public override string ToString() => $"Revit {((IGH_Goo) this).TypeName} : {DisplayName}";

    object ICloneable.Clone() => MemberwiseClone();
    #endregion

    #region IGH_Goo
    string IGH_Goo.TypeName
    {
      get
      {
        var type = GetType();
        var name = type.GetTypeInfo().GetCustomAttribute(typeof(Kernel.Attributes.NameAttribute)) as Kernel.Attributes.NameAttribute;
        return name?.Name ?? type.Name;
      }
    }
    string IGH_Goo.TypeDescription => $"Represents a Revit {((IGH_Goo) this).TypeName.ToLowerInvariant()}";
    public virtual bool IsValid => Document.IsValid();
    public virtual string IsValidWhyNot => IsValid ? string.Empty : "Not Valid";
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
      protected set { document = value; ResetValue(); }
    }

    protected internal void AssertValidDocument(DB.Document doc, string paramName)
    {
      if (!(doc?.Equals(Document) ?? false))
        throw new System.ArgumentException("Invalid Document", paramName);
    }

    object value;
    public virtual object Value
    {
      get => value;
      protected set => this.value = value;
    }

    protected virtual void ResetValue() => value = default;

    public abstract string DisplayName { get; }
  }

  /// <summary>
  /// Interface to implement into classes that can be created-duplicated-updated-deleted without starting a Revit Transaction.
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
    public override bool IsValid => base.IsValid && !(Value is null);
    #endregion

    protected ValueObject() { }

    protected ValueObject(DB.Document doc, object val) : base(doc, val) { }
  }

  /// <summary>
  /// Interface to implement into classes that can not be created-duplicated-updated-deleted without starting a Revit Transaction.
  /// For example: <see cref="DB.CurtainGrid"/>
  /// </summary>
  public interface IGH_ReferenceObject : IGH_DocumentObject
  {
    DB.ElementId Id { get; }
  }

  public abstract class ReferenceObject : DocumentObject, IGH_ReferenceObject, IEquatable<ReferenceObject>
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
  }
}

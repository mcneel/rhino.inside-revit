using System;
using System.ComponentModel;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public interface IGH_ElementId : IGH_Goo
  {
    DB.Document Document { get; }
    DB.ElementId Id { get; }

    Guid DocumentGUID { get; }
    string UniqueID { get; }

    bool IsReferencedElement { get; }
    bool IsElementLoaded { get; }
    bool LoadElement();
    void UnloadElement();
  }

  public class ElementId : GH_Goo<DB.ElementId>, IEquatable<ElementId>, IGH_ElementId
  {
    public override string TypeName => "Revit Model Object";
    public override string TypeDescription => "Represents a Revit model object";
    public override bool IsValid => (!(Value is null || Value == DB.ElementId.InvalidElementId)) && (Document?.IsValidObject ?? false);
    public override sealed IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();
    protected virtual Type ScriptVariableType => typeof(DB.ElementId);
    public static implicit operator DB.ElementId(ElementId self) { return self.Value; }

    public static ElementId FromElementId(DB.Document doc, DB.ElementId id)
    {
      if (id == DB.ElementId.InvalidElementId)
        return null;

      if (Category.FromElementId(doc, id) is Category c)
        return c;

      if (ParameterKey.FromElementId(doc, id) is ParameterKey p)
        return p;

      return Element.FromElementId(doc, id);
    }

    public void SetValue(DB.Document doc, DB.ElementId id)
    {
      Document = doc;
      DocumentGUID = doc.GetFingerprintGUID();

      Value = id;
      UniqueID = doc?.GetElement(id)?.UniqueId ??
                 (
                   id.IntegerValue < DB.ElementId.InvalidElementId.IntegerValue ?
                     UniqueId.Format(Guid.Empty, id.IntegerValue) : 
                     string.Empty
                 );
    }

    #region IGH_ElementId
    DB.Document document;
    public DB.Document Document { get => document?.IsValidObject != true ? null : document; protected set { document = value; } }
    public DB.ElementId Id => Value;
    public Guid DocumentGUID { get; protected set; } = Guid.Empty;
    public string UniqueID   { get; protected set; } = string.Empty;
    public bool IsReferencedElement => !string.IsNullOrEmpty(UniqueID);
    public bool IsElementLoaded => m_value is object;
    public virtual bool LoadElement()
    {
      if (Document is null)
      {
        Value = null;
        if (!Revit.ActiveUIApplication.TryGetDocument(DocumentGUID, out var doc))
        {
          Document = null;
          return false;
        }

        Document = doc;
      }
      else if (IsElementLoaded)
        return true;

      if (Document is object)
        return Document.TryGetElementId(UniqueID, out m_value);

      return false;
    }
    public void UnloadElement() { m_value = null; Document = null; }
    #endregion

    public ElementId() { Value = DB.ElementId.InvalidElementId; }
    protected ElementId(DB.Document doc, DB.ElementId id)    => SetValue(doc, id);

    public override bool CastFrom(object source)
    {
      if (source is GH_Integer integer)
      {
        Value = new DB.ElementId(integer.Value);
        UniqueID = string.Empty;
        return true;
      }
      if (source is DB.ElementId id)
      {
        Value = id;
        UniqueID = string.Empty;
        return true;
      }

      return false;
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if(target is IGH_ElementId)
      {
        target = (Q) (object) null;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(DB.ElementId)))
      {
        target = (Q) (object) Value;
        return true;
      }
      if(typeof(Q).IsAssignableFrom(typeof(GH_Integer)))
      {
        target = (Q) (object) new GH_Integer(Value.IntegerValue);
        return true;
      }
      if(typeof(Q).IsAssignableFrom(typeof(GH_String)))
      {
        target = (Q) (object) new GH_String(UniqueID);
        return true;
      }

      return base.CastTo<Q>(ref target);
    }

    public bool Equals(ElementId id) => id?.UniqueID == UniqueID;
    public override bool Equals(object obj) => (obj is ElementId id) ? Equals(id) : base.Equals(obj);
    public override int GetHashCode() => UniqueID.GetHashCode();

    [TypeConverter(typeof(Proxy.ObjectConverter))]
    protected class Proxy : IGH_GooProxy
    {
      protected readonly ElementId owner;
      public Proxy(ElementId o) { owner = o; if(this is IGH_GooProxy proxy) proxy.UserString = proxy.FormatInstance(); }
      public override string ToString() => owner.DisplayName;

      IGH_Goo IGH_GooProxy.ProxyOwner => owner;
      string IGH_GooProxy.UserString { get; set; }
      bool IGH_GooProxy.IsParsable => IsParsable();

      public virtual bool IsParsable() => false;
      public virtual void Construct() { }
      public virtual string FormatInstance() => owner.DisplayName;
      public virtual bool FromString(string str) => throw new NotImplementedException();
      public virtual string MutateString(string str) => str.Trim();

      public bool Valid => owner.IsValid;
      public string TypeName => owner.TypeName;
      public string TypeDescription => owner.TypeDescription;

      [System.ComponentModel.Description("The document this element belongs to.")]
      public string Document => owner.Document.GetFilePath();
      [System.ComponentModel.Description("The Guid of document this element belongs to.")]
      public Guid DocumentGUID => owner.Document.GetFingerprintGUID();
      [System.ComponentModel.Description("The element identifier in this session.")]
      public int Id => owner.Id?.IntegerValue ?? -1;
      [System.ComponentModel.Description("A stable unique identifier for an element within the document.")]
      public string UniqueID => owner.UniqueID;
      [System.ComponentModel.Description("API Object Type.")]
      public virtual Type ObjectType => owner.ScriptVariable()?.GetType() ?? owner.ScriptVariableType;
      [System.ComponentModel.Description("Element is built in Revit.")]
      public bool IsBuiltIn => owner.Id.IsBuiltInId();
      [System.ComponentModel.Description("A human readable name for the Element.")]
      public string Name => owner.DisplayName;

      class ObjectConverter : ExpandableObjectConverter
      {
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
          var properties = base.GetProperties(context, value, attributes);
          if (value is Proxy proxy)
          {
            var element = proxy.owner.Document?.GetElement(proxy.owner.Id);
            if (element is object)
            {
              var parameters = element.GetParameters(RevitAPI.ParameterSet.Any).
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
        readonly DB.Parameter parameter;
        public ParameterPropertyDescriptor(DB.Parameter p) : base(p.Definition?.Name ?? p.Id.IntegerValue.ToString(), null) { parameter = p; }
        public override Type ComponentType => typeof(Proxy);
        public override bool IsReadOnly => true;
        public override string Name => parameter.Definition?.Name ?? string.Empty;
        public override string Category => parameter.Definition is null ? string.Empty : DB.LabelUtils.GetLabelFor(parameter.Definition.ParameterGroup);
        public override string Description
        {
          get
          {
            var description = string.Empty;
            if (parameter.Element is object && parameter.Definition is object)
            {
              try { description = parameter.StorageType == DB.StorageType.ElementId ? "ElementId" : DB.LabelUtils.GetLabelFor(parameter.Definition.ParameterType); }
              catch (Autodesk.Revit.Exceptions.InvalidOperationException)
              { description = parameter.Definition.UnitType == DB.UnitType.UT_Number ? "Enumerate" : DB.LabelUtils.GetLabelFor(parameter.Definition.UnitType); }
            }

            if (parameter.IsReadOnly)
              description = "Read only " + description;

            description += "\nParameterId : " + ((DB.BuiltInParameter)parameter.Id.IntegerValue).ToStringGeneric();
            return description;
          }
        }
        public override bool Equals(object obj)
        {
          if (obj is ParameterPropertyDescriptor other)
            return other.parameter.Id == parameter.Id;

          return false;
        }
        public override int GetHashCode() => parameter.Id.IntegerValue;
        public override bool ShouldSerializeValue(object component) { return false; }
        public override void ResetValue(object component) { }
        public override bool CanResetValue(object component) { return false; }
        public override void SetValue(object component, object value) { }
        public override Type PropertyType => typeof(string);
        public override object GetValue(object component) =>
          parameter.Element is object && parameter.Definition is object ?
          (parameter.StorageType == DB.StorageType.String ? parameter.AsString() :
          parameter.AsValueString()) : null;
      }
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);

    public override sealed string ToString()
    {
      var tip = IsValid ?
        $"{TypeName} : {DisplayName}" :
        (IsReferencedElement && !IsElementLoaded) ?
        $"Unresolved {TypeName} : {UniqueID}" :
        $"Invalid {TypeName}";

      return
      (
        Revit.ActiveDBApplication.Documents.Size > 1 ? 
        $"{tip} @ {Document?.Title ?? DocumentGUID.ToString()}" :
        tip
      );
    }

    public virtual string DisplayName => Id is null ? UniqueID : $"id {Id.IntegerValue}";

    public override sealed bool Read(GH_IReader reader)
    {
      Value = null;
      Document = null;

      var documentGUID = Guid.Empty;
      reader.TryGetGuid("DocumentGUID", ref documentGUID);
      DocumentGUID = documentGUID;

      string uniqueID = string.Empty;
      reader.TryGetString("UniqueID", ref uniqueID);
      UniqueID = uniqueID;

      return true;
    }

    public override sealed bool Write(GH_IWriter writer)
    {
      if(DocumentGUID != Guid.Empty)
        writer.SetGuid("DocumentGUID", DocumentGUID);

      if(!string.IsNullOrEmpty(UniqueID))
        writer.SetString("UniqueID", UniqueID);

      return true;
    }
  }
}

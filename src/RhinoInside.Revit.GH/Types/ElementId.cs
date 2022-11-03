using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel.Types;
using Grasshopper.Special;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  // public interface IGH_PersistentReference
  /// <summary>
  /// Interface to implement into classes that has a stable <see cref="ARDB.Reference"/>.
  /// For example: <see cref="ARDB.Element"/>, <see cref="ARDB.GeometryObject"/>
  /// </summary>
  public interface IGH_ElementId : IGH_ReferenceObject, IEquatable<IGH_ElementId>
  {
    ARDB.Reference Reference { get; }
    ARDB.ElementId Id { get; }
  }

  public abstract class ElementId : ReferenceObject,
    IGH_ElementId,
    IGH_ItemDescription
  {
    #region System.Object
    public bool Equals(IGH_ElementId other) => other is object &&
      other.DocumentGUID == DocumentGUID && other.UniqueID == UniqueID;
    public override bool Equals(object obj) => (obj is IGH_ElementId id) ? Equals(id) : base.Equals(obj);
    public override int GetHashCode() => DocumentGUID.GetHashCode() ^ UniqueID.GetHashCode();

    public override string ToString()
    {
      var valid = IsValid;
      string Invalid = Id == ARDB.ElementId.InvalidElementId ?
        (string.IsNullOrWhiteSpace(UniqueID) ? string.Empty : "Unresolved ") :
        valid ? string.Empty :
        (IsReferencedData ? "❌ Deleted " : "⚠ Invalid ");
      string TypeName = ((IGH_Goo) this).TypeName;
      string InstanceName = DisplayName ?? string.Empty;

      if (!string.IsNullOrWhiteSpace(InstanceName))
        InstanceName = $" : {InstanceName}";

      if (!IsReferencedData)
        return $"{Invalid}{TypeName}{InstanceName}";

      string InstanceId = valid ? $" : id {Id.ToValue()}" : $" : {UniqueID}";

      using (var Documents = Revit.ActiveDBApplication.Documents)
      {
        if (Documents.Size > 1)
          InstanceId = $"{InstanceId} @ {Document?.GetTitle() ?? DocumentGUID.ToString("B")}";
      }

      return $"{Invalid}{TypeName}{InstanceName}{InstanceId}";
    }
    #endregion

    #region IGH_Goo
    public override bool IsValid => base.IsValid && Id.IsValid();
    public override string IsValidWhyNot
    {
      get
      {
        if (DocumentGUID == Guid.Empty) return $"DocumentGUID '{Guid.Empty}' is invalid";
        if (!External.DB.UniqueId.TryParse(UniqueID, out var _, out var _)) return $"UniqueID '{UniqueID}' is invalid";

        if (Document is null)
        {
          return $"Referenced Revit document '{DocumentGUID}' was closed.";
        }
        else
        {
          var id = Id;
          if (id is null) return $"Referenced Revit element '{UniqueID}' is not available.";
          if (id == ARDB.ElementId.InvalidElementId) return "Id is equal to InvalidElementId.";
        }

        return default;
      }
    }

    public virtual object ScriptVariable() => Value;

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.ElementId)))
      {
        target = (Q) (object) Id;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Integer)))
      {
        target = (Q) (object) new GH_Integer(Id.IntegerValue);
        return true;
      }

      target = default;
      return false;
    }

    [TypeConverter(typeof(Proxy.ObjectConverter))]
    protected class Proxy : IGH_GooProxy
    {
      protected readonly ElementId owner;
      public Proxy(ElementId o) { owner = o; ((IGH_GooProxy) this).UserString = FormatInstance(); }
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

      protected virtual bool IsValidId(ARDB.Document doc, ARDB.ElementId id) =>
        owner.GetType() == Element.FromElementId(doc, id).GetType();

      [Description("A stable unique identifier for an element within the document.")]
      public string UniqueID => owner.UniqueID;
      [Description("API Object Type.")]
      public virtual Type ObjectType => owner.Value?.GetType();
      [Description("Element is built in Revit.")]
      public bool IsBuiltIn => owner.IsReferencedData && owner.Id.IsBuiltInId();

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

    public virtual IGH_GooProxy EmitProxy() => new Proxy(this);
    #endregion

    #region IGH_ItemDescription
    Bitmap IGH_ItemDescription.GetImage(Size size) => default;
    string IGH_ItemDescription.Name => DisplayName;
    string IGH_ItemDescription.NickName => $"{{{Id?.ToString()}}}";
    string IGH_ItemDescription.Description => Document?.GetTitle();
    #endregion

    #region IGH_ReferencedData
    public override bool IsReferencedData => DocumentGUID != Guid.Empty;
    #endregion

    #region IGH_ElementId
    public abstract ARDB.Reference Reference { get; }
    public abstract ARDB.ElementId Id { get; }
    #endregion

    public ElementId() { }

    protected ElementId(ARDB.Document doc, object value) : base(doc, value) { }
  }
}

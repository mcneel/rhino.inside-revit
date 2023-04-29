using System;
using System.ComponentModel;
using System.Linq;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  public partial class Element
  {
    [TypeConverter(typeof(Proxy.ObjectConverter))]
    protected new class Proxy : Reference.Proxy
    {
      protected new Element owner => base.owner as Element;

      public Proxy(Element e) : base(e) { (this as IGH_GooProxy).UserString = FormatInstance(); }

      public override string FormatInstance()
      {
        return owner.DisplayName;
      }

      protected virtual bool IsValidId(ARDB.Document doc, ARDB.ElementId id) =>
        owner.GetType() == Element.FromElementId(doc, id).GetType();

      [DisplayName("Document"), Description("The document that references this element."), Category("Reference")]
      public string ReferenceDocument => owner.ReferenceDocument?.GetTitle();

      [DisplayName("Built In"), Description("Element is built in Revit."), Category("Object")]
      public bool IsBuiltIn => owner.IsReferencedData && owner.Id.IsBuiltInId();

      [DisplayName("Element ID"), Description("The element identifier in this session."), Category("Object")]
      //[RefreshProperties(RefreshProperties.All)]
      public virtual long? Id => owner.Id?.ToValue();

      [DisplayName("Unique ID"), Description("A stable unique identifier for an element within the model."), Category("Object")]
      public virtual string UniqueId => owner.UniqueId;

      [Description("A human readable name for the Element."), Category("Object")]
      public string Name => owner.Nomen;

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
        public override string Category => parameter?.Definition?.GetGroupType()?.LocalizedLabel;
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

    public override IGH_GooProxy EmitProxy() => new Proxy(this);
  }
}

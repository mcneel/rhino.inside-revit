using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Types
{
  // public interface IGH_PersistentReference
  /// <summary>
  /// Interface to implement into classes that has a stable <see cref="DB.Reference"/>.
  /// For example: <see cref="DB.Element"/>, <see cref="DB.GeometryObject"/>
  /// </summary>
  public interface IGH_ElementId : IGH_ReferenceObject
  {
    DB.Reference Reference { get; }

    Guid DocumentGUID { get; }
    string UniqueID { get; }

    bool IsReferencedElement { get; }
    bool IsElementLoaded { get; }
    bool LoadElement();
    void UnloadElement();
  }

  public abstract class ElementId : ReferenceObject, IGH_ElementId, IEquatable<ElementId>, IGH_QuickCast
  {
    #region System.Object
    public bool Equals(ElementId other) => other is object &&
      other.DocumentGUID == DocumentGUID && other.UniqueID == UniqueID;
    public override bool Equals(object obj) => (obj is ElementId id) ? Equals(id) : base.Equals(obj);
    public override int GetHashCode() => DocumentGUID.GetHashCode() ^ UniqueID.GetHashCode();

    public override string ToString()
    {
      var TypeName = $"Revit {((IGH_Goo) this).TypeName}";

      if (!IsReferencedElement)
        return $"{TypeName} : {DisplayName}";

      var tip = IsValid ?
      (
        IsElementLoaded ?
        $"{TypeName} : {DisplayName}" :
        $"Unresolved {TypeName} : {UniqueID}"
      ) :
      $"Invalid {TypeName}" + (Id is object ? $" : {Id.IntegerValue}" : string.Empty);

      using (var Documents = Revit.ActiveDBApplication.Documents)
      {
        return
        (
          Documents.Size > 1 ?
          $"{tip} @ {Document?.Title ?? DocumentGUID.ToString()}" :
          tip
        );
      }
    }
    #endregion

    #region GH_ISerializable
    protected override bool Read(GH_IReader reader)
    {
      UnloadElement();

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

    #region IGH_Goo
    public override bool IsValid => Document.IsValid() && (Id.IsBuiltInId() || Value is object);
    public override string IsValidWhyNot => IsValid ? string.Empty : "Not Valid";
    public virtual object ScriptVariable() => Value;

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(DB.ElementId)))
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

      [System.ComponentModel.Description("The document this element belongs to.")]
      public string Document => owner.Document.GetFilePath();
      [System.ComponentModel.Description("The Guid of document this element belongs to.")]
      public Guid DocumentGUID => owner.DocumentGUID;
      protected virtual bool IsValidId(DB.Document doc, DB.ElementId id) => true;

      [System.ComponentModel.Description("A stable unique identifier for an element within the document.")]
      public string UniqueID => owner.UniqueID;
      [System.ComponentModel.Description("API Object Type.")]
      public virtual Type ObjectType => owner.Value?.GetType();
      [System.ComponentModel.Description("Element is built in Revit.")]
      public bool IsBuiltIn => owner.Id.IsBuiltInId();

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
              var parameters = element.GetParameters(DBX.ParameterClass.Any).
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

    public virtual IGH_GooProxy EmitProxy() => new Proxy(this);
    #endregion

    #region DocumentObject
    public override object Value
    {
      get
      {
        if (base.Value is null && IsElementLoaded)
          base.Value = FetchValue();

        return base.Value;
      }
      protected set => base.Value = value;
    }
    #endregion

    #region IGH_ElementId
    public abstract DB.Reference Reference { get; }

    public Guid DocumentGUID { get; protected set; } = Guid.Empty;
    public string UniqueID { get; protected set; } = string.Empty;
    public bool IsReferencedElement => DocumentGUID != Guid.Empty;

    public abstract bool IsElementLoaded { get; }
    public abstract bool LoadElement();
    protected abstract object FetchValue();
    public virtual void UnloadElement()
    {
      ResetValue();

      if (IsReferencedElement)
        Document = default;
    }
    #endregion

    #region IGH_QuickCast
    GH_QuickCastType IGH_QuickCast.QC_Type => GH_QuickCastType.text;
    int IGH_QuickCast.QC_Hash() => FullUniqueId.Format(DocumentGUID, UniqueID).GetHashCode();

    double IGH_QuickCast.QC_Distance(IGH_QuickCast other)
    {
      try
      {
        switch (other.QC_Type)
        {
          case GH_QuickCastType.@bool:    return other.QC_Bool() == ((IGH_QuickCast) this).QC_Bool() ? 0.0 : 1.0;
          case GH_QuickCastType.@int:     return Math.Abs(other.QC_Int() - ((IGH_QuickCast) this).QC_Int());
          case GH_QuickCastType.num:      return Math.Abs(other.QC_Num() - ((IGH_QuickCast) this).QC_Num());
          case GH_QuickCastType.pt:       return other.QC_Pt().DistanceTo(((IGH_QuickCast) this).QC_Pt());
          case GH_QuickCastType.vec:      return new Point3d(other.QC_Vec()).DistanceTo(new Point3d(((IGH_QuickCast) this).QC_Vec()));
          case GH_QuickCastType.interval:
            var otherInterval = other.QC_Interval();
            var thisInterval = ((IGH_QuickCast) this).QC_Interval();
            var d0 = Math.Abs(otherInterval.T0 - thisInterval.T0);
            var d1 = Math.Abs(otherInterval.T1 - thisInterval.T1);
            return d0 + d1;
        }
      }
      catch { }

      var dist0 = GH_StringMatcher.LevenshteinDistance(FullUniqueId.Format(DocumentGUID, UniqueID), other.QC_Text());
      var dist1 = GH_StringMatcher.LevenshteinDistance(FullUniqueId.Format(DocumentGUID, UniqueID).ToUpperInvariant(), other.QC_Text().ToUpperInvariant());
      return 0.5 * (dist0 + dist1);
    }

    int IGH_QuickCast.QC_CompareTo(IGH_QuickCast other)
    {
      if (other.QC_Type != GH_QuickCastType.text)
        return GH_QuickCastType.text.CompareTo(other.QC_Type);

      return FullUniqueId.Format(DocumentGUID, UniqueID).CompareTo(other.QC_Text());
    }

    bool IGH_QuickCast.QC_Bool() => IsValid;
    int IGH_QuickCast.QC_Int() => Id?.IntegerValue ?? throw new InvalidCastException();
    double IGH_QuickCast.QC_Num() => Id?.IntegerValue ?? throw new InvalidCastException();
    string IGH_QuickCast.QC_Text() => FullUniqueId.Format(DocumentGUID, UniqueID);
    Color IGH_QuickCast.QC_Col() => throw new InvalidCastException();
    Point3d IGH_QuickCast.QC_Pt() => throw new InvalidCastException();
    Vector3d IGH_QuickCast.QC_Vec() => throw new InvalidCastException();
    Complex IGH_QuickCast.QC_Complex() => throw new InvalidCastException();
    Matrix IGH_QuickCast.QC_Matrix() => throw new InvalidCastException();
    Interval IGH_QuickCast.QC_Interval() => throw new InvalidCastException();
    #endregion

    public ElementId() { }

    protected ElementId(DB.Document doc, object value) : base(doc, value) { }

    #region Properties
    public override string DisplayName => IsReferencedElement ?
      Id is null ? "INVALID" : Id.IntegerValue.ToString() :
      "<None>";
    #endregion
  }
}

using System;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using Grasshopper.Special;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;
using DBXS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Parameter")]
  public class ParameterKey : Element,
    IEquatable<ParameterKey>,
    IGH_ItemDescription,
    IGH_Goo
  {
    string IGH_Goo.TypeName
    {
      get
      {
        var parameterClass = Class;
        return parameterClass != DBX.ParameterClass.Invalid ?
        $"Revit {Class} Parameter" : "Revit Parameter";
      }
    }

    public ParameterKey() { }
    public ParameterKey(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public ParameterKey(DB.ParameterElement element) : base(element) { }

    public ParameterKey(DB.Definition value)
    {
      name = value.Name;
      dataType = value.GetDataType();
      group = value.GetGroupType();
    }

    public ParameterKey(DB.ExternalDefinition value) :
      this((DB.Definition) value)
    {
      guid = value.GUID;
      visible = value.Visible;
      Description = value.Description;
      userModifiable = value.UserModifiable;
#if REVIT_2020
      hideWhenNoValue = value.HideWhenNoValue;
#endif
    }

    public ParameterKey(DB.Document doc, DB.InternalDefinition value) :
      this(doc, value.Id)
    {
      name = value.Name;
      dataType = value.GetDataType();
      group = value.GetGroupType();

      if (doc.GetElement(value.Id) is DB.SharedParameterElement parameter)
        guid = parameter.GuidValue;

      visible = value.Visible;
    }

    public ParameterKey Duplicate() => (ParameterKey) MemberwiseClone();

    #region System.Object
    public override bool Equals(object obj) =>
      obj is ParameterKey other && Equals(other);

    public override int GetHashCode() => IsReferencedData ?
      base.GetHashCode() :
      (GUID, Name, Description, DataType, Group, Visible, UserModifiable, HideWhenNoValue).
      GetHashCode();
    #endregion

    #region IEquatable
    public bool Equals(ParameterKey other)
    {
      return IsReferencedData ?
      base.Equals(other) :
      GUID == other.GUID &&
      Name == other.Name &&
      Description == other.Description &&
      DataType == other.DataType &&
      Group == other.Group &&
      Visible == other.Visible &&
      UserModifiable == other.UserModifiable &&
      HideWhenNoValue == other.HideWhenNoValue;
    }
    #endregion

    #region DocumentObject
    public static new ParameterKey FromElementId(DB.Document doc, DB.ElementId id)
    {
      if (id.IsParameterId(doc))
        return new ParameterKey(doc, id);

      return null;
    }

    public new DB.ParameterElement Value => base.Value as DB.ParameterElement;

    protected virtual bool SetValue(DB.Parameter parameter)
    {
      SetValue(parameter.Element.Document, parameter.Id);
      if (parameter.Definition is DB.InternalDefinition definition)
      {
        name = definition.Name;
        dataType = definition.GetDataType();
        group = definition.GetGroupType();
        visible = definition.Visible;
      }

      //if (dataType == DBXS.DataType.Empty)
      //{
      //  switch (parameter.StorageType)
      //  {
      //    case DB.StorageType.Integer: dataType = DBXS.SpecType.Int.Integer; break;
      //    case DB.StorageType.Double: dataType = DBXS.SpecType.Measurable.Number; break;
      //    case DB.StorageType.String: dataType = DBXS.SpecType.String.Text; break;
      //    case DB.StorageType.ElementId:
      //      if (parameter.HasValue)
      //      {
      //        if (Document.GetElement(parameter.AsElementId()) is DB.Element value)
      //        if (value.Category is DB.Category category)
      //        if (category.Id.TryGetBuiltInCategory(out var categoryId))
      //        {
      //          dataType = (DBXS.CategoryId) categoryId;
      //        }
      //      }
      //      break;
      //  }
      //}
      
      userModifiable = parameter.UserModifiable;
      if (parameter.IsShared) guid = parameter.GUID;
      else guid = null;

      return true;
    }

    public override string DisplayName
    {
      get
      {
        try
        {
          if (Id is object && Id.TryGetBuiltInParameter(out var builtInParameter))
            return DB.LabelUtils.GetLabelFor(builtInParameter) ?? base.DisplayName;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

        return base.DisplayName ?? GUID.GetValueOrDefault().ToString("B");
      }
    }
    #endregion

    #region IGH_Goo
    public override bool IsValid => IsReferencedData ?
      ((Id?.TryGetBuiltInParameter(out var _) == true) || base.IsValid) :
      (name is object && DB.NamingUtils.IsValidName(name) || GUID.HasValue);

    protected override Type ValueType => typeof(DB.ParameterElement);
    public override object ScriptVariable() => Name;

    public sealed override bool CastFrom(object source)
    {
      if (base.CastFrom(source))
        return true;

      var document = Revit.ActiveDBDocument;
      var parameterId = DB.ElementId.InvalidElementId;

      if (source is IGH_Goo goo)
      {
        if (source is IGH_Element element)
        {
          document = element.Document;
          parameterId = element.Id;
        }
        else if (source is ParameterId id)
        {
          source = (DB.BuiltInParameter) id.Value;
        }
        else if (source is ParameterValue parameterValue)
        {
          source = parameterValue.Value;
        }
        else source = goo.ScriptVariable();
      }

      switch (source)
      {
        case int integer: parameterId = new DB.ElementId(integer); break;
        case DB.BuiltInParameter bip: parameterId = new DB.ElementId(bip); break;
        case DB.ElementId id: parameterId = id; break;
        case DB.Parameter parameter: return SetValue(parameter);
        case string n: name = n; return true;
        case Guid g: guid = g; return true;
      }

      if (parameterId.TryGetBuiltInParameter(out var _))
      {
        SetValue(document, parameterId);
        return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(GH_Guid)))
      {
        if (GUID.HasValue)
        {
          target = (Q) (object) new GH_Guid(GUID.Value);
          return true;
        }
        else
        {
          target = default;
          return false;
        }
      }

      if (typeof(Q).IsAssignableFrom(typeof(ParameterId)))
      {
        if (Id.TryGetBuiltInParameter(out var bip))
        {
          target = (Q) (object) new ParameterId(bip);
          return true;
        }
        else
        {
          target = default;
          return false;
        }
      }

      if (typeof(Q).IsAssignableFrom(typeof(DB.ExternalDefinitionCreationOptions)))
      {
        if (IsValid)
        {
          var options = new DB.ExternalDefinitionCreationOptions(Name, DataType)
          {
            Description = Description ?? string.Empty,
            Visible = Visible.GetValueOrDefault(true),
            UserModifiable = UserModifiable.GetValueOrDefault(true),
#if REVIT_2020
            HideWhenNoValue = HideWhenNoValue.GetValueOrDefault(false)
#endif
          };

          if (GUID.HasValue) options.GUID = GUID.Value;
          target = (Q) (object) options;
          return true;
        }
        else
        {
          target = default;
          return false;
        }
      }

      return base.CastTo(out target);
    }

    new class Proxy : Element.Proxy
    {
      protected new ParameterKey owner => base.owner as ParameterKey;

      public Proxy(ParameterKey o) : base(o) { (this as IGH_GooProxy).UserString = FormatInstance(); }

      public override bool IsParsable() => owner.IsReferencedData;
      public override string FormatInstance()
      {
        if (owner.IsReferencedData)
        {
          int value = owner.Id?.IntegerValue ?? -1;
          if (Enum.IsDefined(typeof(DB.BuiltInParameter), value))
            return ((DB.BuiltInParameter) value).ToStringGeneric();

          return value.ToString();
        }
        else return owner.DisplayName;
      }
      public override bool FromString(string str)
      {
        if (Enum.TryParse(str, out DB.BuiltInParameter builtInParameter))
        {
          owner.SetValue(owner.Document ?? Revit.ActiveUIDocument.Document, new DB.ElementId(builtInParameter));
          return true;
        }

        return false;
      }

      #region Misc
      protected override bool IsValidId(DB.Document doc, DB.ElementId id) => id.IsParameterId(doc);
      public override Type ObjectType => !owner.IsReferencedData ?
        typeof(DB.Definition) :
        IsBuiltIn ? typeof(DB.BuiltInParameter) : base.ObjectType;

      [System.ComponentModel.Description("BuiltIn parameter Id.")]
      public DB.BuiltInParameter? BuiltInId
      {
        get
        {
          if (owner.Id.TryGetBuiltInParameter(out var bip)) return bip;
          return default;
        }
      }

      [System.ComponentModel.Description("Forge Id.")]
      public DBXS.ParameterId SchemaId => owner.Id.TryGetBuiltInParameter(out var bip) ? (DBXS.ParameterId) bip : default;
      #endregion

      #region Definition
      const string Definition = "Definition";

      [System.ComponentModel.Category(Definition), System.ComponentModel.Description("The Guid that identifies this parameter as a shared parameter.")]
      public Guid? Guid => owner.GUID;

      [System.ComponentModel.Category(Definition), System.ComponentModel.Description("Internal parameter data storage type.")]
      public DB.StorageType? StorageType => BuiltInId.HasValue ? Revit.ActiveDBDocument?.get_TypeOfStorage(BuiltInId.Value) : owner.DataType.ToStorageType();

      [System.ComponentModel.Category(Definition), System.ComponentModel.Description("Parameter data Type")]
      public string Type => owner.DataType?.Label;

      [System.ComponentModel.Category(Definition)]
      public string Group => owner.Group?.Label;

      [System.ComponentModel.Category(Definition), System.ComponentModel.Description("Visible in UI.")]
      public bool? Visible => owner.Visible;
      #endregion
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);
    #endregion

    #region GH_ISerializable
    protected override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (guid.HasValue)
        writer.SetGuid("GUID", guid.Value);

      if (name is object)
        writer.SetString("Name", name);

      if (description is object)
        writer.SetString("Description", description);

      if (dataType is object)
        writer.SetString("DataType", dataType.FullName);

      if (group is object)
        writer.SetString("Group", group.FullName);

      if (visible.HasValue)
        writer.SetBoolean("Visible", visible.Value);

      if (userModifiable.HasValue)
        writer.SetBoolean("UserModifiable", userModifiable.Value);

      if (hideWhenNoValue.HasValue)
        writer.SetBoolean("HideWhenNoValue", hideWhenNoValue.Value);

      return true;
    }

    protected override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      var _guid = default(Guid);
      if (reader.TryGetGuid("GUID", ref _guid))
        guid = _guid;
      else
        guid = null;

      name = default;
      reader.TryGetString("Name", ref name);
      
      description = default;
      reader.TryGetString("Description", ref description);

      var _dataType = default(string);
      dataType = reader.TryGetString("DataType", ref _dataType) ?
        new DBXS.DataType(_dataType) : null;

      var _group = default(string);
      group = reader.TryGetString("Group", ref _group) ?
        new DBXS.ParameterGroup(_group) : null;

      var _visible = default(bool);
      if (reader.TryGetBoolean("Visible", ref _visible)) visible = _visible;
      else visible = null;

      var _userModifiable = default(bool);
      if (reader.TryGetBoolean("UserModifiable", ref _userModifiable)) userModifiable = _userModifiable;
      else userModifiable = null;

      var _hideWhenNoValue = default(bool);
      if (reader.TryGetBoolean("HideWhenNoValue", ref _hideWhenNoValue)) hideWhenNoValue = _hideWhenNoValue;
      else hideWhenNoValue = null;

      return true;
    }
    #endregion

    #region IGH_ItemDescription
    System.Drawing.Bitmap IGH_ItemDescription.GetImage(System.Drawing.Size size) => default;
    string IGH_ItemDescription.NickName
    {
      get
      {
        if (Id is object && Id.TryGetBuiltInParameter(out var bip))
          return ((DBXS.ParameterId) bip).Name;

        if (IsReferencedData)
        {
          return GUID.HasValue ?
            $"{{{Id?.IntegerValue}}} : {GUID.Value:B}" :
            $"{{{Id?.IntegerValue}}}";
        }
        else
        {
          return GUID.HasValue ?
            $"{GUID.Value:B}" :
            string.Empty;
        }
      }
    }
    string IGH_ItemDescription.Description =>
      Id is object && Id.TryGetBuiltInParameter(out var bip) ?
      ((DBXS.ParameterId) bip).Namespace :
      DataType?.Label;

    #endregion

    #region Properties
    Guid? guid;
    public Guid? GUID => (Value as DB.SharedParameterElement)?.GuidValue ?? guid;

    string name;
    public override string Name
    {
      get
      {
        if (!IsReferencedData) return name;

        try
        {
          if (Id is object && Id.TryGetBuiltInParameter(out var builtInParameter))
            return DB.LabelUtils.GetLabelFor(builtInParameter) ?? base.Name;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

        return base.Name;
      }
      set
      {
        if (!IsReferencedData)
        {
          if (!DB.NamingUtils.IsValidName(value))
            throw new ArgumentException("Invalid parameter name");

          name = value;
        }
        else base.Name = value;
      }
    }

    string description;
    public override string Description
    {
      get => description;
      set
      {
        if (IsReferencedData) throw new InvalidOperationException();
        description = value;
      }
    }

    DBXS.DataType dataType;
    public DBXS.DataType DataType
    {
      get => Value?.GetDefinition()?.GetDataType() ?? dataType;
      set
      {
        if (IsReferencedData) throw new InvalidOperationException();
        dataType = value;
      }
    }

    DBXS.ParameterGroup group;
    public DBXS.ParameterGroup Group
    {
      get => Value?.GetDefinition()?.GetGroupType() ?? group;
      set
      {
        if (IsReferencedData)
        {
          if (Id.IsBuiltInId()) throw new InvalidOperationException("This operation is not supported on built-in parameters");
          Value?.GetDefinition()?.SetGroupType(value);
        }
        else group = value;
      }
    }

    bool? visible;
    public bool? Visible
    {
      get => Value?.GetDefinition()?.Visible ?? visible;
      set
      {
        if (IsReferencedData) throw new InvalidOperationException();
        visible = value;
      }
    }

    bool? userModifiable;
    public bool? UserModifiable
    {
      get
      {
        if (!IsReferencedData) return userModifiable;
        if (Id is object && Document is DB.Document doc)
        {
          if (doc.IsFamilyDocument)
          {
            var familyParameter = Id.TryGetBuiltInParameter(out var builtInParameter) ?
              doc.FamilyManager.get_Parameter(builtInParameter) :
              Value is DB.ParameterElement element ?
              doc.FamilyManager.get_Parameter(element.GetDefinition()) :
              default;

            return familyParameter?.UserModifiable;
          }
          else
          {
            if (Value is DB.ParameterElement element)
            {
              if (element is DB.GlobalParameter)
                return true;

              return doc.ParameterBindings.Contains(element.GetDefinition());
            }
          }
        }

        return null;
      }
      set
      {
        if (IsReferencedData) throw new InvalidOperationException();
        userModifiable = value;
      }
    }

    bool? hideWhenNoValue;
    public bool? HideWhenNoValue
    {
      get
      {
        if (!IsReferencedData) return hideWhenNoValue;
        switch (Value)
        {
#if REVIT_2020
          case DB.SharedParameterElement shared: return shared.ShouldHideWhenNoValue();
#else
          case DB.SharedParameterElement shared: return false;
#endif
          case DB.GlobalParameter _: return false;
        }

        return default;
      }
      set
      {
        if (IsReferencedData) throw new InvalidOperationException();
        hideWhenNoValue = value;
      }
    }

    bool? reporting;
    public bool? IsReporting
    {
      get
      {
        if (IsValid)
        {
          if (IsReferencedData)
          {
            if (Document is DB.Document doc)
            {
              if (doc.IsFamilyDocument)
              {
                var familyParameter = Id.TryGetBuiltInParameter(out var builtInParameter) ?
                  doc.FamilyManager.get_Parameter(builtInParameter) :
                  Value is DB.ParameterElement element ?
                  doc.FamilyManager.get_Parameter(element.GetDefinition()) :
                  default;

                return familyParameter?.IsReporting;
              }
              else if (Value is DB.GlobalParameter global)
              {
                return global.IsReporting;
              }
            }
          }
          else return reporting;
        }

        return default;
      }
      set
      {
        if (!value.HasValue || !IsValid) return;
        if (IsReferencedData)
        {
          if(Document is DB.Document doc)
          {
            if (doc.IsFamilyDocument)
            {
              var familyParameter = Id.TryGetBuiltInParameter(out var builtInParameter) ?
                doc.FamilyManager.get_Parameter(builtInParameter) :
                Value is DB.ParameterElement element ?
                doc.FamilyManager.get_Parameter(element.GetDefinition()) :
                default;

              if (familyParameter is object && familyParameter.IsReporting != value.Value)
              {
                if (value.Value) doc.FamilyManager.MakeReporting(familyParameter);
                else doc.FamilyManager.MakeNonReporting(familyParameter);
              }
            }
            else if (Value is DB.GlobalParameter global && global.IsReporting != value.Value)
            {
              global.IsReporting = value.Value;
            }
          }
        }
        else reporting = value;
      }
    }

    string formula;
    public string Formula
    {
      get
      {
        if (IsValid)
        {
          if (IsReferencedData)
          {
            if (Document is DB.Document doc)
            {
              if (doc.IsFamilyDocument)
              {
                var familyParameter = Id.TryGetBuiltInParameter(out var builtInParameter) ?
                  doc.FamilyManager.get_Parameter(builtInParameter) :
                  Value is DB.ParameterElement element ?
                  doc.FamilyManager.get_Parameter(element.GetDefinition()) :
                  default;

                return familyParameter?.Formula;
              }
              else if (Value is DB.GlobalParameter global)
              {
                return global.GetFormula();
              }
            }
          }
          else return formula;
        }

        return default;
      }
      set
      {
        if (value is null || !IsValid) return;
        if (IsReferencedData)
        {
          if (Document is DB.Document doc)
          {
            if (doc.IsFamilyDocument)
            {
              var familyParameter = Id.TryGetBuiltInParameter(out var builtInParameter) ?
                doc.FamilyManager.get_Parameter(builtInParameter) :
                Value is DB.ParameterElement element ?
                doc.FamilyManager.get_Parameter(element.GetDefinition()) :
                default;

              if (familyParameter is object && familyParameter.Formula != value)
              {
                doc.FamilyManager.SetFormula(familyParameter, value);
              }
            }
            else if (Value is DB.GlobalParameter global && global.GetFormula() != value)
            {
              global.SetFormula(value);
            }
          }
        }
        else formula = value;
      }
    }

    public DBX.ParameterClass Class
    {
      get
      {
        if (Id is object && Id.TryGetBuiltInParameter(out var _))
          return DBX.ParameterClass.BuiltIn;

        if (!IsReferencedData)
        {
          if (GUID.HasValue) return DBX.ParameterClass.Shared;
          return DBX.ParameterClass.Invalid;
        }

        switch (Value)
        {
          case DB.GlobalParameter _: return DBX.ParameterClass.Global;
          case DB.SharedParameterElement _: return DBX.ParameterClass.Shared;
          case DB.ParameterElement project:
            switch (project.get_Parameter(DB.BuiltInParameter.ELEM_DELETABLE_IN_FAMILY).AsInteger())
            {
              case 0: return DBX.ParameterClass.Family;
              case 1: return DBX.ParameterClass.Project;
            }
            break;
        }

        return DBX.ParameterClass.Invalid;
      }
    }

    public DBX.ParameterBinding Binding
    {
      get
      {
        if (!IsReferencedData) return DBX.ParameterBinding.Unknown;

        if (Document is DB.Document doc)
        {
          if (doc.IsFamilyDocument)
          {
            var familyParameter = Id.TryGetBuiltInParameter(out var bip) ?
              doc.FamilyManager.get_Parameter(bip) :
              Value?.GetDefinition() is DB.InternalDefinition definition ?
              doc.FamilyManager.get_Parameter(definition) :
              default;

            return familyParameter is null ?
              DBX.ParameterBinding.Unknown :
              familyParameter.IsInstance ?
              DBX.ParameterBinding.Instance :
              DBX.ParameterBinding.Type;
          }
          else switch (Value)
          {
            case DB.GlobalParameter _: return DBX.ParameterBinding.Global;
            case DB.ParameterElement parameterElement:
              var definition = parameterElement.GetDefinition();
              if (!Id.IsBuiltInId())
              {
                switch (doc.ParameterBindings.get_Item(definition))
                {
                  case DB.InstanceBinding _: return DBX.ParameterBinding.Instance;
                  case DB.TypeBinding _: return DBX.ParameterBinding.Type;
                }
              }

              return DBX.ParameterBinding.Unknown;
          }
        }

        return DBX.ParameterBinding.Unknown;
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Parameter Value")]
  public class ParameterValue : DocumentObject,
    IEquatable<ParameterValue>,
    IGH_Goo,
    IGH_QuickCast,
    IConvertible
  {
    public ParameterValue() { }
    public ParameterValue(DB.Parameter value) : base(value.Element.Document, value) { }

    #region System.Object
    public override string ToString() => Value.AsGoo()?.ToString();
    public override bool Equals(object obj) =>
      (obj is ElementId id) ? Equals(id) : base.Equals(obj);

    public override int GetHashCode()
    {
      int hashCode = 0;
      if (Value is DB.Parameter value)
      {
        hashCode ^= value.Id.GetHashCode();
        hashCode ^= value.StorageType.GetHashCode();

        if (value.HasValue)
        {
          switch (value.StorageType)
          {
            case DB.StorageType.Integer: hashCode ^= value.AsInteger().GetHashCode(); break;
            case DB.StorageType.Double: hashCode ^= value.AsDouble().GetHashCode(); break;
            case DB.StorageType.String: hashCode ^= value.AsString().GetHashCode(); break;
            case DB.StorageType.ElementId: hashCode ^= value.AsElementId().GetHashCode(); break;
          }
        }
      }

      return hashCode;
    }
    #endregion

    #region IEquatable
    public bool Equals(ParameterValue other)
    {
      if (other is null) return false;
      if (Value is DB.Parameter A && other.Value is DB.Parameter B)
      {
        if
        (
          A.Id.IntegerValue == B.Id.IntegerValue &&
          A.StorageType == B.StorageType &&
          A.HasValue == B.HasValue
        )
        {
          if (!Value.HasValue)
            return true;

          switch (Value.StorageType)
          {
            case DB.StorageType.None: return true;
            case DB.StorageType.Integer: return A.AsInteger() == B.AsInteger();
            case DB.StorageType.Double: return A.AsDouble() == B.AsDouble();
            case DB.StorageType.String: return A.AsString() == B.AsString();
            case DB.StorageType.ElementId: return A.AsElementId() == B.AsElementId();
          }
        }
      }

      return false;
    }
    #endregion

    #region IGH_Goo
    public override bool IsValid => base.IsValid && Value is object;
    public override bool CastFrom(object source)
    {
      if (source is DB.Parameter parameter)
      {
        base.Value = parameter;
        return true;
      }

      return false;
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(DB.Parameter)))
      {
        target = (Q) (object) Value;
        return true;
      }

      var goo = Value.AsGoo();
      if (goo is null)
      {
        target = default;
        return true;
      }

      if (goo is Q q)
      {
        target = q;
        return true;
      }

      return goo.CastTo(out target);
    }

    object IGH_Goo.ScriptVariable() => Value.AsGoo() is IGH_Goo goo ? goo.ScriptVariable() : default;
    #endregion

    #region IGH_QuickCast
    GH_QuickCastType IGH_QuickCast.QC_Type
    {
      get
      {
        //return GH_QuickCastType.text;
        switch (Value.ToConvertible())
        {
          default:
          case bool     _: return GH_QuickCastType.@bool;
          case char     _: return GH_QuickCastType.@int;
          case sbyte    _: return GH_QuickCastType.@int;
          case byte     _: return GH_QuickCastType.@int;
          case short    _: return GH_QuickCastType.@int;
          case ushort   _: return GH_QuickCastType.@int;
          case int      _: return GH_QuickCastType.@int;
          case uint     _: return GH_QuickCastType.@int;
          case long     _: return GH_QuickCastType.@int;
          case ulong    _: return GH_QuickCastType.@int;
          case float    _: return GH_QuickCastType.num;
          case double   _: return GH_QuickCastType.num;
          case decimal  _: return GH_QuickCastType.num;
          case DateTime _: return GH_QuickCastType.num;
          case string   _: return GH_QuickCastType.text;
        }
      }
    }

    double IGH_QuickCast.QC_Distance(IGH_QuickCast other)
    {
      var quickCast = Value.AsGoo() as IGH_QuickCast;
      var quickType = quickCast?.QC_Type ?? (GH_QuickCastType) (-1);

      switch (quickType)
      {
        case GH_QuickCastType.@bool:    return Math.Abs((quickCast.QC_Bool() ? 0.0 : 1.0) - (other.QC_Bool() ? 0.0 : 1.0));
        case GH_QuickCastType.@int:     return Math.Abs(((double) quickCast.QC_Int()) - ((double) other.QC_Int()));
        case GH_QuickCastType.num:      return Math.Abs(((double) quickCast.QC_Num()) - ((double) other.QC_Num()));
        case GH_QuickCastType.text:
          var thisText = quickCast.QC_Text(); var otherText = other.QC_Text();
          var dist0 = Grasshopper.Kernel.GH_StringMatcher.LevenshteinDistance(thisText, otherText);
          var dist1 = Grasshopper.Kernel.GH_StringMatcher.LevenshteinDistance(thisText.ToUpperInvariant(), otherText.ToUpperInvariant());
          return 0.5 * (dist0 + dist1);
        case GH_QuickCastType.col:
          var thisCol = quickCast.QC_Col(); var otherCol = other.QC_Col();
          var colorRGBA = new Rhino.Geometry.Point4d(thisCol.R - otherCol.R, thisCol.G - otherCol.G, thisCol.B - otherCol.B, thisCol.A - otherCol.A);
          colorRGBA *= 1.0 / 255.0;
          return Math.Sqrt(colorRGBA.X * colorRGBA.X + colorRGBA.Y * colorRGBA.Y + colorRGBA.Z * colorRGBA.Z + colorRGBA.W * colorRGBA.W);
        case GH_QuickCastType.pt:       return quickCast.QC_Pt().DistanceTo(other.QC_Pt());
        case GH_QuickCastType.vec:      return ((Rhino.Geometry.Point3d) quickCast.QC_Vec()).DistanceTo((Rhino.Geometry.Point3d) other.QC_Vec());
        case GH_QuickCastType.complex:  throw new InvalidOperationException();
        case GH_QuickCastType.interval: throw new InvalidOperationException();
        case GH_QuickCastType.matrix:   throw new InvalidOperationException();
        default:                        throw new InvalidOperationException();
      }
    }

    int IGH_QuickCast.QC_Hash() => Value.ToConvertible()?.GetHashCode() ?? 0;
    bool IGH_QuickCast.QC_Bool() => System.Convert.ToBoolean(Value.ToConvertible());
    int IGH_QuickCast.QC_Int() => System.Convert.ToInt32(Value.ToConvertible());
    double IGH_QuickCast.QC_Num() => System.Convert.ToDouble(Value.ToConvertible());
    string IGH_QuickCast.QC_Text() => System.Convert.ToString(Value.ToConvertible());
    System.Drawing.Color IGH_QuickCast.QC_Col() => System.Drawing.Color.FromArgb(System.Convert.ToInt32(Value.ToConvertible()));
    Rhino.Geometry.Point3d IGH_QuickCast.QC_Pt() => (Value.AsGoo() as IGH_QuickCast)?.QC_Pt() ??
      throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Rhino.Geometry.Point3d)}");
    Rhino.Geometry.Vector3d IGH_QuickCast.QC_Vec() => (Value.AsGoo() as IGH_QuickCast)?.QC_Vec() ??
      throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Rhino.Geometry.Vector3d)}");
    Complex IGH_QuickCast.QC_Complex() => (Value.AsGoo() as IGH_QuickCast)?.QC_Complex() ??
      throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Complex)}");
    Rhino.Geometry.Matrix IGH_QuickCast.QC_Matrix() => (Value.AsGoo() as IGH_QuickCast)?.QC_Matrix() ??
      throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Rhino.Geometry.Matrix)}");
    Rhino.Geometry.Interval IGH_QuickCast.QC_Interval() => (Value.AsGoo() as IGH_QuickCast)?.QC_Interval() ??
      throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Rhino.Geometry.Interval)}");
    int IGH_QuickCast.QC_CompareTo(IGH_QuickCast other)
    {
      var quickCast = Value.AsGoo() as IGH_QuickCast;
      var quickType = quickCast?.QC_Type ?? (GH_QuickCastType) (-1);

      if (quickType != other.QC_Type) quickType.CompareTo(other.QC_Type);
      return quickCast.QC_CompareTo(other);
    }
    #endregion

    #region IConvertible
    TypeCode IConvertible.GetTypeCode() => TypeCode.Object;

    object IConvertible.ToType(Type conversionType, IFormatProvider provider) => Value.ToConvertible().ToType(conversionType, provider);
    bool IConvertible.ToBoolean(IFormatProvider provider) => Value.ToConvertible().ToBoolean(provider);
    sbyte IConvertible.ToSByte(IFormatProvider provider) => Value.ToConvertible().ToSByte(provider);
    byte IConvertible.ToByte(IFormatProvider provider) => Value.ToConvertible().ToByte(provider);
    char IConvertible.ToChar(IFormatProvider provider) => Value.ToConvertible().ToChar(provider);
    short IConvertible.ToInt16(IFormatProvider provider) => Value.ToConvertible().ToInt16(provider);
    ushort IConvertible.ToUInt16(IFormatProvider provider) => Value.ToConvertible().ToUInt16(provider);
    uint IConvertible.ToUInt32(IFormatProvider provider) => Value.ToConvertible().ToUInt32(provider);
    int IConvertible.ToInt32(IFormatProvider provider) => Value.ToConvertible().ToInt32(provider);
    long IConvertible.ToInt64(IFormatProvider provider) => Value.ToConvertible().ToInt64(provider);
    ulong IConvertible.ToUInt64(IFormatProvider provider) => Value.ToConvertible().ToUInt64(provider);
    float IConvertible.ToSingle(IFormatProvider provider) => Value.ToConvertible().ToSingle(provider);
    double IConvertible.ToDouble(IFormatProvider provider) => Value.ToConvertible().ToDouble(provider);
    decimal IConvertible.ToDecimal(IFormatProvider provider) => Value.ToConvertible().ToDecimal(provider);
    DateTime IConvertible.ToDateTime(IFormatProvider provider) => Value.ToConvertible().ToDateTime(provider);
    string IConvertible.ToString(IFormatProvider provider) => Value.ToConvertible().ToString(provider);
    #endregion

    #region DocumentObject
    public new DB.Parameter Value => base.Value as DB.Parameter;

    public override string DisplayName
    {
      get
      {
        if (Value is DB.Parameter param)
          return param.Definition?.Name;

        return default;
      }
    }
    #endregion
  }
}

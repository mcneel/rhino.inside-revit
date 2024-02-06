using System;
using System.Collections.Generic;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using Grasshopper.Special;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

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
        return parameterClass != ERDB.ParameterClass.Invalid ?
        $"Revit {parameterClass} Parameter" : "Revit Parameter";
      }
    }

    public ParameterKey() { }
    public ParameterKey(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public ParameterKey(ARDB.ParameterElement element) : base(element) { }

    ParameterKey(ARDB.Definition value)
    {
      name = value.Name;
      dataType = value.GetDataType();
      group = value.GetGroupType();
    }

    public ParameterKey(ARDB.ExternalDefinition value) :
      this((ARDB.Definition) value)
    {
      guid = value.GUID;
      visible = value.Visible;
      description = value.Description;
      userModifiable = value.UserModifiable;
#if REVIT_2020
      hideWhenNoValue = value.HideWhenNoValue;
#endif
    }

    public ParameterKey(ARDB.Document doc, ARDB.InternalDefinition value) :
      this(doc, value.Id)
    {
      name = value.Name;
      dataType = value.GetDataType();
      group = value.GetGroupType();

      if (doc.GetElement(value.Id) is ARDB.SharedParameterElement parameter)
      {
        guid = parameter.GuidValue;
#if REVIT_2020
        hideWhenNoValue = parameter.ShouldHideWhenNoValue();
#endif
      }

      visible = value.Visible;
    }

    public ParameterKey Duplicate() => (ParameterKey) MemberwiseClone();

    #region System.Object
    public override bool Equals(object obj) => obj is ParameterKey other && Equals(other);

    public override int GetHashCode() => IsReferencedData ?
      base.GetHashCode() :
      (GUID, Nomen, Description, DataType, Group, Visible, UserModifiable, HideWhenNoValue).
      GetHashCode();
    #endregion

    #region IEquatable
    public bool Equals(ParameterKey other)
    {
      return IsReferencedData ?
      base.Equals(other) :
      GUID == other.GUID &&
      Nomen == other.Nomen &&
      Description == other.Description &&
      DataType == other.DataType &&
      Group == other.Group &&
      Visible == other.Visible &&
      UserModifiable == other.UserModifiable &&
      HideWhenNoValue == other.HideWhenNoValue;
    }
    #endregion

    #region DocumentObject
    public static new ParameterKey FromElementId(ARDB.Document doc, ARDB.ElementId id)
    {
      if (id.IsParameterId(doc))
        return new ParameterKey(doc, id);

      return null;
    }

    public new ARDB.ParameterElement Value => base.Value as ARDB.ParameterElement;

    public override string DisplayName
    {
      get
      {
        try
        {
          if (Id is object && Id.TryGetBuiltInParameter(out var builtInParameter))
            return ARDB.LabelUtils.GetLabelFor(builtInParameter) ?? base.DisplayName;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

        return base.DisplayName ?? GUID.GetValueOrDefault().ToString("B");
      }
    }
    #endregion

    #region IGH_Goo
    public override bool IsValid => IsReferencedData ?
      ((Id?.TryGetBuiltInParameter(out var _) == true) || base.IsValid) :
      (name is object && ElementNaming.IsValidName(name) || GUID.HasValue);

    protected override Type ValueType => typeof(ARDB.ParameterElement);
    public override object ScriptVariable()
    {
      if (Id.TryGetBuiltInParameter(out var builtin))
        return builtin;

      if (IsReferencedData)
        return Value;

      if (CastTo(out ARDB.ExternalDefinitionCreationOptions external))
        return external;

      return null;
    }

    public sealed override bool CastFrom(object source)
    {
      if (base.CastFrom(source))
        return true;

      var document = Revit.ActiveDBDocument;
      var parameterId = ARDB.ElementId.InvalidElementId;

      if (source is IGH_Goo goo)
      {
        if (source is IGH_Element element)
        {
          document = element.Document;
          parameterId = element.Id;
        }
        else if (source is ParameterId id)
        {
          source = (ARDB.BuiltInParameter) id.Value;
        }
        else if (source is ParameterValue parameterValue)
        {
          source = parameterValue.Value;
        }
        else source = goo.ScriptVariable();
      }

      switch (source)
      {
        case int integer: parameterId = ElementIdExtension.FromValue(integer); break;
        case ARDB.BuiltInParameter bip: parameterId = new ARDB.ElementId(bip); break;
        case ARDB.ElementId id: parameterId = id; break;
        case ARDB.Parameter parameter: return SetParameter(parameter);
        case string n:
          if (ERDB.Schemas.ParameterId.IsParameterId(n))
          {
            parameterId = new ARDB.ElementId(new ERDB.Schemas.ParameterId(n));
            break;
          }

          if (ElementNaming.IsValidName(n))
          {
            name = n;
            return true;
          }

          return false;
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

      if (typeof(Q).IsAssignableFrom(typeof(ARDB.ExternalDefinitionCreationOptions)))
      {
        if (IsValid)
        {
          var options = new ARDB.ExternalDefinitionCreationOptions(Nomen, DataType)
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
          var id = owner.Id ?? ARDB.ElementId.InvalidElementId;
          if (id.TryGetBuiltInParameter(out var bip) == true)
            return bip.ToStringGeneric();

          return id.ToValue().ToString();
        }
        else return owner.DisplayName;
      }
      public override bool FromString(string str)
      {
        if (Enum.TryParse(str, out ARDB.BuiltInParameter builtInParameter))
        {
          owner.SetValue(owner.Document ?? Revit.ActiveUIDocument.Document, new ARDB.ElementId(builtInParameter));
          return true;
        }

        return false;
      }

      #region Misc
      protected override bool IsValidId(ARDB.Document doc, ARDB.ElementId id) => id.IsParameterId(doc);
      public override Type ObjectType => !owner.IsReferencedData ?
        typeof(ARDB.Definition) :
        IsBuiltIn ? typeof(ARDB.BuiltInParameter) : base.ObjectType;

      [System.ComponentModel.Description("BuiltIn parameter Id.")]
      public ARDB.BuiltInParameter? BuiltInId
      {
        get
        {
          if (owner.Id.TryGetBuiltInParameter(out var bip)) return bip;
          return default;
        }
      }

      [System.ComponentModel.Description("Forge Id.")]
      public ERDB.Schemas.ParameterId SchemaId => owner.Id.TryGetBuiltInParameter(out var bip) ?
        (ERDB.Schemas.ParameterId) bip : default;
      #endregion

      #region Definition
      const string Definition = "Definition";

      [System.ComponentModel.Category(Definition), System.ComponentModel.Description("The Guid that identifies this parameter as a shared parameter.")]
      public Guid? Guid => owner.GUID;

      [System.ComponentModel.Category(Definition), System.ComponentModel.Description("Internal parameter data storage type.")]
      public ARDB.StorageType? StorageType => BuiltInId.HasValue ? Revit.ActiveDBDocument?.get_TypeOfStorage(BuiltInId.Value) : owner.DataType.ToStorageType();

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
        new External.DB.Schemas.DataType(_dataType) : null;

      var _group = default(string);
      group = reader.TryGetString("Group", ref _group) ?
        new External.DB.Schemas.ParameterGroup(_group) : null;

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
    System.Drawing.Bitmap IGH_ItemDescription.GetTypeIcon(System.Drawing.Size size) => Properties.Resources.Parameter;

    string IGH_ItemDescription.Identity
    {
      get
      {
        if (Id is object && Id.TryGetBuiltInParameter(out var bip))
          return ((ERDB.Schemas.ParameterId) bip).Name;

        if (IsReferencedData)
        {
          return GUID.HasValue ?
            $"{{{Id?.ToValue()}}} : {GUID.Value:B}" :
            $"{{{Id?.ToValue()}}}";
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
      ((External.DB.Schemas.ParameterId) bip).Namespace :
      DataType?.Label;
    #endregion

    #region Properties
    Guid? guid;
    public Guid? GUID => (Value as ARDB.SharedParameterElement)?.GuidValue ?? guid;

    string name;
    public override string Nomen
    {
      get
      {
        if (!IsReferencedData) return name;

        try
        {
          if (Id is object && Id.TryGetBuiltInParameter(out var builtInParameter))
            return ARDB.LabelUtils.GetLabelFor(builtInParameter) ?? base.Nomen;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

        return base.Nomen;
      }
      set
      {
        if (!IsReferencedData)
        {
          if (!ElementNaming.IsValidName(value))
            throw new ArgumentException("Invalid parameter name");

          name = value;
        }
        else if (Document.IsFamilyDocument && Class == ERDB.ParameterClass.Family)
        {
          var familyParameter = Document.FamilyManager.get_Parameter(Nomen);
          Document.FamilyManager.RenameParameter(familyParameter, value);
        }
        else base.Nomen = value;
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

    internal static readonly Dictionary<ARDB.BuiltInParameter, ERDB.Schemas.DataType> BuiltInParametersTypes = new Dictionary<ARDB.BuiltInParameter, ERDB.Schemas.DataType>()
    {
      { ARDB.BuiltInParameter.RASTER_SYMBOL_WIDTH,  ERDB.Schemas.SpecType.Measurable.Length },
      { ARDB.BuiltInParameter.RASTER_SYMBOL_HEIGHT, ERDB.Schemas.SpecType.Measurable.Length },
      { ARDB.BuiltInParameter.RASTER_SHEETWIDTH,    ERDB.Schemas.SpecType.Measurable.Length },
      { ARDB.BuiltInParameter.RASTER_SHEETHEIGHT,   ERDB.Schemas.SpecType.Measurable.Length }
    };

    ERDB.Schemas.DataType dataType;
    public ERDB.Schemas.DataType DataType
    {
      get
      {
        if (dataType is null && Document is ARDB.Document doc)
        {
          if (Id is object && Id.TryGetBuiltInParameter(out var builtInParameter))
          {
            if (!BuiltInParametersTypes.TryGetValue(builtInParameter, out dataType))
            {
              switch (doc.get_TypeOfStorage(builtInParameter))
              {
                case ARDB.StorageType.Integer:
                  dataType = ERDB.Schemas.SpecType.Int.Integer;
                  break;

                case ARDB.StorageType.Double:

                  var categoriesWhereDefined = doc.GetBuiltInCategoriesWithParameters().
                    Select(bic => new ARDB.ElementId(bic)).
                    Where(cid => ARDB.TableView.GetAvailableParameters(doc, cid).AsReadOnlyElementIdSet().Contains(Id)).
                    ToArray();

                  // Look into a Schedule table
                  using (var scope = ERDB.DisposableScope.RollBackScope(doc))
                  {
                    foreach (var categoryId in categoriesWhereDefined)
                    {
                      var schedule = default(ARDB.ViewSchedule);
                      if (ARDB.ViewSchedule.IsValidCategoryForSchedule(categoryId))
                      {
                        if (categoryId.ToBuiltInCategory() == ARDB.BuiltInCategory.OST_Areas)
                        {
                          using (var collector = new ARDB.FilteredElementCollector(doc))
                          {
                            var areaSchemeId = collector.OfClass(typeof(ARDB.AreaScheme)).FirstElementId();
                            schedule = ARDB.ViewSchedule.CreateSchedule(doc, categoryId, areaSchemeId);
                          }
                        }
                        else schedule = ARDB.ViewSchedule.CreateSchedule(doc, categoryId);
                      }
                      else if (ARDB.ViewSchedule.IsValidCategoryForMaterialTakeoff(categoryId))
                      {
                        schedule = ARDB.ViewSchedule.CreateMaterialTakeoff(doc, categoryId);
                      }

                      if (schedule is object)
                      {
                        try
                        {
                          using (var field = schedule.Definition.AddField(ARDB.ScheduleFieldType.Instance, Id))
                            dataType = field.GetDataType();

                          break;
                        }
                        catch (Autodesk.Revit.Exceptions.ArgumentsInconsistentException) { }

                        try
                        {
                          using (var field = schedule.Definition.AddField(ARDB.ScheduleFieldType.ElementType, Id))
                            dataType = field.GetDataType();

                          break;
                        }
                        catch (Autodesk.Revit.Exceptions.ArgumentsInconsistentException) { }
                      }
                      else if (builtInParameter.ToString().EndsWith("_COST"))
                      {
                        dataType = ERDB.Schemas.SpecType.Measurable.Currency;
                        break;
                      }
                    }

                    dataType = dataType ?? ERDB.Schemas.SpecType.Measurable.Number;
                  }
                  break;

                case ARDB.StorageType.ElementId:
                  if (builtInParameter.ToString().EndsWith("_IMAGE"))
                    dataType = ERDB.Schemas.SpecType.Reference.Image;
                  else if (builtInParameter.ToString().EndsWith("_MATERIAL"))
                    dataType = ERDB.Schemas.SpecType.Reference.Material;

                  break;

                case ARDB.StorageType.String:
                  if (builtInParameter.ToString().EndsWith("_URL"))
                    dataType = ERDB.Schemas.SpecType.String.Url;
                  else
                    dataType = ERDB.Schemas.SpecType.String.Text;
                  break;
              }

              BuiltInParametersTypes.Add(builtInParameter, dataType);
            }
          }
          else dataType = Value?.GetDefinition()?.GetDataType();
        }

        return dataType;
      }
      set
      {
        if (IsReferencedData) throw new InvalidOperationException();
        dataType = value;
      }
    }

    ERDB.Schemas.ParameterGroup group;
    public ERDB.Schemas.ParameterGroup Group
    {
      get => group ?? (group = Value?.GetDefinition()?.GetGroupType());
      set
      {
        if (IsReferencedData)
        {
          if (Id.IsBuiltInId()) throw new InvalidOperationException("This operation is not supported on built-in parameters");
          Value?.GetDefinition()?.SetGroupType(value);
        }

        group = value;
      }
    }

    bool? visible;
    public bool? Visible
    {
      get => visible ?? (visible = Value?.GetDefinition()?.Visible);
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
        if (Id is object && Document is ARDB.Document doc)
        {
          if (doc.IsFamilyDocument)
          {
            var familyParameter = Id.TryGetBuiltInParameter(out var builtInParameter) ?
              doc.FamilyManager.get_Parameter(builtInParameter) :
              Value is ARDB.ParameterElement element ?
              doc.FamilyManager.get_Parameter(element.GetDefinition()) :
              default;

            return familyParameter?.UserModifiable;
          }
          else
          {
            if (Value is ARDB.ParameterElement element)
            {
              if (element is ARDB.GlobalParameter)
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
          case ARDB.SharedParameterElement shared: return shared.ShouldHideWhenNoValue();
#else
          case ARDB.SharedParameterElement shared: return false;
#endif
          case ARDB.GlobalParameter _: return false;
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
            if (Document is ARDB.Document doc)
            {
              if (doc.IsFamilyDocument)
              {
                var familyParameter = Id.TryGetBuiltInParameter(out var builtInParameter) ?
                  doc.FamilyManager.get_Parameter(builtInParameter) :
                  Value is ARDB.ParameterElement element ?
                  doc.FamilyManager.get_Parameter(element.GetDefinition()) :
                  default;

                return familyParameter?.IsReporting;
              }
              else if (Value is ARDB.GlobalParameter global)
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
          if(Document is ARDB.Document doc)
          {
            if (doc.IsFamilyDocument)
            {
              var familyParameter = Id.TryGetBuiltInParameter(out var builtInParameter) ?
                doc.FamilyManager.get_Parameter(builtInParameter) :
                Value is ARDB.ParameterElement element ?
                doc.FamilyManager.get_Parameter(element.GetDefinition()) :
                default;

              if (familyParameter is object && familyParameter.IsReporting != value.Value)
              {
                if (value.Value) doc.FamilyManager.MakeReporting(familyParameter);
                else doc.FamilyManager.MakeNonReporting(familyParameter);
              }
            }
            else if (Value is ARDB.GlobalParameter global && global.IsReporting != value.Value)
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
            if (Document is ARDB.Document doc)
            {
              if (doc.IsFamilyDocument)
              {
                var familyParameter = Id.TryGetBuiltInParameter(out var builtInParameter) ?
                  doc.FamilyManager.get_Parameter(builtInParameter) :
                  Value is ARDB.ParameterElement element ?
                  doc.FamilyManager.get_Parameter(element.GetDefinition()) :
                  default;

                return familyParameter?.Formula;
              }
              else if (Value is ARDB.GlobalParameter global)
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
          if (Document is ARDB.Document doc)
          {
            if (doc.IsFamilyDocument)
            {
              var familyParameter = Id.TryGetBuiltInParameter(out var builtInParameter) ?
                doc.FamilyManager.get_Parameter(builtInParameter) :
                Value is ARDB.ParameterElement element ?
                doc.FamilyManager.get_Parameter(element.GetDefinition()) :
                default;

              if (familyParameter is object && familyParameter.Formula != value)
              {
                doc.FamilyManager.SetFormula(familyParameter, value);
              }
            }
            else if (Value is ARDB.GlobalParameter global && global.GetFormula() != value)
            {
              global.SetFormula(value);
            }
          }
        }
        else formula = value;
      }
    }

    public ERDB.ParameterClass Class
    {
      get
      {
        if (Id is object && Id.TryGetBuiltInParameter(out var _))
          return ERDB.ParameterClass.BuiltIn;

        if (!IsReferencedData)
        {
          if (GUID.HasValue) return ERDB.ParameterClass.Shared;
          return ERDB.ParameterClass.Invalid;
        }

        switch (Value)
        {
          case ARDB.GlobalParameter _: return ERDB.ParameterClass.Global;
          case ARDB.SharedParameterElement _: return ERDB.ParameterClass.Shared;
          case ARDB.ParameterElement project:
            switch (project.get_Parameter(ARDB.BuiltInParameter.ELEM_DELETABLE_IN_FAMILY).AsInteger())
            { 
              case 0: return ERDB.ParameterClass.Family;
              case 1: return ERDB.ParameterClass.Project;
            }
            break;
        }

        return ERDB.ParameterClass.Invalid;
      }
    }

    public ERDB.ParameterScope Scope
    {
      get
      {
        if (!IsReferencedData) return ERDB.ParameterScope.Unknown;

        if (Document is ARDB.Document doc)
        {
          if (doc.IsFamilyDocument)
          {
            var familyParameter = Id.TryGetBuiltInParameter(out var bip) ?
              doc.FamilyManager.get_Parameter(bip) :
              Value?.GetDefinition() is ARDB.InternalDefinition definition ?
              doc.FamilyManager.get_Parameter(definition) :
              default;

            return familyParameter is null ?
              ERDB.ParameterScope.Unknown :
              familyParameter.IsInstance ?
              ERDB.ParameterScope.Instance :
              ERDB.ParameterScope.Type;
          }
          else switch (Value)
          {
            case ARDB.GlobalParameter _: return ERDB.ParameterScope.Global;
            case ARDB.ParameterElement parameterElement:
              var definition = parameterElement.GetDefinition();
              if (!Id.IsBuiltInId())
              {
                switch (doc.ParameterBindings.get_Item(definition))
                {
                  case ARDB.InstanceBinding _:  return ERDB.ParameterScope.Instance;
                  case ARDB.TypeBinding _:      return ERDB.ParameterScope.Type;
                }
              }

              return ERDB.ParameterScope.Unknown;
          }
        }

        return ERDB.ParameterScope.Unknown;
      }
    }
    #endregion

    #region Parameter
    protected bool SetParameter(ARDB.Parameter parameter)
    {
      SetValue(parameter.Element.Document, parameter.Id);
      if (parameter.Definition is ARDB.InternalDefinition definition)
      {
        name = definition.Name;
        dataType = definition.GetDataType();
        group = definition.GetGroupType();
        visible = definition.Visible;
      }

      //if (dataType == External.DB.Schemas.DataType.Empty)
      //{
      //  switch (parameter.StorageType)
      //  {
      //    case ARDB.StorageType.Integer: dataType = External.DB.Schemas.SpecType.Int.Integer; break;
      //    case ARDB.StorageType.Double:  dataType = External.DB.Schemas.SpecType.Measurable.Number; break;
      //    case ARDB.StorageType.String:  dataType = External.DB.Schemas.SpecType.String.Text; break;
      //    case ARDB.StorageType.ElementId:
      //      if (parameter.HasValue)
      //      {
      //        if (Document.GetElement(parameter.AsElementId()) is ARDB.Element value)
      //          if (value.Category is ARDB.Category category)
      //            if (category.Id.TryGetBuiltInCategory(out var categoryId))
      //            {
      //              dataType = (External.DB.Schemas.CategoryId) categoryId;
      //            }
      //      }
      //      break;
      //  }
      //}

      userModifiable = parameter.UserModifiable;
      if (parameter.IsShared) guid = parameter.GUID;
      else guid = null;

      return true;
    }

    internal ARDB.Parameter GetParameter(ARDB.Element element)
    {
      if (IsReferencedData)
      {
        switch (Class)
        {
          case ERDB.ParameterClass.BuiltIn:
            return Id.TryGetBuiltInParameter(out var builtInParameter) ? element.get_Parameter(builtInParameter) : default;

          case ERDB.ParameterClass.Project:
            return element.GetParameter(Nomen, DataType, ERDB.ParameterClass.Project);

          case ERDB.ParameterClass.Family:
            return element.GetParameter(Nomen, DataType, ERDB.ParameterClass.Family);

          case ERDB.ParameterClass.Shared:
            return element.get_Parameter(GUID.Value); 
        }
      }
      else
      {
        if (GUID.HasValue)
          return element.get_Parameter(GUID.Value);

        if (Id.TryGetBuiltInParameter(out var bip))
          return element.get_Parameter(bip);

        if (!string.IsNullOrEmpty(Nomen))
          return element.GetParameter(Nomen, ERDB.ParameterClass.Any);
      }

      return default;
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
    public ParameterValue(ARDB.Parameter value) : base(value.Element.Document, value) { }

    #region System.Object
    public override string ToString() => Value.AsGoo()?.ToString();
    public override bool Equals(object obj) => obj is ParameterValue id && Equals(id);

    public override int GetHashCode()
    {
      int hashCode = 0;
      if (Value is ARDB.Parameter value)
      {
        hashCode ^= value.Id.GetHashCode();
        hashCode ^= value.StorageType.GetHashCode();

        if (value.HasValue)
        {
          switch (value.StorageType)
          {
            case ARDB.StorageType.Integer:    hashCode ^= value.AsInteger().GetHashCode(); break;
            case ARDB.StorageType.Double:     hashCode ^= value.AsDouble().GetHashCode(); break;
            case ARDB.StorageType.String:     hashCode ^= value.AsString().GetHashCode(); break;
            case ARDB.StorageType.ElementId:  hashCode ^= value.AsElementId().GetHashCode(); break;
          }
        }
      }

      return hashCode;
    }
    #endregion

    #region IEquatable
    public bool Equals(ParameterValue other)
    {
      if (Value is ARDB.Parameter A && other?.Value is ARDB.Parameter B)
      {
        if
        (
          A.Id == B.Id &&
          A.StorageType == B.StorageType &&
          A.HasValue == B.HasValue
        )
        {
          if (!A.HasValue)
            return true;

          switch (A.StorageType)
          {
            case ARDB.StorageType.None:       return true;
            case ARDB.StorageType.Integer:    return A.AsInteger() == B.AsInteger();
            case ARDB.StorageType.Double:     return A.AsDouble() == B.AsDouble();
            case ARDB.StorageType.String:     return A.AsString() == B.AsString();
            case ARDB.StorageType.ElementId:  return A.AsElementId() == B.AsElementId() && Document.Equals(other.Document);
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
      if (source is ARDB.Parameter parameter)
      {
        base.Value = parameter;
        return true;
      }

      return false;
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.Parameter)))
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
    GH_QuickCastType IGH_QuickCast.QC_Type => GH_QuickCastType.text;
    int IGH_QuickCast.QC_Hash() => (Value.AsGoo() as IGH_QuickCast)?.QC_Hash() ?? 0;
    double IGH_QuickCast.QC_Distance(IGH_QuickCast other) => (Value.AsGoo() as IGH_QuickCast)?.QC_Distance(other) ?? double.NaN;

    bool IGH_QuickCast.QC_Bool() => (Value.AsGoo() as IGH_QuickCast)?.QC_Bool() ??
      throw new InvalidCastException($"'{DisplayName}' cannot be cast to {nameof(System.Double)}");
    int IGH_QuickCast.QC_Int() => (Value.AsGoo() as IGH_QuickCast)?.QC_Int() ??
      throw new InvalidCastException($"'{DisplayName}' cannot be cast to {nameof(System.Int32)}");
    double IGH_QuickCast.QC_Num() => (Value.AsGoo() as IGH_QuickCast)?.QC_Num() ??
      throw new InvalidCastException($"'{DisplayName}' cannot be cast to {nameof(System.Double)}");

    string IGH_QuickCast.QC_Text()
    {
      var value = Value.AsGoo();
      return value is GH_Number number ?
        number.Value.ToString("G17", System.Globalization.CultureInfo.InvariantCulture) :
        (value as IGH_QuickCast)?.QC_Text() ??
        throw new InvalidCastException($"'{DisplayName}' cannot be cast to {nameof(System.String)}");
    }
    System.Drawing.Color IGH_QuickCast.QC_Col() => (Value.AsGoo() as IGH_QuickCast)?.QC_Col() ??
      throw new InvalidCastException($"'{DisplayName}' cannot be cast to {nameof(System.Drawing.Color)}");
    Rhino.Geometry.Point3d IGH_QuickCast.QC_Pt() => (Value.AsGoo() as IGH_QuickCast)?.QC_Pt() ??
      throw new InvalidCastException($"'{DisplayName}' cannot be cast to {nameof(Rhino.Geometry.Point3d)}");
    Rhino.Geometry.Vector3d IGH_QuickCast.QC_Vec() => (Value.AsGoo() as IGH_QuickCast)?.QC_Vec() ??
      throw new InvalidCastException($"'{DisplayName}' cannot be cast to {nameof(Rhino.Geometry.Vector3d)}");
    Complex IGH_QuickCast.QC_Complex() => (Value.AsGoo() as IGH_QuickCast)?.QC_Complex() ??
      throw new InvalidCastException($"'{DisplayName}' cannot be cast to {nameof(Complex)}");
    Rhino.Geometry.Matrix IGH_QuickCast.QC_Matrix() => (Value.AsGoo() as IGH_QuickCast)?.QC_Matrix() ??
      throw new InvalidCastException($"'{DisplayName}' cannot be cast to {nameof(Rhino.Geometry.Matrix)}");
    Rhino.Geometry.Interval IGH_QuickCast.QC_Interval() => (Value.AsGoo() as IGH_QuickCast)?.QC_Interval() ??
      throw new InvalidCastException($"'{DisplayName}' cannot be cast to {nameof(Rhino.Geometry.Interval)}");
    int IGH_QuickCast.QC_CompareTo(IGH_QuickCast other) => (Value.AsGoo() as IGH_QuickCast)?.QC_CompareTo(other) ??
      throw new InvalidCastException($"'{DisplayName}' cannot be compared");
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
    public new ARDB.Parameter Value => base.Value is ARDB.Parameter parameter && parameter.Element is object ? parameter : null;

    public override string DisplayName
    {
      get
      {
        if (Value is ARDB.Parameter param)
          return param.Definition?.Name;

        return default;
      }
    }
    #endregion
  }
}

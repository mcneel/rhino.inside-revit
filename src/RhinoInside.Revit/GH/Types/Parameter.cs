using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class ParameterKey : Element
  {
    public override string TypeName => "Revit ParameterKey";
    public override string TypeDescription => "Represents a Revit parameter definition";
    override public object ScriptVariable() => null;
    protected override Type ScriptVariableType => typeof(DB.ParameterElement);

    #region IGH_ElementId
    public override bool LoadElement()
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
        return Document.TryGetParameterId(UniqueID, out m_value);

      return false;
    }
    #endregion

    public ParameterKey() { }
    public ParameterKey(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public ParameterKey(DB.ParameterElement element) : base(element) { }

    new public static ParameterKey FromElementId(DB.Document doc, DB.ElementId id)
    {
      if (id.IsParameterId(doc))
        return new ParameterKey(doc, id);

      return null;
    }

    public override sealed bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      var parameterId = DB.ElementId.InvalidElementId;
      switch (source)
      {
        case DB.ParameterElement     parameterElement: SetValue(parameterElement.Document, parameterElement.Id); return true;
        case DB.Parameter            parameter:        SetValue(parameter.Element.Document, parameter.Id); return true;
        case DB.ElementId id:        parameterId = id; break;
        case int integer:            parameterId = new DB.ElementId(integer); break;
      }

      if (parameterId.IsParameterId(Revit.ActiveDBDocument))
      {
        SetValue(Revit.ActiveDBDocument, parameterId);
        return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(GH_Guid)))
      {
        target = (Q) (object) (Document.GetElement(Value) as DB.SharedParameterElement)?.GuidValue;
        return true;
      }

      return base.CastTo<Q>(ref target);
    }

    new class Proxy : Element.Proxy
    {
      public Proxy(ParameterKey o) : base(o) { (this as IGH_GooProxy).UserString = FormatInstance(); }

      public override bool IsParsable() => true;
      public override string FormatInstance() => ((DB.BuiltInParameter) owner.Value.IntegerValue).ToStringGeneric();
      public override bool FromString(string str)
      {
        if (Enum.TryParse(str, out DB.BuiltInParameter builtInParameter))
        {
          owner.Value = new DB.ElementId(builtInParameter);
          return true;
        }

        return false;
      }

      DB.BuiltInParameter builtInParameter => owner.Id.TryGetBuiltInParameter(out var bip) ? bip : DB.BuiltInParameter.INVALID;
      DB.ParameterElement parameter => IsBuiltIn ? null : owner.Document?.GetElement(owner.Id) as DB.ParameterElement;

      [System.ComponentModel.Description("The Guid that identifies this parameter as a shared parameter.")]
      public Guid Guid => (parameter as DB.SharedParameterElement)?.GuidValue ?? Guid.Empty;
      [System.ComponentModel.Description("The user-visible name for the parameter.")]
      public string Name => builtInParameter != DB.BuiltInParameter.INVALID ? DB.LabelUtils.GetLabelFor(builtInParameter) : parameter?.GetDefinition().Name ?? string.Empty;
      [System.ComponentModel.Description("API Object Type.")]
      public override Type ObjectType => IsBuiltIn ? typeof(DB.BuiltInParameter) : parameter?.GetType();

      [System.ComponentModel.Category("Other"), System.ComponentModel.Description("Internal parameter data storage type.")]
      public DB.StorageType StorageType => builtInParameter != DB.BuiltInParameter.INVALID ? Revit.ActiveDBDocument.get_TypeOfStorage(builtInParameter) : parameter?.GetDefinition().ParameterType.ToStorageType() ?? DB.StorageType.None;
      [System.ComponentModel.Category("Other"), System.ComponentModel.Description("Visible in UI.")]
      public bool Visible => IsBuiltIn ? Valid : parameter?.GetDefinition().Visible ?? false;
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);

    public override string Tooltip
    {
      get
      {
        if (Document?.GetElement(Id) is DB.ParameterElement element)
          return element.Name;

        try
        {
          var builtInParameterLabel = DB.LabelUtils.GetLabelFor((DB.BuiltInParameter) Value.IntegerValue);
          return builtInParameterLabel ?? string.Empty;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

        return base.Tooltip;
      }
    }
  }

  public class ParameterValue : GH_Goo<DB.Parameter>
  {
    public override string TypeName => "Revit ParameterValue";
    public override string TypeDescription => "Represents a Revit parameter value on an element";
    protected Type ScriptVariableType => typeof(DB.Parameter);
    public override bool IsValid => Value is object;
    public override sealed IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    double ToRhino(double value, DB.ParameterType type)
    {
      switch (type)
      {
        case DB.ParameterType.Length: return value * Math.Pow(Revit.ModelUnits, 1.0);
        case DB.ParameterType.Area:   return value * Math.Pow(Revit.ModelUnits, 2.0);
        case DB.ParameterType.Volume: return value * Math.Pow(Revit.ModelUnits, 3.0);
      }

      return value;
    }

    public override bool CastFrom(object source)
    {
      if (source is DB.Parameter parameter)
      {
        Value = parameter;
        return true;
      }

      return false;
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsSubclassOf(ScriptVariableType))
      {
        target = (Q) (object) Value;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(ScriptVariableType))
      {
        target = (Q) (object) Value;
        return true;
      }

      switch (Value.StorageType)
      {
        case DB.StorageType.Integer:
          if (typeof(Q).IsAssignableFrom(typeof(GH_Boolean)))
          {
            target = Value.Element is null ? (Q) (object) null :
                     (Q) (object) new GH_Boolean(Value.AsInteger() != 0);
            return true;
          }
          else if (typeof(Q).IsAssignableFrom(typeof(GH_Integer)))
          {
            target = Value.Element is null ? (Q) (object) null :
                     (Q) (object) new GH_Integer(Value.AsInteger());
            return true;
          }
          else if (typeof(Q).IsAssignableFrom(typeof(GH_Number)))
          {
            target = Value.Element is null ? (Q) (object) null :
                     (Q) (object) new GH_Number((double)Value.AsInteger());
            return true;
          }
          else if (typeof(Q).IsAssignableFrom(typeof(GH_Colour)))
          {
            if (Value.Element is object)
            {
              int value = Value.AsInteger();
              int r = value % 256;
              value /= 256;
              int g = value % 256;
              value /= 256;
              int b = value % 256;

              target = (Q) (object) new GH_Colour(System.Drawing.Color.FromArgb(r, g, b));
            }
            else
              target = (Q) (object) null;
            return true;
          }
          break;
        case DB.StorageType.Double:
          if (typeof(Q).IsAssignableFrom(typeof(GH_Number)))
          {
            target = Value.Element is null ? (Q) (object) null :
                     (Q) (object) new GH_Number(ToRhino(Value.AsDouble(), Value.Definition.ParameterType));
            return true;
          }
          else if (typeof(Q).IsAssignableFrom(typeof(GH_Integer)))
          {
            if (Value.Element is object)
            {
              var value = Math.Round(ToRhino(Value.AsDouble(), Value.Definition.ParameterType));
              if (int.MinValue <= value && value <= int.MaxValue)
              {
                target = (Q) (object) new GH_Integer((int) value);
                return true;
              }
            }
            else
            {
              target = (Q) (object) null;
              return true;
            }
          }
          break;
        case DB.StorageType.String:
          if (typeof(Q).IsAssignableFrom(typeof(GH_String)))
          {
            target = Value.Element is null ? (Q) (object) null :
                     (Q) (object) new GH_String(Value.AsString());
            return true;
          }
          break;
        case DB.StorageType.ElementId:
          if (typeof(Q).IsSubclassOf(typeof(ID)))
          {
            target = Value.Element is null ? (Q) (object) null :
                     (Q) (object) ID.FromElementId(Value.Element.Document, Value.AsElementId());
            return true;
          }
          break;
      }

      return base.CastTo<Q>(ref target);
    }

    public override bool Equals(object obj)
    {
      if (obj is ParameterValue paramValue)
      {
        if
        (
          paramValue.Value.Id.IntegerValue == Value.Id.IntegerValue &&
          paramValue.Value.Element.Id.IntegerValue == Value.Element.Id.IntegerValue &&
          paramValue.Value.StorageType == Value.StorageType &&
          paramValue.Value.HasValue == Value.HasValue
        )
        {
          if (!Value.HasValue)
            return true;

          switch (Value.StorageType)
          {
            case DB.StorageType.None:    return true;
            case DB.StorageType.Integer: return paramValue.Value.AsInteger() == Value.AsInteger();
            case DB.StorageType.Double:  return paramValue.Value.AsDouble() == Value.AsDouble();
            case DB.StorageType.String:   return paramValue.Value.AsString() == Value.AsString();
            case DB.StorageType.ElementId: return paramValue.Value.AsElementId().IntegerValue == Value.AsElementId().IntegerValue;
          }
        }
      }

      return base.Equals(obj);
    }

    public override int GetHashCode() => Value.Id.IntegerValue;
    
    public override string ToString()
    {
      if (!IsValid)
        return null;

      string value = default;
      try
      {
        if (Value.HasValue)
        {
          switch (Value.StorageType)
          {
            case DB.StorageType.Integer:
              if (Value.Definition.ParameterType == DB.ParameterType.YesNo)
                value = Value.AsInteger() == 0 ? "False" : "True";
              else
                value = Value.AsInteger().ToString();
              break;
            case DB.StorageType.Double: value = ToRhino(Value.AsDouble(), Value.Definition.ParameterType).ToString(); break;
            case DB.StorageType.String: value = Value.AsString(); break;
            case DB.StorageType.ElementId:

              if (ID.FromElementId(Value.Element.Document, Value.AsElementId()) is ID goo)
                return goo.ToString();

              value = string.Empty;
              break;
            default:
              throw new NotImplementedException();
          }
        }
      }
      catch (Autodesk.Revit.Exceptions.InternalException) { }

      return value;
    }
  }
}

namespace RhinoInside.Revit.GH.Parameters
{
  public class ParameterKey : ElementIdNonGeometryParam<Types.ParameterKey, DB.ElementId>
  {
    public override Guid ComponentGuid => new Guid("A550F532-8C68-460B-91F3-DA0A5A0D42B5");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public ParameterKey() : base("Parameter Key", "Parameter Key", "Represents a Revit parameter definition.", "Params", "Revit") { }

    protected override Types.ParameterKey PreferredCast(object data) => null;
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      if (Kind > GH_ParamKind.input || DataType == GH_ParamData.remote)
      {
        base.AppendAdditionalMenuItems(menu);
        return;
      }

      Menu_AppendWireDisplay(menu);
      Menu_AppendDisconnectWires(menu);

      Menu_AppendReverseParameter(menu);
      Menu_AppendFlattenParameter(menu);
      Menu_AppendGraftParameter(menu);
      Menu_AppendSimplifyParameter(menu);

      {
        var parametersListBox = new ListBox();
        parametersListBox.BorderStyle = BorderStyle.FixedSingle;
        parametersListBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
        parametersListBox.Height = (int) (100 * GH_GraphicsUtil.UiScale);
        parametersListBox.SelectedIndexChanged += ParametersListBox_SelectedIndexChanged;

        var categoriesBox = new ComboBox();
        categoriesBox.DropDownStyle = ComboBoxStyle.DropDownList;
        categoriesBox.DropDownHeight = categoriesBox.ItemHeight * 15;
        categoriesBox.SetCueBanner("Category filter…");
        categoriesBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
        categoriesBox.Tag = parametersListBox;
        categoriesBox.SelectedIndexChanged += CategoriesBox_SelectedIndexChanged;

        var categoriesTypeBox = new ComboBox();
        categoriesTypeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        categoriesTypeBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
        categoriesTypeBox.Tag = categoriesBox;
        categoriesTypeBox.SelectedIndexChanged += CategoryType_SelectedIndexChanged;
        categoriesTypeBox.Items.Add("All Categories");
        categoriesTypeBox.Items.Add("Model");
        categoriesTypeBox.Items.Add("Annotation");
        categoriesTypeBox.Items.Add("Tags");
        categoriesTypeBox.Items.Add("Internal");
        categoriesTypeBox.Items.Add("Analytical");
        categoriesTypeBox.SelectedIndex = 0;

        Menu_AppendCustomItem(menu, categoriesTypeBox);
        Menu_AppendCustomItem(menu, categoriesBox);
        Menu_AppendCustomItem(menu, parametersListBox);
      }

      Menu_AppendManageCollection(menu);
      Menu_AppendSeparator(menu);

      Menu_AppendDestroyPersistent(menu);
      Menu_AppendInternaliseData(menu);

      if (Exposure != GH_Exposure.hidden)
        Menu_AppendExtractParameter(menu);
    }

    private void RefreshCategoryList(ComboBox categoriesBox, DB.CategoryType categoryType)
    {
      var categories = Revit.ActiveUIDocument.Document.Settings.Categories.Cast<DB.Category>().Where(x => x.AllowsBoundParameters);

      if (categoryType != DB.CategoryType.Invalid)
      {
        if (categoryType == (DB.CategoryType) 3)
          categories = categories.Where(x => x.IsTagCategory);
        else
          categories = categories.Where(x => x.CategoryType == categoryType && !x.IsTagCategory);
      }

      categoriesBox.SelectedIndex = -1;
      categoriesBox.Items.Clear();
      foreach (var category in categories.OrderBy(x => x.Name))
      {
        var tag = Types.Category.FromCategory(category);
        int index = categoriesBox.Items.Add(tag);
      }
    }

    private void RefreshParametersList(ListBox parametersListBox, ComboBox categoriesBox)
    {
      parametersListBox.Items.Clear();

      IEnumerable<DB.ElementId> parameters = null;
      if (categoriesBox.SelectedIndex == -1)
      {
        parameters = categoriesBox.Items.
                     Cast<Types.Category>().
                     SelectMany(x => DB.TableView.GetAvailableParameters(Revit.ActiveUIDocument.Document, x.Id)).
                     GroupBy(x => x.IntegerValue).
                     Select(x => x.First());
      }
      else
      {
        parameters = DB.TableView.GetAvailableParameters(Revit.ActiveUIDocument.Document, (categoriesBox.Items[categoriesBox.SelectedIndex] as Types.Category).Id);
      }

      var current = InstantiateT();
      if (SourceCount == 0 && PersistentDataCount == 1)
      {
        if (PersistentData.get_FirstItem(true) is Types.ParameterKey firstValue)
          current = firstValue.Duplicate() as Types.ParameterKey;
      }

      foreach (var parameter in parameters.Select(x => Types.ParameterKey.FromElementId(Revit.ActiveUIDocument.Document, x)).OrderBy(x => x.ToString()))
      {
        int index = parametersListBox.Items.Add(parameter);
        if (parameter.UniqueID == current.UniqueID)
          parametersListBox.SelectedIndex = index;
      }
    }

    private void CategoryType_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox categoriesTypeBox && categoriesTypeBox.Tag is ComboBox categoriesBox)
      {
        RefreshCategoryList(categoriesBox, (DB.CategoryType) categoriesTypeBox.SelectedIndex);
        RefreshParametersList(categoriesBox.Tag as ListBox, categoriesBox);
      }
    }

    private void CategoriesBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox categoriesBox && categoriesBox.Tag is ListBox parametersListBox)
        RefreshParametersList(parametersListBox, categoriesBox);
    }

    private void ParametersListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is Types.ParameterKey value)
          {
            RecordUndoEvent($"Set: {value}");
            PersistentData.Clear();
            PersistentData.Append(value.Duplicate() as Types.ParameterKey);
          }
        }

        ExpireSolution(true);
      }
    }
  }

  public class ParameterValue : GH_Param<Types.ParameterValue>
  {
    public override Guid ComponentGuid => new Guid("3E13D360-4B29-42C7-8F3E-2AB8F74B4EA8");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override System.Drawing.Bitmap Icon => ImageBuilder.BuildIcon("#");

    public ParameterValue() : base("ParameterValue", "ParameterValue", "Represents a Revit parameter value on an element.", "Params", "Revit", GH_ParamAccess.item) { }
    protected ParameterValue(string name, string nickname, string description, string category, string subcategory, GH_ParamAccess access) :
    base(name, nickname, description, category, subcategory, access)
    { }

    protected override string Format(Types.ParameterValue data)
    {
      if (data is null)
        return $"Null {TypeName}";

      try
      {
        if (data.Value is DB.Parameter parameter)
        {
          return parameter.HasValue ?
                 parameter.AsValueString() :
                 string.Empty;
        }
      }
      catch(Exception) { }

      return $"Invalid {TypeName}";
    }
  }

  public class ParameterParam : ParameterValue
  {
    public override Guid ComponentGuid => new Guid("43F0E4E9-3DC4-4965-AB80-07E28E203A91");

    public ParameterParam() : base(string.Empty, string.Empty, string.Empty, "Params", "Revit", GH_ParamAccess.item) { }
    public ParameterParam(DB.Parameter p) : this()
    {
      ParameterName = p.Definition.Name;
      ParameterType = p.Definition.ParameterType;
      ParameterGroup = p.Definition.ParameterGroup;
      ParameterBinding = p.Element is DB.ElementType ? RevitAPI.ParameterBinding.Type : RevitAPI.ParameterBinding.Instance;

      if (p.IsShared)
        ParameterSharedGUID = p.GUID;
      else if (p.Id.TryGetBuiltInParameter(out var parameterBuiltInId))
        ParameterBuiltInId = parameterBuiltInId;

      try { Name = $"{DB.LabelUtils.GetLabelFor(ParameterGroup)} : {ParameterName}"; }
      catch (Autodesk.Revit.Exceptions.InvalidOperationException) { Name = ParameterName; }

      NickName = Name;
      MutableNickName = false;

      try { Description = p.StorageType == DB.StorageType.ElementId ? "ElementId" : DB.LabelUtils.GetLabelFor(p.Definition.ParameterType); }
      catch (Autodesk.Revit.Exceptions.InvalidOperationException)
      { Description = p.Definition.UnitType == DB.UnitType.UT_Number ? "Enumerate" : DB.LabelUtils.GetLabelFor(p.Definition.UnitType); }

      if (ParameterSharedGUID.HasValue)
        Description = $"Shared parameter {ParameterSharedGUID.Value:B}\n{Description}";
      else if (ParameterBuiltInId != DB.BuiltInParameter.INVALID)
        Description = $"BuiltIn parameter {ParameterBuiltInId.ToStringGeneric()}\n{Description}";
      else
        Description = $"{ParameterBinding} project parameter\n{Description}";
    }

    public string ParameterName                        { get; private set; } = string.Empty;
    public DB.ParameterType ParameterType              { get; private set; } = DB.ParameterType.Invalid;
    public DB.BuiltInParameterGroup ParameterGroup     { get; private set; } = DB.BuiltInParameterGroup.INVALID;
    public RevitAPI.ParameterBinding ParameterBinding  { get; private set; } = RevitAPI.ParameterBinding.Unknown;
    public DB.BuiltInParameter ParameterBuiltInId      { get; private set; } = DB.BuiltInParameter.INVALID;
    public Guid? ParameterSharedGUID                   { get; private set; } = default;

    public override sealed bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      ///////////////////////////////////////////////////////////////
      // Keep this code while in WIP to read WIP components
      if
      (
        Enum.TryParse(Name, out DB.BuiltInParameter builtInId) &&
        Enum.IsDefined(typeof(DB.BuiltInParameter), builtInId)
      )
        ParameterBuiltInId = builtInId;
      ///////////////////////////////////////////////////////////////

      var parameterName = default(string);
      reader.TryGetString("ParameterName", ref parameterName);
      ParameterName = parameterName;

      var parameterType = (int) DB.ParameterType.Invalid;
      reader.TryGetInt32("ParameterType", ref parameterType);
      ParameterType = (DB.ParameterType) parameterType;

      var parameterGroup = (int) DB.BuiltInParameterGroup.INVALID;
      reader.TryGetInt32("ParameterGroup", ref parameterGroup);
      ParameterGroup = (DB.BuiltInParameterGroup) parameterGroup;

      var parameterBinding = (int) RevitAPI.ParameterBinding.Unknown;
      reader.TryGetInt32("ParameterBinding", ref parameterBinding);
      ParameterBinding = (RevitAPI.ParameterBinding) parameterBinding;

      var parameterBuiltInId = (int) DB.BuiltInParameter.INVALID;
      reader.TryGetInt32("ParameterBuiltInId", ref parameterBuiltInId);
      ParameterBuiltInId = (DB.BuiltInParameter) parameterBuiltInId;

      var parameterSharedGUID = default(Guid);
      if (reader.TryGetGuid("ParameterSharedGUID", ref parameterSharedGUID))
        ParameterSharedGUID = parameterSharedGUID;
      else
        ParameterSharedGUID = default;

      return true;
    }

    public override sealed bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if(!string.IsNullOrEmpty(ParameterName))
        writer.SetString("ParameterName", ParameterName);

      if (ParameterGroup != DB.BuiltInParameterGroup.INVALID)
        writer.SetInt32("ParameterGroup", (int) ParameterGroup);

      if (ParameterType != DB.ParameterType.Invalid)
        writer.SetInt32("ParameterType", (int) ParameterType);

      if (ParameterBinding != RevitAPI.ParameterBinding.Unknown)
        writer.SetInt32("ParameterBinding", (int) ParameterBinding);

      if (ParameterBuiltInId != DB.BuiltInParameter.INVALID)
        writer.SetInt32("ParameterBuiltInId", (int) ParameterBuiltInId);

      if (ParameterSharedGUID.HasValue)
        writer.SetGuid("ParameterSharedGUID", ParameterSharedGUID.Value);

      return true;
    }

    public override int GetHashCode()
    {
      if (ParameterSharedGUID.HasValue)
        return ParameterSharedGUID.Value.GetHashCode();

      if (ParameterBuiltInId != DB.BuiltInParameter.INVALID)
        return (int) ParameterBuiltInId;

      return new { ParameterName, ParameterType, ParameterBinding }.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if(obj is ParameterParam value)
      {
        if (ParameterSharedGUID.HasValue)
          return value.ParameterSharedGUID.HasValue && ParameterSharedGUID == value.ParameterSharedGUID.Value;

        if (ParameterBuiltInId != DB.BuiltInParameter.INVALID)
          return ParameterBuiltInId == value.ParameterBuiltInId;

        return ParameterName == value.ParameterName &&
               ParameterType == value.ParameterType &&
               ParameterBinding == value.ParameterBinding;
      }

      return false;
    }

    public DB.Parameter GetParameter(DB.Element element)
    {
      if(ParameterSharedGUID.HasValue)
        return element.get_Parameter(ParameterSharedGUID.Value);

      if(ParameterBuiltInId != DB.BuiltInParameter.INVALID)
        return element.get_Parameter(ParameterBuiltInId);

      return element.GetParameter(ParameterName, ParameterType, ParameterBinding, RevitAPI.ParameterSet.Project);
    }
  }
}

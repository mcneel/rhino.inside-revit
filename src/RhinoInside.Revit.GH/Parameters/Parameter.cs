using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;
using EDBS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.GH.Parameters
{
  using External.DB.Extensions;

  public class ParameterKey : Element<Types.ParameterKey, ARDB.ParameterElement>
  {
    public override Guid ComponentGuid => new Guid("A550F532-8C68-460B-91F3-DA0A5A0D42B5");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public ParameterKey() : base("Parameter", "Parameter", "Contains a collection of Revit parameters", "Params", "Revit") { }

    public static bool GetDocumentParameter(IGH_Component component, IGH_DataAccess DA, string name, out Types.ParameterKey key)
    {
      if (!component.Params.GetData(DA, name, out key, x => x.IsValid)) return false;

      if (!key.IsReferencedData)
      {
        if (!Document.TryGetCurrentDocument(component, out var document))
          return false;

        var keyName = key.Nomen;
        var parameterId = document.Value.IsFamilyDocument ?
              document.Value.FamilyManager.
              get_Parameter(keyName)?.Id ??
              ARDB.ElementId.InvalidElementId :
              ARDB.GlobalParametersManager.FindByName(document.Value, keyName);

        key = Types.ParameterKey.FromElementId(document.Value, parameterId);
        if (key is object) return true;

        if (document.Value.IsFamilyDocument)
          throw new Exceptions.RuntimeArgumentException(name, $"Family parameter '{keyName}' is not defined on document '{document.Title}'");
        else
          throw new Exceptions.RuntimeArgumentException(name, $"Global parameter '{keyName}' is not defined on document '{document.Title}'");
      }

      return key.IsValid;
    }

    public static bool GetProjectParameter(IGH_Component component, IGH_DataAccess DA, string name, out Types.ParameterKey key)
    {
      if (!component.Params.GetData(DA, name, out key, x => x.IsValid)) return false;

      if (!key.IsReferencedData)
      {
        if (!Document.TryGetCurrentDocument(component, out var document))
          return false;

        var keyName = key.Nomen;
        if (!document.Value.IsFamilyDocument)
        {
          var parameterId = ARDB.ElementId.InvalidElementId;
          using (var iterator = document.Value.ParameterBindings.ForwardIterator())
          {
            while (iterator.MoveNext())
            {
              if (iterator.Key is ARDB.InternalDefinition definition)
              {
                if (definition.Name == keyName)
                  parameterId = definition.Id;
              }
            }
          }

          key = Types.ParameterKey.FromElementId(document.Value, parameterId);
          if (key is object) return true;
        }

        throw new Exceptions.RuntimeArgumentException(name, $"Project parameter '{keyName}' is not defined on document '{document.Title}'");
      }
      else if (key.Document.IsFamilyDocument || key.Value is ARDB.GlobalParameter)
        throw new Exceptions.RuntimeArgumentException(name, $"Parameter '{key.Nomen}' is not a valid reference to a project parameter");

      return key.IsValid;
    }

    #region UI

    protected ARDB.BuiltInCategory SelectedBuiltInCategory = ARDB.BuiltInCategory.INVALID;

    public override void Menu_AppendActions(ToolStripDropDown menu)
    {
      var activeApp = Revit.ActiveUIApplication;
      var doc = activeApp.ActiveUIDocument?.Document;
      if (doc is object)
      {
        {
          var commandId = doc.IsFamilyDocument ?
            Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.FamilyTypes) :
            Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.ProjectParameters);

          var commandName = doc.IsFamilyDocument ?
            "Open Family Parameters…" :
            "Open Project Parameters…";

          Menu_AppendItem
          (
            menu, commandName,
            (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
            activeApp.CanPostCommand(commandId), false
          );
        }

#if REVIT_2022
        {
          var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.GlobalParameters);
          Menu_AppendItem
          (
            menu, "Open Global Parameters…",
            (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
            !doc.IsFamilyDocument && activeApp.CanPostCommand(commandId), false
          );
        }
#endif
      }

      base.Menu_AppendActions(menu);
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0) return;
      if (Revit.ActiveUIDocument?.Document is null) return;

      var listBox = new ListBox
      {
        Sorted = true,
        BorderStyle = BorderStyle.FixedSingle,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Height = (int) (100 * GH_GraphicsUtil.UiScale),
        DisplayMember = nameof(Types.Element.DisplayName)
      };
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

      var categoriesBox = new ComboBox
      {
        Sorted = true,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        DisplayMember = nameof(Types.Element.DisplayName),
        Tag = listBox
      };
      categoriesBox.DropDownHeight = categoriesBox.ItemHeight * 15;
      categoriesBox.SetCueBanner("Category filter…");
      categoriesBox.SelectedIndexChanged += CategoriesBox_SelectedIndexChanged;

      var categoriesTypeBox = new ComboBox
      {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Tag = categoriesBox
      };
      categoriesTypeBox.SelectedIndexChanged += CategoryType_SelectedIndexChanged;
      categoriesTypeBox.Items.Add("All Categories");
      categoriesTypeBox.Items.Add("Model");
      categoriesTypeBox.Items.Add("Annotation");
      categoriesTypeBox.Items.Add("Tags");
      categoriesTypeBox.Items.Add("Internal");
      categoriesTypeBox.Items.Add("Analytical");
      categoriesTypeBox.SelectedIndex = 0;

      listBox.Tag = categoriesBox;

      Menu_AppendCustomItem(menu, categoriesTypeBox);
      Menu_AppendCustomItem(menu, categoriesBox);
      Menu_AppendCustomItem(menu, listBox);
    }

    private void RefreshCategoryList(ComboBox categoriesBox, ARDB.CategoryType categoryType)
    {
      var doc = Revit.ActiveUIDocument.Document;
      var categories = doc.GetBuiltInCategoriesWithParameters().Select(x => doc.GetCategory(x));

      if (categoryType != ARDB.CategoryType.Invalid)
      {
        if (categoryType == (ARDB.CategoryType) 3)
          categories = categories.Where(x => x.IsTagCategory);
        else
          categories = categories.Where(x => x.CategoryType == categoryType && !x.IsTagCategory);
      }

      categoriesBox.BeginUpdate();
      categoriesBox.SelectedIndex = -1;
      categoriesBox.Items.Clear();

      foreach (var category in categories)
        categoriesBox.Items.Add(Types.Category.FromCategory(category));

      if (SelectedBuiltInCategory != ARDB.BuiltInCategory.INVALID)
      {
        var currentCategory = new Types.Category(doc, new ARDB.ElementId(SelectedBuiltInCategory));
        categoriesBox.SelectedIndex = categoriesBox.Items.Cast<Types.Category>().IndexOf(currentCategory, 0).FirstOr(-1);
      }

      categoriesBox.EndUpdate();
    }

    private void RefreshParametersList(ListBox listBox, ComboBox categoriesBox)
    {
      var doc = Revit.ActiveUIDocument.Document;

      var current = default(Types.ParameterKey);
      if (SourceCount == 0 && PersistentDataCount == 1)
      {
        if (PersistentData.get_FirstItem(true) is Types.ParameterKey firstValue)
          current = firstValue as Types.ParameterKey;
      }

      var parameters = default(IEnumerable<ARDB.ElementId>);
      if (categoriesBox.SelectedIndex == -1)
      {
        parameters = categoriesBox.Items.
                      Cast<Types.Category>().
                      SelectMany(x => ARDB.TableView.GetAvailableParameters(doc, x.Id)).
                      GroupBy(x => x.ToValue()).
                      Select(x => x.First());
      }
      else
      {
        parameters = ARDB.TableView.GetAvailableParameters(doc, (categoriesBox.Items[categoriesBox.SelectedIndex] as Types.Category).Id);
      }

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.BeginUpdate();
      listBox.Items.Clear();

      foreach (var parameter in parameters)
        listBox.Items.Add(Types.ParameterKey.FromElementId(doc, parameter));

      listBox.SelectedIndex = listBox.Items.Cast<Types.ParameterKey>().IndexOf(PersistentValue, 0).FirstOr(-1);
      listBox.EndUpdate();
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void CategoryType_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox categoriesTypeBox && categoriesTypeBox.Tag is ComboBox categoriesBox)
      {
        RefreshCategoryList(categoriesBox, (ARDB.CategoryType) categoriesTypeBox.SelectedIndex);
        RefreshParametersList(categoriesBox.Tag as ListBox, categoriesBox);
      }
    }

    private void CategoriesBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox categoriesBox && categoriesBox.Tag is ListBox parametersListBox)
      {
        SelectedBuiltInCategory = ARDB.BuiltInCategory.INVALID;
        if (categoriesBox.SelectedItem is Types.Category category)
          category.Id.TryGetBuiltInCategory(out SelectedBuiltInCategory);

        RefreshParametersList(parametersListBox, categoriesBox);
      }
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is Types.ParameterKey value)
          {
            RecordPersistentDataEvent($"Set: {value}");
            PersistentData.Clear();
            PersistentData.Append(value);
            OnObjectChanged(GH_ObjectEventType.PersistentData);
          }
        }

        ExpireSolution(true);
      }
    }
    #endregion

    #region IO
    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      var selectedBuiltInCategory = string.Empty;
      if (reader.TryGetString("SelectedBuiltInCategory", ref selectedBuiltInCategory))
        SelectedBuiltInCategory = new EDBS.CategoryId(selectedBuiltInCategory);
      else
        SelectedBuiltInCategory = ARDB.BuiltInCategory.INVALID;

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (SelectedBuiltInCategory != ARDB.BuiltInCategory.INVALID)
        writer.SetString("SelectedBuiltInCategory", ((EDBS.CategoryId) SelectedBuiltInCategory).FullName);

      return true;
    }
    #endregion
  }

  public class ParameterValue : Param<Types.ParameterValue>
  {
    public override Guid ComponentGuid => new Guid("3E13D360-4B29-42C7-8F3E-2AB8F74B4EA8");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override string IconTag => "#";

    protected override Types.ParameterValue PreferredCast(object data)
    {
      return data is ARDB.Parameter parameter ? new Types.ParameterValue(parameter) : default;
    }

    public ParameterValue() : base
    (
      name: "Parameter Value",
      nickname: "Parameter Value",
      description: "Contains a collection of Revit parameter values on an element.",
      category: "Params",
      subcategory: "Revit"
    )
    { }

    protected ParameterValue(string name, string nickname, string description, string category, string subcategory) :
    base(name, nickname, description, category, subcategory)
    { }

    protected override string Format(Types.ParameterValue data)
    {
      if (data is null)
        return $"Null {TypeName}";

      try
      {
        if (data.Value is ARDB.Parameter parameter && parameter.Definition is ARDB.Definition definition)
        {
          if (parameter.HasValue)
          {
            var text = string.Empty;
            if (parameter.StorageType == ARDB.StorageType.String)
              text = parameter.AsString();
            else if (parameter.Element.GetParameterFormatOptions(parameter.Id) is ARDB.FormatOptions options)
              using (options) { text = parameter.AsValueString(options); }
            else
              text = parameter.AsValueString();

            return $"{definition.Name} : {text}";
          }

          return $"{definition.Name} : <null>";
        }
      }
      catch { }

      return $"Invalid {TypeName}";
    }
  }

  public class ParameterParam : ParameterValue
  {
    public override Guid ComponentGuid => new Guid("43F0E4E9-3DC4-4965-AB80-07E28E203A91");

    public ParameterParam() : base
    (
      name: string.Empty,
      nickname: string.Empty,
      description: string.Empty,
      category: "Params",
      subcategory: "Revit"
    )
    { }

    public ParameterParam(ARDB.Parameter p) : this()
    {
      ParameterName = p.Definition.Name;
      ParameterType = p.Definition.GetDataType();
      ParameterGroup = p.Definition.GetGroupType();
      ParameterScope = p.Element is ARDB.ElementType ? ERDB.ParameterScope.Type : ERDB.ParameterScope.Instance;

      if (p.IsShared)
      {
        ParameterClass = ERDB.ParameterClass.Shared;
        ParameterSharedGUID = p.GUID;
      }
      else if (p.Id.TryGetBuiltInParameter(out var parameterBuiltInId))
      {
        ParameterClass = ERDB.ParameterClass.BuiltIn;
        ParameterBuiltInId = parameterBuiltInId;
      }
      else if (p.Element.Document.GetElement(p.Id) is ARDB.ParameterElement paramElement)
      {
        if (paramElement is ARDB.GlobalParameter)
        {
          ParameterClass = ERDB.ParameterClass.Global;
        }
        else switch (paramElement.get_Parameter(ARDB.BuiltInParameter.ELEM_DELETABLE_IN_FAMILY).AsInteger())
          {
            case 0: ParameterClass = ERDB.ParameterClass.Family; break;
            case 1: ParameterClass = ERDB.ParameterClass.Project; break;
          }
      }

      if (ParameterGroup is object && ParameterGroup != EDBS.ParameterGroup.Empty)
        Name = $"{ParameterGroup.Label} : {ParameterName}";
      else
        Name = $"Other : {ParameterName}";

      NickName = Name;
      MutableNickName = false;

      EDBS.DataType dataType = p.Definition?.GetDataType();
      Description = EDBS.CategoryId.IsCategoryId(dataType, out var _) ? $"Family Type" : dataType.Label;

      if (string.IsNullOrEmpty(Description))
        Description = p.StorageType.ToString();

      if (ParameterSharedGUID.HasValue)
        Description = $"Shared parameter {ParameterSharedGUID.Value:B}\n{Description}";
      else if (ParameterBuiltInId != EDBS.ParameterId.Empty)
        Description = $"BuiltIn Parameter \"{ParameterBuiltInId.FullName}\"\n{Description}";
      else if (ParameterScope != ERDB.ParameterScope.Unknown)
        Description = $"{ParameterClass} parameter ({ParameterScope})\n{Description}";
      else
        Description = $"{ParameterClass} parameter\n{Description}";
    }

    public string ParameterName { get; private set; } = string.Empty;
    public EDBS.DataType ParameterType { get; private set; } = EDBS.DataType.Empty;
    public EDBS.ParameterGroup ParameterGroup { get; private set; } = EDBS.ParameterGroup.Empty;
    public ERDB.ParameterScope ParameterScope { get; private set; } = ERDB.ParameterScope.Unknown;
    public ERDB.ParameterClass ParameterClass { get; private set; } = ERDB.ParameterClass.Any;
    public EDBS.ParameterId ParameterBuiltInId { get; private set; } = EDBS.ParameterId.Empty;
    public Guid? ParameterSharedGUID { get; private set; } = default;

    public sealed override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      object GetValue(string name)
      {
        if (reader.FindItem(name) is GH_IO.Types.GH_Item item)
        {
          if (item.Type == GH_IO.Types.GH_Types.gh_int32) return item._int32;
          if (item.Type == GH_IO.Types.GH_Types.gh_string) return item._string;
        }

        return default;
      }

      var parameterName = default(string);
      reader.TryGetString("ParameterName", ref parameterName);
      ParameterName = parameterName;

      ParameterType = EDBS.DataType.Empty;
      switch (GetValue("ParameterType"))
      {
#if !REVIT_2023
        case int enumerate: ParameterType = ((ARDB.ParameterType) enumerate).ToDataType(); break;
#endif
        case string schema: ParameterType = new EDBS.DataType(schema); break;
      }

      ParameterGroup = EDBS.ParameterGroup.Empty;
      switch (GetValue("ParameterGroup"))
      {
#if !REVIT_2024
        case int enumerate: ParameterGroup = ((ARDB.BuiltInParameterGroup) enumerate).ToParameterGroup(); break;
#endif
        case string schema: ParameterGroup = new EDBS.ParameterGroup(schema); break;
      }

      var parameterScope = (int) ERDB.ParameterScope.Unknown;
      reader.TryGetInt32("ParameterScope", ref parameterScope);
      ParameterScope = (ERDB.ParameterScope) parameterScope;

      ParameterBuiltInId = EDBS.ParameterId.Empty;
      switch (GetValue("ParameterBuiltInId"))
      {
        case int enumerate: ParameterBuiltInId = (ARDB.BuiltInParameter) enumerate; break;
        case string schema: ParameterBuiltInId = new EDBS.ParameterId(schema); break;
      }

      var parameterSharedGUID = default(Guid);
      if (reader.TryGetGuid("ParameterSharedGUID", ref parameterSharedGUID))
        ParameterSharedGUID = parameterSharedGUID;
      else
        ParameterSharedGUID = default;

      var parameterClass = (int) ERDB.ParameterClass.Any;
      if (reader.TryGetInt32("ParameterClass", ref parameterClass))
        ParameterClass = (ERDB.ParameterClass) parameterClass;
      else if (ParameterSharedGUID.HasValue)
        ParameterClass = ERDB.ParameterClass.Shared;
      else if (ParameterBuiltInId != EDBS.ParameterId.Empty)
        ParameterClass = ERDB.ParameterClass.BuiltIn;
      else if (ParameterScope != ERDB.ParameterScope.Unknown)
        ParameterClass = ERDB.ParameterClass.Project;

      return true;
    }

    public sealed override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (!string.IsNullOrEmpty(ParameterName))
        writer.SetString("ParameterName", ParameterName);

      if (!EDBS.ParameterGroup.IsNullOrEmpty(ParameterGroup))
        writer.SetString("ParameterGroup", ParameterGroup.FullName);

      if (!EDBS.DataType.IsNullOrEmpty(ParameterType))
        writer.SetString("ParameterType", ParameterType.FullName);

      if (ParameterScope != ERDB.ParameterScope.Unknown)
        writer.SetInt32("ParameterScope", (int) ParameterScope);

      if (!EDBS.ParameterId.IsNullOrEmpty(ParameterBuiltInId))
        writer.SetString("ParameterBuiltInId", ParameterBuiltInId.FullName);

      if (ParameterSharedGUID.HasValue)
        writer.SetGuid("ParameterSharedGUID", ParameterSharedGUID.Value);

      if (ParameterClass != ERDB.ParameterClass.Any)
        writer.SetInt32("ParameterClass", (int) ParameterClass);

      return true;
    }

    public override int GetHashCode()
    {
      if (ParameterSharedGUID.HasValue)
        return ParameterSharedGUID.Value.GetHashCode();

      if (!EDBS.ParameterId.IsNullOrEmpty(ParameterBuiltInId))
        return ParameterBuiltInId.GetHashCode();

      return new { ParameterName, ParameterType, ParameterScope, ParameterClass }.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (obj is ParameterParam value)
      {
        if (ParameterSharedGUID.HasValue)
          return value.ParameterSharedGUID.HasValue && ParameterSharedGUID == value.ParameterSharedGUID.Value;

        if (!EDBS.ParameterId.IsNullOrEmpty(ParameterBuiltInId))
          return ParameterBuiltInId == value.ParameterBuiltInId;

        return ParameterName == value.ParameterName &&
               ParameterType == value.ParameterType &&
               ParameterScope == value.ParameterScope &&
               ParameterClass == value.ParameterClass;
      }

      return false;
    }

    public ARDB.Parameter GetParameter(ARDB.Element element)
    {
      if (ParameterSharedGUID.HasValue)
        return element.get_Parameter(ParameterSharedGUID.Value);

      if (!EDBS.ParameterId.IsNullOrEmpty(ParameterBuiltInId))
        return element.GetParameter(ParameterBuiltInId);

      return element.GetParameter(ParameterName, ParameterType, ParameterScope, ParameterClass);
    }
  }
}

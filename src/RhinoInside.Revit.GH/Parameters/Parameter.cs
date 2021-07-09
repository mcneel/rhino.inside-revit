using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;
using EDBS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.GH.Parameters
{
  public class ParameterKey : Element<Types.ParameterKey, DB.ElementId>
  {
    public override Guid ComponentGuid => new Guid("A550F532-8C68-460B-91F3-DA0A5A0D42B5");
    public override GH_Exposure Exposure => GH_Exposure.septenary;

    public ParameterKey() : base("Parameter Key", "ParaKey", "Contains a collection of Revit parameter keys", "Params", "Revit Primitives") { }

    #region UI
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.ProjectParameters);
      Menu_AppendItem
      (
        menu, $"Open Project Parameters…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0) return;
      if (Revit.ActiveUIDocument?.Document is null) return;

      var listBox = new ListBox();
      listBox.BorderStyle = BorderStyle.FixedSingle;
      listBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
      listBox.Height = (int) (100 * GH_GraphicsUtil.UiScale);
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
      listBox.Sorted = true;

      var categoriesBox = new ComboBox();
      categoriesBox.DropDownStyle = ComboBoxStyle.DropDownList;
      categoriesBox.DropDownHeight = categoriesBox.ItemHeight * 15;
      categoriesBox.SetCueBanner("Category filter…");
      categoriesBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
      categoriesBox.Tag = listBox;
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
      Menu_AppendCustomItem(menu, listBox);
    }

    private void RefreshCategoryList(ComboBox categoriesBox, DB.CategoryType categoryType)
    {
      if (Revit.ActiveUIDocument is null) return;

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
        int index = categoriesBox.Items.Add(tag.EmitProxy());
      }
    }

    private void RefreshParametersList(ListBox listBox, ComboBox categoriesBox)
    {
      if (Revit.ActiveUIDocument is null) return;

      var doc = Revit.ActiveUIDocument.Document;
      var selectedIndex = -1;

      try
      {
        listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
        listBox.Items.Clear();

        var current = default(Types.ParameterKey);
        if (SourceCount == 0 && PersistentDataCount == 1)
        {
          if (PersistentData.get_FirstItem(true) is Types.ParameterKey firstValue)
            current = firstValue as Types.ParameterKey;
        }

        {
          var parameters = default(IEnumerable<DB.ElementId>);
          if (categoriesBox.SelectedIndex == -1)
          {
            parameters = categoriesBox.Items.
                         Cast<IGH_GooProxy>().
                         Select(x => x.ProxyOwner).
                         Cast<Types.Category>().
                         SelectMany(x => DB.TableView.GetAvailableParameters(doc, x.Id)).
                         GroupBy(x => x.IntegerValue).
                         Select(x => x.First());
          }
          else
          {
            parameters = DB.TableView.GetAvailableParameters(doc, ((categoriesBox.Items[categoriesBox.SelectedIndex] as IGH_GooProxy).ProxyOwner as Types.Category).Id);
          }

          foreach (var parameter in parameters)
          {
            var tag = Types.ParameterKey.FromElementId(doc, parameter);
            int index = listBox.Items.Add(tag.EmitProxy());
            if (tag.UniqueID == current?.UniqueID)
              selectedIndex = index;
          }
        }
      }
      finally
      {
        listBox.SelectedIndex = selectedIndex;
        listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
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

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is IGH_GooProxy value)
          {
            RecordPersistentDataEvent($"Set: {value}");
            PersistentData.Clear();
            PersistentData.Append(value.ProxyOwner as Types.ParameterKey);
            OnObjectChanged(GH_ObjectEventType.PersistentData);
          }
        }

        ExpireSolution(true);
      }
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
      return data is DB.Parameter parameter ? new Types.ParameterValue(parameter) : default;
    }

    public ParameterValue() : base
    (
      name: "Parameter Value",
      nickname: "Parameter Value",
      description: "Contains a collection of Revit parameter values on an element.",
      category: "Params",
      subcategory: "Revit Primitives"
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
        if (data.Value is DB.Parameter parameter && parameter.Definition is DB.Definition definition)
        {
          return parameter.HasValue ?
            $"{definition.Name} : {(parameter.StorageType == DB.StorageType.String ? parameter.AsString() : parameter.AsValueString())}" :
            $"{definition.Name} : <null>";
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
      subcategory: "Revit Primitives"
    )
    { }

    public ParameterParam(DB.Parameter p) : this()
    {
      ParameterName = p.Definition.Name;
      ParameterType = p.Definition.GetDataType();
      ParameterGroup = p.Definition.GetGroupType();
      ParameterBinding = p.Element is DB.ElementType ? DBX.ParameterBinding.Type : DBX.ParameterBinding.Instance;

      if (p.IsShared)
      {
        ParameterClass = DBX.ParameterClass.Shared;
        ParameterSharedGUID = p.GUID;
      }
      else if (p.Id.TryGetBuiltInParameter(out var parameterBuiltInId))
      {
        ParameterClass = DBX.ParameterClass.BuiltIn;
        ParameterBuiltInId = parameterBuiltInId;
      }
      else if(p.Element.Document.GetElement(p.Id) is DB.ParameterElement paramElement)
      {
        if (paramElement is DB.GlobalParameter)
        {
          ParameterClass = DBX.ParameterClass.Global;
        }
        else switch (paramElement.get_Parameter(DB.BuiltInParameter.ELEM_DELETABLE_IN_FAMILY).AsInteger())
        {
          case 0: ParameterClass = DBX.ParameterClass.Family;  break;
          case 1: ParameterClass = DBX.ParameterClass.Project; break;
        }
      }

      try { Name = $"{ParameterGroup.Label} : {ParameterName}"; }
      catch (Autodesk.Revit.Exceptions.InvalidOperationException) { Name = ParameterName; }

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
      else if(ParameterBinding != DBX.ParameterBinding.Unknown)
        Description = $"{ParameterClass} parameter ({ParameterBinding})\n{Description}";
      else
        Description = $"{ParameterClass} parameter\n{Description}";
    }

    public string ParameterName                        { get; private set; } = string.Empty;
    public EDBS.DataType ParameterType                 { get; private set; } = EDBS.DataType.Empty;
    public EDBS.ParameterGroup ParameterGroup          { get; private set; } = EDBS.ParameterGroup.Empty;
    public DBX.ParameterBinding ParameterBinding       { get; private set; } = DBX.ParameterBinding.Unknown;
    public DBX.ParameterClass ParameterClass           { get; private set; } = DBX.ParameterClass.Any;
    public EDBS.ParameterId ParameterBuiltInId         { get; private set; } = EDBS.ParameterId.Empty;
    public Guid? ParameterSharedGUID                   { get; private set; } = default;

    public sealed override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      object GetValue(string name)
      {
        if (reader.FindItem(name) is GH_IO.Types.GH_Item item)
        {
          if (item.Type == GH_IO.Types.GH_Types.gh_int32)  return item._int32;
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
        case int enumerate: ParameterType = ((DB.ParameterType) enumerate).ToDataType(); break;
        case string schema: ParameterType = new EDBS.DataType(schema); break;
      }

      ParameterGroup = EDBS.ParameterGroup.Empty;
      switch (GetValue("ParameterGroup"))
      {
        case int enumerate: ParameterGroup = ((DB.BuiltInParameterGroup) enumerate).ToParameterGroup(); break;
        case string schema: ParameterGroup = new EDBS.ParameterGroup(schema); break;
      }

      var parameterBinding = (int) DBX.ParameterBinding.Unknown;
      reader.TryGetInt32("ParameterBinding", ref parameterBinding);
      ParameterBinding = (DBX.ParameterBinding) parameterBinding;

      ParameterBuiltInId = EDBS.ParameterId.Empty;
      switch (GetValue("ParameterBuiltInId"))
      {
        case int enumerate: ParameterBuiltInId = (DB.BuiltInParameter) enumerate; break;
        case string schema: ParameterBuiltInId = new EDBS.ParameterId(schema); break;
      }

      var parameterSharedGUID = default(Guid);
      if (reader.TryGetGuid("ParameterSharedGUID", ref parameterSharedGUID))
        ParameterSharedGUID = parameterSharedGUID;
      else
        ParameterSharedGUID = default;

      var parameterClass = (int) DBX.ParameterClass.Any;
      if (reader.TryGetInt32("ParameterClass", ref parameterClass))
        ParameterClass = (DBX.ParameterClass) parameterClass;
      else if(ParameterSharedGUID.HasValue)
        ParameterClass = DBX.ParameterClass.Shared;
      else if(ParameterBuiltInId != EDBS.ParameterId.Empty)
        ParameterClass = DBX.ParameterClass.BuiltIn;
      else if(ParameterBinding != DBX.ParameterBinding.Unknown)
        ParameterClass = DBX.ParameterClass.Project;

      return true;
    }

    public sealed override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if(!string.IsNullOrEmpty(ParameterName))
        writer.SetString("ParameterName", ParameterName);

      if (ParameterGroup != EDBS.ParameterGroup.Empty)
        writer.SetString("ParameterGroup", ParameterGroup.FullName);

      if (ParameterType != EDBS.DataType.Empty)
        writer.SetString("ParameterType", ParameterType.FullName);

      if (ParameterBinding != DBX.ParameterBinding.Unknown)
        writer.SetInt32("ParameterBinding", (int) ParameterBinding);

      if (ParameterBuiltInId != EDBS.ParameterId.Empty)
        writer.SetString("ParameterBuiltInId", ParameterBuiltInId.FullName);

      if (ParameterSharedGUID.HasValue)
        writer.SetGuid("ParameterSharedGUID", ParameterSharedGUID.Value);

      if (ParameterClass != DBX.ParameterClass.Any)
        writer.SetInt32("ParameterClass", (int) ParameterClass);

      return true;
    }

    public override int GetHashCode()
    {
      if (ParameterSharedGUID.HasValue)
        return ParameterSharedGUID.Value.GetHashCode();

      if (ParameterBuiltInId != EDBS.ParameterId.Empty)
        return ParameterBuiltInId.GetHashCode();

      return new { ParameterName, ParameterType, ParameterBinding, ParameterClass }.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if(obj is ParameterParam value)
      {
        if (ParameterSharedGUID.HasValue)
          return value.ParameterSharedGUID.HasValue && ParameterSharedGUID == value.ParameterSharedGUID.Value;

        if (ParameterBuiltInId != EDBS.ParameterId.Empty)
          return ParameterBuiltInId == value.ParameterBuiltInId;

        return ParameterName == value.ParameterName &&
               ParameterType == value.ParameterType &&
               ParameterBinding == value.ParameterBinding &&
               ParameterClass == value.ParameterClass;
      }

      return false;
    }

    public DB.Parameter GetParameter(DB.Element element)
    {
      if(ParameterSharedGUID.HasValue)
        return element.get_Parameter(ParameterSharedGUID.Value);

      if(ParameterBuiltInId != EDBS.ParameterId.Empty)
        return element.GetParameter(ParameterBuiltInId);

      return element.GetParameter(ParameterName, ParameterType, ParameterBinding, ParameterClass);
    }
  }
}

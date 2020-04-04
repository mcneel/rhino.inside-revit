using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class ParameterKey : ElementIdWithoutPreviewParam<Types.ParameterKey, DB.ElementId>
  {
    public override Guid ComponentGuid => new Guid("A550F532-8C68-460B-91F3-DA0A5A0D42B5");
    public override GH_Exposure Exposure => GH_Exposure.septenary;

    public ParameterKey() : base("ParameterKey", "ParaKey", "Represents a Revit parameter definition.", "Params", "Revit") { }

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

      Menu_AppendPrincipalParameter(menu);
      Menu_AppendReverseParameter(menu);
      Menu_AppendFlattenParameter(menu);
      Menu_AppendGraftParameter(menu);
      Menu_AppendSimplifyParameter(menu);

      {
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
        int index = categoriesBox.Items.Add(tag.EmitProxy());
      }
    }

    private void RefreshParametersList(ListBox listBox, ComboBox categoriesBox)
    {
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
            RecordUndoEvent($"Set: {value}");
            PersistentData.Clear();
            PersistentData.Append(value.ProxyOwner as Types.ParameterKey);
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
      catch { }

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
      {
        ParameterClass = RevitAPI.ParameterClass.Shared;
        ParameterSharedGUID = p.GUID;
      }
      else if (p.Id.TryGetBuiltInParameter(out var parameterBuiltInId))
      {
        ParameterClass = RevitAPI.ParameterClass.BuiltIn;
        ParameterBuiltInId = parameterBuiltInId;
      }
      else if(p.Element.Document.GetElement(p.Id) is DB.ParameterElement paramElement)
      {
        if (paramElement is DB.GlobalParameter)
        {
          ParameterClass = RevitAPI.ParameterClass.Global;
        }
        else switch (paramElement.get_Parameter(DB.BuiltInParameter.ELEM_DELETABLE_IN_FAMILY).AsInteger())
        {
          case 0: ParameterClass = RevitAPI.ParameterClass.Family;  break;
          case 1: ParameterClass = RevitAPI.ParameterClass.Project; break;
        }
      }

      try { Name = $"{DB.LabelUtils.GetLabelFor(ParameterGroup)} : {ParameterName}"; }
      catch (Autodesk.Revit.Exceptions.InvalidOperationException) { Name = ParameterName; }

      NickName = Name;
      MutableNickName = false;

      try { Description = p.StorageType == DB.StorageType.ElementId ? "ElementId" : DB.LabelUtils.GetLabelFor(p.Definition.ParameterType); }
      catch (Autodesk.Revit.Exceptions.InvalidOperationException)
      { Description = p.Definition.UnitType == DB.UnitType.UT_Number ? "Enumerate" : DB.LabelUtils.GetLabelFor(p.Definition.UnitType); }

      if(string.IsNullOrEmpty(Description))
        Description = ParameterType.ToString();

      if (ParameterSharedGUID.HasValue)
        Description = $"Shared parameter {ParameterSharedGUID.Value:B}\n{Description}";
      else if (ParameterBuiltInId != DB.BuiltInParameter.INVALID)
        Description = $"BuiltIn parameter {ParameterBuiltInId.ToStringGeneric()}\n{Description}";
      else if(ParameterBinding != RevitAPI.ParameterBinding.Unknown)
        Description = $"{ParameterClass} parameter ({ParameterBinding})\n{Description}";
      else
        Description = $"{ParameterClass} parameter\n{Description}";
    }

    public string ParameterName                        { get; private set; } = string.Empty;
    public DB.ParameterType ParameterType              { get; private set; } = DB.ParameterType.Invalid;
    public DB.BuiltInParameterGroup ParameterGroup     { get; private set; } = DB.BuiltInParameterGroup.INVALID;
    public RevitAPI.ParameterBinding ParameterBinding  { get; private set; } = RevitAPI.ParameterBinding.Unknown;
    public RevitAPI.ParameterClass ParameterClass    { get; private set; } = RevitAPI.ParameterClass.Any;
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

      var parameterClass = (int) RevitAPI.ParameterClass.Any;
      if (reader.TryGetInt32("ParameterClass", ref parameterClass))
        ParameterClass = (RevitAPI.ParameterClass) parameterClass;
      else if(ParameterSharedGUID.HasValue)
        ParameterClass = RevitAPI.ParameterClass.Shared;
      else if(ParameterBuiltInId != DB.BuiltInParameter.INVALID)
        ParameterClass = RevitAPI.ParameterClass.BuiltIn;
      else if(ParameterBinding != RevitAPI.ParameterBinding.Unknown)
        ParameterClass = RevitAPI.ParameterClass.Project;

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

      if (ParameterClass != RevitAPI.ParameterClass.Any)
        writer.SetInt32("ParameterClass", (int) ParameterClass);

      return true;
    }

    public override int GetHashCode()
    {
      if (ParameterSharedGUID.HasValue)
        return ParameterSharedGUID.Value.GetHashCode();

      if (ParameterBuiltInId != DB.BuiltInParameter.INVALID)
        return (int) ParameterBuiltInId;

      return new { ParameterName, ParameterType, ParameterBinding, ParameterClass }.GetHashCode();
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
               ParameterBinding == value.ParameterBinding &&
               ParameterClass == value.ParameterClass;
      }

      return false;
    }

    public DB.Parameter GetParameter(DB.Element element)
    {
      if(ParameterSharedGUID.HasValue)
        return element.get_Parameter(ParameterSharedGUID.Value);

      if(ParameterBuiltInId != DB.BuiltInParameter.INVALID)
        return element.get_Parameter(ParameterBuiltInId);

      return element.GetParameter(ParameterName, ParameterType, ParameterBinding, ParameterClass);
    }
  }

  public class BuiltInParameterGroups : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("5D331B12-DA6C-46A7-AA13-F463E42650D1");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public BuiltInParameterGroups()
    {
      Category = "Revit";
      SubCategory = "Parameter";
      Name = "BuiltInParameterGroups";
      NickName = "BuiltInParameterGroups";
      Description = "Provides a picker of a BuiltInParameterGroup";

      ListItems.Clear();

      foreach (var builtInParameterGroup in Enum.GetValues(typeof(DB.BuiltInParameterGroup)).Cast<DB.BuiltInParameterGroup>().OrderBy((x) => DB.LabelUtils.GetLabelFor(x)))
      {
        ListItems.Add(new GH_ValueListItem(DB.LabelUtils.GetLabelFor(builtInParameterGroup), ((int) builtInParameterGroup).ToString()));
        if (builtInParameterGroup == DB.BuiltInParameterGroup.PG_IDENTITY_DATA)
          SelectItem(ListItems.Count - 1);
      }
    }
  }

  public class BuiltInParameterByName : ValueList
  {
    public override Guid ComponentGuid => new Guid("C1D96F56-F53C-4DFC-8090-EC2050BDBB66");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public BuiltInParameterByName()
    {
      Name = "BuiltInParameter Picker";
      Description = "Provides a BuiltInParameter picker";
    }

    public override void AddedToDocument(GH_Document document)
    {
      if (NickName == Name)
        NickName = "'Parameter name here…";

      base.AddedToDocument(document);
    }

    protected override void RefreshList(string ParamName)
    {
      var selectedItems = ListItems.Where(x => x.Selected).Select(x => x.Expression).ToList();

      ListItems.Clear();
      if (ParamName.Length == 0 || ParamName[0] == '\'')
        return;

      if (Revit.ActiveDBDocument != null)
      {
        int selectedItemsCount = 0;
        {
          foreach (var builtInParameter in Enum.GetNames(typeof(DB.BuiltInParameter)))
          {
            if (!builtInParameter.IsSymbolNameLike(ParamName))
              continue;

            if (SourceCount == 0)
            {
              // If is a no pattern match update NickName case
              if (string.Equals(builtInParameter, ParamName, StringComparison.OrdinalIgnoreCase))
                ParamName = builtInParameter;
            }

            var builtInParameterValue = (DB.BuiltInParameter) Enum.Parse(typeof(DB.BuiltInParameter), builtInParameter);

            var label = string.Empty;
            try { label = DB.LabelUtils.GetLabelFor(builtInParameterValue); }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

            var item = new GH_ValueListItem(builtInParameter + " - \"" + label + "\"", ((int) builtInParameterValue).ToString());
            item.Selected = selectedItems.Contains(item.Expression);
            ListItems.Add(item);

            selectedItemsCount += item.Selected ? 1 : 0;
          }
        }

        // If no selection and we are not in CheckList mode try to select default model types
        if (ListItems.Count == 0)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, string.Format("No ElementType found using pattern \"{0}\"", ParamName));
        }
      }
    }

    protected override void RefreshList(IEnumerable<IGH_Goo> goos)
    {
      ListItems.Clear();
    }
  }
}

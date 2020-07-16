using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class ElementType : ElementIdWithoutPreviewParam<Types.ElementType, DB.ElementType>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    public override Guid ComponentGuid => new Guid("97DD546D-65C3-4D00-A609-3F5FBDA67142");

    public ElementType() : base("Type", "Type", "Represents a Revit document element type.", "Params", "Revit Primitives") { }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      var elementTypesBox = new ListBox();
      elementTypesBox.BorderStyle = BorderStyle.FixedSingle;
      elementTypesBox.Width = (int) (300 * GH_GraphicsUtil.UiScale);
      elementTypesBox.Height = (int) (100 * GH_GraphicsUtil.UiScale);
      elementTypesBox.SelectedIndexChanged += ElementTypesBox_SelectedIndexChanged;
      elementTypesBox.Sorted = true;

      var familiesBox = new ComboBox();
      familiesBox.DropDownStyle = ComboBoxStyle.DropDownList;
      familiesBox.DropDownHeight = familiesBox.ItemHeight * 15;
      familiesBox.SetCueBanner("Family filter…");
      familiesBox.Width = (int) (300 * GH_GraphicsUtil.UiScale);

      var categoriesBox = new ComboBox();
      categoriesBox.DropDownStyle = ComboBoxStyle.DropDownList;
      categoriesBox.DropDownHeight = categoriesBox.ItemHeight * 15;
      categoriesBox.SetCueBanner("Category filter…");
      categoriesBox.Width = (int) (300 * GH_GraphicsUtil.UiScale);

      familiesBox.Tag = Tuple.Create(elementTypesBox, categoriesBox);
      familiesBox.SelectedIndexChanged += FamiliesBox_SelectedIndexChanged;
      categoriesBox.Tag = Tuple.Create(elementTypesBox, familiesBox);
      categoriesBox.SelectedIndexChanged += CategoriesBox_SelectedIndexChanged;

      var categoriesTypeBox = new ComboBox();
      categoriesTypeBox.DropDownStyle = ComboBoxStyle.DropDownList;
      categoriesTypeBox.Width = (int) (300 * GH_GraphicsUtil.UiScale);
      categoriesTypeBox.Tag = categoriesBox;
      categoriesTypeBox.SelectedIndexChanged += CategoryType_SelectedIndexChanged;
      categoriesTypeBox.Items.Add("All Categories");
      categoriesTypeBox.Items.Add("Model");
      categoriesTypeBox.Items.Add("Annotation");
      categoriesTypeBox.Items.Add("Tags");
      categoriesTypeBox.Items.Add("Internal");
      categoriesTypeBox.Items.Add("Analytical");

      if ((DB.ElementType) Current is DB.ElementType current)
      {
        if (current.Category.IsTagCategory)
          categoriesTypeBox.SelectedIndex = 3;
        else
          categoriesTypeBox.SelectedIndex = (int) current.Category.CategoryType;

        var categoryIndex = 0;
        var currentCategory = Types.Category.FromCategory(current.Category);
        foreach (var category in categoriesBox.Items.Cast<Types.Category>())
        {
          if (currentCategory.Equals(category))
          {
            categoriesBox.SelectedIndex = categoryIndex;
            break;
          }
          categoryIndex++;
        }

        var familyIndex = 0;
        foreach (var familyName in familiesBox.Items.Cast<string>())
        {
          if (current.GetFamilyName() == familyName)
          {
            familiesBox.SelectedIndex = familyIndex;
            break;
          }
          familyIndex++;
        }
      }
      else categoriesTypeBox.SelectedIndex = 0;

      Menu_AppendCustomItem(menu, categoriesTypeBox);
      Menu_AppendCustomItem(menu, categoriesBox);
      Menu_AppendCustomItem(menu, familiesBox);
      Menu_AppendCustomItem(menu, elementTypesBox);
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
      categoriesBox.DisplayMember = "DisplayName";
      foreach (var category in categories.OrderBy(x => x.Name))
        categoriesBox.Items.Add(Types.Category.FromCategory(category));
    }

    private void RefreshFamiliesBox(ComboBox familiesBox, ComboBox categoriesBox)
    {
      familiesBox.SelectedIndex = -1;
      familiesBox.Items.Clear();

      using (var collector = new DB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
      {
        var categories = (
                  categoriesBox.SelectedItem is Types.Category category ?
                    Enumerable.Repeat(category, 1) :
                    categoriesBox.Items.OfType<Types.Category>()
                  ).
                  Select(x => x.Id).
                  ToArray();

        foreach (var familyName in collector.WhereElementIsElementType().
          WherePasses(new DB.ElementMulticategoryFilter(categories)).
          ToElements().Cast<DB.ElementType>().GroupBy(x => x.GetFamilyName()).Select(x => x.Key))
        {
          familiesBox.Items.Add(familyName);
        }
      }
    }

    private void RefreshElementTypesList(ListBox listBox, ComboBox categoriesBox, ComboBox familiesBox)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ElementTypesBox_SelectedIndexChanged;
      listBox.Items.Clear();

      {
        var categories = (
                            categoriesBox.SelectedItem is Types.Category category ?
                            Enumerable.Repeat(category, 1) :
                            categoriesBox.Items.OfType<Types.Category>()
                          ).
                          Select(x => x.Id).
                          ToArray();

        if (categories.Length > 0)
        {
          var elementTypes = default(IEnumerable<DB.ElementId>);
          using (var collector = new DB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
          {
            elementTypes = collector.WhereElementIsElementType().
                            WherePasses(new DB.ElementMulticategoryFilter(categories)).
                            ToElementIds();
          }

          var familyName = familiesBox.SelectedItem as string;

          listBox.DisplayMember = "DisplayName";
          foreach (var elementType in elementTypes)
          {
            if
            (
              !string.IsNullOrEmpty(familyName) &&
              doc.GetElement(elementType) is DB.ElementType type &&
              type.GetFamilyName() != familyName
            )
              continue;

            listBox.Items.Add(Types.ElementType.FromElementId(doc, elementType));
          }
        }
      }

      listBox.SelectedIndex = listBox.Items.OfType<Types.ElementType>().IndexOf(Current, 0).FirstOr(-1);
      listBox.SelectedIndexChanged += ElementTypesBox_SelectedIndexChanged;
    }

    private void CategoryType_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox categoriesTypeBox && categoriesTypeBox.Tag is ComboBox categoriesBox)
      {
        RefreshCategoryList(categoriesBox, (DB.CategoryType) categoriesTypeBox.SelectedIndex);
        if (categoriesBox.Tag is Tuple<ListBox, ComboBox> tuple)
        {
          RefreshFamiliesBox(tuple.Item2, categoriesBox);
          RefreshElementTypesList(tuple.Item1, categoriesBox, tuple.Item2);
        }
      }
    }

    private void CategoriesBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox categoriesBox && categoriesBox.Tag is Tuple<ListBox, ComboBox> tuple)
      {
        RefreshFamiliesBox(tuple.Item2, categoriesBox);
        RefreshElementTypesList(tuple.Item1, categoriesBox, tuple.Item2);
      }
    }

    private void FamiliesBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox familiesBox && familiesBox.Tag is Tuple<ListBox, ComboBox> tuple)
        RefreshElementTypesList(tuple.Item1, tuple.Item2, familiesBox);
    }

    private void ElementTypesBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is Types.ElementType value)
          {
            RecordUndoEvent($"Set: {value}");
            PersistentData.Clear();
            PersistentData.Append(value);
          }
        }

        ExpireSolution(true);
      }
    }
  }

  public class ElementTypeByName : ValueList
  {
    public override Guid ComponentGuid => new Guid("D3FB53D3-9118-4F11-A32D-AECB30AA418D");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public ElementTypeByName()
    {
      Name = "ElementType Picker";
      Description = "Provides an Element type picker";
    }

    public override void AddedToDocument(GH_Document document)
    {
      if (NickName == Name)
        NickName = "'Family name here…";

      base.AddedToDocument(document);
    }

    protected override void RefreshList(string familyName)
    {
      var selectedItems = ListItems.Where(x => x.Selected).Select(x => x.Expression).ToList();
      ListItems.Clear();

      if (familyName.Length == 0 || familyName[0] == '\'')
        return;

      if (Revit.ActiveDBDocument is object)
      {
        int selectedItemsCount = 0;
        using (var collector = new DB.FilteredElementCollector(Revit.ActiveDBDocument))
        {
          var elementCollector = collector.WhereElementIsElementType();

          if (Components.ElementCollectorComponent.TryGetFilterStringParam(DB.BuiltInParameter.SYMBOL_FAMILY_NAME_PARAM, ref familyName, out var familyNameFilter))
            elementCollector = elementCollector.WherePasses(familyNameFilter);

          var elementTypes = elementCollector.Cast<DB.ElementType>();

          foreach (var elementType in elementTypes)
          {
            if (familyName is object)
            {
              if (!elementType.GetFamilyName().IsSymbolNameLike(familyName))
                continue;
            }

            if (SourceCount == 0)
            {
              // If is a no pattern match update NickName case
              if (string.Equals(elementType.GetFamilyName(), familyName, StringComparison.OrdinalIgnoreCase))
                familyName = elementType.GetFamilyName();
            }

            var item = new GH_ValueListItem(elementType.GetFamilyName() + " : " + elementType.Name, elementType.Id.IntegerValue.ToString());
            item.Selected = selectedItems.Contains(item.Expression);
            ListItems.Add(item);

            selectedItemsCount += item.Selected ? 1 : 0;
          }
        }

        // If no selection and we are not in CheckList mode try to select default model types
        if (ListItems.Count == 0)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, string.Format("No ElementType found using pattern \"{0}\"", familyName));
        }
        else if (selectedItemsCount == 0 && ListMode != GH_ValueListMode.CheckList)
        {
          var defaultElementTypeIds = new HashSet<string>();
          foreach (var typeGroup in Enum.GetValues(typeof(DB.ElementTypeGroup)).Cast<DB.ElementTypeGroup>())
          {
            var elementTypeId = Revit.ActiveDBDocument.GetDefaultElementTypeId(typeGroup);
            if (elementTypeId != DB.ElementId.InvalidElementId)
              defaultElementTypeIds.Add(elementTypeId.IntegerValue.ToString());
          }

          foreach (var item in ListItems)
            item.Selected = defaultElementTypeIds.Contains(item.Expression);
        }
      }
    }

    protected override void RefreshList(IEnumerable<IGH_Goo> goos)
    {
      var selectedItems = ListItems.Where(x => x.Selected).Select(x => x.Expression).ToList();
      ListItems.Clear();

      if (Revit.ActiveDBDocument is object)
      {
        int selectedItemsCount = 0;
        using (var collector = new DB.FilteredElementCollector(Revit.ActiveDBDocument))
        using (var elementTypeCollector = collector.WhereElementIsElementType())
        {
          foreach (var goo in goos)
          {
            var e = new Types.Element();
            if (e.CastFrom(goo))
            {
              switch ((DB.Element) e)
              {
                case DB.Family family:
                  foreach (var elementType in elementTypeCollector.Cast<DB.ElementType>())
                  {
                    if (elementType.GetFamilyName() != family.Name)
                      continue;

                    var item = new GH_ValueListItem(elementType.GetFamilyName() + " : " + elementType.Name, elementType.Id.IntegerValue.ToString());
                    item.Selected = selectedItems.Contains(item.Expression);
                    ListItems.Add(item);

                    selectedItemsCount += item.Selected ? 1 : 0;
                  }
                  break;
                case DB.ElementType elementType:
                  {
                    var item = new GH_ValueListItem(elementType.GetFamilyName() + " : " + elementType.Name, elementType.Id.IntegerValue.ToString());
                    item.Selected = selectedItems.Contains(item.Expression);
                    ListItems.Add(item);

                    selectedItemsCount += item.Selected ? 1 : 0;
                  }
                  break;
                case DB.Element element:
                  {
                    var type = Revit.ActiveDBDocument.GetElement(element.GetTypeId()) as DB.ElementType;
                    var item = new GH_ValueListItem(type.GetFamilyName() + " : " + type.Name, type.Id.IntegerValue.ToString());
                    item.Selected = selectedItems.Contains(item.Expression);
                    ListItems.Add(item);

                    selectedItemsCount += item.Selected ? 1 : 0;
                  }
                  break;
              }
            }
            else
            {
              var c = new Types.Category();
              if (c.CastFrom(goo))
              {
                foreach (var elementType in elementTypeCollector.OfCategoryId(c.Value).Cast<DB.ElementType>())
                {
                  var item = new GH_ValueListItem(elementType.GetFamilyName() + " : " + elementType.Name, elementType.Id.IntegerValue.ToString());
                  item.Selected = selectedItems.Contains(item.Expression);
                  ListItems.Add(item);

                  selectedItemsCount += item.Selected ? 1 : 0;
                }
              }
              else
              {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Unable to convert some input data.");
              }
            }
          }
        }

        // If no selection and we are not in CheckList mode try to select default model types
        if (ListItems.Count > 0 && selectedItemsCount == 0 && ListMode != GH_ValueListMode.CheckList)
        {
          var defaultElementTypeIds = new HashSet<string>();
          foreach (var typeGroup in Enum.GetValues(typeof(DB.ElementTypeGroup)).Cast<DB.ElementTypeGroup>())
          {
            var elementTypeId = Revit.ActiveDBDocument.GetDefaultElementTypeId(typeGroup);
            if (elementTypeId != DB.ElementId.InvalidElementId)
              defaultElementTypeIds.Add(elementTypeId.IntegerValue.ToString());
          }

          foreach (var item in ListItems)
            item.Selected = defaultElementTypeIds.Contains(item.Expression);
        }
      }
    }

    protected override string HtmlHelp_Source()
    {
      var nTopic = new Grasshopper.GUI.HTML.GH_HtmlFormatter(this)
      {
        Title = Name,
        Description =
        @"<p>This component is a special interface object that allows for quick picking a Revit ElementType object.</p>" +
        @"<p>Double click on it and use the name input box to enter a family name, alternativelly you can enter a name patter. " +
        @"If a pattern is used, this param list will be filled up with all the element types that match it.</p>" +
        @"<p>Several kind of patterns are supported, the method used depends on the first pattern character:</p>" +
        @"<dl>" +
        @"<dt><b>></b></dt><dd>Starts with</dd>" +
        @"<dt><b><</b></dt><dd>Ends with</dd>" +
        @"<dt><b>?</b></dt><dd>Contains, same as a regular search</dd>" +
        @"<dt><b>:</b></dt><dd>Wildcards, see Microsoft.VisualBasic " + "<a target=\"_blank\" href=\"https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/operators/like-operator#pattern-options\">LikeOperator</a></dd>" +
        @"<dt><b>;</b></dt><dd>Regular expresion, see " + "<a target=\"_blank\" href=\"https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference\">here</a> as reference</dd>" +
        @"</dl>",
        ContactURI = AssemblyInfo.ContactURI,
        WebPageURI = AssemblyInfo.WebPageURI
      };

      nTopic.AddRemark(@"You can also connect a list of categories, families or types at left as an input and this component will be filled up with all types that belong to those objects.");

      return nTopic.HtmlFormat();
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Documents.ElementTypes
{
  public class ElementTypeByName : ValueList
  {
    public override Guid ComponentGuid => new Guid("D3FB53D3-9118-4F11-A32D-AECB30AA418D");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public ElementTypeByName()
    {
      Name = "ElementType.ByName";
      Description = "Provides an Element type picker";
    }

    public override void AddedToDocument(GH_Document document)
    {
      if (NickName == Name)
        NickName = "'Family name here…";

      base.AddedToDocument(document);
    }

    protected override void RefreshList(string FamilyName)
    {
      var selectedItems = ListItems.Where(x => x.Selected).Select(x => x.Expression).ToList();
      ListItems.Clear();

      if (FamilyName.Length == 0 || FamilyName[0] == '\'')
        return;

      if (Revit.ActiveDBDocument is object)
      {
        int selectedItemsCount = 0;
        using (var collector = new DB.FilteredElementCollector(Revit.ActiveDBDocument))
        {
          foreach (var elementType in collector.WhereElementIsElementType().Cast<DB.ElementType>())
          {
            if (!elementType.FamilyName.IsSymbolNameLike(FamilyName))
              continue;

            if (SourceCount == 0)
            {
              // If is a no pattern match update NickName case
              if (string.Equals(elementType.FamilyName, FamilyName, StringComparison.OrdinalIgnoreCase))
                FamilyName = elementType.FamilyName;
            }

            var item = new GH_ValueListItem(elementType.FamilyName + " : " + elementType.Name, elementType.Id.IntegerValue.ToString());
            item.Selected = selectedItems.Contains(item.Expression);
            ListItems.Add(item);

            selectedItemsCount += item.Selected ? 1 : 0;
          }
        }

        // If no selection and we are not in CheckList mode try to select default model types
        if (ListItems.Count == 0)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, string.Format("No ElementType found using pattern \"{0}\"", FamilyName));
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
                    if (elementType.FamilyName != family.Name)
                      continue;

                    var item = new GH_ValueListItem(elementType.FamilyName + " : " + elementType.Name, elementType.Id.IntegerValue.ToString());
                    item.Selected = selectedItems.Contains(item.Expression);
                    ListItems.Add(item);

                    selectedItemsCount += item.Selected ? 1 : 0;
                  }
                  break;
                case DB.ElementType elementType:
                  {
                    var item = new GH_ValueListItem(elementType.FamilyName + " : " + elementType.Name, elementType.Id.IntegerValue.ToString());
                    item.Selected = selectedItems.Contains(item.Expression);
                    ListItems.Add(item);

                    selectedItemsCount += item.Selected ? 1 : 0;
                  }
                  break;
                case DB.Element element:
                  {
                    var type = Revit.ActiveDBDocument.GetElement(element.GetTypeId()) as DB.ElementType;
                    var item = new GH_ValueListItem(type.FamilyName + " : " + type.Name, type.Id.IntegerValue.ToString());
                    item.Selected = selectedItems.Contains(item.Expression);
                    ListItems.Add(item);

                    selectedItemsCount += item.Selected ? 1 : 0;
                  }
                  break;
              }
            }
            else
            {
              var c = new Types.Documents.Categories.Category();
              if (c.CastFrom(goo))
              {
                foreach (var elementType in elementTypeCollector.OfCategoryId(c.Value).Cast<DB.ElementType>())
                {
                  var item = new GH_ValueListItem(elementType.FamilyName + " : " + elementType.Name, elementType.Id.IntegerValue.ToString());
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
        ContactURI = @"https://discourse.mcneel.com/c/serengeti/inside"
      };

      nTopic.AddRemark(@"You can also connect a list of categories, families or types at left as an input and this component will be filled up with all types that belong to those objects.");

      return nTopic.HtmlFormat();
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
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

      if (Revit.ActiveDBDocument is DB.Document doc)
      {
        int selectedItemsCount = 0;
        using (var collector = new DB.FilteredElementCollector(doc))
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

            var referenceId = FullUniqueId.Format(doc.GetFingerprintGUID(), elementType.UniqueId);
            var item = new GH_ValueListItem($"{elementType.GetFamilyName()}  : {elementType.Name}", $"\"{referenceId}\"");
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

      if (Revit.ActiveDBDocument is DB.Document doc)
      {
        int selectedItemsCount = 0;
        using (var collector = new DB.FilteredElementCollector(doc))
        using (var elementTypeCollector = collector.WhereElementIsElementType())
        {
          foreach (var goo in goos)
          {
            var e = new Types.Element();
            if (e.CastFrom(goo))
            {
              switch (e.Value)
              {
                case DB.Family family:
                  foreach (var elementType in elementTypeCollector.Cast<DB.ElementType>())
                  {
                    if (elementType.GetFamilyName() != family.Name)
                      continue;

                    var referenceId = FullUniqueId.Format(doc.GetFingerprintGUID(), elementType.UniqueId);
                    var item = new GH_ValueListItem($"{elementType.GetFamilyName()} : {elementType.Name}", $"\"{referenceId}\"");
                    item.Selected = selectedItems.Contains(item.Expression);
                    ListItems.Add(item);

                    selectedItemsCount += item.Selected ? 1 : 0;
                  }
                  break;
                case DB.ElementType elementType:
                {
                  var referenceId = FullUniqueId.Format(doc.GetFingerprintGUID(), elementType.UniqueId);
                  var item = new GH_ValueListItem(elementType.GetFamilyName() + " : " + elementType.Name, $"\"{referenceId}\"");
                  item.Selected = selectedItems.Contains(item.Expression);
                  ListItems.Add(item);

                  selectedItemsCount += item.Selected ? 1 : 0;
                }
                break;
                case DB.Element element:
                {
                  var type = doc.GetElement(element.GetTypeId()) as DB.ElementType;
                  var referenceId = FullUniqueId.Format(doc.GetFingerprintGUID(), type.UniqueId);
                  var item = new GH_ValueListItem(type.GetFamilyName() + " : " + type.Name, $"\"{referenceId}\"");
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
                foreach (var elementType in elementTypeCollector.WhereCategoryIdEqualsTo(c.Id).Cast<DB.ElementType>())
                {
                  var referenceId = FullUniqueId.Format(doc.GetFingerprintGUID(), elementType.UniqueId);
                  var item = new GH_ValueListItem(elementType.GetFamilyName() + " : " + elementType.Name, $"\"{referenceId}\"");
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
            var elementTypeId = doc.GetDefaultElementTypeId(typeGroup);
            if (elementTypeId != DB.ElementId.InvalidElementId)
            {
              var type = doc.GetElement(elementTypeId);
              var referenceId = FullUniqueId.Format(doc.GetFingerprintGUID(), type.UniqueId);
              defaultElementTypeIds.Add(elementTypeId.IntegerValue.ToString());
            }
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

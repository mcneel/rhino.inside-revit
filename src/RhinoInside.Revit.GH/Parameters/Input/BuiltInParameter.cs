using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
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

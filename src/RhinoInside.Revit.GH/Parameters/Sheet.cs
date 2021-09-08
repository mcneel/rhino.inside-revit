using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Sheet : Element<Types.IGH_Sheet, DB.ViewSheet>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    public override Guid ComponentGuid => new Guid("3c0d65b7-4173-423c-97e9-c6124e8c258a");

    public Sheet() : base("Sheet", "Sheet", "Contains a Revit sheet", "Params", "Revit Primitives") { }

    protected override Types.IGH_Sheet InstantiateT() => new Types.Sheet();

    #region UI
    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0) return;
      if (Revit.ActiveUIDocument?.Document is null) return;

      var listBox = new ListBox
      {
        Sorted = true,
        BorderStyle = BorderStyle.FixedSingle,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Height = (int) (100 * GH_GraphicsUtil.UiScale)
      };
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

      var placeholderCheckBox = new CheckBox
      {
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Text = "Show Placeholders Only",
        Checked = PersistentValue is Types.Sheet sheet ? sheet.Value.IsPlaceholder : false,
        Tag = listBox
      };
      placeholderCheckBox.CheckedChanged += PlaceHolderCheckBox_CheckedChanged;

      RefreshViewsList(listBox, placeholderCheckBox.Checked);

      Menu_AppendCustomItem(menu, placeholderCheckBox);
      Menu_AppendCustomItem(menu, listBox);
    }

    private void PlaceHolderCheckBox_CheckedChanged(object sender, EventArgs e)
    {
      if (sender is CheckBox checkbox)
      {
        if (checkbox.Tag is ListBox listBox)
          RefreshViewsList(listBox, checkbox.Checked);
      }
    }

    private void RefreshViewsList(ListBox listBox, bool isPlaceholder = false)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.Items.Clear();

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var sheets = collector.OfClass(typeof(DB.ViewSheet))
                              .Cast<DB.ViewSheet>()
                              .Where(x => !x.IsTemplate)
                              .Where(x => x.IsPlaceholder == isPlaceholder);

        listBox.DisplayMember = "DisplayName";
        foreach (var sheet in sheets)
          listBox.Items.Add(new Types.Sheet(sheet));
      }

      listBox.SelectedIndex = listBox.Items.OfType<Types.Sheet>().IndexOf(PersistentValue, 0).FirstOr(-1);
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is Types.Sheet sheet)
          {
            RecordPersistentDataEvent($"Set: {sheet}");
            PersistentData.Clear();
            PersistentData.Append(sheet);
            OnObjectChanged(GH_ObjectEventType.PersistentData);
          }
        }

        ExpireSolution(true);
      }
    }
    #endregion
  }
}

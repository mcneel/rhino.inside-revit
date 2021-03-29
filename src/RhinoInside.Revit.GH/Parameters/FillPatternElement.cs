using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class FillPatternElement : Element<Types.FillPatternElement, DB.FillPatternElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    public override Guid ComponentGuid => new Guid("EFDDB3D7-CF2A-4972-B1C4-29374BB89149");

    public FillPatternElement() : base("Fill Pattern", "Fill Pattern", "Represents a Revit document fill pattern.", "Params", "Revit Primitives") { }

    #region UI
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.FillPatterns);
      Menu_AppendItem
      (
        menu, $"Open Fill Patterns…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0)
        return;

      var listBox = new ListBox();
      listBox.BorderStyle = BorderStyle.FixedSingle;
      listBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
      listBox.Height = (int) (100 * GH_GraphicsUtil.UiScale);
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
      listBox.Sorted = true;

      var patternTargetBox = new ComboBox();
      patternTargetBox.DropDownStyle = ComboBoxStyle.DropDownList;
      patternTargetBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
      patternTargetBox.Tag = listBox;
      patternTargetBox.SelectedIndexChanged += PatternTargetBox_SelectedIndexChanged;
      patternTargetBox.SetCueBanner("Fill Pattern target filter…");
      patternTargetBox.Sorted = true;

      using (var collector = new DB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
      {
        listBox.Items.Clear();

        var patterns = collector.
                        OfClass(typeof(DB.FillPatternElement)).
                        Cast<DB.FillPatternElement>().
                        GroupBy(x => x.GetFillPattern().Target);

        foreach (var pattern in patterns)
          patternTargetBox.Items.Add(pattern.Key);

        if (Current?.Value is DB.FillPatternElement current)
        {
          var targetIndex = 0;
          foreach (var patternTarget in patternTargetBox.Items.Cast<DB.FillPatternTarget>())
          {
            if (current.GetFillPattern().Target == patternTarget)
            {
              patternTargetBox.SelectedIndex = targetIndex;
              break;
            }
            targetIndex++;
          }
        }
        else patternTargetBox.SelectedIndex = (int) DB.FillPatternTarget.Drafting;
      }

      Menu_AppendCustomItem(menu, patternTargetBox);
      Menu_AppendCustomItem(menu, listBox);
    }

    private void PatternTargetBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox comboBox)
      {
        if (comboBox.Tag is ListBox listBox)
          RefreshPatternList(listBox, comboBox.SelectedItem as DB.FillPatternTarget?);
      }
    }

    private void RefreshPatternList(ListBox listBox, DB.FillPatternTarget? patternTarget)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.Items.Clear();

      using (var collector = new DB.FilteredElementCollector(doc).OfClass(typeof(DB.FillPatternElement)))
      {
        var patterns = collector.
                        Cast<DB.FillPatternElement>().
                        Where(x => !patternTarget.HasValue || x.GetFillPattern().Target == patternTarget);

        listBox.DisplayMember = "DisplayName";
        foreach (var pattern in patterns)
          listBox.Items.Add(new Types.FillPatternElement(pattern));
      }

      listBox.SelectedIndex = listBox.Items.OfType<Types.FillPatternElement>().IndexOf(Current, 0).FirstOr(-1);
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is Types.FillPatternElement value)
          {
            RecordUndoEvent($"Set: {value}");
            PersistentData.Clear();
            PersistentData.Append(value);
          }
        }

        ExpireSolution(true);
      }
    }
    #endregion
  }
}

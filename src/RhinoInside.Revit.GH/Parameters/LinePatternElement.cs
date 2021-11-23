using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class LinePatternElement : Element<Types.LinePatternElement, ARDB.LinePatternElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    public override Guid ComponentGuid => new Guid("EB5AB657-AE01-42F0-BF98-071DA6D7A2D2");

    public LinePatternElement() : base("Line Pattern", "Line Pattern", "Contains a collection of Revit line pattern elements", "Params", "Revit Primitives") { }

    #region UI
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.LinePatterns);
      Menu_AppendItem
      (
        menu, $"Open Line Patterns…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
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
        Height = (int) (100 * GH_GraphicsUtil.UiScale)
      };
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

      Menu_AppendCustomItem(menu, listBox);
      RefreshPatternList(listBox);
    }

    private void RefreshPatternList(ListBox listBox)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.Items.Clear();

      using (var collector = new ARDB.FilteredElementCollector(doc).OfClass(typeof(ARDB.LinePatternElement)))
      {
        var patterns = collector.
                        Cast<ARDB.LinePatternElement>();

        listBox.DisplayMember = "DisplayName";

        listBox.Items.Add(new Types.LinePatternElement(doc, new ARDB.ElementId((int) External.DB.BuiltInLinePattern.Solid)));
        foreach (var pattern in patterns)
          listBox.Items.Add(new Types.LinePatternElement(pattern));
      }

      listBox.SelectedIndex = listBox.Items.Cast<Types.LinePatternElement>().IndexOf(PersistentValue, 0).FirstOr(-1);
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is Types.LinePatternElement value)
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
  }
}

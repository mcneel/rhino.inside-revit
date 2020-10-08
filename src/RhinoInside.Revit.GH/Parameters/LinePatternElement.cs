using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class LinePatternElement : ElementIdWithoutPreviewParam<Types.LinePatternElement, DB.LinePatternElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary /*| GH_Exposure.hidden*/;
    public override Guid ComponentGuid => new Guid("EB5AB657-AE01-42F0-BF98-071DA6D7A2D2");

    public LinePatternElement() : base("Line Pattern", "Line Pattern", "Represents a Revit document line pattern.", "Params", "Revit Primitives") { }

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

      Menu_AppendCustomItem(menu, listBox);
      RefreshPatternList(listBox);
    }

    private void RefreshPatternList(ListBox listBox)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.Items.Clear();

      using (var collector = new DB.FilteredElementCollector(doc).OfClass(typeof(DB.LinePatternElement)))
      {
        var patterns = collector.
                        Cast<DB.LinePatternElement>();

        listBox.DisplayMember = "DisplayName";

        listBox.Items.Add(new Types.LinePatternElement(doc, new DB.ElementId(-3000010)));
        foreach (var pattern in patterns)
          listBox.Items.Add(new Types.LinePatternElement(pattern));
      }

      listBox.SelectedIndex = listBox.Items.OfType<Types.LinePatternElement>().IndexOf(Current, 0).FirstOr(-1);
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
            RecordUndoEvent($"Set: {value}");
            PersistentData.Clear();
            PersistentData.Append(value);
          }
        }

        ExpireSolution(true);
      }
    }
  }
}

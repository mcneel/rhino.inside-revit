using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Level : GraphicalElementT<Types.Level, DB.Level>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary;
    public override Guid ComponentGuid => new Guid("3238F8BC-8483-4584-B47C-48B4933E478E");

    public Level() : base("Level", "Level", "Represents a Revit document level.", "Params", "Revit Primitives") { }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0)
        return;

      if (MutableNickName)
      {
        var listBox = new ListBox();
        listBox.BorderStyle = BorderStyle.FixedSingle;
        listBox.Width = (int) (250 * GH_GraphicsUtil.UiScale);
        listBox.Height = (int) (100 * GH_GraphicsUtil.UiScale);
        listBox.SelectionMode = SelectionMode.MultiExtended;
        listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

        Menu_AppendCustomItem(menu, listBox);
        RefreshLevelList(listBox);
      }

      base.Menu_AppendPromptOne(menu);
   }

    private void RefreshLevelList(ListBox listBox)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.DisplayMember = "DisplayName";
      listBox.Items.Clear();

      using (var collector = new DB.FilteredElementCollector(doc).OfClass(typeof(DB.Level)))
      {
        var levels = collector.Cast<DB.Level>().
          OrderBy(x => x.Elevation).
          Select(x => new Types.Level(x)).
          ToList();

        foreach (var level in levels)
          listBox.Items.Add(level);

        var selectedItems = levels.Intersect(PersistentData.OfType<Types.Level>());

        foreach ( var item in selectedItems)
          listBox.SelectedItems.Add(item);
      }

      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        RecordUndoEvent($"Set: {NickName}");
        PersistentData.Clear();
        PersistentData.AppendRange(listBox.SelectedItems.OfType<Types.Level>());

        ExpireSolution(true);
      }
    }
  }

  public class Grid : GraphicalElementT<Types.Grid, DB.Grid>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary;
    public override Guid ComponentGuid => new Guid("7D2FB886-A184-41B8-A7D6-A6FDB85CF4E4");

    public Grid() : base("Grid", "Grid", "Represents a Revit document grid.", "Params", "Revit Primitives") { }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0)
        return;

      if (MutableNickName)
      {
        var listBox = new ListBox();
        listBox.BorderStyle = BorderStyle.FixedSingle;
        listBox.Width = (int) (250 * GH_GraphicsUtil.UiScale);
        listBox.Height = (int) (100 * GH_GraphicsUtil.UiScale);
        listBox.SelectionMode = SelectionMode.MultiExtended;
        listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
        listBox.Sorted = true;

        Menu_AppendCustomItem(menu, listBox);
        RefreshGridList(listBox);
      }

      base.Menu_AppendPromptOne(menu);
    }

    private void RefreshGridList(ListBox listBox)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.DisplayMember = "DisplayName";
      listBox.Items.Clear();

      using (var collector = new DB.FilteredElementCollector(doc).OfClass(typeof(DB.Grid)))
      {
        var items = collector.Cast<DB.Grid>().
          Select(x => new Types.Grid(x)).
          ToList();

        foreach (var item in items)
          listBox.Items.Add(item);

        var selectedItems = items.Intersect(PersistentData.OfType<Types.Grid>());

        foreach (var item in selectedItems)
          listBox.SelectedItems.Add(item);
      }

      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        RecordUndoEvent($"Set: {NickName}");
        PersistentData.Clear();
        PersistentData.AppendRange(listBox.SelectedItems.OfType<Types.Grid>());

        ExpireSolution(true);
      }
    }
  }
}

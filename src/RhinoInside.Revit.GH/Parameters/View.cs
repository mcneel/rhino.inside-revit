using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class View : ElementIdWithoutPreviewParam<Types.View, DB.View>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    public override Guid ComponentGuid => new Guid("2DC4B866-54DB-4CE6-94C0-C51B33D35B49");
    protected override Types.View PreferredCast(object data) => Types.View.FromElement(data as DB.View) as Types.View;

    public View() : base("View", "View", "Represents a Revit view.", "Params", "Revit Primitives") { }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      var listBox = new ListBox();
      listBox.BorderStyle = BorderStyle.FixedSingle;
      listBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
      listBox.Height = (int) (100 * GH_GraphicsUtil.UiScale);
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
      listBox.Sorted = true;

      var viewTypeBox = new ComboBox();
      viewTypeBox.DropDownStyle = ComboBoxStyle.DropDownList;
      viewTypeBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
      viewTypeBox.Tag = listBox;
      viewTypeBox.SelectedIndexChanged += ViewTypeBox_SelectedIndexChanged;
      viewTypeBox.SetCueBanner("View type filter…");

      using (var collector = new DB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
      {
        listBox.Items.Clear();

        var views = collector.
                    OfClass(typeof(DB.View)).
                    Cast<DB.View>().
                    GroupBy(x => x.ViewType);

        foreach(var view in views)
          viewTypeBox.Items.Add(view.Key);
      }

      RefreshViewsList(listBox, DB.ViewType.Undefined);

      Menu_AppendCustomItem(menu, viewTypeBox);
      Menu_AppendCustomItem(menu, listBox);
    }

    private void ViewTypeBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox comboBox)
      {
        if (comboBox.Tag is ListBox listBox)
          RefreshViewsList(listBox, (DB.ViewType) comboBox.SelectedItem);
      }
    }

    private void RefreshViewsList(ListBox listBox, DB.ViewType viewType)
    {
      var doc = Revit.ActiveUIDocument.Document;
      var selectedIndex = -1;

      try
      {
        listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
        listBox.Items.Clear();

        var current = default(Types.View);
        if (SourceCount == 0 && PersistentDataCount == 1)
        {
          if (PersistentData.get_FirstItem(true) is Types.View firstValue)
            current = firstValue.Duplicate() as Types.View;
        }

        using (var collector = new DB.FilteredElementCollector(doc))
        {
          var views = collector.
                      OfClass(typeof(DB.View)).
                      Cast<DB.View>().
                      Where(x => viewType == DB.ViewType.Undefined || x.ViewType == viewType);

          foreach (var view in views)
          {
            var tag = new Types.View(view);
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
            PersistentData.Append(value.ProxyOwner.Duplicate() as Types.View);
          }
        }

        ExpireSolution(true);
      }
    }
  }
}

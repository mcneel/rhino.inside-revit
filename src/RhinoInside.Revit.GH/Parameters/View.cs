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
  public class View : ElementIdWithoutPreviewParam<Types.IGH_View, DB.View>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    public override Guid ComponentGuid => new Guid("2DC4B866-54DB-4CE6-94C0-C51B33D35B49");

    public View() : base("View", "View", "Represents a Revit view.", "Params", "Revit Primitives") { }

    protected override Types.IGH_View InstantiateT() => new Types.View();

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
                    Where(x => !x.IsTemplate).
                    GroupBy(x => x.Document.GetElement<DB.ViewFamilyType>(x.GetTypeId())?.ViewFamily ?? DB.ViewFamily.Invalid);

        viewTypeBox.DisplayMember = "Text";
        foreach (var view in views)
        {
          if(view.Key != DB.ViewFamily.Invalid)
            viewTypeBox.Items.Add(new Types.ViewFamily(view.Key));
        }

        if (Current?.APIView is DB.View current)
        {
          var familyIndex = 0;
          foreach (var viewFamily in viewTypeBox.Items.Cast<Types.ViewFamily>())
          {
            var type = current.Document.GetElement<DB.ViewFamilyType>(current.GetTypeId());
            if (type.ViewFamily == viewFamily.Value)
            {
              viewTypeBox.SelectedIndex = familyIndex;
              break;
            }
            familyIndex++;
          }
        }
        else RefreshViewsList(listBox, DB.ViewFamily.Invalid);
      }

      Menu_AppendCustomItem(menu, viewTypeBox);
      Menu_AppendCustomItem(menu, listBox);
    }

    private void ViewTypeBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox comboBox)
      {
        if (comboBox.Tag is ListBox listBox)
          RefreshViewsList(listBox, ((Types.ViewFamily) comboBox.SelectedItem)?.Value ?? DB.ViewFamily.Invalid);
      }
    }

    private void RefreshViewsList(ListBox listBox, DB.ViewFamily viewFamily)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.Items.Clear();

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var views = collector.
                    OfClass(typeof(DB.View)).
                    Cast<DB.View>().
                    Where(x => !x.IsTemplate).
                    Where(x => viewFamily == DB.ViewFamily.Invalid || x.Document.GetElement<DB.ViewFamilyType>(x.GetTypeId())?.ViewFamily == viewFamily);

        listBox.DisplayMember = "DisplayName";
        foreach (var view in views)
          listBox.Items.Add(new Types.View(view));
      }

      listBox.SelectedIndex = listBox.Items.OfType<Types.View>().IndexOf(Current, 0).FirstOr(-1);
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is Types.View value)
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

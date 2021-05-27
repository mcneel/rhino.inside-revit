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
  public class View : Element<Types.IGH_View, DB.View>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    public override Guid ComponentGuid => new Guid("2DC4B866-54DB-4CE6-94C0-C51B33D35B49");

    public View() : base("View", "View", "Contains a collection of Revit view elements", "Params", "Revit Primitives") { }

    protected override Types.IGH_View InstantiateT() => new Types.View();

    #region UI
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      {
        var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.Default3DView);
        Menu_AppendItem
        (
          menu, "Default 3D View…",
          (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
          activeApp.CanPostCommand(commandId), false
        );
      }
#if REVIT_2019
      {
        var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.CloseInactiveViews);
        Menu_AppendItem
        (
          menu, "Close Inactive Views…",
          (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
          activeApp.CanPostCommand(commandId), false
        );
      }
#endif
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0 || Revit.ActiveUIDocument is null)
        return;

      var listBox = new ListBox
      {
        Sorted = true,
        BorderStyle = BorderStyle.FixedSingle,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Height = (int) (100 * GH_GraphicsUtil.UiScale)
      };
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

      var viewTypeBox = new ComboBox
      {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Tag = listBox
      };
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

        var viewFamily = (PersistentValue?.Type?.Value as DB.ViewFamilyType)?.ViewFamily ??
          DB.ViewFamily.Invalid;

        if (viewFamily != DB.ViewFamily.Invalid)
        {
          var familyIndex = 0;
          foreach (var family in viewTypeBox.Items.Cast<Types.ViewFamily>())
          {
            if (family.Value == viewFamily)
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

      listBox.SelectedIndex = listBox.Items.OfType<Types.View>().IndexOf(PersistentValue, 0).FirstOr(-1);
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

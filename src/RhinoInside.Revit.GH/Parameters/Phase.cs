using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.2")]
  public class Phase : Element<Types.Phase, ARDB.Phase>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("353FFB47-46D6-4FEE-8DBD-40E683416531");

    public Phase() : base("Phase", "Phase", "Contains a collection of Revit construction phase elements", "Params", "Revit") { }

    #region UI
    public override void Menu_AppendActions(ToolStripDropDown menu)
    {
      base.Menu_AppendActions(menu);
      menu.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.Phases, "Open Phases…");
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0) return;
      if (Revit.ActiveUIDocument?.Document is null) return;

      var listBox = new ListBox
      {
        Sorted = false, // Sorted by SequenceNumber
        BorderStyle = BorderStyle.FixedSingle,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Height = (int) (100 * GH_GraphicsUtil.UiScale),
        SelectionMode = SelectionMode.MultiExtended,
        DisplayMember = nameof(Types.Phase.DisplayName)
      };
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

      RefreshPhasesList(listBox);

      Menu_AppendCustomItem(menu, listBox);
    }

    private void RefreshPhasesList(ListBox listBox)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.BeginUpdate();
      listBox.Items.Clear();
      listBox.Items.Add(new Types.Phase());

      {
        var phases = doc.Phases.
          Cast<ARDB.Phase>().
          Select(x => new Types.Phase(x)).
          ToList();

        foreach (var phase in phases)
          listBox.Items.Add(phase);

        var selectedItems = phases.Intersect(PersistentData.OfType<Types.Phase>());

        foreach (var item in selectedItems)
          listBox.SelectedItems.Add(item);
      }

      listBox.EndUpdate();
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        RecordPersistentDataEvent($"Set: {NickName}");
        PersistentData.Clear();
        PersistentData.AppendRange(listBox.SelectedItems.OfType<Types.Phase>());
        OnObjectChanged(GH_ObjectEventType.PersistentData);

        ExpireSolution(true);
      }
    }
    #endregion
  }
}

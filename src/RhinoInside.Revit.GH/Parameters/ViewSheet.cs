using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class ViewSheet : Element<Types.IGH_Sheet, ARDB.ViewSheet>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary;
    public override Guid ComponentGuid => new Guid("3C0D65B7-4173-423C-97E9-C6124E8C258A");

    public ViewSheet() : base("Sheet", "Sheet", "Contains a collection of Revit sheet view elements", "Params", "Revit Primitives") { }

    protected override Types.IGH_Sheet InstantiateT() => new Types.ViewSheet();

    #region UI
    static readonly List<(string title, Func<ARDB.ViewSheet, bool> qualifier)> sheetTypeQualifiers
       = new List<(string title, Func<ARDB.ViewSheet, bool> qualifier)>
    {
      ( title: "All sheets", qualifier: (s) => true ),
      ( title: "Placeholder sheets", qualifier: (s) => s.IsPlaceholder ),
      ( title: "Assembly sheets", qualifier: (s) => s.IsAssemblyView ),
    };

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

      var sheetTypeBox = new ComboBox
      {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Tag = listBox
      };

      sheetTypeBox.SetCueBanner("Sheet type filter…");
      sheetTypeBox.Items.AddRange(
        sheetTypeQualifiers.Select(q => q.title).ToArray()
        );

      if (PersistentValue is Types.ViewSheet sheet)
      {
        var mostStringentMatchedQualifier = sheetTypeQualifiers.Where(q => q.qualifier(sheet.Value)).LastOrDefault();
        sheetTypeBox.SelectedIndex = sheetTypeQualifiers.IndexOf(mostStringentMatchedQualifier);
      }

      sheetTypeBox.SelectedIndexChanged += PlaceHolderCheckBox_CheckedChanged;

      // refresh with the first selector or any other
      RefreshViewsList(listBox, sheetTypeBox.SelectedIndex >= 0 ? sheetTypeBox.SelectedIndex : 0);

      Menu_AppendCustomItem(menu, sheetTypeBox);
      Menu_AppendCustomItem(menu, listBox);
    }

    private void PlaceHolderCheckBox_CheckedChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox sheetTypeBox)
      {
        if (sheetTypeBox.Tag is ListBox listBox)
          RefreshViewsList(listBox, sheetTypeBox.SelectedIndex);
      }
    }

    private void RefreshViewsList(ListBox listBox, int typeIndex = 0)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.Items.Clear();

      var qualifierInfo = sheetTypeQualifiers[typeIndex];
      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var sheets = collector.OfClass(typeof(ARDB.ViewSheet))
                              .Cast<ARDB.ViewSheet>()
                              .Where(x => !x.IsTemplate)
                              .Where(x => qualifierInfo.qualifier(x));

        listBox.DisplayMember = "DisplayName";
        foreach (var sheet in sheets)
          listBox.Items.Add(new Types.ViewSheet(sheet));
      }

      listBox.SelectedIndex = listBox.Items.OfType<Types.ViewSheet>().IndexOf(PersistentValue, 0).FirstOr(-1);
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is Types.ViewSheet sheet)
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

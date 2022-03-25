using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Parameters
{
  public class ViewSheet : View<Types.ViewSheet, ARDB.ViewSheet>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary;
    public override Guid ComponentGuid => new Guid("3C0D65B7-4173-423C-97E9-C6124E8C258A");

    public ViewSheet() : base("Sheet", "Sheet", "Contains a collection of Revit sheet views", "Params", "Revit") { }

    #region UI
    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var NewSheetId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.NewSheet);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(NewSheetId),
        Revit.ActiveUIApplication.CanPostCommand(NewSheetId)
      );
    }

    static readonly (string Text, Predicate<ARDB.ViewSheet> Qualifier)[] SheetTypeQualifiers =
      new(string, Predicate<ARDB.ViewSheet>)[]
      {
        ( "Sheets",             s => !s.IsPlaceholder && !s.IsAssemblyView ),
        ( "Assembly sheets",    s => s.IsAssemblyView ),
        ( "Placeholder sheets", s => s.IsPlaceholder ),
      };

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0) return;
      if (Revit.ActiveUIDocument?.Document is null) return;

      Menu_AppendPromptNew(menu);

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
      sheetTypeBox.Items.AddRange(SheetTypeQualifiers.Select(q => q.Text).ToArray());

      if (PersistentValue is Types.ViewSheet sheet)
      {
        var mostStringentMatchedQualifier = SheetTypeQualifiers.Where(q => q.Qualifier(sheet.Value)).LastOrDefault();
        sheetTypeBox.SelectedIndex = Array.IndexOf(SheetTypeQualifiers, mostStringentMatchedQualifier);
      }
      else sheetTypeBox.SelectedIndex = 0;

      sheetTypeBox.SelectedIndexChanged += SheetTypeBox_SelectedIndexChanged;

      // refresh with the first selector or any other
      RefreshViewsList(listBox, sheetTypeBox.SelectedIndex);

      Menu_AppendCustomItem(menu, sheetTypeBox);
      Menu_AppendCustomItem(menu, listBox);
    }

    private void SheetTypeBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox sheetTypeBox)
      {
        if (sheetTypeBox.Tag is ListBox listBox)
        {
          RefreshViewsList(listBox, sheetTypeBox.SelectedIndex);
        }
      }
    }

    private void RefreshViewsList(ListBox listBox, int typeIndex)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.Items.Clear();

      var qualifierInfo = SheetTypeQualifiers[typeIndex];
      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var sheets = collector.
                     OfClass(typeof(ARDB.ViewSheet)).
                     Cast<ARDB.ViewSheet>().
                     Where(x => !x.IsTemplate).
                     Where(x => qualifierInfo.Qualifier(x));

        listBox.DisplayMember = nameof(Types.ViewSheet.DisplayName);
        foreach (var sheet in sheets)
          listBox.Items.Add(new Types.ViewSheet(sheet));
      }

      listBox.SelectedIndex = listBox.Items.OfType<Types.ViewSheet>().IndexOf(PersistentValue, 0).FirstOr(-1);
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }
    #endregion
  }
}

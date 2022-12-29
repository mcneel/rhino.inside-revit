using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Parameters
{
  using Convert.System.Drawing;

  public class Color : Grasshopper.Kernel.Parameters.Param_Colour
  {
    public override Guid ComponentGuid => new Guid("51F2A94A-A0F9-4E61-B6B8-DF4025E393DA");
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public Color()
    {
      Name = "Color";
      NickName = "Color";
      Description= "Contains a collection of RGB colours";
      Category = "Revit";
      SubCategory = "Object Styles";
    }

    #region PersistentData
    protected GH_Colour PersistentValue
    {
      get
      {
        var value = PersistentData.PathCount == 1 &&
          PersistentData.DataCount == 1 ?
          PersistentData.get_FirstItem(false) :
          default;

        value = (GH_Colour) value?.Duplicate();
        return value?.IsValid == true ? value : default;
      }
    }
    #endregion

    #region UI
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu)
    {
      //base.Menu_AppendPromptMore(menu);
      var pick = Menu_AppendItem(menu, $"Set one {TypeName}");

      Menu_AppendItem(pick.DropDown, $"Empty {TypeName}", Menu_SetEmpty, SourceCount == 0, @checked: false);
      Menu_AppendItem(pick.DropDown, $"Revit {TypeName}…", Menu_PromptRevit, SourceCount == 0, @checked: false);
      Menu_AppendItem(pick.DropDown, $"Rhino {TypeName}…", Menu_PromptRhino, SourceCount == 0, @checked: false);
    }

    private void Menu_SetEmpty(object sender, EventArgs e)
    {
      RecordPersistentDataEvent($"Set: Empty {TypeName}");
      PersistentData.Clear();
      PersistentData.Append(new GH_Colour());
      OnObjectChanged(GH_ObjectEventType.PersistentData);

      ExpireSolution(true);
    }

    private void Menu_PromptRevit(object sender, EventArgs e)
    {
      using (var picker = new ARUI.ColorSelectionDialog())
      {
        if (PersistentValue is GH_Colour current)
        {
          var currentValue = current.Value;
          picker.OriginalColor = currentValue.ToColor();
        }

        switch (picker.Show())
        {
          case ARUI.ItemSelectionDialogResult.Confirmed:
            var value = picker.SelectedColor.ToColor();

            RecordPersistentDataEvent($"Set: {value}");
            PersistentData.Clear();
            PersistentData.Append(new GH_Colour(value));
            OnObjectChanged(GH_ObjectEventType.PersistentData);

            ExpireSolution(true);
            break;
        }
      }
    }

    private void Menu_PromptRhino(object sender, EventArgs e)
    {
      using (var picker = new ARUI.ColorSelectionDialog())
      {
        var currentValue = System.Drawing.Color.Empty;
        if (PersistentValue is GH_Colour current)
          currentValue = current.Value;
          
        if (Rhino.UI.Dialogs.ShowColorDialog(ref currentValue))
        {
          RecordPersistentDataEvent($"Set: {GH_Format.FormatColour(currentValue)}");
          PersistentData.Clear();
          PersistentData.Append(new GH_Colour(currentValue));
          OnObjectChanged(GH_ObjectEventType.PersistentData);

          ExpireSolution(true);
        }
      }
    }
    #endregion
  }
}

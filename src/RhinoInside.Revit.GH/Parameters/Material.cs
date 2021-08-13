using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Material : Element<Types.Material, DB.Material>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    public override Guid ComponentGuid => new Guid("B18EF2CC-2E67-4A5E-9241-9010FB7D27CE");

    public Material() : base("Material", "Material", "Contains a collection of Revit material elements", "Params", "Revit Primitives") { }

    #region UI
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.Materials);
      Menu_AppendItem
      (
        menu, "Open Materials…",
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

      var materialCategoryBox = new ComboBox
      {
        Sorted = true,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Tag = listBox
      };
      materialCategoryBox.SelectedIndexChanged += MaterialCategoryBox_SelectedIndexChanged;
      materialCategoryBox.SetCueBanner("Material class filter…");

      using (var collector = new DB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
      {
        listBox.Items.Clear();

        var materials = collector.
                        OfClass(typeof(DB.Material)).
                        Cast<DB.Material>().
                        GroupBy(x => x.MaterialClass);

        foreach(var cat in materials)
          materialCategoryBox.Items.Add(cat.Key);

        if (PersistentValue?.Value is DB.Material current)
        {
          var familyIndex = 0;
          foreach (var materialClass in materialCategoryBox.Items.Cast<string>())
          {
            if (current.MaterialClass == materialClass)
            {
              materialCategoryBox.SelectedIndex = familyIndex;
              break;
            }
            familyIndex++;
          }
        }
        else RefreshMaterialsList(listBox, default);
      }      

      Menu_AppendCustomItem(menu, materialCategoryBox);
      Menu_AppendCustomItem(menu, listBox);
    }

    private void MaterialCategoryBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox comboBox)
      {
        if (comboBox.Tag is ListBox listBox)
          RefreshMaterialsList(listBox, comboBox.SelectedItem as string);
      }
    }

    private void RefreshMaterialsList(ListBox listBox, string materialClass)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.Items.Clear();
      listBox.Items.Add(new Types.Material());

      using (var collector = new DB.FilteredElementCollector(doc).OfClass(typeof(DB.Material)))
      {
        var materials = collector.
                        Cast<DB.Material>().
                        Where(x => string.IsNullOrEmpty(materialClass) || x.MaterialClass == materialClass);

        listBox.DisplayMember = "DisplayName";
        foreach (var material in materials)
          listBox.Items.Add(new Types.Material(material));
      }

      listBox.SelectedIndex = listBox.Items.Cast<Types.Material>().IndexOf(PersistentValue, 0).FirstOr(-1);
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is Types.Material value)
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

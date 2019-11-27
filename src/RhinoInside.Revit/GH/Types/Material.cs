using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Material : Element
  {
    public override string TypeName => "Revit Material";
    public override string TypeDescription => "Represents a Revit material";
    protected override Type ScriptVariableType => typeof(DB.Material);
    public static explicit operator DB.Material(Material self) =>
      self.Document?.GetElement(self) as DB.Material;

    public Material() { }
    public Material(DB.Material material) : base(material) { }
  }
}

namespace RhinoInside.Revit.GH.Parameters
{
  public class Material : ElementIdNonGeometryParam<Types.Material, DB.Material>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("B18EF2CC-2E67-4A5E-9241-9010FB7D27CE");
    protected override Types.Material PreferredCast(object data) => Types.Material.FromElement(data as DB.Material) as Types.Material;

    public Material() : base("Material", "Material", "Represents a Revit document material.", "Params", "Revit") { }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      if (Kind > GH_ParamKind.input || DataType == GH_ParamData.remote)
      {
        base.AppendAdditionalMenuItems(menu);
        return;
      }

      Menu_AppendWireDisplay(menu);
      Menu_AppendDisconnectWires(menu);

      Menu_AppendReverseParameter(menu);
      Menu_AppendFlattenParameter(menu);
      Menu_AppendGraftParameter(menu);
      Menu_AppendSimplifyParameter(menu);

      {
        var listBox = new ListBox();
        listBox.BorderStyle = BorderStyle.FixedSingle;
        listBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
        listBox.Height = (int) (100 * GH_GraphicsUtil.UiScale);
        listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
        listBox.Sorted = true;

        var materialCategoryBox = new ComboBox();
        materialCategoryBox.DropDownStyle = ComboBoxStyle.DropDownList;
        materialCategoryBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
        materialCategoryBox.Tag = listBox;
        materialCategoryBox.SelectedIndexChanged += MaterialCategoryBox_SelectedIndexChanged;
        materialCategoryBox.SetCueBanner("Material category filter…");

        using (var collector = new DB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
        {
          listBox.Items.Clear();

          var materials = collector.
                          OfClass(typeof(DB.Material)).
                          Cast<DB.Material>().
                          GroupBy(x => x.MaterialCategory);

          foreach(var cat in materials)
            materialCategoryBox.Items.Add(cat.Key);
        }

        RefreshMaterialsList(listBox, null);

        Menu_AppendCustomItem(menu, materialCategoryBox);
        Menu_AppendCustomItem(menu, listBox);
      }

      Menu_AppendManageCollection(menu);
      Menu_AppendSeparator(menu);

      Menu_AppendDestroyPersistent(menu);
      Menu_AppendInternaliseData(menu);

      if (Exposure != GH_Exposure.hidden)
        Menu_AppendExtractParameter(menu);
    }

    private void MaterialCategoryBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox comboBox)
      {
        if (comboBox.Tag is ListBox listBox)
          RefreshMaterialsList(listBox, comboBox.SelectedItem as string);
      }
    }

    private void RefreshMaterialsList(ListBox listBox, string category)
    {
      var current = InstantiateT();
      if (SourceCount == 0 && PersistentDataCount == 1)
      {
        if (PersistentData.get_FirstItem(true) is Types.Material firstValue)
          current = firstValue.Duplicate() as Types.Material;
      }

      using (var collector = new DB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
      {
        listBox.Items.Clear();

        var materials = collector.
                        OfClass(typeof(DB.Material)).
                        Cast<DB.Material>();

        foreach (var material in materials)
        {
          if (!string.IsNullOrEmpty(category) && material.MaterialCategory != category)
            continue;

          var tag = new Types.Material(material);
          int index = listBox.Items.Add(tag.EmitProxy());
          if (tag.UniqueID == current.UniqueID)
            listBox.SelectedIndex = index;
        }
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
            PersistentData.Append(value.ProxyOwner.Duplicate() as Types.Material);
          }
        }

        ExpireSolution(true);
      }
    }

  }
}

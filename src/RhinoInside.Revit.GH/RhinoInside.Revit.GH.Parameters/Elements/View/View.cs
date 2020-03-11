using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.GH.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Elements.View
{
  public class View : ElementIdNonGeometryParam<Types.Elements.View.View, DB.View>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("2DC4B866-54DB-4CE6-94C0-C51B33D35B49");
    protected override Types.Elements.View.View PreferredCast(object data) => Types.Elements.View.View.FromElement(data as DB.View) as Types.Elements.View.View;

    public View() : base("View", "View", "Represents a Revit view.", "Params", "Revit") { }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      if (Kind > GH_ParamKind.input || DataType == GH_ParamData.remote)
      {
        base.AppendAdditionalMenuItems(menu);
        return;
      }

      Menu_AppendWireDisplay(menu);
      Menu_AppendDisconnectWires(menu);

      Menu_AppendPrincipalParameter(menu);
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

      Menu_AppendManageCollection(menu);
      Menu_AppendSeparator(menu);

      Menu_AppendDestroyPersistent(menu);
      Menu_AppendInternaliseData(menu);

      if (Exposure != GH_Exposure.hidden)
        Menu_AppendExtractParameter(menu);
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

        var current = default(Types.Elements.View.View);
        if (SourceCount == 0 && PersistentDataCount == 1)
        {
          if (PersistentData.get_FirstItem(true) is Types.Elements.View.View firstValue)
            current = firstValue.Duplicate() as Types.Elements.View.View;
        }

        using (var collector = new DB.FilteredElementCollector(doc).OfClass(typeof(DB.View)))
        {
          var views = collector.
                      Cast<DB.View>().
                      Where(x => viewType == DB.ViewType.Undefined || x.ViewType == viewType);

          foreach (var view in views)
          {
            var tag = new Types.Elements.View.View(view);
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
            PersistentData.Append(value.ProxyOwner.Duplicate() as Types.Elements.View.View);
          }
        }

        ExpireSolution(true);
      }
    }
  }
}

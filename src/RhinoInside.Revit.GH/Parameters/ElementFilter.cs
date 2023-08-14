using System;
using Grasshopper.GUI;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class FilterElement : Element<Types.IGH_FilterElement, ARDB.FilterElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("E912B97D-EDCB-40D8-80B4-7A34BABC3125");

    public FilterElement() : base
    (
      name: "Filter",
      nickname: "Filter",
      description: "Contains a collection of Revit filter",
      category: "Params",
      subcategory: "Revit"
    )
    { }

    protected override Types.IGH_FilterElement InstantiateT() => new Types.FilterElement();

    #region UI
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[]
      {
        //"Element Filter",
      }
    );

    public override void Menu_AppendActions(ToolStripDropDown menu)
    {
      base.Menu_AppendActions(menu);
      menu.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.Filters, "Edit Filters…");
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
        Height = (int) (100 * GH_GraphicsUtil.UiScale),
        DisplayMember = nameof(Types.Element.DisplayName)
      };
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

      var filterTypeBox = new ComboBox
      {
        Sorted = true,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Tag = listBox
      };
      filterTypeBox.SelectedIndexChanged += FilterTypeBox_SelectedIndexChanged;
      filterTypeBox.SetCueBanner("Filter type…");
      filterTypeBox.Items.Add("Rule-based Filters");
      filterTypeBox.Items.Add("Selection Filters");

      RefreshFiltersList(listBox, default);

      Menu_AppendCustomItem(menu, filterTypeBox);
      Menu_AppendCustomItem(menu, listBox);
    }

    static readonly string[] FilterTypes = new string[]
    {
      null,
      typeof(ARDB.ParameterFilterElement).FullName,
      typeof(ARDB.SelectionFilterElement).FullName
    };

    private void FilterTypeBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox comboBox)
      {
        if (comboBox.Tag is ListBox listBox)
          RefreshFiltersList(listBox, FilterTypes[comboBox.SelectedIndex + 1]);
      }
    }

    private void RefreshFiltersList(ListBox listBox, string filterType)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.BeginUpdate();
      listBox.Items.Clear();
      listBox.Items.Add(new Types.FilterElement());

      using (var collector = new ARDB.FilteredElementCollector(doc).OfClass(typeof(ARDB.FilterElement)))
      {
        var filters = collector.Cast<ARDB.FilterElement>().
                      Where(x => filterType is null || x.GetType().FullName == filterType);

        foreach (var filter in filters)
          listBox.Items.Add(Types.FilterElement.FromElement(filter));
      }

      listBox.SelectedIndex = listBox.Items.Cast<Types.FilterElement>().IndexOf(PersistentValue, 0).FirstOr(-1);
      listBox.EndUpdate();
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is Types.FilterElement value)
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

  public class ElementFilter : Param<Types.ElementFilter>
  {
    public override Guid ComponentGuid => new Guid("BFCFC49C-747E-40D9-AAEE-93CE06EAAF2B");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override string IconTag => "Y";

    public ElementFilter() : base
    (
      name: "Element Filter",
      nickname: "Element Filter",
      description: "Contains a collection of Revit element filters",
      category: "Params",
      subcategory: "Revit"
    )
    { }
  }

  public class FilterRule : Param<Types.FilterRule>
  {
    public override Guid ComponentGuid => new Guid("F08E1292-F855-48C7-9921-BD12EF0F67D2");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override string IconTag => "R";

    public FilterRule() : base
    (
      name: "Filter Rule",
      nickname: "Filter Rule",
      description: "Contains a collection of Revit filter rules",
      category: "Params",
      subcategory: "Revit"
    )
    { }
  }
}

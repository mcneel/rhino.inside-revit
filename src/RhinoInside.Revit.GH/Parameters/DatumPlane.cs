using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  using External.DB.Extensions;
  using RhinoInside.Revit.GH.Exceptions;

  public class Level : GraphicalElementT<Types.Level, ARDB.Level>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("3238F8BC-8483-4584-B47C-48B4933E478E");

    public Level() : base("Level", "Level", "Contains a collection of Revit level elements", "Params", "Revit") { }

    #region UI
    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var Level = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.Level);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(Level),
        Revit.ActiveUIApplication.CanPostCommand(Level),
        false
      );
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0) return;
      if (Revit.ActiveUIDocument?.Document is null) return;

      if (MutableNickName)
      {
        var listBox = new ListBox
        {
          BorderStyle = BorderStyle.FixedSingle,
          Width = (int) (250 * GH_GraphicsUtil.UiScale),
          Height = (int) (100 * GH_GraphicsUtil.UiScale),
          SelectionMode = SelectionMode.MultiExtended
        };
        listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

        Menu_AppendCustomItem(menu, listBox);
        RefreshLevelList(listBox);
      }

      base.Menu_AppendPromptOne(menu);
   }

    private void RefreshLevelList(ListBox listBox)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.DisplayMember = nameof(Types.Level.DisplayName);
      listBox.Items.Clear();

      using (var collector = new ARDB.FilteredElementCollector(doc).OfClass(typeof(ARDB.Level)))
      {
        var levels = collector.Cast<ARDB.Level>().
          Select(x => new Types.Level(x)).
          OrderBy(x => x.Elevation).
          ThenBy(x => x.DisplayName, default(ElementNameComparer)).
          ToList();

        foreach (var level in levels)
          listBox.Items.Add(level);

        var selectedItems = levels.Intersect(PersistentData.OfType<Types.Level>());

        foreach ( var item in selectedItems)
          listBox.SelectedItems.Add(item);
      }

      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        RecordPersistentDataEvent($"Set: {NickName}");
        PersistentData.Clear();
        PersistentData.AppendRange(listBox.SelectedItems.OfType<Types.Level>());
        OnObjectChanged(GH_ObjectEventType.PersistentData);

        ExpireSolution(true);
      }
    }
    #endregion

    public static bool TryGetDataOrDefault<TOutput>
    (
      IGH_Component component,
      IGH_DataAccess DA,
      string name,
      out TOutput level,
      Types.Document document,
      double elevation
    )
      where TOutput : class
    {
      if (!component.Params.TryGetData(DA, name, out level)) return false;
      if (level is null)
      {
        var data = Types.Level.FromElement(document.Value.GetNearestLevel(elevation / Revit.ModelUnits));
        if (data is null)
          return false;

        level = data as TOutput;
        if (level is null)
          return data.CastTo(out level);
      }

      // Validate document
      switch (level)
      {
        case ARDB.Element element:
          if (!document.Value.IsEquivalent(element.Document))
            throw new Exceptions.RuntimeArgumentException(name, "Failed to assign a type from a diferent document.");
          break;

        case Types.IGH_ElementId id:
          if (!document.Value.IsEquivalent(id.Document))
            throw new Exceptions.RuntimeArgumentException(name, "Failed to assign a type from a diferent document.");
          break;
      }

      return true;
    }

    public static bool GetDataOrDefault<TOutput>
    (
      IGH_Component component,
      IGH_DataAccess DA,
      string name,
      out TOutput level,
      Types.Document document,
      double elevation
    )
      where TOutput : class
    {
      if (!component.Params.TryGetData(DA, name, out level)) return false;
      if (level is null)
      {
        var data = Types.Level.FromElement(document.Value.GetNearestLevel(elevation / Revit.ModelUnits));
        if (data is null)
          throw new RuntimeArgumentException(nameof(elevation), "No suitable level has been found.");

        level = data as TOutput;
        if (level is null)
          return data.CastTo(out level);
      }

      // Validate document
      switch (level)
      {
        case ARDB.Element element:
          if (!document.Value.IsEquivalent(element.Document))
            throw new Exceptions.RuntimeArgumentException(name, "Failed to assign a type from a diferent document.");
          break;

        case Types.IGH_ElementId id:
          if (!document.Value.IsEquivalent(id.Document))
            throw new Exceptions.RuntimeArgumentException(name, "Failed to assign a type from a diferent document.");
          break;
      }

      return true;
    }
  }

  public class Grid : GraphicalElementT<Types.Grid, ARDB.Grid>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("7D2FB886-A184-41B8-A7D6-A6FDB85CF4E4");

    public Grid() : base("Grid", "Grid", "Contains a collection of Revit grid elements", "Params", "Revit") { }

    #region UI
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Curve", "Surface" }
    );

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var Grid = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.Grid);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(Grid),
        Revit.ActiveUIApplication.CanPostCommand(Grid),
        false
      );
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0) return;
      if (Revit.ActiveUIDocument?.Document is null) return;

      if (MutableNickName)
      {
        var listBox = new ListBox
        {
          BorderStyle = BorderStyle.FixedSingle,
          Width = (int) (250 * GH_GraphicsUtil.UiScale),
          Height = (int) (100 * GH_GraphicsUtil.UiScale),
          SelectionMode = SelectionMode.MultiExtended
        };
        listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

        Menu_AppendCustomItem(menu, listBox);
        RefreshGridList(listBox);
      }

      base.Menu_AppendPromptOne(menu);
    }

    private void RefreshGridList(ListBox listBox)
    {
      var doc = Revit.ActiveUIDocument.Document;

      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.DisplayMember = nameof(Types.Grid.DisplayName);
      listBox.Items.Clear();

      using (var collector = new ARDB.FilteredElementCollector(doc).OfClass(typeof(ARDB.Grid)))
      {
        var items = collector.Cast<ARDB.Grid>().
          Select(x => new Types.Grid(x)).
          OrderBy(x => x.DisplayName, default(ElementNameComparer)).
          ToList();

        foreach (var item in items)
          listBox.Items.Add(item);

        var selectedItems = items.Intersect(PersistentData.OfType<Types.Grid>());

        foreach (var item in selectedItems)
          listBox.SelectedItems.Add(item);
      }

      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        RecordPersistentDataEvent($"Set: {NickName}");
        PersistentData.Clear();
        PersistentData.AppendRange(listBox.SelectedItems.OfType<Types.Grid>());
        OnObjectChanged(GH_ObjectEventType.PersistentData);

        ExpireSolution(true);
      }
    }
    #endregion
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Dimension : GraphicalElement<Types.Dimension, ARDB.Dimension>
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("BC546B0C-1BF0-48C6-AAA9-F4FD429DAD39");

    public Dimension() : base
    (
      name: "Dimension",
      nickname: "Dimension",
      description: "Contains a collection of Revit dimension elements",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }

    #region UI
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Curve", }
    );

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var create = Menu_AppendItem(menu, $"Set new {TypeName}");

      create.DropDown.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.AlignedDimension, "Aligned");
      create.DropDown.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.LinearDimension, "Linear");
      create.DropDown.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.AngularDimension, "Angular");
      create.DropDown.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.RadialDimension, "Radial");
      create.DropDown.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.DiameterDimension, "Diameter");
      create.DropDown.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.ArcLengthDimension, "Arc Length");
      create.DropDown.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.SpotElevation, "Spot Elevation");
      create.DropDown.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.SpotCoordinate, "Spot Coordinate");
      create.DropDown.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.SpotSlope, "Spot Slope");
    }
    #endregion
  }

  public class DimensionType : ElementType<Types.DimensionType, ARDB.DimensionType>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("1554AF4F-19C2-49E7-B836-28383AF7F035");

    public DimensionType() : base
    (
      name: "Dimension Type",
      nickname: "DimType",
      description: "Contains a collection of Revit dimension types",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }

    #region UI
    public override void Menu_AppendActions(ToolStripDropDown menu)
    {
      base.Menu_AppendActions(menu);

      var open = Menu_AppendItem(menu, $"Open");
      open.DropDown.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.LinearDimensionTypes, "Linear types…");
      open.DropDown.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.AngularDimensionTypes, "Angular types…");
      open.DropDown.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.RadialDimensionTypes, "Radial types…");
      open.DropDown.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.DiameterDimensionTypes, "Diameter types…");
      open.DropDown.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.SpotElevationTypes, "Spot elevation types…");
      open.DropDown.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.SpotCoordinateTypes, "Spot coordinate types…");
      open.DropDown.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.SpotSlopeTypes, "Spot slope types…");
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) => Rhinoceros.InvokeInHostContext(() =>
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
      listBox.SelectedIndexChanged += ElementTypesBox_SelectedIndexChanged;

      var familiesBox = new ComboBox
      {
        Sorted = true,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Tag = listBox
      };
      familiesBox.SelectedIndexChanged += FamiliesBox_SelectedIndexChanged;
      familiesBox.SetCueBanner("Family filter…");
      RefreshFamiliesBox(familiesBox);

      if (PersistentValue?.Value is ARDB.DimensionType current)
      {
        var familyIndex = 0;
        foreach (var familyName in familiesBox.Items.Cast<string>())
        {
          if (current.FamilyName == familyName)
          {
            familiesBox.SelectedIndex = familyIndex;
            break;
          }
          familyIndex++;
        }
      }

      RefreshElementTypesList(listBox, familiesBox);

      Menu_AppendCustomItem(menu, familiesBox);
      Menu_AppendCustomItem(menu, listBox);
    });

    private void RefreshFamiliesBox(ComboBox familiesBox) => Rhinoceros.InvokeInHostContext(() =>
    {
      try
      {
        familiesBox.SelectedIndex = ListBox.NoMatches;
        familiesBox.BeginUpdate();
        familiesBox.Items.Clear();

        using (var collector = new ARDB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
        {
          var familyNames = collector.WhereElementIsElementType().
          OfClass(typeof(ARDB.DimensionType)).Cast<ARDB.DimensionType>().
          Select(x => x.FamilyName).Distinct(ElementNaming.NameEqualityComparer);

          familiesBox.Items.AddRange(familyNames.ToArray());
        }
      }
      finally { familiesBox.EndUpdate(); }
    });

    private void RefreshElementTypesList(ListBox listBox, ComboBox familiesBox) => Rhinoceros.InvokeInHostContext(() =>
    {
      try
      {
        listBox.SelectedIndexChanged -= ElementTypesBox_SelectedIndexChanged;
        listBox.BeginUpdate();
        listBox.Items.Clear();

        {
          using (var collector = new ARDB.FilteredElementCollector(Revit.ActiveUIDocument.Document))
          {
            var familyName = familiesBox.SelectedItem as string;
            listBox.DisplayMember = string.IsNullOrWhiteSpace(familyName) ? nameof(Types.ElementType.DisplayName) : nameof(Types.ElementType.Nomen);

            var elementTypes = collector.WhereElementIsElementType().OfClass(typeof(ARDB.DimensionType)).
                            WhereParameterEqualsTo(ARDB.BuiltInParameter.ALL_MODEL_FAMILY_NAME, familyName);

            listBox.Items.AddRange
            (
              elementTypes.Cast<ARDB.DimensionType>().
              Where(x => !string.IsNullOrWhiteSpace(x.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_TYPE_NAME).AsString())).
              Where(x => string.IsNullOrEmpty(familyName) || ElementNaming.NameEqualityComparer.Equals(x.FamilyName, familyName)).
              Select(Types.ElementType.FromElement).ToArray()
            );
          }
        }

        listBox.SelectedIndex = listBox.Items.Cast<Types.DimensionType>().IndexOf(PersistentValue, 0).FirstOr(ListBox.NoMatches);
      }
      finally
      {
        listBox.EndUpdate();
        listBox.SelectedIndexChanged += ElementTypesBox_SelectedIndexChanged;
      }
    });

    private void FamiliesBox_SelectedIndexChanged(object sender, EventArgs e) 
    {
      if (sender is ComboBox familiesBox)
      {
        if (familiesBox.Tag is ListBox listBox)
          RefreshElementTypesList(listBox, familiesBox);
      }
    }

    private void ElementTypesBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is Types.DimensionType value)
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

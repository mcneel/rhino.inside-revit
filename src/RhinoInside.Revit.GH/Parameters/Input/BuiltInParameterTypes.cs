using System;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using DBXS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class BuiltInParameterTypes : Grasshopper.Special.ValueSet<Types.ParameterType>
  {
    public override Guid ComponentGuid => new Guid("8AB856C6-DE20-44C8-9CF0-9460DABCF7EE");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override System.Drawing.Bitmap Icon =>
      ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
      base.Icon;

    public DBXS.DisciplineType DisciplineType = DBXS.DisciplineType.Common;

    public BuiltInParameterTypes() : base
    (
      name: "Built-In Parameter Types",
      nickname: "Parameter Types",
      description: "Provides a picker for parameters types",
      category: "Revit",
      subcategory: "Parameter"
    )
    {
      IconDisplayMode = GH_IconDisplayMode.name;
    }

    protected override void LoadVolatileData()
    {
      if (SourceCount == 0)
      {
        MutableNickName = false;

        m_data.Clear();
        if (DisciplineType == DBXS.DisciplineType.Empty)
        {
          NickName = Name;
          m_data.AppendRange(Types.ParameterType.EnumValues.OrderBy(x => x.Value.Label));
        }
        else
        {
          NickName = $"{Name} ({DisciplineType.Label})";
          m_data.AppendRange
          (
            Types.ParameterType.EnumValues.Where
            (
              x => DBXS.SpecType.IsSpecType(x.Value, out var spec) && spec.DisciplineType == DisciplineType
            ).
            OrderBy(x => x.Value.Label)
          );
        }
      }
      else
      {
        MutableNickName = true;
      }

      base.LoadVolatileData();
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      base.Menu_AppendPromptOne(menu);

      if (SourceCount == 0 && Kind == GH_ParamKind.floating)
      {
        var disciplines = new DBXS.DisciplineType[]
        {
          DBXS.DisciplineType.Empty,
          DBXS.DisciplineType.Common,
          DBXS.DisciplineType.Electrical,
          DBXS.DisciplineType.Energy,
          DBXS.DisciplineType.Hvac,
          DBXS.DisciplineType.Infrastructure,
          DBXS.DisciplineType.Piping,
          DBXS.DisciplineType.Structural
        };

        foreach (var discipline in disciplines)
        {
          var text = discipline == DBXS.DisciplineType.Empty ? "All Disciplines" : discipline.Label;
          var item = Menu_AppendItem(menu, text, Menu_DisciplineTypeClicked, true, discipline.Equals(DisciplineType));
          item.Tag = discipline;
        }
      }
    }

    private void Menu_DisciplineTypeClicked(object sender, EventArgs e)
    {
      if (sender is ToolStripMenuItem item)
      {
        if (item.Tag is DBXS.DisciplineType value)
        {
          RecordUndoEvent("Set Discipline Type");
          DisciplineType = value;
          OnObjectChanged(GH_ObjectEventType.Custom);

          ExpireSolution(true);
        }
      }
    }

    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      string disciplineType = string.Empty;
      DisciplineType = reader.TryGetString("DisciplineType", ref disciplineType) ?
      new DBXS.DisciplineType(disciplineType) :
      DBXS.DisciplineType.Empty;

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (DisciplineType != DBXS.DisciplineType.Empty)
        writer.SetString("DisciplineType", DisciplineType.FullName);

      return true;
    }
  }
}

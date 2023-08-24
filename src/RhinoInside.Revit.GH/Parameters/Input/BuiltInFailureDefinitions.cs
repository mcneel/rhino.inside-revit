using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  [ComponentVersion(introduced: "1.10", updated: "1.17")]
  public class BuiltInFailureDefinitions : Grasshopper.Special.ValueSet<Types.FailureDefinition>
  {
    public override Guid ComponentGuid => new Guid("73E14FBB-24EA-44FE-85ED-5D028154029B");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;

    protected override System.Drawing.Bitmap Icon =>
      ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
      base.Icon;

    public ARDB.FailureSeverity FailureSeverity = ARDB.FailureSeverity.Warning;

    public BuiltInFailureDefinitions() : base
    (
      name: "Built-In Failure Definitions",
      nickname: "Failure Definitions",
      description: "Provides a picker for built-in failure definitions",
      category: "Revit",
      subcategory: "Document"
    )
    {
      IconDisplayMode = GH_IconDisplayMode.name;
    }

    protected override void LoadVolatileData()
    {
      if (SourceCount == 0)
      {
        MutableNickName = false;
        if (FailureSeverity == ARDB.FailureSeverity.None)
        {
          NickName = Name;
        }
        else
        {
          NickName = $"{Name} ({Types.FailureSeverity.NamedValues[(int) FailureSeverity]})";
        }

        m_data.Clear();
        using (var registry = Autodesk.Revit.ApplicationServices.Application.GetFailureDefinitionRegistry())
        {
          var definitions = registry.ListAllFailureDefinitions() as IEnumerable<ARDB.FailureDefinitionAccessor>;
          if (FailureSeverity != ARDB.FailureSeverity.None)
            definitions = definitions.Where(x => x.GetSeverity() == FailureSeverity);

          m_data.AppendRange(definitions.Select(x => new Types.FailureDefinition(x.GetId().Guid)));
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
        var severities = new ARDB.FailureSeverity[]
        {
          ARDB.FailureSeverity.Warning,
          ARDB.FailureSeverity.Error,
          ARDB.FailureSeverity.DocumentCorruption
        };

        foreach (var severity in severities)
        {
          var text = severity == ARDB.FailureSeverity.None ? "All" : Types.FailureSeverity.NamedValues[(int) severity];
          var item = Menu_AppendItem(menu, text, Menu_SeverityClicked, true, severity.Equals(FailureSeverity));
          item.Tag = severity;
        }
      }
    }

    private void Menu_SeverityClicked(object sender, EventArgs e)
    {
      if (sender is ToolStripMenuItem item)
      {
        if (item.Tag is ARDB.FailureSeverity value)
        {
          RecordUndoEvent("Set Failure Severity");
          FailureSeverity = value;
          OnObjectChanged(GH_ObjectEventType.Custom);

          ExpireSolution(true);
        }
      }
    }

    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      int failureSeverity = (int) ARDB.FailureSeverity.None;
      reader.TryGetInt32(nameof(FailureSeverity), ref failureSeverity);
      FailureSeverity = (ARDB.FailureSeverity) failureSeverity;

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (FailureSeverity != ARDB.FailureSeverity.None)
        writer.SetInt32(nameof(FailureSeverity), (int) FailureSeverity);

      return true;
    }
  }
}

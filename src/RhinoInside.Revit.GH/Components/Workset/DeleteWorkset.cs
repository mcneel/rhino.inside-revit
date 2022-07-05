using System;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Worksets
{
  [ComponentVersion(introduced: "1.9")]
  public class DeleteWorkset : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("BF1B9BE9-2D5F-45DC-947B-A8ACFA6D6E2D");
#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
#else
    public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.hidden;
    public override bool SDKCompliancy(int exeVersion, int exeServiceRelease) => false;
#endif
    protected override string IconTag => String.Empty;

    public DeleteWorkset() : base
    (
      name: "Delete Workset",
      nickname: "Delete",
      description: "Deletes worksets from Revit document",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Workset()
        {
          Name = "Workset",
          NickName = "W",
          Description = "Workset to delete",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Workset()
        {
          Name = "Target Workset",
          NickName = "TW",
          Description = "Workset to move elements on before delete",
          Optional = true
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Succeeded",
          NickName = "S",
          Description = "Workset delete succeeded",
        }
      ),
    };

    bool Simulated;
    int deletedWorksets;

    protected override void BeforeSolveInstance()
    {
#if !REVIT_2023
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"'{Name}' component is only supported on Revit 2023 or above.");
#endif

      Message = string.Empty;
      base.BeforeSolveInstance();

      deletedWorksets = 0;
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Workset", out Types.Workset workset, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Target Workset", out Types.Workset target, x => x.IsValid)) return;

#if REVIT_2023
      using (var settings = new ARDB.DeleteWorksetSettings())
      {
        if (target is object)
        {
          settings.DeleteWorksetOption = ARDB.DeleteWorksetOption.MoveElementsToWorkset;
          settings.WorksetId = target.Id;
        }
        else
        {
          settings.DeleteWorksetOption = ARDB.DeleteWorksetOption.DeleteAllElements;
        }

        var succeeded = false;
        if (Simulated) succeeded = ARDB.WorksetTable.CanDeleteWorkset(workset.Document, workset.Id, settings);
        else
        {
          StartTransaction(workset.Document);
          ARDB.WorksetTable.DeleteWorkset(workset.Document, workset.Id, settings);
          succeeded = true;
        }

        if (succeeded) deletedWorksets++;
        DA.SetData("Succeeded", succeeded);
      }
#endif
    }

    protected override void AfterSolveInstance()
    {
      if (RunCount > 0)
      {
        if (Simulated && Message == string.Empty)
          Message = "Simulated";

        if (Simulated)
        {
          Status = ARDB.TransactionStatus.RolledBack;
        }
        else
        {
          if (deletedWorksets == 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No worksets were deleted");
          else
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{deletedWorksets} worksets were deleted.");
        }
      }

      base.AfterSolveInstance();
    }

#region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      Menu_AppendItem(menu, "Simulated", Menu_SimulatedClicked, true, Simulated);
    }

    private void Menu_SimulatedClicked(object sender, EventArgs e)
    {
      if (sender is ToolStripMenuItem item)
      {
        RecordUndoEvent($"Set: Simulated");
        Simulated = !Simulated;

        Message = Simulated ? "Simulated" : string.Empty;

        ClearData();
        ExpireDownStreamObjects();
        ExpireSolution(true);
      }
    }
#endregion

#region IO
    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      Simulated = default;
      reader.TryGetBoolean("Simulated", ref Simulated);

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (Simulated != default)
        writer.SetBoolean("Simulated", Simulated);

      return true;
    }
#endregion
  }
}

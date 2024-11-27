using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Worksets
{
  [ComponentVersion(introduced: "1.10")]
  public class CreateWorkset : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("A406C6A0-5EEE-4A26-80A0-A9A3BF8FD7E0");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    public CreateWorkset() : base
    (
      name: "Ensure Workset",
      nickname: "Workset",
      description: "Ensures a user-created workset exist at Document",
      category: "Revit",
      subCategory: "Document"
    )
    {
      FailureProcessingMode = ARDB.FailureProcessingResult.ProceedWithCommit;
    }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document() { Optional = true }, ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Workset name",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Workset()
        {
          Name = "Workset",
          NickName = "W",
          Description = "Workset",
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;
      if (!Params.GetData(DA, "Name", out string name)) return;

      var workset = default(ARDB.Workset);

      if (doc.IsWorkshared is false)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Document '{doc.Title}' is not a workshared document.");

        workset = doc.Value.GetWorksetTable().GetWorkset(new ARDB.WorksetId(0));

        if (!ElementNaming.NameEqualityComparer.Equals(workset.Name, name))
        {
          switch (FailureProcessingMode)
          {
            case ARDB.FailureProcessingResult.Continue:
              workset = null;
              break;

            case ARDB.FailureProcessingResult.ProceedWithCommit:
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to found workset '{name}'. Using default workset");
              break;

            case ARDB.FailureProcessingResult.ProceedWithRollBack:
              throw new Exceptions.RuntimeErrorException($"Failed to found workset '{name}'.");

            case ARDB.FailureProcessingResult.WaitForUserInput:
              // TODO:
              //var message = new ARDB.FailureMessage(ERDB.ExternalFailures.WorksetFailures.FailedToFoundWorkset);
              //doc.Value.PostFailure(message);
              workset = null;
              AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to found workset '{name}'.");
              break;
          }
        }
      }
      else
      {
        using (var collector = new ARDB.FilteredWorksetCollector(doc.Value))
          workset = collector.Cast<ARDB.Workset>().FirstOrDefault(x => ElementNaming.NameEqualityComparer.Equals(x.Name, name));

        var worksetKind = workset?.Kind;
        if (worksetKind != ARDB.WorksetKind.UserWorkset)
        {
          workset = null;
          var message = worksetKind is null ?
            $"Failed to found workset '{name}'." :
            $"Workset '{name}' already exist but is not a User-Created Workset.";

          switch (FailureProcessingMode)
          {
            case ARDB.FailureProcessingResult.ProceedWithCommit:
              if (worksetKind is null)
              {
                if (ElementNaming.IsValidName(name))
                {
                  StartTransaction(doc.Value);
                  workset = ARDB.Workset.Create(doc.Value, name);
                }
                else throw new Exceptions.RuntimeArgumentException("Name", "Input parameter 'Name' collected invalid data");
              }
              else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);
              break;

            case ARDB.FailureProcessingResult.ProceedWithRollBack:
              throw new Exceptions.RuntimeErrorException(message);

            case ARDB.FailureProcessingResult.WaitForUserInput:
              // TODO:
              //var message = new ARDB.FailureMessage(ERDB.ExternalFailures.WorksetFailures.FailedToFoundWorkset);
              //doc.Value.PostFailure(message);
              AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message);
              workset = null;
              break;
          }
        }
      }

      if (workset is object)
        DA.SetData("Workset", (doc.Value, workset));
    }
  }
}

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;
using ERDB = RhinoInside.Revit.External.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components
{
  using External.DB.Extensions;

  internal enum TransactionExtent
  {
    Default,
    Component,
    Instance,
    Scope,
  }

  /// <summary>
  /// Base type for any component that has to deal with Revit <see cref="ARDB.Transaction"/> instances on many documents.
  /// </summary>
  public abstract class TransactionalChainComponent : TransactionalComponent, ERDB.ITransactionNotification
  {
    protected TransactionalChainComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory)
    { }

    internal virtual TransactionExtent TransactionExtent => TransactionExtent.Component;

    public override bool RequiresFailed
    (
      IGH_DataAccess access, int index, object value,
      string message
    )
    {
      if (base.RequiresFailed(access, index, value, message)) return true;

      if (FailureProcessingMode >= ARDB.FailureProcessingResult.ProceedWithRollBack)
        access.AbortComponentSolution();

      return false;
    }

    ERDB.TransactionChain chain;

    public ARDB.TransactionStatus StartTransaction(ARDB.Document document)
    {
      if (document.IsLinked)
        throw new InvalidOperationException($"Document '{document.GetName()}' is a linked file.{OS.NewLine}Only primary documents (projects or families) are editable.");

      return chain.Start(document);
    }

    protected ARDB.TransactionStatus CommitTransaction()
    {
      if (TransactionExtent != TransactionExtent.Scope)
        throw new InvalidOperationException();

      try
      {
        if (chain.HasStarted())
         TransactionStatus = chain.Commit();
      }
      catch (Exception e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
      }

      return TransactionStatus;
    }

    protected void RollBackTransaction()
    {
      if (TransactionExtent != TransactionExtent.Scope)
        throw new InvalidOperationException();

      try
      {
        if (chain.HasStarted())
          TransactionStatus = chain.RollBack();
      }
      catch (Exception e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
      }
    }

    // Setp 1.
    protected override void BeforeSolveInstance()
    {
      base.BeforeSolveInstance();

      chain = new ERDB.TransactionChain
      (
        new ERDB.TransactionHandlingOptions
        {
          FailuresPreprocessor = CreateFailuresPreprocessor(),
          TransactionNotification = this,
          KeepFailuresAfterRollback = FailureProcessingMode == ARDB.FailureProcessingResult.WaitForUserInput
        },
        Name
      );
    }

    // Step 2.
    protected sealed override void SolveInstance(IGH_DataAccess DA)
    {
      base.SolveInstance(DA);

      if (TransactionExtent != TransactionExtent.Component)
      {
        try
        {
          TransactionStatus = ARDB.TransactionStatus.Pending;
          TransactionStatus = IsAborted ? chain.RollBack() : chain.Commit();
        }
        finally
        {
          switch (TransactionStatus)
          {
            case ARDB.TransactionStatus.Uninitialized:
              break;

            case ARDB.TransactionStatus.Committed:
              break;

            default:
              break;
          }
        }
      }
    }

    protected override bool TryCatchException(IGH_DataAccess DA, Exception e)
    {
      if (base.TryCatchException(DA, e))
        return true;

      if (TransactionExtent != TransactionExtent.Component)
        TransactionStatus = chain.RollBack();

      return false;
    }

    // Step 3.
    protected /*sealed*/ override void AfterSolveInstance()
    {
      using (chain)
      {
        if (chain.HasStarted())
        {
          try
          {
            TransactionStatus = ARDB.TransactionStatus.Pending;
            TransactionStatus = IsAborted ? chain.RollBack() : chain.Commit();
          }
          catch (Exception e)
          {
            TransactionStatus = ARDB.TransactionStatus.Error;
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
            ResetData();
          }
          finally
          {
            switch (TransactionStatus)
            {
              case ARDB.TransactionStatus.Uninitialized:
                break;

              case ARDB.TransactionStatus.Committed:
                ClearInvalidOutputElements();
                break;

              default:
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Transaction {TransactionStatus} and aborted.");
                ResetData();
                break;
            }
          }
        }

        chain = default;
      }

      base.AfterSolveInstance();
    }

    protected void UpdateDocument(ARDB.Document document, Action solve)
    {
      StartTransaction(document);

      if (FailureProcessingMode <= ARDB.FailureProcessingResult.ProceedWithCommit)
      {
        using (var sub = new ARDB.SubTransaction(document))
        {
          sub.Start();
          solve();
          sub.Commit();
        }
      }
      else solve();
    }

    protected void UpdateElement(ARDB.Element element, Action solve) => UpdateDocument(element.Document, solve);

    /// <summary>
    /// Set to null those output elements that are invalid.
    /// </summary>
    void ClearInvalidOutputElements()
    {
      foreach (var output in Params.Output)
      {
        if (!typeof(Types.IGH_Element).IsAssignableFrom(output.Type))
          continue;

        var data = output.VolatileData;
        var pathCount = data.PathCount;
        for (int p = 0; p < pathCount; ++p)
        {
          var branch = data.get_Branch(p);

          var count = branch.Count;
          for (int e = 0; e < count; ++e)
          {
            if (branch[e] is Types.IGH_Element id && id.Value is ARDB.Element element && !element.IsValidObject)
              branch[e] = null;
          }
        }
      }
    }

    #region ERDB.ITransactionNotification
    External.UI.EditScope editScope = null;
    EventHandler<ARUI.Events.DialogBoxShowingEventArgs> dialogBoxShowing = null;

    // Step 2.1
    bool ERDB.ITransactionNotification.OnStart(ARDB.Document document)
    {
      TransactionStatus = ARDB.TransactionStatus.Uninitialized;
      return OnStart(document);
    }

    protected virtual bool OnStart(ARDB.Document document) => true;

    // Step 2.2
    void ERDB.ITransactionNotification.OnStarted(ARDB.Document document)
    {
      TransactionStatus = ARDB.TransactionStatus.Started;
      OnStarted(document);
    }
    protected virtual void OnStarted(ARDB.Document document) { }

    // Step 3.1
    void ERDB.ITransactionNotification.OnPrepare(IReadOnlyCollection<ARDB.Document> documents)
    {
      // Disable Rhino UI in case any warning-message dialog popups
      var activeApplication = Revit.ActiveUIApplication;
      activeApplication.DialogBoxShowing += dialogBoxShowing = (sender, args) =>
      {
        if (editScope is null)
          editScope = new External.UI.EditScope(activeApplication);
      };

      OnPrepare(documents);
    }
    protected virtual void OnPrepare(IReadOnlyCollection<ARDB.Document> documents) { }

    // Step 3.2
    void ERDB.ITransactionNotification.OnDone(ARDB.TransactionStatus status)
    {
      TransactionStatus = status;

      try
      {
        OnDone(status);
      }
      finally
      {
        // Restore Rhino UI in case any warning-message dialog popups
        if (dialogBoxShowing is object)
        {
          Revit.ActiveUIApplication.DialogBoxShowing -= dialogBoxShowing;
          dialogBoxShowing = null;
          using (editScope) editScope = default;
        }
      }
    }
    protected virtual void OnDone(ARDB.TransactionStatus status) { }
    #endregion

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      {
        Menu_AppendSeparator(menu);
        var failures = Menu_AppendItem(menu, "Error Mode");

        var skip = Menu_AppendItem(failures.DropDown, "⏭ Skip", (s, a) => { FailureProcessingMode = ARDB.FailureProcessingResult.Continue; ExpireSolution(true); }, true, FailureProcessingMode == ARDB.FailureProcessingResult.Continue);
        skip.ToolTipText = $"Any failing element will be skipped.{OS.NewLine}A null will be returned in its place.";

        var @continue = Menu_AppendItem(failures.DropDown, "⏯ Continue", (s, a) => { FailureProcessingMode = ARDB.FailureProcessingResult.ProceedWithCommit; ExpireSolution(true); }, true, FailureProcessingMode == ARDB.FailureProcessingResult.ProceedWithCommit);
        @continue.ToolTipText = $"If suitable, a default resolution will be applied.{OS.NewLine}Otherwise, a null will be returned.";

        var cancel = Menu_AppendItem(failures.DropDown, "⏪ Cancel", (s, a) => { FailureProcessingMode = ARDB.FailureProcessingResult.ProceedWithRollBack; ExpireSolution(true); }, true, FailureProcessingMode == ARDB.FailureProcessingResult.ProceedWithRollBack);
        cancel.ToolTipText = $"A failing element will cancel the whole '{Name}' operation.{OS.NewLine}Nothing will be returned in this case.";

        var custom = Menu_AppendItem(failures.DropDown, "⏸ Pause…", (s, a) => { FailureProcessingMode = ARDB.FailureProcessingResult.WaitForUserInput; ExpireSolution(true); }, true, FailureProcessingMode == ARDB.FailureProcessingResult.WaitForUserInput);
        custom.ToolTipText = $"Do a pause and let me decide.{OS.NewLine}Shows Revit failures report dialog.";
      }
    }
    #endregion
  }
}

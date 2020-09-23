using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Autodesk.Revit.UI.Events;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class TransactionalComponent :
    ZuiComponent,
    DB.IFailuresPreprocessor,
    DB.ITransactionFinalizer
  {
    protected TransactionalComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    #region Transaction
    DB.TransactionStatus status = DB.TransactionStatus.Uninitialized;
    public DB.TransactionStatus Status
    {
      get => status;
      protected set => status = value;
    }

    internal new class Attributes : ZuiComponent.Attributes
    {
      public Attributes(TransactionalComponent owner) : base(owner) { }

      protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
      {
        if (channel == GH_CanvasChannel.Objects && !Owner.Locked && Owner is TransactionalComponent component)
        {
          if (component.Status != DB.TransactionStatus.RolledBack && component.Status != DB.TransactionStatus.Uninitialized)
          {
            var palette = GH_CapsuleRenderEngine.GetImpliedPalette(Owner);
            if (palette == GH_Palette.Normal && !Owner.IsPreviewCapable)
              palette = GH_Palette.Hidden;

            // Errors and warnings should be refelected in Canvas
            if (palette == GH_Palette.Normal || palette == GH_Palette.Hidden)
            {
              var style = GH_CapsuleRenderEngine.GetImpliedStyle(palette, Selected, Owner.Locked, Owner.Hidden);
              var fill = style.Fill;
              var edge = style.Edge;
              var text = style.Text;

              switch (component.Status)
              {
                case DB.TransactionStatus.Uninitialized: palette = GH_Palette.Grey; break;
                case DB.TransactionStatus.Started: palette = GH_Palette.White; break;
                case DB.TransactionStatus.RolledBack:     /*palette = GH_Palette.Normal;*/ break;
                case DB.TransactionStatus.Committed: palette = GH_Palette.Black; break;
                case DB.TransactionStatus.Pending: palette = GH_Palette.Blue; break;
                case DB.TransactionStatus.Error: palette = GH_Palette.Pink; break;
                case DB.TransactionStatus.Proceed: palette = GH_Palette.Brown; break;
              }
              var replacement = GH_CapsuleRenderEngine.GetImpliedStyle(palette, Selected, Owner.Locked, Owner.Hidden);

              try
              {
                style.Edge = replacement.Edge;
                style.Fill = replacement.Fill;
                style.Text = replacement.Text;

                base.Render(canvas, graphics, channel);
              }
              finally
              {
                style.Fill = fill;
                style.Edge = edge;
                style.Text = text;
              }

              return;
            }
          }
        }

        base.Render(canvas, graphics, channel);
      }
    }

    public override void CreateAttributes() => m_attributes = new Attributes(this);

    protected DB.Transaction NewTransaction(DB.Document doc) => NewTransaction(doc, Name);
    protected DB.Transaction NewTransaction(DB.Document doc, string name)
    {
      var transaction = new DB.Transaction(doc, name);

      var options = transaction.GetFailureHandlingOptions();
      options = options.SetClearAfterRollback(true);
      options = options.SetDelayedMiniWarnings(false);
      options = options.SetForcedModalHandling(true);

      options = options.SetFailuresPreprocessor(this);
      options = options.SetTransactionFinalizer(this);

      transaction.SetFailureHandlingOptions(options);

      return transaction;
    }

    protected DB.TransactionStatus CommitTransaction(DB.Document doc, DB.Transaction transaction)
    {
      // Disable Rhino UI if any warning-error dialog popup
      External.EditScope editScope = null;
      EventHandler<DialogBoxShowingEventArgs> _ = null;
      try
      {
        Revit.ApplicationUI.DialogBoxShowing += _ = (sender, args) =>
        {
          if (editScope is null)
            editScope = new External.EditScope();
        };

        if (transaction.GetStatus() == DB.TransactionStatus.Started)
        {
          OnBeforeCommit(doc, transaction);

          return transaction.Commit();
        }
        else return transaction.RollBack();
      }
      finally
      {
        Revit.ApplicationUI.DialogBoxShowing -= _;

        if (editScope is IDisposable disposable)
          disposable.Dispose();
      }
    }
    #endregion

    // Setp 1.
    protected override void BeforeSolveInstance() => status = DB.TransactionStatus.Uninitialized;

    // Step 2.
    protected virtual void OnAfterStart(DB.Document document, DB.Transaction transaction) { }

    // Step 3.
    //protected override void TrySolveInstance(IGH_DataAccess DA) { }

    // Step 4.
    protected virtual void OnBeforeCommit(DB.Document document, DB.Transaction transaction) { }

    // Step 5.
    //protected override void AfterSolveInstance() {}

    // Step 5.1
    #region IFailuresPreprocessor
    void AddRuntimeMessage(DB.FailureMessageAccessor error, bool? solved = null)
    {
      if (error.GetFailureDefinitionId() == DBX.ExternalFailures.TransactionFailures.SimulatedTransaction)
      {
        // Simulation signal is already reflected in the canvas changing the component color,
        // So it's up to the component show relevant information about what 'simulation' means.
        // As an example Purge component shows a remarks that reads like 'No elements were deleted'.
        //AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, error.GetDescriptionText());

        return;
      }

      var level = GH_RuntimeMessageLevel.Remark;
      switch (error.GetSeverity())
      {
        case DB.FailureSeverity.Warning: level = GH_RuntimeMessageLevel.Warning; break;
        case DB.FailureSeverity.Error: level = GH_RuntimeMessageLevel.Error; break;
      }

      string solvedMark = string.Empty;
      if (error.GetSeverity() > DB.FailureSeverity.Warning)
      {
        switch (solved)
        {
          case false: solvedMark = "❌ "; break;
          case true: solvedMark = "✔ "; break;
        }
      }

      var description = error.GetDescriptionText();
      var text = string.IsNullOrEmpty(description) ?
        $"{solvedMark}{level} {{{error.GetFailureDefinitionId().Guid}}}" :
        $"{solvedMark}{description}";

      int idsCount = 0;
      foreach (var id in error.GetFailingElementIds())
        text += idsCount++ == 0 ? $" {{{id.IntegerValue}" : $", {id.IntegerValue}";
      if (idsCount > 0) text += "} ";

      AddRuntimeMessage(level, text);
    }

    // Override to add handled failures to your component (Order is important).
    protected virtual IEnumerable<DB.FailureDefinitionId> FailureDefinitionIdsToFix => null;

    DB.FailureProcessingResult FixFailures(DB.FailuresAccessor failuresAccessor, IEnumerable<DB.FailureDefinitionId> failureIds)
    {
      foreach (var failureId in failureIds)
      {
        int solvedErrors = 0;

        foreach (var error in failuresAccessor.GetFailureMessages().Where(x => x.GetFailureDefinitionId() == failureId))
        {
          if (!failuresAccessor.IsFailureResolutionPermitted(error))
            continue;

          // Don't try to fix two times same issue
          if (failuresAccessor.GetAttemptedResolutionTypes(error).Any())
            continue;

          AddRuntimeMessage(error, true);

          failuresAccessor.ResolveFailure(error);
          solvedErrors++;
        }

        if (solvedErrors > 0)
          return DB.FailureProcessingResult.ProceedWithCommit;
      }

      return DB.FailureProcessingResult.Continue;
    }

    public virtual DB.FailureProcessingResult PreprocessFailures(DB.FailuresAccessor failuresAccessor)
    {
      if (!failuresAccessor.IsTransactionBeingCommitted())
        return DB.FailureProcessingResult.Continue;

      if (failuresAccessor.GetSeverity() >= DB.FailureSeverity.DocumentCorruption)
        return DB.FailureProcessingResult.ProceedWithRollBack;

      if (failuresAccessor.GetSeverity() >= DB.FailureSeverity.Error)
      {
        // Handled failures in order
        {
          var failureDefinitionIdsToFix = FailureDefinitionIdsToFix;
          if (failureDefinitionIdsToFix != null)
          {
            var result = FixFailures(failuresAccessor, failureDefinitionIdsToFix);
            if (result != DB.FailureProcessingResult.Continue)
              return result;
          }
        }

        // Unhandled failures in incomming order
        {
          var failureDefinitionIdsToFix = failuresAccessor.GetFailureMessages().GroupBy(x => x.GetFailureDefinitionId()).Select(x => x.Key);
          var result = FixFailures(failuresAccessor, failureDefinitionIdsToFix);
          if (result != DB.FailureProcessingResult.Continue)
            return result;
        }
      }

      if (failuresAccessor.GetSeverity() >= DB.FailureSeverity.Warning)
      {
        // Unsolved failures or warnings
        foreach (var error in failuresAccessor.GetFailureMessages().OrderBy(error => error.GetSeverity()))
          AddRuntimeMessage(error, false);

        failuresAccessor.DeleteAllWarnings();
      }

      if (failuresAccessor.GetSeverity() >= DB.FailureSeverity.Error)
        return DB.FailureProcessingResult.ProceedWithRollBack;

      return DB.FailureProcessingResult.Continue;
    }
    #endregion

    // Step 5.2
    #region ITransactionFinalizer
    // Step 5.2.A
    public virtual void OnCommitted(DB.Document document, string strTransactionName)
    {
      if (Status < DB.TransactionStatus.Pending)
        Status = DB.TransactionStatus.Committed;
    }

    // Step 5.2.B
    public virtual void OnRolledBack(DB.Document document, string strTransactionName)
    {
      if (Status < DB.TransactionStatus.Pending)
        Status = DB.TransactionStatus.RolledBack;
    }
    #endregion
  }

  public enum TransactionExtent
  {
    Default,
    Instance,
    Component,
  }

  public abstract class TransactionalChainComponent : TransactionalComponent
  {
    protected TransactionalChainComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected override bool AbortOnUnhandledException => TransactionExtent == TransactionExtent.Component;

    TransactionExtent transactionExtent = TransactionExtent.Default;
    protected TransactionExtent TransactionExtent
    {
      get => transactionExtent == TransactionExtent.Default ? TransactionExtent.Component : transactionExtent;
      set
      {
        if (Phase == GH_SolutionPhase.Computing)
          throw new InvalidOperationException();

        transactionExtent = value;
      }
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (TransactionExtent == TransactionExtent.Component)
      {
        base.SolveInstance(DA);
      }
      else
      {
        try
        {
          Status = DB.TransactionStatus.Uninitialized;

          try
          {
            base.SolveInstance(DA);

            if (CurrentTransactions?.Count > 0)
              Status = CommitTransactions();
          }
          finally
          {
            switch (Status)
            {
              case DB.TransactionStatus.Uninitialized:
              case DB.TransactionStatus.Started:
              case DB.TransactionStatus.Committed:
                break;
              default:
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Transaction {Status} and aborted.");
                ResetData();
                break;
            }
          }
        }
        finally
        {
          if (CurrentTransactions is object)
          {
            foreach (var transaction in CurrentTransactions)
              transaction.Value.Dispose();

            CurrentTransactions = null;
          }
        }
      }
    }

    Dictionary<DB.Document, DB.Transaction> CurrentTransactions;

    protected void StartTransaction(DB.Document document)
    {
      if (CurrentTransactions?.ContainsKey(document) != true)
      {
        var transaction = base.NewTransaction(document, Name);
        if (transaction.Start() != DB.TransactionStatus.Started)
        {
          transaction.Dispose();
          throw new InvalidOperationException($"Unable to start Transaction '{Name}'");
        }

        try
        {
          OnAfterStart(document, transaction);

          if (CurrentTransactions is null)
            CurrentTransactions = new Dictionary<DB.Document, DB.Transaction>();

          CurrentTransactions.Add(document, transaction);
        }
        catch (Exception e)
        {
          transaction.Dispose();
          throw e;
        }
      }
    }

    // Step 5.
    protected virtual void OnBeforeCommit(IReadOnlyDictionary<DB.Document, DB.Transaction> transactions) { }

    protected override void AfterSolveInstance()
    {
      OnBeforeCommit(CurrentTransactions);

      if (CurrentTransactions is object)
      {
        try
        {

          if (!IsAborted)
          {
            var transactionStatus = DB.TransactionStatus.Uninitialized;

            try
            {
              if (CurrentTransactions.Count > 0)
                transactionStatus = CommitTransactions();
            }
            finally
            {
              switch (transactionStatus)
              {
                case DB.TransactionStatus.Uninitialized:
                case DB.TransactionStatus.Started:
                case DB.TransactionStatus.Committed:
                  break;
                default:
                  AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Transaction {transactionStatus} and aborted.");
                  ResetData();
                  break;
              }
            }
          }
        }
        finally
        {
          foreach (var transaction in CurrentTransactions)
            transaction.Value.Dispose();

          CurrentTransactions = null;
        }
      }

      base.AfterSolveInstance();
    }

    IEnumerator<KeyValuePair<DB.Document, DB.Transaction>> currentTransactionsEnumerator;

    protected DB.TransactionStatus CommitTransactions()
    {
      // Disable Rhino UI if any warning-error dialog popup
      External.EditScope editScope = null;
      EventHandler<DialogBoxShowingEventArgs> _ = null;
      try
      {
        Revit.ApplicationUI.DialogBoxShowing += _ = (sender, args) =>
        {
          if (editScope is null)
            editScope = new External.EditScope();
        };

        using (currentTransactionsEnumerator = CurrentTransactions.GetEnumerator())
        {
          return CommitNextTransaction();
        }
      }
      finally
      {
        currentTransactionsEnumerator = default;

        Revit.ApplicationUI.DialogBoxShowing -= _;

        if (editScope is IDisposable disposable)
          disposable.Dispose();
      }
    }

    DB.TransactionStatus CommitNextTransaction()
    {
      if (currentTransactionsEnumerator is null)
        return DB.TransactionStatus.Uninitialized;

      if (currentTransactionsEnumerator.MoveNext())
      {
        var doc = currentTransactionsEnumerator.Current.Key;
        var transaction = currentTransactionsEnumerator.Current.Value;

        if (transaction.GetStatus() == DB.TransactionStatus.Started)
        {
          OnBeforeCommit(doc, transaction);

          return transaction.Commit();
        }
        else return transaction.RollBack();
      }

      return DB.TransactionStatus.Committed;
    }

    public override DB.FailureProcessingResult PreprocessFailures(DB.FailuresAccessor failuresAccessor)
    {
      var result = base.PreprocessFailures(failuresAccessor);
      if (result > DB.FailureProcessingResult.ProceedWithCommit)
        return result;

      return failuresAccessor.IsTransactionBeingCommitted() && CommitNextTransaction() == DB.TransactionStatus.Committed ?
        DB.FailureProcessingResult.Continue :
        DB.FailureProcessingResult.ProceedWithRollBack;
    }
  }
}

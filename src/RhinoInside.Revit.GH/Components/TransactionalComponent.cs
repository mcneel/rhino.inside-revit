using System;
using Autodesk.Revit.UI.Events;
using GH_IO.Serialization;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  /// <summary>
  /// Base type for any component that has to deal with Revit <see cref="ARDB.Transaction"/> instances.
  /// </summary>
  [ComponentVersion(introduced: "1.0", updated: "1.5")]
  public abstract partial class TransactionalComponent : ZuiComponent, ARDB.ITransactionFinalizer
  {
    protected TransactionalComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    #region Transaction
    protected internal ARDB.TransactionStatus TransactionStatus { get; set; } = ARDB.TransactionStatus.Uninitialized;

    protected ARDB.Transaction NewTransaction(ARDB.Document doc) => NewTransaction(doc, Name);
    protected ARDB.Transaction NewTransaction(ARDB.Document doc, string name)
    {
      var transaction = new ARDB.Transaction(doc, name);

      var options = transaction.GetFailureHandlingOptions();
      options = options.SetClearAfterRollback(true);
      options = options.SetDelayedMiniWarnings(false);
      options = options.SetForcedModalHandling(true);

      if(CreateFailuresPreprocessor() is ARDB.IFailuresPreprocessor preprocessor)
        options = options.SetFailuresPreprocessor(preprocessor);

      options = options.SetTransactionFinalizer(this);

      transaction.SetFailureHandlingOptions(options);

      return transaction;
    }

    protected ARDB.TransactionStatus CommitTransaction(ARDB.Document doc, ARDB.Transaction transaction)
    {
      // Disable Rhino UI if any warning-message dialog popup
      var uiApplication = Revit.ActiveUIApplication;
      External.UI.EditScope scope = null;
      EventHandler<DialogBoxShowingEventArgs> _ = null;
      try
      {
        uiApplication.DialogBoxShowing += _ = (sender, args) =>
        {
          if (scope is null)
            scope = new External.UI.EditScope(uiApplication);
        };

        if (transaction.GetStatus() == ARDB.TransactionStatus.Started)
        {
          return transaction.Commit();
        }
        else return transaction.RollBack();
      }
      finally
      {
        uiApplication.DialogBoxShowing -= _;

        if (scope is IDisposable disposable)
          disposable.Dispose();
      }
    }
    #endregion

    // Step 1.
    protected override void BeforeSolveInstance() => TransactionStatus = ARDB.TransactionStatus.Uninitialized;

    // Step 2.
    //protected override void TrySolveInstance(IGH_DataAccess DA) { }

    // Step 3.
    //protected override void AfterSolveInstance() {}

    #region ITransactionFinalizer
    public virtual void OnCommitted(ARDB.Document document, string strTransactionName)
    {
      if (TransactionStatus < ARDB.TransactionStatus.Pending)
        TransactionStatus = ARDB.TransactionStatus.Committed;
    }

    public virtual void OnRolledBack(ARDB.Document document, string strTransactionName)
    {
      if (TransactionStatus < ARDB.TransactionStatus.Pending)
        TransactionStatus = ARDB.TransactionStatus.RolledBack;
    }
    #endregion

    #region IO
    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      int mode = (int) ARDB.FailureProcessingResult.Continue;
      reader.TryGetInt32("FailureProcessingMode", ref mode);
      FailureProcessingMode = (ARDB.FailureProcessingResult) mode;

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (FailureProcessingMode != ARDB.FailureProcessingResult.Continue)
        writer.SetInt32("FailureProcessingMode", (int) FailureProcessingMode);

      return true;
    }
    #endregion
  }
}

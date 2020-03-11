using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Autodesk.Revit.UI.Events;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Exceptions;
using RhinoInside.Revit.GH;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class TransactionComponent : TransactionalComponent
  {
    protected TransactionComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    #region Autodesk.Revit.DB.Transacion support
    protected enum TransactionStrategy
    {
      PerSolution,
      PerComponent
    }
    protected virtual TransactionStrategy TransactionalStrategy => TransactionStrategy.PerComponent;

    protected DB.Transaction CurrentTransaction;
    protected DB.TransactionStatus TransactionStatus => CurrentTransaction?.GetStatus() ?? DB.TransactionStatus.Uninitialized;

    protected void BeginTransaction(DB.Document document)
    {
      if (document is null)
        return;

      CurrentTransaction = new DB.Transaction(document, Name);
      if (CurrentTransaction.Start() != DB.TransactionStatus.Started)
      {
        CurrentTransaction.Dispose();
        CurrentTransaction = null;
        throw new InvalidOperationException($"Unable to start Transaction '{Name}'");
      }
    }

    protected void CommitTransaction() => base.CommitTransaction(Revit.ActiveDBDocument, CurrentTransaction);
    #endregion

    // Step 1.
    protected override void BeforeSolveInstance()
    {
      if (TransactionalStrategy != TransactionStrategy.PerComponent)
        return;

      if (Revit.ActiveDBDocument is DB.Document Document)
      {
        BeginTransaction(Document);

        OnAfterStart(Document, CurrentTransaction.GetName());
      }
    }

    // Step 2.
    //protected override void OnAfterStart(Document document, string strTransactionName) { }

    // Step 3.
    //protected override void TrySolveInstance(IGH_DataAccess DA) { }

    // Step 4.
    //protected override void OnBeforeCommit(Document document, string strTransactionName) { }

    // Step 5.
    protected override void AfterSolveInstance()
    {
      if (TransactionalStrategy != TransactionStrategy.PerComponent)
        return;

      try
      {
        if (RunCount <= 0)
          return;

        if (TransactionStatus == DB.TransactionStatus.Uninitialized)
          return;

        if (Phase != GH_SolutionPhase.Failed)
        {
          CommitTransaction();
        }
      }
      finally
      {
        switch (TransactionStatus)
        {
          case DB.TransactionStatus.Uninitialized:
          case DB.TransactionStatus.Started:
          case DB.TransactionStatus.Committed:
            break;
          default:
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Transaction {TransactionStatus} and aborted.");
            break;
        }

        CurrentTransaction?.Dispose();
        CurrentTransaction = null;
      }
    }
  }
}

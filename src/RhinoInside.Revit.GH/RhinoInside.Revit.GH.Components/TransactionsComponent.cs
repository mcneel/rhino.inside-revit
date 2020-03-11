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
  public abstract class TransactionsComponent : TransactionalComponent
  {
    protected TransactionsComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    #region Autodesk.Revit.DB.Transacion support
    protected enum TransactionStrategy
    {
      PerSolution,
      PerComponent
    }
    protected virtual TransactionStrategy TransactionalStrategy => TransactionStrategy.PerComponent;

    Dictionary<DB.Document, DB.Transaction> CurrentTransactions;

    protected void BeginTransaction(DB.Document document)
    {
      if (CurrentTransactions?.ContainsKey(document) != true)
      {
        var transaction = new DB.Transaction(document, Name);
        if (transaction.Start() != DB.TransactionStatus.Started)
        {
          transaction.Dispose();
          throw new InvalidOperationException($"Unable to start Transaction '{Name}'");
        }

        if (CurrentTransactions is null)
          CurrentTransactions = new Dictionary<DB.Document, DB.Transaction>();

        CurrentTransactions.Add(document, transaction);
      }
    }

    // Step 5.
    protected override void AfterSolveInstance()
    {
      if (TransactionalStrategy != TransactionStrategy.PerComponent)
        return;

      if (CurrentTransactions is null)
        return;

      try
      {
        if (RunCount <= 0)
          return;

        foreach (var transaction in CurrentTransactions)
        {
          try
          {
            if (Phase != GH_SolutionPhase.Failed && transaction.Value.GetStatus() != DB.TransactionStatus.Uninitialized)
            {
              CommitTransaction(transaction.Key, transaction.Value);
            }
          }
          finally
          {
            var transactionStatus = transaction.Value.GetStatus();
            switch (transactionStatus)
            {
              case DB.TransactionStatus.Uninitialized:
              case DB.TransactionStatus.Started:
              case DB.TransactionStatus.Committed:
                break;
              default:
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Transaction {transactionStatus} and aborted.");
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
    #endregion
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB
{
  internal static class TransactionExtension
  {
    public readonly struct TransactionAwaitable
    {
      readonly TransactionAwaiter awaiter;
      internal TransactionAwaitable(Transaction transaction) => awaiter = new TransactionAwaiter(transaction);
      public TransactionAwaiter GetAwaiter() => awaiter;

      [HostProtection(Synchronization = true)]
      public class TransactionAwaiter : UI.ExternalEventHandler, ICriticalNotifyCompletion, ITransactionFinalizer
      {
        public readonly string Name;
        readonly ITransactionFinalizer TransactionFinalizer;
        TransactionStatus result = TransactionStatus.Pending;
        Autodesk.Revit.UI.ExternalEvent external;
        Action continuation;
        Exception exception;

        internal TransactionAwaiter(Transaction transaction)
        {
          using (var options = transaction.GetFailureHandlingOptions())
          {
            TransactionFinalizer = options.GetTransactionFinalizer();

            Name = transaction.GetName();
            result = UI.HostedApplication.Active.InvokeInHostContext
            (() => transaction.Commit(options.SetTransactionFinalizer(this).SetForcedModalHandling(false)));
          }
        }

        #region Awaiter
        public bool IsCompleted => result != TransactionStatus.Pending;
        public TransactionStatus GetResult() => exception is null ? result : throw exception;
        #endregion

        #region ICriticalNotifyCompletion
        [SecuritySafeCritical]
        void INotifyCompletion.OnCompleted(Action action) => Rise(action);

        [SecuritySafeCritical]
        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action action) => Rise(action);

        void Rise(Action action)
        {
          // If TransactionAwaitable is not awaited or Transaction runs synchronously we are done.
          if (action is null)
            return;

          continuation = action;
          external = Autodesk.Revit.UI.ExternalEvent.Create(this);
          switch (external.Raise())
          {
            case Autodesk.Revit.UI.ExternalEventRequest.Accepted: break;
            case Autodesk.Revit.UI.ExternalEventRequest.Pending: throw new InvalidOperationException();
            case Autodesk.Revit.UI.ExternalEventRequest.Denied: throw new NotSupportedException();
            case Autodesk.Revit.UI.ExternalEventRequest.TimedOut: throw new TimeoutException();
            default: throw new NotImplementedException();
          }
        }
        #endregion

        #region ITransactionFinalizer
        void ITransactionFinalizer.OnCommitted(Document document, string strTransactionName)
        {
          try { TransactionFinalizer?.OnCommitted(document, strTransactionName); }
          catch (TargetInvocationException e) { exception = e.InnerException; }

          result = TransactionStatus.Committed;
        }

        void ITransactionFinalizer.OnRolledBack(Document document, string strTransactionName)
        {
          try { TransactionFinalizer?.OnRolledBack(document, strTransactionName); }
          catch (TargetInvocationException e) { exception = e.InnerException; }

          result = TransactionStatus.RolledBack;
        }
        #endregion

        #region IExternalEventHandler
        protected override void Execute(Autodesk.Revit.UI.UIApplication app)
        {
          using (external)
            continuation.Invoke();
        }

        public override string GetName() => Name;
        #endregion
      }
    }

    public static TransactionAwaitable CommitAsync(this Transaction self) => new TransactionAwaitable(self);
  }

  public interface ITransactionNotification
  {
    /// <summary>
    /// This method is called before start a transaction.
    /// </summary>
    /// <param name="document">The document associated with the transaction.</param>
    /// <returns>True to allow a new transacion on <paramref name="document"/> or false to prevent it.</returns>
    bool OnStart(Document document);

    /// <summary>
    /// This method is called after a transaction is started.
    /// </summary>
    /// <param name="document">The document associated with the transaction.</param>
    void OnStarted(Document document);

    /// <summary>
    /// This method is called before committing the transaction chain.
    /// </summary>
    /// <param name="documents">Documents associated with the transaction chain.</param>
    void OnPrepare(IReadOnlyCollection<Document> documents);

    /// <summary>
    /// This method is called at the end the transaction chain even no transaction is started.
    /// </summary>
    /// <param name="status">Status of the whole transaction chain.</param>
    void OnDone(TransactionStatus status);
  }

  public struct TransactionHandlingOptions
  {
    //public bool CommitOneByOne;
    public bool KeepFailuresAfterRollback;
    public bool DelayedMiniWarnings;
    public bool AllowModelessHandling;
    public IFailuresPreprocessor FailuresPreprocessor;
    public ITransactionFinalizer TransactionFinalizer;
    public ITransactionNotification TransactionNotification;
  }

  /// <summary>
  /// TransactionChain provide control over a subset of changes on several documents as an atomic unique change.
  /// </summary>
  /// <remarks>
  /// A TransactionChain behaves like a <see cref="Autodesk.Revit.DB.Transaction"/> but on several documents
  /// at the same time.
  /// </remarks>
  public sealed class TransactionChain : IFailuresPreprocessor, ITransactionFinalizer, IDisposable
  {
    readonly Dictionary<Document, Transaction> transactionChain = new Dictionary<Document, Transaction>();
    IEnumerator<Transaction> transactionLinks;
    internal readonly string name;

    public bool IsValidObject => transactionChain.All(x => x.Key.IsValidObject && x.Value.IsValidObject);
    public string GetName() => name;
    TransactionHandlingOptions HandlingOptions { get; set; }

    public bool HasStarted() => transactionChain.Count > 0;
    public bool HasStarted(Document doc) =>
      transactionChain.TryGetValue(doc, out var transaction) && transaction.HasStarted();

    public bool HasEnded() => transactionChain.Count == 0;
    public bool HasEnded(Document doc) =>
      transactionChain.TryGetValue(doc, out var transaction) && transaction.HasEnded();

    public TransactionChain() => name = "Unnamed";
    public TransactionChain(Document document) : this()
    {
      Start(document);
    }
    public TransactionChain(params Document[] documents) : this()
    {
      foreach (var doc in documents)
        Start(doc);
    }

    public TransactionChain(string name) => this.name = name;
    public TransactionChain(string name, Document document) :
      this(name)
    {
      Start(document);
    }
    public TransactionChain(string name, params Document[] documents) :
      this(name)
    {
      foreach (var doc in documents)
        Start(doc);
    }

    public TransactionChain(TransactionHandlingOptions options, string name)
    {
      this.name = name;
      this.HandlingOptions = options;
    }
    public TransactionChain(TransactionHandlingOptions options, string name, Document document) :
      this(options, name)
    {
      Start(document);
    }
    public TransactionChain(TransactionHandlingOptions options, string name, params Document[] documents) :
      this(options, name)
    {
      foreach (var doc in documents)
        Start(doc);
    }

    void IDisposable.Dispose()
    {
      // Should not throw any Exception.
      // Commit and Rollback relay on this.

      transactionLinks = default;

      foreach (var transaction in transactionChain.Values.Reverse())
      {
        // Some exceptions like assigning an already used name to a DB.Material
        // corrupts the transaction, so we catch any exception here.
        if (!transaction.IsValidObject) continue;
        try { transaction.Dispose(); }
        catch { }
      }

      transactionChain.Clear();
    }

    internal TransactionStatus Start(Document doc)
    {
      var result = TransactionStatus.Started;

      if (!transactionChain.ContainsKey(doc))
      {
        var transaction = new Transaction(doc, name);
        try
        {
          transaction.SetFailureHandlingOptions
          (
            transaction.GetFailureHandlingOptions().
            SetClearAfterRollback(!HandlingOptions.KeepFailuresAfterRollback).
            SetDelayedMiniWarnings(HandlingOptions.DelayedMiniWarnings).
            SetForcedModalHandling(!HandlingOptions.AllowModelessHandling).
            SetFailuresPreprocessor(this).
            SetTransactionFinalizer(this)
          );

          result = HandlingOptions.TransactionNotification?.OnStart(doc) ?? true ?
            transaction.Start() : TransactionStatus.Uninitialized;

          if (result != TransactionStatus.Started)
          {
            transaction.Dispose();
            throw new InvalidOperationException($"Failed to start Transaction '{name}' on document '{doc.Title.TripleDot(16)}'");
          }

          HandlingOptions.TransactionNotification?.OnStarted(doc);

          transactionChain.Add(doc, transaction);
        }
        catch
        {
          transaction.Dispose();
          throw;
        }
      }

      return result;
    }

    public TransactionStatus Commit()
    {
      if (transactionChain.Count == 0)
        return TransactionStatus.Uninitialized;

      var status = TransactionStatus.Error;
      try
      {
        using (this)
        {
          HandlingOptions.TransactionNotification?.OnPrepare(transactionChain.Keys);

          using (transactionLinks = transactionChain.Values.GetEnumerator())
            status = CommitNextTransaction();
        }
      }
      finally
      {
        if(status != TransactionStatus.Pending)
          HandlingOptions.TransactionNotification?.OnDone(status);
      }

      return status;
    }

    public TransactionStatus RollBack()
    {
      if (transactionChain.Count == 0)
        return TransactionStatus.Uninitialized;

      var status = TransactionStatus.Error;
      try
      {
        using (this)
        {
          foreach (var transaction in transactionChain.Values.Reverse())
            transaction.RollBack();

          status = TransactionStatus.RolledBack;
        }
      }
      finally
      {
        HandlingOptions.TransactionNotification?.OnDone(status);
      }

      return status;
    }

    #region ITransactionChainNotification
    TransactionStatus CommitNextTransaction()
    {
      if (transactionLinks.MoveNext())
      {
        var transaction = transactionLinks.Current;

        if (transaction.GetStatus() == TransactionStatus.Started)
        {
          transaction.SetFailureHandlingOptions
          (
            transaction.GetFailureHandlingOptions().
            SetClearAfterRollback(!HandlingOptions.KeepFailuresAfterRollback).
            SetDelayedMiniWarnings(HandlingOptions.DelayedMiniWarnings).
            SetForcedModalHandling(!HandlingOptions.AllowModelessHandling).
            SetFailuresPreprocessor(this).
            SetTransactionFinalizer(this)
          );

          return transaction.Commit();
        }
        else
          return transaction.RollBack();
      }

      return TransactionStatus.Committed;
    }

    FailureProcessingResult IFailuresPreprocessor.PreprocessFailures(FailuresAccessor failuresAccessor)
    {
      var result = HandlingOptions.FailuresPreprocessor?.PreprocessFailures(failuresAccessor) ??
                   FailureProcessingResult.Continue;

      if (transactionChain.ContainsKey(failuresAccessor.GetDocument()) == true)
      {
        if (result < FailureProcessingResult.ProceedWithRollBack)
        {
          if (!failuresAccessor.IsTransactionBeingCommitted() || CommitNextTransaction() != TransactionStatus.Committed)
            result = FailureProcessingResult.ProceedWithRollBack;
        }
      }

      return result;
    }
    #endregion

    #region ITransactionFinalizer
    void ITransactionFinalizer.OnCommitted(Document document, string strTransactionName)
    {
      HandlingOptions.TransactionFinalizer?.OnCommitted(document, strTransactionName);
    }

    void ITransactionFinalizer.OnRolledBack(Document document, string strTransactionName)
    {
      HandlingOptions.TransactionFinalizer?.OnRolledBack(document, strTransactionName);
    }
    #endregion
  }

  /// <summary>
  /// Adaptive-transactions are objects that provide control over a subset of changes in a document.
  /// </summary>
  /// <remarks>
  /// An AdaptiveTransaction behaves like a <see cref="Autodesk.Revit.DB.Transaction"/> in case the
  /// <see cref="Autodesk.Revit.DB.Document"/> has no active Transaction running on it, otherwise
  /// as a <see cref="Autodesk.Revit.DB.SubTransaction"/>.
  /// </remarks>
  public sealed class AdaptiveTransaction : IDisposable
  {
    readonly string name;
    readonly Document document;
    Transaction transaction;
    SubTransaction subTransaction;

    public AdaptiveTransaction(Document document) : this(document, "Unnamed") {}
    public AdaptiveTransaction(Document document, string name)
    {
      this.document = document;
      this.name = name;
    }

    public bool IsValidObject => transaction?.IsValidObject != false && subTransaction?.IsValidObject != false;
    public string GetName() => name;
    public TransactionStatus GetStatus()
    {
      if (transaction is object) return transaction.GetStatus();
      if (subTransaction is object) return subTransaction.GetStatus();
      return TransactionStatus.Uninitialized;
    }

    public bool HasStarted()
    {
      if (transaction is object) return transaction.HasStarted();
      if (subTransaction is object) return subTransaction.HasStarted();
      return false;
    }

    public bool HasEnded()
    {
      if (transaction is object) return transaction.HasEnded();
      if (subTransaction is object) return subTransaction.HasEnded();
      return true;
    }

    public TransactionStatus Start()
    {
      if (HasStarted())
        throw new InvalidOperationException($"{name} transaction is already started.");

      TransactionStatus status;

      if (document.IsModifiable)
      {
        var subtr = new SubTransaction(document);
        status = subtr.Start();
        subTransaction = subtr;
      }
      else
      {
        var trans = new Transaction(document, name);
        status = trans.Start();
        transaction = trans;
      }

      return status;
    }

    public TransactionStatus Commit()
    {
      if (!HasStarted())
        throw new InvalidOperationException("AdaptiveTransaction is not started.");

      using (this)
      {
        if (transaction is object) return transaction.Commit();
        if (subTransaction is object) return subTransaction.Commit();
        return TransactionStatus.Uninitialized;
      }
    }

    public TransactionStatus RollBack()
    {
      if (!HasStarted())
        throw new InvalidOperationException("AdaptiveTransaction is not started.");

      using (this)
      {
        if (transaction is object) return transaction.RollBack();
        if (subTransaction is object) return subTransaction.RollBack();
        return TransactionStatus.Uninitialized;
      }
    }

    public void Dispose()
    {
      transaction?.Dispose();
      transaction = default;

      subTransaction?.Dispose();
      subTransaction = default;
    }
  }

  public static class DisposableScope
  {
    /// <summary>
    /// Implementation class for <see cref="CommitScope(Document)"/>
    /// </summary>
    public readonly struct CommittableScope : IDisposable, ITransactionFinalizer
    {
      internal static CommittableScope Default;
      readonly Transaction transaction;
      const string name = "Commit Scope";

      internal CommittableScope(Document document)
      {
        transaction = new Transaction(document, name);
        transaction.SetFailureHandlingOptions
        (
          transaction.GetFailureHandlingOptions().
          SetClearAfterRollback(true).
          SetDelayedMiniWarnings(false).
          SetForcedModalHandling(true).
          SetFailuresPreprocessor(FailuresPreprocessor.NoErrors)
        );

        if (transaction.Start() != TransactionStatus.Started)
          throw new InvalidOperationException($"Transaction failed to start on document '{document.Title}'");
      }

      public void Commit() => transaction?.Commit(transaction.GetFailureHandlingOptions().SetTransactionFinalizer(this));

      void ITransactionFinalizer.OnCommitted(Document document, string strTransactionName) { }
      public void OnRolledBack(Document document, string strTransactionName)
      {
        throw new InvalidOperationException($"Transaction failed to commit on document '{document.Title}'");
      }

      void IDisposable.Dispose() => transaction?.Dispose();
    }

    /// <summary>
    /// Starts a Commit scope that will be automatically rolled back when disposed.
    /// </summary>
    /// <param name="document"></param>
    /// <returns><see cref="IDisposable"/> that should be disposed before leaving the scope.</returns>
    /// <remarks>
    /// Use an auto dispose pattern to be sure the returned <see cref="IDisposable"/> is disposed before the calling method returns.
    /// And call <see cref="CommittableScope.Commit()"/> to commit all changes made to the model during the scope.
    /// <para>
    /// C# : using(var scope = document.CommitScope())
    /// </para>
    /// <para>
    /// VB : Using scope [As CommittableScope] = document.CommitScope()
    /// </para>
    /// <para>
    /// Pyhton: with document.CommitScope() as scope:
    /// </para>
    /// </remarks>
    public static CommittableScope CommitScope(this Document document)
    {
      return document.IsModifiable ? CommittableScope.Default : new CommittableScope(document);
    }

    /// <summary>
    /// Starts a RollBack scope that will be automatically rolled back when disposed.
    /// </summary>
    /// <param name="document"></param>
    /// <returns><see cref="IDisposable"/> that should be disposed before leaving the scope.</returns>
    /// <remarks>
    /// Use an auto dispose pattern to be sure the returned <see cref="IDisposable"/> is disposed before the calling method returns.
    /// <para>
    /// C# : using(document.RollBackScope())
    /// </para>
    /// <para>
    /// VB : Using document.RollBackScope()
    /// </para>
    /// <para>
    /// Pyhton: with document.RollBackScope() :
    /// </para>
    /// </remarks>
    public static IDisposable RollBackScope(this Document document)
    {
      if (document.IsModifiable)
      {
        var subTransaction = new SubTransaction(document);
        if (subTransaction.Start() != TransactionStatus.Started)
          throw new InvalidOperationException($"SubTransaction failed to start on document '{document.Title.TripleDot(16)}'");

        return subTransaction;
      }
      else
      {
        var transaction = new Transaction(document, "RollBack Scope");
        transaction.SetFailureHandlingOptions
        (
          transaction.GetFailureHandlingOptions().
          SetClearAfterRollback(true).
          SetDelayedMiniWarnings(false).
          SetForcedModalHandling(true).
          SetFailuresPreprocessor(FailuresPreprocessor.Rollback)
        );

        if (transaction.Start() != TransactionStatus.Started)
          throw new InvalidOperationException($"Transaction failed to start on document '{document.Title.TripleDot(16)}'");

        return transaction;
      }
    }
  }

  public static class FailuresPreprocessor
  {
    public static readonly IFailuresPreprocessor NoWarnings = default(NoWarningsPreprocessor);
    public static readonly IFailuresPreprocessor NoErrors   = default(NoErrorsPreprocessor);
    public static readonly IFailuresPreprocessor Rollback   = default(RollbackPreprocessor);

    struct NoWarningsPreprocessor : IFailuresPreprocessor
    {
      public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
      {
        failuresAccessor.DeleteAllWarnings();
        return FailureProcessingResult.Continue;
      }
    }

    struct NoErrorsPreprocessor : IFailuresPreprocessor
    {
      public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
      {
        if (failuresAccessor.GetSeverity() < FailureSeverity.Error)
        {
          failuresAccessor.DeleteAllWarnings();
          return FailureProcessingResult.Continue;
        }
        else if (failuresAccessor.IsFailureResolutionPermitted())
        {
          var fixCount = 0;
          var failures = failuresAccessor.GetFailureMessages(FailureSeverity.Error);
          foreach (var failure in failures)
          {
            if (!failuresAccessor.IsFailureResolutionPermitted(failure)) continue;
            if (!failure.HasResolutions()) continue;
            if (failuresAccessor.GetAttemptedResolutionTypes(failure).Any()) continue;

            failure.SetCurrentResolutionType(FailureResolutionType.Default);
            failuresAccessor.ResolveFailure(failure);
            fixCount++;
          }

          if(fixCount > 0)
            return FailureProcessingResult.ProceedWithCommit;
        }

        return FailureProcessingResult.ProceedWithRollBack;
      }
    }

    struct RollbackPreprocessor : IFailuresPreprocessor
    {
      public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
      {
        return FailureProcessingResult.ProceedWithRollBack;
      }
    }
  }
}

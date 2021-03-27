using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Microsoft.Win32.SafeHandles;

namespace RhinoInside.Revit.External.UI
{
  internal sealed class EditScope : IDisposable
  {
    readonly UIHostApplication uiApplication;
    readonly WindowHandle activeWindow = WindowHandle.ActiveWindow;
    readonly bool WasExposed = Rhinoceros.MainWindow.Visible;
    readonly bool WasEnabled = Revit.MainWindow.Enabled;

    public EditScope(UIHostApplication app)
    {
      uiApplication = app;
      Rhinoceros.MainWindow.HideOwnedPopups();
      if (WasExposed) Rhinoceros.MainWindow.Visible = false;

      Revit.MainWindow.Enabled = true;
      WindowHandle.ActiveWindow = Revit.MainWindow;
    }

    internal static async void PostCommand(UIHostApplication app, RevitCommandId commandId)
    {
      using (var scope = new EditScope(app))
        await scope.ExecuteCommandAsync(commandId);
    }

    internal CommandAwaitable ExecuteCommandAsync(RevitCommandId commandId) =>
      new CommandAwaitable(uiApplication.Value as UIApplication, commandId);

    void IDisposable.Dispose()
    {
      if (WasExposed) Rhinoceros.MainWindow.Visible = WasExposed;
      Rhinoceros.MainWindow.ShowOwnedPopups();

      Revit.MainWindow.Enabled = WasEnabled;
      WindowHandle.ActiveWindow = activeWindow;
    }
  }

  #region CommandAwaitable
  internal class DocumentChangeRecord
  {
    public DocumentChangeRecord
    (
      UndoOperation operation,
      Document document,
      IList<string> transactionNames,
      ICollection<ElementId> addedElementIds,
      ICollection<ElementId> deletedElementIds,
      ICollection<ElementId> modifiedElementIds
    )
    {
      Operation = operation;
      Document = document;
      TransactionNames = transactionNames;
      AddedElementIds = addedElementIds;
      DeletedElementIds = deletedElementIds;
      ModifiedElementIds = modifiedElementIds;
    }

    public DocumentChangeRecord(DocumentChangedEventArgs args)
    {
      Operation = args.Operation;
      Document = args.GetDocument();
      TransactionNames = args.GetTransactionNames();
      AddedElementIds = args.GetAddedElementIds();
      DeletedElementIds = args.GetDeletedElementIds();
      ModifiedElementIds = args.GetModifiedElementIds();
    }

    public UndoOperation Operation { get; private set; }
    public Document Document { get; private set; }
    public IList<string> TransactionNames { get; private set; }
    public ICollection<ElementId> AddedElementIds { get; private set; }
    public ICollection<ElementId> DeletedElementIds { get; private set; }
    public ICollection<ElementId> ModifiedElementIds { get; private set; }
  }

  internal class DocumentExtract
  {
    public readonly IList<DocumentChangeRecord> Records = new List<DocumentChangeRecord>();

    public ICollection<Document> GetDocuments()
    {
      var documents = new HashSet<Document>();
      foreach (var record in Records)
        documents.Add(record.Document);

      return documents;
    }

    public int GetSummary
    (
      Document doc,
      out ICollection<ElementId> added,
      out ICollection<ElementId> deleted,
      out ICollection<ElementId> modified
    )
    {
      if (Records.Count == 0)
      {
        added = deleted = modified = default;
        return 0;
      }

      var count = 0;
      added = new HashSet<ElementId>();
      deleted = new HashSet<ElementId>();
      var modifiedMap = new Dictionary<ElementId, int>();

      foreach (var record in Records)
      {
        if (!record.Document.Equals(doc)) continue;
        count++;

        if
        (
          record.Operation == UndoOperation.TransactionCommitted ||
          record.Operation == UndoOperation.TransactionRedone
        )
        {
          foreach (var a in record.AddedElementIds) added.Add(a);
          foreach (var d in record.DeletedElementIds) deleted.Add(d);
          foreach (var m in record.ModifiedElementIds)
          {
            if (!modifiedMap.TryGetValue(m, out var c)) c = 0;
            else modifiedMap.Remove(m);
            if (++c != 0) modifiedMap.Add(m, c);
          }
        }
        else
        {
          foreach (var a in record.AddedElementIds) added.Remove(a);
          foreach (var d in record.DeletedElementIds) deleted.Remove(d);
          foreach (var m in record.ModifiedElementIds)
          {
            if (!modifiedMap.TryGetValue(m, out var c)) c = 0;
            else modifiedMap.Remove(m);
            if (--c != 0) modifiedMap.Add(m, c);
          }
        }
      }

      modified = new HashSet<ElementId>(modifiedMap.Keys);

      return added.Count + deleted.Count + modified.Count;
    }
  }

  internal struct CommandAwaitable
  {
    public readonly UIApplication UIApplication;
    readonly RevitCommandId CommandId;
    internal CommandAwaitable(UIApplication app, RevitCommandId commandId)
    {
      UIApplication = app;
      CommandId = commandId;
    }
    public CommandAwaiter GetAwaiter() => new CommandAwaiter(UIApplication, CommandId);

    [HostProtection(Synchronization = true)]
    public class CommandAwaiter : ExternalEventHandler, ICriticalNotifyCompletion
    {
      public readonly UIApplication UIApplication;
      public readonly RevitCommandId CommandId;
      readonly DocumentExtract result = new DocumentExtract();
      Action action;
      ExternalEvent external;

      internal CommandAwaiter(UIApplication app, RevitCommandId commandId)
      {
        UIApplication = app;
        CommandId = commandId;
        action = default;
        external = default;
      }

      #region Awaiter
      public bool IsCompleted => UIApplication is null;
      public DocumentExtract GetResult() => result;
      #endregion

      #region ICriticalNotifyCompletion
      [SecuritySafeCritical]
      void INotifyCompletion.OnCompleted(Action continuation) =>
        Post(continuation);

      [SecuritySafeCritical]
      void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation) =>
        Post(continuation);

      void Post(Action continuation)
      {
        UIApplication.PostCommand(CommandId);
        UIApplication.Application.DocumentChanged += DocumentChanged;

        action = continuation;
        external = ExternalEvent.Create(this);
        switch (external.Raise())
        {
          case ExternalEventRequest.Accepted: break;
          case ExternalEventRequest.Pending: throw new InvalidOperationException();
          case ExternalEventRequest.Denied: throw new NotSupportedException();
          case ExternalEventRequest.TimedOut: throw new TimeoutException();
          default: throw new NotImplementedException();
        }
      }
      #endregion

      #region IExternalEventHandler
      protected override void Execute(UIApplication app)
      {
        UIApplication.Application.DocumentChanged -= DocumentChanged;

        using (external)
          action.Invoke();
      }

      private void DocumentChanged(object sender, DocumentChangedEventArgs e) =>
        result.Records.Add(new DocumentChangeRecord(e));

      public override string GetName() => CommandId.Name;
      #endregion
    }
  }
  #endregion
}

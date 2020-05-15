using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using Autodesk.Revit.UI;
using Microsoft.Win32.SafeHandles;

namespace RhinoInside.Revit.External
{
  /// <summary>
  /// Represents a gate to access Revit API context.
  /// </summary>
  static class ActivationGate
  {
    /// <summary>
    /// The event arguments used by the <see cref="ActivationGate.CanOpen"/> event.
    /// </summary>
    public class CanOpenEventArgs : EventArgs
    {
      bool canOpen = true;
      public bool CanOpen
      {
        get => canOpen;
        set => canOpen |= value;
      }
    }

    /// <summary>
    /// It will be fired right before <see cref="ActivationGate"/> opens to check if it may open.
    /// </summary>
    /// <remarks>
    /// <see cref="CanOpen"/> event handlers will be called in FIFO order.
    /// </remarks>
    public static event EventHandler<CanOpenEventArgs> CanOpen { add => canOpen += value; remove => canOpen -= value; }
    static event EventHandler<CanOpenEventArgs> canOpen;

    /// <summary>
    /// It will be fired right after <see cref="ActivationGate"/> opens.
    /// </summary>
    /// <remarks>
    /// <see cref="Enter"/> event handlers will be called in FIFO order.
    /// </remarks>
    public static event EventHandler Enter { add => enter += value; remove => enter -= value; }
    static event EventHandler enter;

    /// <summary>
    /// It will be fired right before Activation gate closes.
    /// </summary>
    /// <remarks>
    /// <see cref="Exit"/> event handlers will be called in LIFO order.
    /// </remarks>
    public static event EventHandler Exit { add => exit = value + exit; remove => exit -= value; }
    static event EventHandler exit;

    [ThreadStatic]
    static WindowHandle windowToActivate = default;

    [ThreadStatic]
    static readonly Dictionary<IntPtr, ExternalEvent> gates = new Dictionary<IntPtr, ExternalEvent>();

    /// <summary>
    /// Returns an <see cref="IEnumerable{IntPtr}"/> of currently registered windows as Gate windows.
    /// </summary>
    public static IEnumerable<IntPtr> GateWindows => gates.Select(x => x.Key);

    /// <summary>
    /// Registers a window as a Gate window.
    /// </summary>
    /// <param name="hWnd">HWND of the window to register.</param>
    /// <returns>true on success, false on failure.</returns>
    public static bool AddGateWindow(IntPtr hWnd)
    {
      using (var window = new WindowHandle(hWnd))
        if (window.IsInvalid && window.ThreadId == ThreadHandle.CurrentThreadId)
          throw new ArgumentException("Invalid handle value", nameof(hWnd));

      if (hook is null)
        hook = new Hook();

      if (gates.ContainsKey(hWnd))
        return false;

      gates.Add(hWnd, ExternalEvent.Create(new TryActivateEventHandler()));
      return true;
    }

    static bool IsExternalWindow(IntPtr hWnd, out ExternalEvent gate)
    {
      for (var window = new WindowHandle(hWnd); !window.IsInvalid; window = window.Owner)
        if (gates.TryGetValue(window.Handle, out gate))
          return true;

      gate = default;
      return false;
    }

    /// <summary>
    /// Returns true if active window is a gate window or owned by a gate window.
    /// </summary>
    public static bool IsActive => IsExternalWindow(WindowHandle.ActiveWindow.Handle, out var _);

    [ThreadStatic]
    static bool isOpen = default;

    [ThreadStatic]
    static object TopState = default;

    /// <summary>
    /// Returns true if gate is open and Revit API si fully available.
    /// </summary>
    public static bool IsOpen
    {
      get => isOpen;
      private set
      {
        if (isOpen == value)
          return;

        if (isOpen == false)
        {
          if (TopState as UI.Application is null)
          {
            var args = new CanOpenEventArgs();
            canOpen?.Invoke(TopState, args);
            if (!args.CanOpen)
              throw new Exceptions.CancelException();
          }

          isOpen = true;
          enter?.Invoke(TopState, EventArgs.Empty);
        }
        else
        {
          exit?.Invoke(TopState, EventArgs.Empty);
          isOpen = false;
        }
      }
    }

    internal static void Open(Action action, object state) =>
      Open(() => { action.Invoke(); return System.Reflection.Missing.Value; }, state);

    internal static T Open<T>(Func<T> func, object state)
    {
      var prevState = TopState;
      var wasOpen = IsOpen;

      try
      {
        TopState = state;
        IsOpen = true;
        var result = func.Invoke();

        if (IsActive && state as UI.Application is null)
        {
          while (Rhinoceros.Run()) { }
        }

        return result;
      }
      finally
      {
        IsOpen = wasOpen;
        TopState = prevState;

        if (IsActive && !IsOpen)
        {
          // Return control to Revit
          Revit.MainWindow.Enabled = true;
          WindowHandle.ActiveWindow = Revit.MainWindow;
        }
      }
    }

    #region Awaitables
    public struct YieldAwaitable
    {
      readonly string Name;
      internal YieldAwaitable(string name) { Name = name; }
      public YieldAwaiter GetAwaiter() => new YieldAwaiter(Name);

      [HostProtection(Synchronization = true)]
      public class YieldAwaiter : UI.EventHandler, ICriticalNotifyCompletion
      {
        public readonly string Name;
        Action action;
        ExternalEvent external;
        UIApplication result;

        internal YieldAwaiter(string name)
        {
          Name = name;
          action = default;
          external = default;
          result = default;
        }

        #region Awaiter
        public bool IsCompleted => false;
        public UIApplication GetResult() => result;
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
          action = continuation;
          external = ExternalEvent.Create(this);
          switch (external.Raise())
          {
            case ExternalEventRequest.Accepted: break;
            case ExternalEventRequest.Pending:  throw new InvalidOperationException();
            case ExternalEventRequest.Denied:   throw new NotSupportedException();
            case ExternalEventRequest.TimedOut: throw new TimeoutException();
            default:                            throw new NotImplementedException();
          }
        }
        #endregion

        #region IExternalEventHandler
        protected override void Execute(UIApplication app)
        {
          result = app;
          using (external)
            action.Invoke();
        }

        public override string GetName() => Name;
        #endregion
      }
    }

    /// <summary>
    /// Creates an awaitable task that asynchronously yields back to the current context when
    /// gate is open and Revit API is fully available.
    /// </summary>
    /// <param name="name"><see cref="Autodesk.Revit.UI.IExternalEventHandler"/> name</param>
    /// <returns></returns>
    public static YieldAwaitable Yield([CallerMemberName] string name = "") =>
      new YieldAwaitable(name ?? MethodBase.GetCurrentMethod().Name);

    public struct OpenAwaitable
    {
      readonly string Name;
      internal OpenAwaitable(string name) { Name = name; }
      public OpenAwaiter GetAwaiter() => new OpenAwaiter(Name);

      [HostProtection(Synchronization = true)]
      public class OpenAwaiter : UI.EventHandler, ICriticalNotifyCompletion
      {
        public readonly string Name;
        Action action;
        ExternalEvent external;

        internal OpenAwaiter(string name)
        {
          Name = name;
          action = default;
          external = default;
        }

        #region Awaiter
        public bool IsCompleted => IsOpen;
        public void GetResult() { }
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
          action = continuation;
          external = ExternalEvent.Create(this);
          switch (external.Raise())
          {
            case ExternalEventRequest.Accepted: break;
            case ExternalEventRequest.Pending:  throw new InvalidOperationException();
            case ExternalEventRequest.Denied:   throw new NotSupportedException();
            case ExternalEventRequest.TimedOut: throw new TimeoutException();
            default:                            throw new NotImplementedException();
          }
        }
        #endregion

        #region IExternalEventHandler
        protected override void Execute(UIApplication app)
        {
          using (external)
            action.Invoke();
        }

        public override string GetName() => Name;
        #endregion
      }
    }

    /// <summary>
    /// Creates an awaitable task that runs synchronously if gate is already open or
    /// asynchronously waits until gate opens and yields back to the current context.
    /// </summary>
    /// <param name="name"><see cref="Autodesk.Revit.UI.IExternalEventHandler"/> name</param>
    /// <returns></returns>
    public static OpenAwaitable Open([CallerMemberName] string name = "") =>
      new OpenAwaitable(name ?? MethodBase.GetCurrentMethod().Name);
    #endregion

    #region Implementation
    class TryActivateEventHandler : UI.EventHandler
    {
      public override string GetName() => "RhinoInside.Revit.External.ActivationGate";
      protected override void Execute(UIApplication app)
      {
        if (windowToActivate?.IsInvalid == false)
        {
          try { WindowHandle.ActiveWindow = windowToActivate; }
          finally { windowToActivate = default; }
        }
      }
    }

    class Hook : ComputerBasedTrainingHook
    {
      protected override int DispatchHook(int nCode, IntPtr wParam, IntPtr lParam)
      {
        switch (nCode)
        {
          case 5: // HCBT_ACTIVATE
          {
            if (!IsOpen && IsExternalWindow(wParam, out var gate))
            {
              windowToActivate = new WindowHandle(wParam);

              if (gate.IsPending)
                WindowHandle.ActiveWindow.Flash();
              else
                gate.Raise();

              return 1; // Prevents activation now.
            }
          }
          break;
          case 4: // HCBT_DESTROYWND
          {
            if (gates.TryGetValue(wParam, out var gate))
            {
              gate.Dispose();

              if (gates.Remove(wParam) && gates.Count == 0)
              {
                try { return base.DispatchHook(nCode, wParam, lParam); }
                finally { Dispose(); hook = default; }
              }
            }
          }
          break;
        }

        return base.DispatchHook(nCode, wParam, lParam);
      }
    }

    [ThreadStatic]
    static Hook hook = default;
    #endregion
  }

  public sealed class EditScope : IDisposable
  {
    readonly WindowHandle activeWindow = WindowHandle.ActiveWindow;
    readonly bool WasExposed = Rhinoceros.MainWindow.Visible;
    readonly bool WasEnabled = Revit.MainWindow.Enabled;

    public EditScope()
    {
                      Rhinoceros.MainWindow.HideOwnedPopups();
      if (WasExposed) Rhinoceros.MainWindow.Visible = false;

      Revit.MainWindow.Enabled = true;
      WindowHandle.ActiveWindow = Revit.MainWindow;
    }

    void IDisposable.Dispose()
    {
      if (WasExposed) Rhinoceros.MainWindow.Visible = WasExposed;
                      Rhinoceros.MainWindow.ShowOwnedPopups();

      Revit.MainWindow.Enabled = WasEnabled;
      WindowHandle.ActiveWindow = activeWindow;
    }
  }
}

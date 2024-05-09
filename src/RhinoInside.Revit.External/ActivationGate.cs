using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using Autodesk.Revit.UI;
using Microsoft.Win32.SafeHandles;
using Microsoft.Win32.SafeHandles.InteropServices;
using Rhino;
using Rhino.Input;

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
    static WindowHandle windowToActivate;

    class Gate
    {
      public readonly WindowHandle Window;
      public readonly ExternalEvent ExternalEvent;
      public readonly HashSet<IntPtr> ExternalWindows = new HashSet<IntPtr>();

      public Gate(WindowHandle gate, ExternalEvent externalEvent = default)
      {
        Window = gate;
        ExternalEvent = externalEvent ?? CreateActivationEvent();
      }
    }

    public static ExternalEvent CreateActivationEvent() => ExternalEvent.Create(new TryActivateEventHandler());

    [ThreadStatic]
    static readonly Dictionary<IntPtr, Gate> gates = new Dictionary<IntPtr, Gate>();

    /// <summary>
    /// Returns an <see cref="IEnumerable{IntPtr}"/> of currently registered windows as Gate windows.
    /// </summary>
    public static IEnumerable<IntPtr> GateWindows => gates.Select(x => x.Key);

    static WindowHandle HostMainWindow = WindowHandle.Zero;

    /// <summary>
    /// Attach Activation Gate to the Host main window.
    /// </summary>
    /// <param name="hWnd">HWND of the host main window.</param>
    public static void SetHostWindow(IntPtr hWnd)
    {
      var window = (WindowHandle) hWnd;

      if (!window.IsZero)
      {
        if (window.IsInvalid)
          throw new ArgumentException("Invalid handle vale", nameof(hWnd));

        if (!HostMainWindow.IsZero)
          throw new InvalidOperationException("Failed to change host main window");
      }

      HostMainWindow = window;
    }

    /// <summary>
    /// Registers a window as a Gate window.
    /// </summary>
    /// <param name="hWnd">HWND of the gate window to register.</param>
    /// <returns>true on success, false on failure.</returns>
    public static bool AddGateWindow(IntPtr hWnd, ExternalEvent externalEvent = default)
    {
      var window = (WindowHandle) hWnd;
      if (window.IsInvalid && window.ThreadId == ThreadHandle.CurrentThreadId)
        throw new ArgumentException("Invalid handle value", nameof(hWnd));

      if (HasGate(hWnd, out var _))
        return false;

      if (hook is null)
        hook = new Hook();

      gates.Add(hWnd, new Gate(window, externalEvent));
      return true;
    }

    public static bool RemoveGateWindow(IntPtr hWnd)
    {
      if (!gates.TryGetValue(hWnd, out var gate))
        return false;

      if (!gates.Remove(hWnd))
        return false;

      gate.ExternalEvent.Dispose();

      if (gates.Count == 0)
      {
        hook.Dispose();
        hook = null;
      }

      return true;
    }

    static bool HasGate(IntPtr hWnd, out Gate gate)
    {
      for (var window = (WindowHandle)hWnd; !window.IsInvalid; window = window.Owner)
      {
        if (gates.TryGetValue(window.Handle, out gate))
          return true;
      }

      gate = default;
      return false;
    }

    static bool IsExternalWindow(WindowHandle hWnd, out ExternalEvent externalEvent)
    {
      for (var window = hWnd; !window.IsInvalid; window = window.Owner)
      {
        if
        (
          gates.TryGetValue(window.Handle, out var gate) &&
          (window == hWnd || gate.ExternalWindows.Contains(hWnd.Handle))
        )
        {
          externalEvent = gate.ExternalEvent;
          return true;
        }
      }

      externalEvent = default;
      return false;
    }

    /// <summary>
    /// Returns true if active window is a gate window or owned by a gate window.
    /// </summary>
    public static bool IsActive => IsExternalWindow(WindowHandle.ActiveWindow, out var _);

    [ThreadStatic]
    static bool isOpen = default;

    [ThreadStatic]
    static object TopState = default;

    /// <summary>
    /// Returns true if gate is open and Revit API si fully available.
    /// </summary>
    public static bool IsOpen
    {
      get => isOpen && HostMainWindow.ThreadId == ThreadHandle.CurrentThreadId;
      private set
      {
        if (isOpen == value)
          return;

        if (isOpen == false)
        {
          if (TopState as UI.ExternalApplication is null)
          {
            var args = new CanOpenEventArgs();
            canOpen?.Invoke(TopState, args);
            if (!args.CanOpen)
              throw new CancelException();
          }

          isOpen = true;
          try { enter?.Invoke(TopState, EventArgs.Empty); }
          catch { }
        }
        else
        {
          try { exit?.Invoke(TopState, EventArgs.Empty); }
          catch { }
          isOpen = false;
        }
      }
    }

    internal static void Open(Action action, object state = default) =>
      Open(() => { action.Invoke(); return System.Reflection.Missing.Value; }, state);

    internal static T Open<T>(Func<T> func, object state = default)
    {
      var prevState = TopState;
      var wasOpen = IsOpen;

      try
      {
        TopState = state;
        IsOpen = true;
        var result = func.Invoke();

        if (!wasOpen && IsActive && state as UI.ExternalApplication is null)
        {
          while (UI.HostedApplication.Active.DoEvents()) { }
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
          HostMainWindow.Enabled = true;
          WindowHandle.ActiveWindow = HostMainWindow;
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
      public class YieldAwaiter : UI.ExternalEventHandler, ICriticalNotifyCompletion
      {
        public readonly string Name;
        ExternalEvent external;
        Action continuation;
        UIApplication result;

        internal YieldAwaiter(string name)
        {
          Name = name;
          continuation = default;
          external = default;
          result = default;
        }

        #region Awaiter
        public bool IsCompleted => !external?.IsPending ?? false;
        public UIApplication GetResult() => result;
        #endregion

        #region ICriticalNotifyCompletion
        [SecuritySafeCritical]
        void INotifyCompletion.OnCompleted(Action action) =>
          Post(action);

        [SecuritySafeCritical]
        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action action) =>
          Post(action);

        void Post(Action action)
        {
          continuation = action;
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
            continuation.Invoke();
        }

        public override string GetName() => Name;
        #endregion
      }
    }
    /// <summary>
    /// Creates an awaitable task that asynchronously yields back to the current context when
    /// gate is open next time and Revit API is fully available.
    /// <para>
    /// Awaitable task result is the current Revit <see cref="Autodesk.Revit.UI.UIApplication"/>.
    /// </para>
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
      public class OpenAwaiter : UI.ExternalEventHandler, ICriticalNotifyCompletion
      {
        public readonly string Name;
        ExternalEvent external;
        Action continuation;
        readonly bool result = !IsOpen;

        internal OpenAwaiter(string name)
        {
          Name = name;
          continuation = default;
          external = default;
        }

        #region Awaiter
        public bool IsCompleted => !external?.IsPending ?? IsOpen;
        public bool GetResult() => result;
        #endregion

        #region ICriticalNotifyCompletion
        [SecuritySafeCritical]
        void INotifyCompletion.OnCompleted(Action action) =>
          Post(action);

        [SecuritySafeCritical]
        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action action) =>
          Post(action);

        void Post(Action action)
        {
          continuation = action;
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
            continuation.Invoke();
        }

        public override string GetName() => Name;
        #endregion
      }
    }

    /// <summary>
    /// Creates an awaitable task that runs synchronously if gate is already open or
    /// asynchronously waits until gate opens and yields back to the current context.
    /// <para>
    /// Awaitable task result is true if the <see cref="ActivationGate"/> is opened here or false if it was already open.
    /// </para>
    /// </summary>
    /// <param name="name"><see cref="Autodesk.Revit.UI.IExternalEventHandler"/> name</param>
    /// <returns></returns>
    public static OpenAwaitable Open([CallerMemberName] string name = "") =>
      new OpenAwaitable(name ?? MethodBase.GetCurrentMethod().Name);
    #endregion

    #region Implementation
    class TryActivateEventHandler : UI.ExternalEventHandler
    {
      public override string GetName() => "RhinoInside.Revit.External.ActivationGate";
      protected override void Execute(UIApplication app)
      {
        if (windowToActivate?.IsInvalid == false)
        {
          try { WindowHandle.ActiveWindow = windowToActivate; }
          catch { /* Windows failed to be activated */ }
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
            if (IsOpen)
            {
              var window = (WindowHandle) wParam;
              if (window == HostMainWindow && !window.Enabled)
              {
                foreach (var gate in gates)
                {
                  try { gate.Value.Window.BringToFront(); }
                  catch { }
                }
              }
            }
            else
            {
              var window = (WindowHandle) wParam;
              if (IsExternalWindow(window, out var externalEvent))
              {
                windowToActivate = window;

                if (externalEvent.IsPending)
                {
                  // HACK : If Rhino is in a Get we should allow it to finish.
                  if (windowToActivate.Handle == RhinoApp.MainWindowHandle())
                  {
                    if (RhinoDoc.ActiveDoc is RhinoDoc rhinoDoc && RhinoGet.InGet(rhinoDoc))
                      return 0;
                  }

                  WindowHandle.ActiveWindow.Flash();
                }
                else
                  externalEvent.Raise();

                return 1; // Prevents activation now.
              }
            }
          }
          break;
          case 3: // HCBT_CREATEWND
          {
            if (IsOpen)
            {
              var createWnd = Marshal.PtrToStructure<User32.CBT_CREATEWND>(lParam);
              var createStruct = Marshal.PtrToStructure<User32.CREATESTRUCT>(createWnd.lpcs);

              if ((createStruct.style & 0x40000000/*WS_CHILD*/) == 0 && HasGate(createStruct.hwndParent, out var gate))
                gate.ExternalWindows.Add(wParam);
            }
          }
          break;
          case 4: // HCBT_DESTROYWND
          {
            if (HasGate(wParam, out var gate))
            {
              if (wParam == gate.Window.Handle) return 1;
              else gate.ExternalWindows.Remove(wParam);
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

    #region Events
    static readonly Dictionary<Delegate, (Delegate Target, int Count)> EventMap = new Dictionary<Delegate, (Delegate, int)>();

    internal static EventHandler<T> AddEventHandler<T>(EventHandler<T> handler)
    {
      lock (EventMap)
      {
        if (EventMap.TryGetValue(handler, out var trampoline)) trampoline.Count++;
        else trampoline.Target = new EventHandler<T>((s, a) => ActivationGate.Open(() => handler.Invoke(s, a), s));

        EventMap[handler] = trampoline;

        return (EventHandler<T>) trampoline.Target;
      }
    }

    internal static EventHandler<T> RemoveEventHandler<T>(EventHandler<T> handler)
    {
      lock (EventMap)
      {
        if (EventMap.TryGetValue(handler, out var trampoline))
        {
          if (trampoline.Count == 0) EventMap.Remove(handler);
          else
          {
            trampoline.Count--;
            EventMap[handler] = trampoline;
          }
        }

        return (EventHandler<T>) trampoline.Target;
      }
    }
    #endregion
  }
}


using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Microsoft.Win32.SafeHandles
{
  using System.ComponentModel;
  using InteropServices;

  #region Kernel32
  internal class LibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
  {
    internal LibraryHandle() : base(false) { }
    protected LibraryHandle(bool ownsHandle) : base(ownsHandle) { }
    public LibraryHandle(IntPtr hInstance) : base(false) => SetHandle(hInstance);

    [System.Security.SecurityCritical]
    override protected bool ReleaseHandle() => true;

    public static bool operator ==(LibraryHandle x, LibraryHandle y) => x.handle == y.handle;
    public static bool operator !=(LibraryHandle x, LibraryHandle y) => x.handle != y.handle;
    public override bool Equals(object obj) => obj is LibraryHandle value && value.handle == handle;
    public override int GetHashCode() => (int) handle;
    public override string ToString() => IsInvalid ? "Zero" : $"0x{(ulong) handle:x16}, \"{ModuleFileName}\"";

    public static LibraryHandle Zero = new LibraryHandle();

    public string ModuleFileName
    {
      get
      {
        var capacity = 0x80;
        var builder = default(StringBuilder);
        var copied = 0u;
        do
        {
          capacity *= 2;
          builder = new StringBuilder(capacity);
          copied = Kernel32.GetModuleFileName(this, builder, (uint) capacity);
        }
        while (copied == capacity - 1);

        return builder.ToString();
      }
    }
  }

  internal class SafeLibraryHandle : LibraryHandle
  {
    internal SafeLibraryHandle() : base(true) { }
    [System.Security.SecurityCritical]
    override protected bool ReleaseHandle() => Kernel32.FreeLibrary(this);
  }

  internal class ThreadHandle
  {
    public static uint CurrentThreadId => Kernel32.GetCurrentThreadId();
  }
  #endregion

  #region User32
  internal class WindowHandle : SafeHandle, System.Windows.Forms.IWin32Window
  {
    public static bool operator ==(WindowHandle x, WindowHandle y) => x.handle == y.handle;
    public static bool operator !=(WindowHandle x, WindowHandle y) => x.handle != y.handle;
    public override bool Equals(object obj) => obj is WindowHandle value && value.handle == handle;
    public override int GetHashCode() => (int) handle;
    public override string ToString() => IsInvalid ? "Zero" : $"0x{(ulong)handle:x16}, \"{Name}\"";

    private static void CheckWin32Error()
    {
      int error = Marshal.GetLastWin32Error();
      if (error != 0)
        throw new Win32Exception(error);
    }

    private WindowHandle() : this(false) { }
    protected WindowHandle(bool ownsHandle) : base(IntPtr.Zero, ownsHandle) { }

    public WindowHandle(IntPtr hWnd) : base(IntPtr.Zero, false) => SetHandle(hWnd);

    #region SafeHandle
    protected override bool ReleaseHandle() => true;
    public override bool IsInvalid => !User32.IsWindow(this);
    public bool IsZero => handle == IntPtr.Zero;
    #endregion

    public static WindowHandle Zero => new WindowHandle();
    public IntPtr Handle => handle;
    public string Name
    {
      get
      {
        var capacity = 0x80;
        var builder = default(StringBuilder);
        var copied = 0;
        do
        {
          capacity *= 2;
          builder = new StringBuilder(capacity);
          copied = User32.InternalGetWindowText(this, builder, capacity);
        }
        while (copied == capacity - 1);

        return builder.ToString();
      }
    }

    public int ThreadId => (int) User32.GetWindowThreadProcessId(this, IntPtr.Zero);

    public WindowHandle Owner => User32.GetWindow(this, 4 /*GW_OWNER*/);
    public WindowHandle ActivePopup => User32.GetWindow(this, 6 /*GW_ENABLEDPOPUP*/);

    public WindowHandle Parent => User32.GetParent(this);
    public bool Visible
    {
      //get => 0 != ((uint) WinUser.GetWindowLongPtr(this, -16 /*GWL_STYLE*/) & 0x10000000);
      get => User32.IsWindowVisible(this);
      set => User32.ShowWindow(this, value ? 8 /*SW_SHOWNA*/ : 0 /*SW_HIDE*/);
    }
    public bool Hide() => User32.ShowWindow(this, 0 /*SW_HIDE*/);
    public bool Show() => User32.ShowWindow(this, 8 /*SW_SHOWNA*/);
    public bool Enabled
    {
      get => User32.IsWindowEnabled(this);
      set => User32.EnableWindow(this, value);
    }

    public ProcessWindowStyle WindowStyle
    {
      get
      {
        if (!Visible)
          return ProcessWindowStyle.Hidden;

        if (User32.IsIconic(this))
          return ProcessWindowStyle.Minimized;

        if (User32.IsZoomed(this))
          return ProcessWindowStyle.Maximized;

        return ProcessWindowStyle.Normal;
      }

      set
      {
        if (WindowStyle != value)
        {
          switch (value)
          {
            case ProcessWindowStyle.Normal:
              User32.ShowWindow(this, 1 /*SW_SHOWNORMAL*/);
              break;
            case ProcessWindowStyle.Hidden:
              User32.ShowWindow(this, 0 /*SW_HIDE*/);
              break;
            case ProcessWindowStyle.Maximized:
              User32.ShowWindow(this, 3 /*SW_MAXIMIZE*/);
              break;
            case ProcessWindowStyle.Minimized:
              User32.ShowWindow(this, 6/*SW_MINIMIZE*/);
              break;
          }
        }
      }
    }

    public void Flash()
    {
      var fInfo = new User32.FLASHWINFO();
      {
        fInfo.cbSize = (uint) Marshal.SizeOf(fInfo);
        fInfo.hwnd = DangerousGetHandle();
        fInfo.dwFlags = 0x03;// FLASHW_ALL;
        fInfo.uCount = 8;
        fInfo.dwTimeout = 70;
      }

      User32.FlashWindowEx(ref fInfo);
    }

    public static WindowHandle ActiveWindow
    {
      get => User32.GetActiveWindow();
      set
      {
        User32.SetActiveWindow(value);
        CheckWin32Error();
      }
    }

    public bool BringToFront() => User32.BringWindowToTop(this);
    public bool ShowOwnedPopups() => User32.ShowOwnedPopups(this, true);
    public bool HideOwnedPopups() => User32.ShowOwnedPopups(this, false);
  }

  internal class SafeWindowHandle : WindowHandle
  {
    private SafeWindowHandle() : base(true) { }
    protected override bool ReleaseHandle() => User32.DestroyWindow(this);
  }

  internal class SafeHookHandle : SafeHandle
  {
    public override bool IsInvalid => handle == IntPtr.Zero;

    private SafeHookHandle() : base(IntPtr.Zero, true) { }

    protected override sealed bool ReleaseHandle() => User32.UnhookWindowsHookEx(this);
  }

  internal class Hook : IDisposable
  {
    SafeHookHandle hHook;
    readonly User32.HookProc hookProc = default;

    internal Hook(User32.HookType type)
    {
      hHook = User32.SetWindowsHookEx(type, hookProc = new User32.HookProc(InternalDispatchHook), LibraryHandle.Zero, Kernel32.GetCurrentThreadId());
    }

    public void Dispose() => hHook.Dispose();

    protected int InternalDispatchHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
      return (nCode < 0) ?
        User32.CallNextHookEx(hHook, nCode, wParam, lParam) :
        DispatchHook(nCode, wParam, lParam);
    }

    protected virtual int DispatchHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
      return User32.CallNextHookEx(hHook, nCode, wParam, lParam);
    }
  }

  internal class ComputerBasedTrainingHook : Hook
  {
    protected ComputerBasedTrainingHook() : base(User32.HookType.WH_CBT) { }
  }
  #endregion
}

namespace Microsoft.Win32.SafeHandles.InteropServices
{
  using DWORD     = UInt32;
  using UINT      = UInt32;

  using HINSTANCE = LibraryHandle;
  using HWND      = WindowHandle;
  using HHOOK     = SafeHookHandle;

  [SuppressUnmanagedCodeSecurity]
  internal static class Kernel32
  {
    internal const string KERNEL32 = "KERNEL32";

    [DllImport(KERNEL32, SetLastError = true)]
    public static extern DWORD GetCurrentThreadId();

    [DllImport(KERNEL32, SetLastError = true)]
    public static extern DWORD GetCurrentProcessId();

    [DllImport(KERNEL32, SetLastError = true)]
    public static extern SafeProcessHandle GetCurrentProcess();

    [DllImport(KERNEL32, SetLastError = true)]
    public static extern SafeLibraryHandle LoadLibraryEx(string lpLibFileName, IntPtr hFile, DWORD dwFlags);

    [DllImport(KERNEL32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr hLibModule);
    public static bool FreeLibrary(SafeLibraryHandle hLibModule) => FreeLibrary(hLibModule.DangerousGetHandle());

    [DllImport(KERNEL32, SetLastError = true)]
    public static extern DWORD GetModuleFileName(HINSTANCE hInstance, StringBuilder lpFilename, DWORD nSize);
  }

  [SuppressUnmanagedCodeSecurity]
  internal static class User32
  {
    internal const string USER32 = "USER32";

    #region Windows API
    [DllImport(USER32, SetLastError = true)]
    public static extern bool IsWindow(HWND hWnd);

    [DllImport(USER32, SetLastError = true)]
    public static extern DWORD GetWindowThreadProcessId(HWND hWnd, IntPtr lpdwProcessId);

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    public static bool DestroyWindow(SafeWindowHandle hWnd) => DestroyWindow(hWnd.Handle);

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowEnabled(HWND hWnd);

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(HWND hWnd);

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsZoomed(HWND hWnd);

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnableWindow(HWND hWnd, [MarshalAs(UnmanagedType.Bool)] bool bEnable);

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(HWND hWnd);

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(HWND hWnd, int nCmdShow);

    [DllImport(USER32, SetLastError = true)]
    public static extern IntPtr GetWindowLongPtr(HWND hWnd, int nIndex);

    [DllImport(USER32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int InternalGetWindowText(HWND hWnd, [Out] StringBuilder pString, int cchMaxCount);

    [DllImport(USER32, SetLastError = true)]
    public static extern HWND GetWindow(HWND hWnd, DWORD uCmd);

    [DllImport(USER32, SetLastError = true)]
    public static extern HWND GetParent(HWND hWnd);

    [DllImport(USER32, SetLastError = true)]
    public static extern HWND SetActiveWindow(HWND hWnd);

    [DllImport(USER32, SetLastError = true)]
    public static extern HWND GetActiveWindow();

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BringWindowToTop(HWND hWnd);

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ShowOwnedPopups(HWND hWnd, [MarshalAs(UnmanagedType.Bool)] bool fShow);

    [StructLayout(LayoutKind.Sequential)]
    internal struct FLASHWINFO
    {
      public UINT cbSize;
      public IntPtr hwnd;
      public DWORD dwFlags;
      public UINT uCount;
      public DWORD dwTimeout;
    }

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool FlashWindowEx(ref FLASHWINFO pwfi);
    #endregion

    #region Hooks API
    public enum HookType : int
    {
      WH_JOURNALRECORD = 0,
      WH_JOURNALPLAYBACK = 1,
      WH_KEYBOARD = 2,
      WH_GETMESSAGE = 3,
      WH_CALLWNDPROC = 4,
      WH_CBT = 5,
      WH_SYSMSGFILTER = 6,
      WH_MOUSE = 7,
      WH_HARDWARE = 8,
      WH_DEBUG = 9,
      WH_SHELL = 10,
      WH_FOREGROUNDIDLE = 11,
      WH_CALLWNDPROCRET = 12,
      WH_KEYBOARD_LL = 13,
      WH_MOUSE_LL = 14
    }

    public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport(USER32, SetLastError = true)]
    public static extern SafeHookHandle SetWindowsHookEx(HookType idHook, HookProc lpfn, HINSTANCE hInsdtsance, DWORD dwThreadId);

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhook);
    public static bool UnhookWindowsHookEx(SafeHookHandle hhook) => UnhookWindowsHookEx(hhook.DangerousGetHandle());

    [DllImport(USER32, SetLastError = true)]
    public static extern int CallNextHookEx(HHOOK hhook, int nCode, IntPtr wParam, IntPtr lParam);
    #endregion
  }
}

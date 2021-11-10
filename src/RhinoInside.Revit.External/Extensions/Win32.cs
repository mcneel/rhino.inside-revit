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
    protected override bool ReleaseHandle() => true;

    public static bool operator ==(LibraryHandle x, LibraryHandle y) => x.handle == y.handle;
    public static bool operator !=(LibraryHandle x, LibraryHandle y) => x.handle != y.handle;
    public override bool Equals(object obj) => obj is LibraryHandle value && value.handle == handle;
    public override int GetHashCode() => (int) handle;
    public override string ToString() => IsInvalid ? "Zero" : $"0x{(ulong) handle:x16}, \"{ModuleFileName}\"";

    public static LibraryHandle Zero = new LibraryHandle();

    public static LibraryHandle GetLoadedModule(string moduleName) => Kernel32.GetModuleHandle(moduleName);

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
    protected override bool ReleaseHandle() => Kernel32.FreeLibrary(this);
  }

  internal class ThreadHandle
  {
    public static uint CurrentThreadId => Kernel32.GetCurrentThreadId();
  }
  #endregion

  #region User32
  [Flags]
  enum ExtendedWindowStyles
  {
    ModalFrame      = 0x00000001,
    NoParentNotify  = 0x00000004,
    TopMost         = 0x00000008,
    MDIChild        = 0x00000040,
    ToolWindow      = 0x00000080,
    WindowEdge      = 0x00000100,
    ContextHelp     = 0x00000400,
    AcceptFiles     = 0x00000010,
    ControlParent   = 0x00010000,
    StaticEdge      = 0x00020000,
    AppWindow       = 0x00040000,
    Layered         = 0x00080000,
    Composited      = 0x02000000,
    NoActivate      = 0x08000000,
  }

  internal class WindowHandle : SafeHandle, System.Windows.Forms.IWin32Window
  {
    public static bool operator ==(WindowHandle x, WindowHandle y) => x.handle == y.handle;
    public static bool operator !=(WindowHandle x, WindowHandle y) => x.handle != y.handle;
    public override bool Equals(object obj) => obj is WindowHandle value && value.handle == handle;
    public override int GetHashCode() => (int) handle;
    public override string ToString() => IsInvalid ? "Zero" : $"0x{(ulong)handle:x16}, \"{Name}\"";

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

    public WindowHandle Parent
    {
      get => User32.GetParent(this);
      set => User32.SetParent(this, value);
    }

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

    public ExtendedWindowStyles ExtendedWindowStyles
    {
      get => (ExtendedWindowStyles) User32.GetWindowLongPtr(this, -20 /*GWL_EXSTYLE*/);
      set => User32.SetWindowLongPtr(this, -20 /*GWL_EXSTYLE*/, (IntPtr) value);
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
      set => User32.SetActiveWindow(value);
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

    protected sealed override bool ReleaseHandle() => User32.UnhookWindowsHookEx(this);
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
  using LONG      = Int32;
  using DWORD     = UInt32;
  using UINT      = UInt32;

  using HMODULE   = LibraryHandle;
  using HINSTANCE = LibraryHandle;
  using HWND      = WindowHandle;
  using HHOOK     = SafeHookHandle;
  using HMENU     = IntPtr;

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

    [DllImport(KERNEL32, SetLastError = true)]
    public static extern HMODULE GetModuleHandle(string lpModuleName);
  }

  [SuppressUnmanagedCodeSecurity]
  internal static class User32
  {
    internal const string USER32 = "USER32";

    #region Windows API
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct CREATESTRUCT
    {
      public IntPtr lpCreateParams;
      public IntPtr hInstance;
      public IntPtr hMenu;
      public IntPtr hwndParent;
      public int cy;
      public int cx;
      public int y;
      public int x;
      public LONG style;
      public IntPtr lpszName;
      public IntPtr lpszClass;
      public DWORD dwExStyle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SIZE
    {
      public int cx;
      public int cy;

      public SIZE(int x, int y)
      {
        cx = x;
        cy = y;
      }
    }

    [DllImport(USER32, SetLastError = true)]
    public static extern bool IsWindow(HWND hWnd);

    [DllImport(USER32, SetLastError = true)]
    public static extern DWORD GetWindowThreadProcessId(HWND hWnd, IntPtr lpdwProcessId);

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    public static bool DestroyWindow(SafeWindowHandle hWnd) => DestroyWindow(hWnd.Handle);

    [DllImport(USER32, SetLastError = true)]
    public static extern IntPtr SetWindowLongPtr(HWND hWnd, int nIndex, IntPtr dwNewLong);

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
    public static extern HWND SetParent(HWND hWnd, HWND hWndNewParent);

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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct CBT_CREATEWND
    {
      public IntPtr lpcs;
      public IntPtr hwndInsertAfter;
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

  [SuppressUnmanagedCodeSecurity]
  internal static class Gdi32
  {
    internal const string GDI32 = "GDI32";

    [DllImport(GDI32, EntryPoint = "DeleteObject")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteObject([In] IntPtr hObject);

  }

  [SuppressUnmanagedCodeSecurity]
  internal static class Shell32
  {
    const string SHELL32 = "SHELL32";

    [Flags]
    public enum SIIGBF
    {
      SIIGBF_RESIZETOFIT = 00,
      SIIGBF_BIGGERSIZEOK = 01,
      SIIGBF_MEMORYONLY = 02,
      SIIGBF_ICONONLY = 04,
      SIIGBF_THUMBNAILONLY = 08,
      SIIGBF_INCACHEONLY = 10
    }

    [ComImport()]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellItemImageFactory
    {
      void GetImage
      (
        [In, MarshalAs(UnmanagedType.Struct)] User32.SIZE size,
        [In] SIIGBF flags,
        [Out] out IntPtr phbm
      );
    }

    [DllImport(SHELL32, CharSet = CharSet.Unicode, PreserveSig = false)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public static extern IShellItemImageFactory SHCreateItemFromParsingName
    (
     [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
     [In] IntPtr pbc,
     [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid
    );

  }

}

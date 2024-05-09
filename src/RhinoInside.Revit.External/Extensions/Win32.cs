using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;

namespace Microsoft.Win32.SafeHandles
{
  using InteropServices;

  #region Kernel32
  internal sealed class LibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
  {
    private LibraryHandle() : base(false) { }
    public LibraryHandle(string fileName) : base(true) => SetHandle(Kernel32.LoadLibrary(fileName));

    [SecurityCritical]
    [ResourceExposure(ResourceScope.Process), ResourceConsumption(ResourceScope.Process)]
    protected override bool ReleaseHandle() => Kernel32.FreeLibrary(handle);

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

  internal class ThreadHandle
  {
    public static uint CurrentThreadId => Kernel32.GetCurrentThreadId();
  }
  #endregion

  #region User32

  enum DialogResult
  {
    IDOK = 1,
    IDCANCEL = 2,
    IDABORT = 3,
    IDRETRY = 4,
    IDIGNORE = 5,
    IDYES = 6,
    IDNO = 7,
    IDCLOSE = 8,
    IDHELP = 9,
    IDTRYAGAIN = 10,
    IDCONTINUE = 11,
  };

  [Flags]
  enum ExtendedWindowStyles
  {
    ModalFrame = 0x00000001,
    NoParentNotify = 0x00000004,
    TopMost = 0x00000008,
    MDIChild = 0x00000040,
    ToolWindow = 0x00000080,
    WindowEdge = 0x00000100,
    ContextHelp = 0x00000400,
    AcceptFiles = 0x00000010,
    ControlParent = 0x00010000,
    StaticEdge = 0x00020000,
    AppWindow = 0x00040000,
    Layered = 0x00080000,
    Composited = 0x02000000,
    NoActivate = 0x08000000,
  }

  [Flags]
  enum SetWindowPosFlags
  {
    SWP_NOSIZE          = 0x0001,
    SWP_NOMOVE          = 0x0002,
    SWP_NOZORDER        = 0x0004,
    SWP_NOREDRAW        = 0x0008,
    SWP_NOACTIVATE      = 0x0010,
    SWP_FRAMECHANGED    = 0x0020,
    SWP_SHOWWINDOW      = 0x0040,
    SWP_HIDEWINDOW      = 0x0080,
    SWP_NOCOPYBITS      = 0x0100,
    SWP_NOOWNERZORDER   = 0x0200,
    SWP_NOSENDCHANGING  = 0x0400,
    SWP_DRAWFRAME       = SWP_FRAMECHANGED,
    SWP_NOREPOSITION    = SWP_NOOWNERZORDER,


    SWP_DEFERERASE      = 0x2000,
    SWP_ASYNCWINDOWPOS  = 0x4000,
  }

  internal class WindowHandle : SafeHandle, System.Windows.Forms.IWin32Window
  {
    public static bool operator ==(WindowHandle x, WindowHandle y) => x.handle == y.handle;
    public static bool operator !=(WindowHandle x, WindowHandle y) => x.handle != y.handle;
    public override bool Equals(object obj) => obj is WindowHandle value && value.handle == handle;
    public override int GetHashCode() => (int) handle;
    public override string ToString() => IsInvalid ? "Zero" : $"0x{(ulong)handle:x16}, \"{Name}\"";

    private WindowHandle() : base(IntPtr.Zero, ownsHandle: false) { }
    protected WindowHandle(IntPtr handle, bool ownsHandle) : base(IntPtr.Zero, ownsHandle) => SetHandle(handle);

    public static explicit operator WindowHandle(IntPtr handle) => new WindowHandle(handle, ownsHandle: false);

    #region SafeHandle
    [SecurityCritical]
    [ResourceExposure(ResourceScope.Machine), ResourceConsumption(ResourceScope.Machine)]
    protected override bool ReleaseHandle() => User32.DestroyWindow(handle);
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

    public WindowHandle Owner
    {
      get => User32.GetWindow(this, 4 /*GW_OWNER*/);
      set => User32.SetWindowLongPtr(this, -8 /*GWL_HWNDPARENT*/, value?.handle ?? IntPtr.Zero);
    }
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
    public bool Minimize(bool minimize) => minimize ? User32.CloseWindow(this) : User32.OpenIcon(this);
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

    public bool TryClose() => User32.PostMessage(this, 0x0010 /*WM_CLOSE*/, IntPtr.Zero, IntPtr.Zero);

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

    public System.Drawing.Rectangle Bounds
    {
      get
      {
        var rect = new User32.RECT(0, 0, 0, 0);
        return User32.GetWindowRect(this, ref rect) ?
          new System.Drawing.Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top):
          System.Drawing.Rectangle.Empty;
      }
      set
      {
        const uint uFlags = (uint) SetWindowPosFlags.SWP_NOZORDER;
        User32.SetWindowPos(this, Zero, value.Left, value.Top, value.Width, value.Height, uFlags);
      }
    }

    public System.Drawing.Rectangle ClientRectangle
    {
      get
      {
        var rect = new User32.RECT(0, 0, 0, 0);
        return User32.GetClientRect(this, ref rect) ?
          new System.Drawing.Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top) :
          System.Drawing.Rectangle.Empty;
      }
    }

    public System.Drawing.Size ClientSize
    {
      get
      {
        var rect = new User32.RECT(0, 0, 0, 0);
        return User32.GetClientRect(this, ref rect) ?
          new System.Drawing.Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top).Size :
          System.Drawing.Size.Empty;
      }
      set
      {
        var bounds = Bounds;
        var client = ClientRectangle;

        const uint uFlags = (uint) (SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOMOVE);
        User32.SetWindowPos(this, Zero, 0, 0, value.Width + (bounds.Width - client.Width), value.Height + (bounds.Height - client.Height), uFlags);
      }
    }
  }

  internal class SafeHookHandle : SafeHandle
  {
    public override bool IsInvalid => handle == IntPtr.Zero;

    private SafeHookHandle() : base(IntPtr.Zero, ownsHandle: true) { }

    [SecurityCritical]
    [ResourceExposure(ResourceScope.Process), ResourceConsumption(ResourceScope.Process)]
    protected sealed override bool ReleaseHandle() => User32.UnhookWindowsHookEx(handle);
  }

  internal class Hook : IDisposable
  {
    readonly SafeHookHandle hHook;
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
#pragma warning disable CA1060 // Move pinvokes to native methods class

  using LONG      = Int32;
  using DWORD     = UInt32;
  using UINT      = UInt32;

  using HANDLE    = SafeHandle;
  using HMODULE   = LibraryHandle;
  using HINSTANCE = LibraryHandle;
  using HDC       = IntPtr;
  using HWND      = WindowHandle;
  using HHOOK     = SafeHookHandle;
  using HMENU     = IntPtr;

  using WPARAM    = IntPtr;
  using LPARAM    = IntPtr;

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

    [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibraryEx(string lpLibFileName, IntPtr hFile, DWORD dwFlags);

    [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibrary(string lpLibFileName);

    [DllImport(KERNEL32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FreeLibrary(IntPtr hLibModule);

    [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern DWORD GetModuleFileName(HINSTANCE hInstance, StringBuilder lpFilename, DWORD nSize);

    [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern HMODULE GetModuleHandle(string lpModuleName);

    [DllImport(KERNEL32, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool WritePrivateProfileString(string Section, string Key, string Value, string FileName);

    [DllImport(KERNEL32, CharSet = CharSet.Unicode)]
    public static extern DWORD GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, DWORD Size, string FileName);
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

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
      public int left;
      public int top;
      public int right;
      public int bottom;

      public RECT(int l, int t, int r, int b)
      {
        left = l;
        top = t;
        right = r;
        bottom = b;
      }
    }

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport(USER32, SetLastError = true)]
    public static extern DWORD GetWindowThreadProcessId(HWND hWnd, IntPtr lpdwProcessId);

    [DllImport(USER32, SetLastError = true)]
    public static extern IntPtr SetWindowLongPtr(HWND hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport(USER32, SetLastError = true)]
    public static extern bool IsWindow(HWND hWnd);

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
    public static extern bool CloseWindow(HWND hWnd);

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool OpenIcon(HWND hWnd);

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

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetWindowRect(HWND hWnd, [MarshalAs(UnmanagedType.Struct)] ref RECT lpRect);

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetClientRect(HWND hWnd, [MarshalAs(UnmanagedType.Struct)] ref RECT lpRect);

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowPos(HWND hWnd, HWND hWndInsertAfter, int X, int Y, int cX, int cY, uint uFlags);

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


    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PostMessage(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);
    #endregion

    #region GDI
    [DllImport(USER32)]
    public static extern HDC GetDC(HWND hWnd);

    [DllImport(USER32)]
    public static extern int ReleaseDC(HWND hWnd, HDC hDC);
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
    public static extern HHOOK SetWindowsHookEx(HookType idHook, HookProc lpfn, HINSTANCE hInsdtsance, DWORD dwThreadId);

    [DllImport(USER32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhook);

    [DllImport(USER32, SetLastError = true)]
    public static extern int CallNextHookEx(HHOOK hhook, int nCode, IntPtr wParam, IntPtr lParam);
    #endregion
  }

  [SuppressUnmanagedCodeSecurity]
  internal static class Gdi32
  {
    internal const string GDI32 = "GDI32";

    [DllImport(GDI32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteObject([In] IntPtr hObject);

    [DllImport(GDI32)]
    public static extern int GetDeviceCaps(HDC hdc, int index);
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

#pragma warning restore CA1060 // Move pinvokes to native methods class
}

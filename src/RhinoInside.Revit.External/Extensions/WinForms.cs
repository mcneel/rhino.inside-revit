namespace System.Windows.Forms.Interop
{
  static class FormExtension
  {
    class OwnerWindow : IWin32Window
    {
      readonly IntPtr handle;
      public OwnerWindow(IntPtr hWnd) { handle = hWnd; }

      IntPtr IWin32Window.Handle => handle;
    }

    internal static void Show(this Form form, IntPtr hOwnerWnd) =>
      form.Show(new OwnerWindow(hOwnerWnd));

    internal static DialogResult ShowDialog(this Form form, IntPtr hOwnerWnd) =>
      form.ShowDialog(new OwnerWindow(hOwnerWnd));

    internal static DialogResult ShowDialog(this CommonDialog form, IntPtr hOwnerWnd) =>
      form.ShowDialog(new OwnerWindow(hOwnerWnd));
  }
}

namespace System.Windows.Interop
{
  static class WindowExtension
  {
    internal static void Show(this Window form, IntPtr hOwnerWnd)
    {
      var interop = new Interop.WindowInteropHelper(form) { Owner = hOwnerWnd };
      try { form.Show(); }
      finally { GC.KeepAlive(interop); }
    }

    internal static bool? ShowDialog(this Window form, IntPtr hOwnerWnd)
    {
      var interop = new Interop.WindowInteropHelper(form) { Owner = hOwnerWnd };
      try { return form.ShowDialog(); }
      finally { GC.KeepAlive(interop); }
    }

    internal static bool? ShowDialog(this Microsoft.Win32.CommonDialog form, IntPtr hOwnerWnd)
    {
      var source = Interop.HwndSource.FromHwnd(hOwnerWnd);
      return form.ShowDialog(source.RootVisual as Window);
    }
  }
}

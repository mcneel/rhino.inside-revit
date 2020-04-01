namespace System.Windows.Forms.InteropExtension
{
  public static class FormExtension
  {
    class OwnerWindow : IWin32Window
    {
      readonly IntPtr handle;
      public OwnerWindow(IntPtr hWnd) { handle = hWnd; }

      IntPtr IWin32Window.Handle => handle;
    }

    public static void Show(this Form form, IntPtr hOwnerWnd) =>
      form.Show(new OwnerWindow(hOwnerWnd));

    public static DialogResult ShowDialog(this Form form, IntPtr hOwnerWnd) =>
      form.ShowDialog(new OwnerWindow(hOwnerWnd));

    public static DialogResult ShowDialog(this CommonDialog form, IntPtr hOwnerWnd) =>
      form.ShowDialog(new OwnerWindow(hOwnerWnd));
  }
}

namespace System.Windows.InteropExtension
{
  public static class WindowExtension
  {
    public static void Show(this Window form, IntPtr hOwnerWnd)
    {
      var iform = new Interop.WindowInteropHelper(form);
      iform.Owner = hOwnerWnd;
      form.Show();
    }

    public static bool? ShowDialog(this Window form, IntPtr hOwnerWnd)
    {
      var iform = new Interop.WindowInteropHelper(form);
      iform.Owner = hOwnerWnd;
      return form.ShowDialog();
    }

    public static bool? ShowDialog(this Microsoft.Win32.CommonDialog form, IntPtr hOwnerWnd)
    {
      var source = Interop.HwndSource.FromHwnd(hOwnerWnd);
      return form.ShowDialog(source.RootVisual as Window);
    }
  }
}

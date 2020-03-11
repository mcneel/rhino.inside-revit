using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace RhinoInside.Revit.GH.Extensions
{
  static class ComboBoxExtension
  {
    [DllImport("USER32", CharSet = CharSet.Unicode, SetLastError = false)]
    [System.Security.SecurityCritical]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, [In, MarshalAs(UnmanagedType.LPWStr)] string lParam);

    public static void SetCueBanner(this ComboBox cb, string cueBannerText)
    {
      if (System.Environment.OSVersion.Version.Major < 6)
        throw new PlatformNotSupportedException();

      SendMessage(cb.Handle, /*CB_SETCUEBANNER*/ 0x1703, IntPtr.Zero, cueBannerText);
      cb.Invalidate();
    }
  }
}

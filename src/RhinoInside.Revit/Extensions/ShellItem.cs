using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.Win32.SafeHandles.InteropServices;

namespace Eto.Wpf.IO
{
  static class ShellItem
  {
    public static bool TryGetImage(string fileName, out Eto.Drawing.Bitmap bitmap) =>
      TryGetImage(fileName, new Eto.Drawing.Size(512, 512), out bitmap);

    public static bool TryGetImage(string fileName, Eto.Drawing.Size size, out Eto.Drawing.Bitmap bitmap)
    {
      try
      {
        if
        (
          System.IO.File.Exists(fileName) &&
          Microsoft.Win32.SafeHandles.InteropServices.Shell32.SHCreateItemFromParsingName
          (
            fileName,
            IntPtr.Zero,
            typeof(Microsoft.Win32.SafeHandles.InteropServices.Shell32.IShellItemImageFactory).GUID
          ) is Microsoft.Win32.SafeHandles.InteropServices.Shell32.IShellItemImageFactory imageFactory
        )
        {
          imageFactory.GetImage
          (
            new Microsoft.Win32.SafeHandles.InteropServices.User32.SIZE
            (
              size.Width,
              size.Height
            ),
            Microsoft.Win32.SafeHandles.InteropServices.Shell32.SIIGBF.SIIGBF_RESIZETOFIT |
            Microsoft.Win32.SafeHandles.InteropServices.Shell32.SIIGBF.SIIGBF_THUMBNAILONLY,
            out var hBitmap
          );

          Marshal.FinalReleaseComObject(imageFactory);
          if (hBitmap != IntPtr.Zero)
          {
            var wpfImage = Imaging.CreateBitmapSourceFromHBitmap
            (
              hBitmap,
              IntPtr.Zero,
              System.Windows.Int32Rect.Empty,
              System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions()
            );

            bitmap = new Eto.Drawing.Bitmap(new Drawing.BitmapHandler(wpfImage));
            Gdi32.DeleteObject(hBitmap);

            return true;
          }
        }
      }
      catch { }

      bitmap = default;
      return false;
    }
  }
}

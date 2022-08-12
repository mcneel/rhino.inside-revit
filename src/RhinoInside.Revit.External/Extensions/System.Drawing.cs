using System;
using System.IO;
using SD = System.Drawing;
using SWM = System.Windows.Media;

namespace System.Drawing.Interop
{
  internal static class ColorExtension
  {
    public static SD.Color ToDrawingColor(this SWM.Color color)
    {
      return (color.A == 0 && color.R == 0 && color.G == 0 && color.B == 0) ?
        SD.Color.Empty :
        SD.Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    public static SWM.Color ToMediaColor(this SD.Color color)
    {
      return SWM.Color.FromArgb(color.A, color.R, color.G, color.B);
    }
  }

  internal static class ImagingExtension
  {
    public static SWM.PixelFormat ToMediaPixelFormat(this SD.Imaging.PixelFormat pixelFormat)
    {
      switch (pixelFormat)
      {
        case System.Drawing.Imaging.PixelFormat.Format16bppGrayScale:
          return System.Windows.Media.PixelFormats.Gray16;
        case System.Drawing.Imaging.PixelFormat.Format16bppRgb555:
          return System.Windows.Media.PixelFormats.Bgr555;
        case System.Drawing.Imaging.PixelFormat.Format16bppRgb565:
          return System.Windows.Media.PixelFormats.Bgr565;

        case System.Drawing.Imaging.PixelFormat.Indexed:
          return System.Windows.Media.PixelFormats.Bgr101010;
        case System.Drawing.Imaging.PixelFormat.Format1bppIndexed:
          return System.Windows.Media.PixelFormats.Indexed1;
        case System.Drawing.Imaging.PixelFormat.Format4bppIndexed:
          return System.Windows.Media.PixelFormats.Indexed4;
        case System.Drawing.Imaging.PixelFormat.Format8bppIndexed:
          return System.Windows.Media.PixelFormats.Indexed8;

        case System.Drawing.Imaging.PixelFormat.Format16bppArgb1555:
          return System.Windows.Media.PixelFormats.Bgr555;

        case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
          return System.Windows.Media.PixelFormats.Bgr24;

        case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
          return System.Windows.Media.PixelFormats.Bgra32;
        case System.Drawing.Imaging.PixelFormat.Format32bppPArgb:
          return System.Windows.Media.PixelFormats.Pbgra32;
        case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
          return System.Windows.Media.PixelFormats.Bgr32;

        case System.Drawing.Imaging.PixelFormat.Format48bppRgb:
          return System.Windows.Media.PixelFormats.Rgb48;

        case System.Drawing.Imaging.PixelFormat.Format64bppArgb:
          return System.Windows.Media.PixelFormats.Prgba64;
      }

      throw new NotSupportedException();
    }

    public static SWM.Imaging.BitmapSource ToBitmapSource(this SD.Icon icon, bool small = false)
    {
      using (var bitmap = icon.ToBitmap())
      {
        var bitmapData = bitmap.LockBits
        (
          new SD.Rectangle(0, 0, bitmap.Width, bitmap.Height),
          SD.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat
        );

        var mediaPixelFormat = bitmap.PixelFormat.ToMediaPixelFormat();
        var bitmapSource = SWM.Imaging.BitmapSource.Create
        (
          bitmapData.Width, bitmapData.Height,
          small ? bitmap.HorizontalResolution * 2 : bitmap.HorizontalResolution,
          small ? bitmap.VerticalResolution * 2 : bitmap.VerticalResolution,
          mediaPixelFormat, null,
          bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride
        );

        bitmap.UnlockBits(bitmapData);
        return bitmapSource;
      }
    }

    /// <summary>
    /// Converts a <see cref="System.Drawing.Bitmap"/> into a <see cref="System.Windows.Media.Imaging.BitmapImage"/>.
    /// </summary>
    /// <param name="bitmap"></param>
    /// <param name="width">The width of the bitmap in device-independent units (1/96th inch per unit).</param>
    /// <param name="height">The height of the bitmap in device-independent units (1/96th inch per unit).</param>
    /// <returns></returns>
    public static SWM.Imaging.BitmapImage ToBitmapImage(this SD.Bitmap bitmap, double width = 0, double height = 0)
    {
      using (var memory = new MemoryStream())
      {
        bitmap.Save(memory, SD.Imaging.ImageFormat.Png);
        memory.Position = 0;

        var bitmapImage = new SWM.Imaging.BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memory;
        bitmapImage.CacheOption = SWM.Imaging.BitmapCacheOption.OnLoad;
        bitmapImage.DecodePixelWidth  = (int) Math.Round(width  * bitmap.HorizontalResolution / 96.0);
        bitmapImage.DecodePixelHeight = (int) Math.Round(height * bitmap.HorizontalResolution / 96.0);
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
      }
    }
  }
}

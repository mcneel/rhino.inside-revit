using System;
using System.IO;
using System.Reflection;
using Drawing = System.Drawing;
using Media = System.Windows.Media;

namespace RhinoInside.Revit
{
  /*internal*/ public static class ImageBuilder
  {
    #region System.Drawing
    public static Drawing.Color ToDrawingColor(this Media.Color color)
    {
      return (color.A == 0 && color.R == 0 && color.G == 0 && color.B == 0) ?
        Drawing.Color.Empty :
        Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    static void DrawIconTag(Drawing.Graphics g, Drawing.Brush textBrush, string tag, int width = 24, int height = 24)
    {
      g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
      g.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
      g.PixelOffsetMode = Drawing.Drawing2D.PixelOffsetMode.HighQuality;

      var format = new Drawing.StringFormat()
      {
        Alignment = Drawing.StringAlignment.Center,
        LineAlignment = Drawing.StringAlignment.Center,
        Trimming = Drawing.StringTrimming.Character,
        FormatFlags = Drawing.StringFormatFlags.NoWrap
      };

      float emSize = ((float) (width) / ((float) tag.Length));
      if (width == 24)
      {
        switch (tag.Length)
        {
          case 1: emSize = 17.0f; break;
          case 2: emSize = 13.0f; break;
          case 3: emSize = 10.4f; break;
          case 4: emSize = 8.0f; break;
          default: emSize = 7.0f; break;
        }
      }

      // Avoid using ClearType rendering on icons that the user can zoom in like icons on Grashopper components.
      g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit;

      var rect = new Drawing.RectangleF(0.2f, 1.3f, width, height);
      using (var Calibri = new Drawing.Font("Calibri", emSize, Drawing.GraphicsUnit.Pixel))
        g.DrawString(tag, Calibri, textBrush, rect, format);
    }

    public static Drawing.Bitmap BuildIcon(string tag, int width = 24, int height = 24)
    {
      var bitmap = new Drawing.Bitmap(width, height);
      using (var g = Drawing.Graphics.FromImage(bitmap))
      {
        var iconBounds = new Drawing.RectangleF(0, 0, width, height);
        iconBounds.Inflate(-0.5f, -0.5f);

        using (var capsule = Grasshopper.GUI.Canvas.GH_Capsule.CreateCapsule(iconBounds, Grasshopper.GUI.Canvas.GH_Palette.Transparent))
          capsule.Render(g, false, false, false);

        DrawIconTag(g, Drawing.Brushes.Black, tag, width, height);
      }

      return bitmap;
    }

    public static Drawing.Bitmap BuildIcon(string tag, Guid componentId, int width = 24, int height = 24)
    {
      var bitmap = new Drawing.Bitmap(width, height);
      using (var g = Drawing.Graphics.FromImage(bitmap))
      {
        var textBrush = Drawing.Brushes.Black;
        if (Grasshopper.Instances.ComponentServer.EmitObjectIcon(componentId) is Drawing.Bitmap icon)
        {
          textBrush = Drawing.Brushes.White;
          g.DrawImage(icon, Drawing.Point.Empty);
        }
        else
        {
          var iconBounds = new Drawing.RectangleF(0, 0, width, height);
          iconBounds.Inflate(-0.5f, -0.5f);

          using (var capsule = Grasshopper.GUI.Canvas.GH_Capsule.CreateCapsule(iconBounds, Grasshopper.GUI.Canvas.GH_Palette.Transparent))
            capsule.Render(g, false, false, false);
        }

        DrawIconTag(g, textBrush, tag, width, height);
      }

      return bitmap;
    }

    public static Drawing.Bitmap BuildIcon(string tag, Drawing.Bitmap baseBitmap, int width = 24, int height = 24)
    {
      var bitmap = new Drawing.Bitmap(width, height);
      using (var g = Drawing.Graphics.FromImage(bitmap))
      {
        g.DrawImage(baseBitmap, new Drawing.Rectangle(0, 0, width, height));

        DrawIconTag(g, Drawing.Brushes.Black, tag, width, height);
      }

      return bitmap;
    }

    public static Drawing.Bitmap BuildImage(string tag, int width, int height, Drawing.Color color)
    {
      var bitmap = new Drawing.Bitmap(width, height);
      using (var g = Drawing.Graphics.FromImage(bitmap))
      {
        g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        var rect = new Drawing.RectangleF(0.5f, 1.0f, width, height);

        var format = new Drawing.StringFormat()
        {
          Alignment = Drawing.StringAlignment.Center,
          LineAlignment = Drawing.StringAlignment.Center
        };

        if (color.IsEmpty)
          g.FillEllipse(Drawing.Brushes.Black, 1.0f, 1.0f, width - 2.0f, height - 2.0f);
        else using (var brush = new Drawing.SolidBrush(color))
            g.FillEllipse(brush, 1.0f, 1.0f, width - 2.0f, height - 2.0f);

        float emSize = ((float) (width) / ((float) tag.Length));
        if (width == 24)
        {
          switch (tag.Length)
          {
            case 1: emSize = 20.0f; break;
            case 2: emSize = 13.0f; break;
            case 3: emSize = 11.0f; break;
            case 4: emSize = 8.0f; break;
          }
        }

        // Avoid using ClearType rendering on icons that the user can zoom in like icons on Grashopper components.
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        using (var Calibri = new System.Drawing.Font("Calibri", emSize, Drawing.GraphicsUnit.Pixel))
          g.DrawString(tag, Calibri, Drawing.Brushes.White, rect, format);
      }

      return bitmap;
    }
    #endregion

    #region System.Windows.Media
    public static Media.Color ToMediaColor(this Drawing.Color color)
    {
      return Media.Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    // FIXME: find a way to detect the scaling on Revit.RevitScreen
    public static double GetRevitScreenScaleFactor() => 1;

    static internal Media.Imaging.BitmapSource LoadRibbonButtonImage(string name, bool small = false)
    {
      const uint defaultDPI = 96;
      int desiredSize = small ? 16 : 32;
      var adjustedIconSize = desiredSize * 2;
      var adjustedDPI = defaultDPI * 2;
      var screenScale = GetRevitScreenScaleFactor();

      string specificSizeName = name.Replace(".png", $"_{desiredSize}.png");
      // if screen has no scaling and a specific size is provided, use that
      // otherwise rebuild icon for size and screen scale
      using (var resource = (screenScale == 1 ? Assembly.GetExecutingAssembly().GetManifestResourceStream($"RhinoInside.Revit.Resources.{specificSizeName}") : null)
                            ?? Assembly.GetExecutingAssembly().GetManifestResourceStream($"RhinoInside.Revit.Resources.{name}"))
      {
        var baseImage = new Media.Imaging.BitmapImage();
        baseImage.BeginInit();
        baseImage.StreamSource = resource;
        baseImage.DecodePixelHeight = System.Convert.ToInt32(adjustedIconSize * screenScale);
        baseImage.EndInit();
        resource.Seek(0, SeekOrigin.Begin);

        var imageWidth = baseImage.PixelWidth;
        var imageFormat = baseImage.Format;
        var imageBytePerPixel = baseImage.Format.BitsPerPixel / 8;
        var palette = baseImage.Palette;

        var stride = imageWidth * imageBytePerPixel;
        var arraySize = stride * imageWidth;
        var imageData = Array.CreateInstance(typeof(byte), arraySize);
        baseImage.CopyPixels(imageData, stride, 0);

        var imageDim = System.Convert.ToInt32(adjustedIconSize * screenScale);
        return Media.Imaging.BitmapSource.Create(
          imageDim,
          imageDim,
          adjustedDPI * screenScale,
          adjustedDPI * screenScale,
          imageFormat,
          palette,
          imageData,
          stride
        );
      }
    }

    public static Media.PixelFormat ToMediaPixelFormat(this Drawing.Imaging.PixelFormat pixelFormat)
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
          return System.Windows.Media.PixelFormats.Bgr32;
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

    public static Media.Imaging.BitmapSource ToBitmapSource(this Drawing.Icon icon, bool small = false)
    {
      using (var bitmap = icon.ToBitmap())
      {
        var bitmapData = bitmap.LockBits
        (
          new Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
          Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat
        );

        var mediaPixelFormat = bitmap.PixelFormat.ToMediaPixelFormat();
        var bitmapSource = Media.Imaging.BitmapSource.Create
        (
          bitmapData.Width, bitmapData.Height,
          small ? bitmap.HorizontalResolution * 2 : bitmap.HorizontalResolution,
          small ? bitmap.VerticalResolution   * 2 : bitmap.VerticalResolution,
          mediaPixelFormat, null,
          bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride
        );

        bitmap.UnlockBits(bitmapData);
        return bitmapSource;
      }
    }

    public static Media.Imaging.BitmapImage ToBitmapImage(this Drawing.Bitmap bitmap, int PixelWidth = 0, int PixelHeight = 0)
    {
      using (var memory = new MemoryStream())
      {
        bitmap.Save(memory, Drawing.Imaging.ImageFormat.Png);
        memory.Position = 0;

        var bitmapImage = new Media.Imaging.BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memory;
        bitmapImage.CacheOption = Media.Imaging.BitmapCacheOption.OnLoad;
        bitmapImage.DecodePixelWidth = PixelWidth;
        bitmapImage.DecodePixelHeight = PixelHeight;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
      }
    }

    public static Media.ImageSource BuildImage(string tag, Media.Color color = default)
    {
      using (var g = Drawing.Graphics.FromHwnd(Revit.MainWindowHandle))
      {
        int pixelX = (int) Math.Round((g.DpiX / 96.0) * 16);
        int pixelY = (int) Math.Round((g.DpiY / 96.0) * 16);
        return BuildImage(tag, 64, 64, color.ToDrawingColor()).ToBitmapImage(pixelX, pixelY);
      }
    }

    public static Media.ImageSource BuildLargeImage(string tag, Media.Color color = default)
    {
      using (var g = Drawing.Graphics.FromHwnd(Revit.MainWindowHandle))
      {
        int pixelX = (int) Math.Round((g.DpiX / 96.0) * 32);
        int pixelY = (int) Math.Round((g.DpiY / 96.0) * 32);
        return BuildImage(tag, 64, 64, color.ToDrawingColor()).ToBitmapImage(pixelX, pixelY);
      }
    }
    #endregion
  }
}

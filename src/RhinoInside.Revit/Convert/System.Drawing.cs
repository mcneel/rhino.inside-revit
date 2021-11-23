using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.System.Drawing
{
  using global::System.Drawing;

  public static class ColorConverter
  {
    public static Color ToColor(this ARDB.Color c)
    {
      return c.IsValid ?
             Color.FromArgb(0xFF, c.Red, c.Green, c.Blue) :
             Color.FromArgb(0, 0, 0, 0);
    }

    public static ARDB.Color ToColor(this Color c)
    {
      return c.ToArgb() == 0 ?
             ARDB.Color.InvalidColorValue :
             new ARDB.Color(c.R, c.G, c.B);
    }
  }

  public static class ColorWithTransparencyConverter
  {
    public static Color ToColor(this ARDB.ColorWithTransparency c)
    {
      return c.IsValidObject ?
             Color.FromArgb(0xFF - (int) c.GetTransparency(), (int) c.GetRed(), (int) c.GetGreen(), (int) c.GetBlue()) :
             Color.FromArgb(0, 0, 0, 0);
    }

    public static ARDB.ColorWithTransparency ToColorWithTransparency(this Color c)
    {
      return new ARDB.ColorWithTransparency(c.R, c.G, c.B, 0xFFu - c.A);
    }
  }

  public static class RectangleConverter
  {
    public static Rectangle ToRectangle(this ARDB.Rectangle value)
    {
      return new Rectangle(value.Left, value.Top, value.Right - value.Left, value.Bottom - value.Top);
    }

    public static ARDB.Rectangle ToRectangle(this Rectangle value)
    {
      return new ARDB.Rectangle(value.Left, value.Top, value.Right, value.Bottom);
    }
  }
}

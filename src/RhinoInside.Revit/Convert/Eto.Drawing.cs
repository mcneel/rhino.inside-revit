using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Eto.Drawing
{
  using global::Eto.Drawing;

  public static class ColorConverter
  {
    public static Color ToColor(this ARDB.Color c)
    {
      return c.IsValid ?
             Color.FromArgb(c.Red, c.Green, c.Blue, 0xFF) :
             Color.FromArgb(0, 0, 0, 0);
    }

    public static ARDB::Color ToColor(this Color c)
    {
      return c.ToArgb() == 0 ?
        ARDB::Color.InvalidColorValue :
        new ARDB::Color((byte) c.Rb, (byte) c.Gb, (byte) c.Bb);
    }
  }
}

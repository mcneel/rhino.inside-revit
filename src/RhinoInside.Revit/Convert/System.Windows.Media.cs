using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.System.Windows.Media
{
  using global::System.Windows.Media;

  public static class ColorConverter
  {
    public static Color ToColor(this ARDB.Color c)
    {
      return c.IsValid ?
             Color.FromArgb(0xFF, c.Red, c.Green, c.Blue) :
             Color.FromArgb(0, 0, 0, 0);
    }

    public static ARDB::Color ToColor(this Color c)
    {
      return c.B == 0 && c.G == 0 && c.R == 0 && c.A == 0 ?
             ARDB::Color.InvalidColorValue :
             new ARDB::Color(c.R, c.G, c.B);
    }
  }
}

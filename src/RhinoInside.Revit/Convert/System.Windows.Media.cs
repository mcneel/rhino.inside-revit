using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.System.Windows.Media
{
  using global::System.Windows.Media;

  public static class ColorConverter
  {
    public static Color ToColor(this DB.Color c)
    {
      return c.IsValid ?
             Color.FromArgb(0xFF, c.Red, c.Green, c.Blue) :
             Color.FromArgb(0, 0, 0, 0);
    }

    public static DB::Color ToColor(this Color c)
    {
      return c.B == 0 && c.G == 0 && c.R == 0 && c.A == 0 ?
             DB::Color.InvalidColorValue :
             new DB::Color(c.R, c.G, c.B);
    }
  }
}

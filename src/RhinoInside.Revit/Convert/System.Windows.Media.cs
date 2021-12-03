using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.System.Windows.Media
{
  using global::System.Windows.Media;

  /// <summary>
  /// Represents a converter for converting <see cref="Color"/> values back and forth Revit and Rhino.
  /// </summary>
  public static class ColorConverter
  {
    /// <summary>
    /// Converts the specified <see cref="ARDB.Color"/> to an equivalent <see cref="Color"/>.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A <see cref="Color"/> that is equivalent to the provided value.</returns>
    public static Color ToColor(this ARDB.Color value)
    {
      return value.IsValid ?
             Color.FromArgb(0xFF, value.Red, value.Green, value.Blue) :
             Color.FromArgb(0, 0, 0, 0);
    }

    /// <summary>
    /// Converts the specified <see cref="Color"/> to an equivalent <see cref="ARDB.Color"/>.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A <see cref="ARDB.Color"/> that is equivalent to the provided value.</returns>
    public static ARDB::Color ToColor(this Color value)
    {
      return value.B == 0 && value.G == 0 && value.R == 0 && value.A == 0 ?
             ARDB::Color.InvalidColorValue :
             new ARDB::Color(value.R, value.G, value.B);
    }
  }
}

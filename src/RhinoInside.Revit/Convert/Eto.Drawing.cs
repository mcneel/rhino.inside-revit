using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Eto.Drawing
{
  using global::Eto.Drawing;

  /// <summary>
  /// Represents a converter for converting <see cref="Color"/> values back and forth Revit and Rhino.
  /// </summary>
  public static class ColorConverter
  {
    /// <summary>
    /// Converts the specified Color to an equivalent <see cref="Color"/>.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>An <see cref="Color"/> that is equivalent to the provided value.</returns>
    public static Color ToColor(this ARDB.Color value)
    {
      return value.IsValid ?
             Color.FromArgb(value.Red, value.Green, value.Blue, 0xFF) :
             Color.FromArgb(0, 0, 0, 0);
    }

    /// <summary>
    /// Converts the specified Color to an equivalent <see cref="ARDB.Color"/>.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A <see cref="ARDB.Color"/> that is equivalent to the provided value.</returns>
    public static ARDB::Color ToColor(this Color value)
    {
      return value.ToArgb() == 0 ?
        ARDB::Color.InvalidColorValue :
        new ARDB::Color((byte) value.Rb, (byte) value.Gb, (byte) value.Bb);
    }
  }
}

using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.System.Drawing
{
  using global::System.Drawing;

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
    public static ARDB.Color ToColor(this Color value)
    {
      return value.ToArgb() == 0 ?
             ARDB.Color.InvalidColorValue :
             new ARDB.Color(value.R, value.G, value.B);
    }
  }

  /// <summary>
  /// Represents a converter for converting <see cref="Color"/> values with transparency back and forth Revit and Rhino.
  /// </summary>
  public static class ColorWithTransparencyConverter
  {
    /// <summary>
    /// Converts the specified <see cref="ARDB.ColorWithTransparency"/> to an equivalent <see cref="Color"/>.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A <see cref="Color"/> that is equivalent to the provided value.</returns>
    public static Color ToColor(this ARDB.ColorWithTransparency value)
    {
      return value.IsValidObject ?
             Color.FromArgb(0xFF - (int) value.GetTransparency(), (int) value.GetRed(), (int) value.GetGreen(), (int) value.GetBlue()) :
             Color.FromArgb(0, 0, 0, 0);
    }

    /// <summary>
    /// Converts the specified <see cref="Color"/> to an equivalent <see cref="ARDB.Color"/>.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A <see cref="ARDB.ColorWithTransparency"/> that is equivalent to the provided value.</returns>
    public static ARDB.ColorWithTransparency ToColorWithTransparency(this Color value)
    {
      return new ARDB.ColorWithTransparency(value.R, value.G, value.B, 0xFFu - value.A);
    }
  }

  /// <summary>
  /// Represents a converter for converting <see cref="Rectangle"/> values back and forth Revit and Rhino.
  /// </summary>
  public static class RectangleConverter
  {
    /// <summary>
    /// Converts the specified <see cref="ARDB.Rectangle"/> to an equivalent <see cref="Rectangle"/>.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A <see cref="Rectangle"/> that is equivalent to the provided value.</returns>
    public static Rectangle ToRectangle(this ARDB.Rectangle value)
    {
      return new Rectangle(value.Left, value.Top, value.Right - value.Left, value.Bottom - value.Top);
    }

    /// <summary>
    /// Converts the specified <see cref="Rectangle"/> to an equivalent <see cref="ARDB.Rectangle"/>.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A <see cref="ARDB.Rectangle"/> that is equivalent to the provided value.</returns>
    public static ARDB.Rectangle ToRectangle(this Rectangle value)
    {
      return new ARDB.Rectangle(value.Left, value.Top, value.Right, value.Bottom);
    }
  }
}

using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.System.Windows.Media
{
  using global::System.Windows.Media;

  /// <summary>
  /// Converter to convert <see cref="Color"/> values to and from <see cref="ARDB.Color"/>.
  /// </summary>
  public static class ColorConverter
  {
    /// <summary>
    /// Converts the specified <see cref="ARDB.Color" /> to an equivalent <see cref="Color" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToColor(ARDB.Color)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using System.Windows.Media;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.System.Windows.Media;
    /// 
    /// Color color = revitColor.ToColor();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("System.Windows")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from System.Windows.Media import Color
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.System.Windows.Media
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.System.Windows.Media)
    /// 
    /// color = revit_color.ToColor()	# type: Color
    /// </code>
    /// 
    /// Using <see cref="ToColor(ARDB.Color)" /> as static method:
    ///
    /// <code language="csharp">
    /// using System.Windows.Media;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.System.Windows.Media;
    /// 
    /// Color color = ColorConverter.ToColor(revitColor)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("System.Windows")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// clr.AddReference("Eto")
    /// from System.Windows.Media import Color
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.System.Windows.Media.ColorConverter as CC
    /// 
    /// color = CC.ToColor(revit_color)	# type: Color
    /// </code>
    ///
    /// </example>
    /// <param name="color">Revit color to convert.</param>
    /// <returns>System color that is equivalent to the provided Revit color.</returns>
    /// <since>1.0</since>
    public static Color ToColor(this ARDB.Color color)
    {
      return color.IsValid ?
             Color.FromArgb(0xFF, color.Red, color.Green, color.Blue) :
             Color.FromArgb(0, 0, 0, 0);
    }

    /// <summary>
    /// Converts the specified <see cref="Color" /> to an equivalent <see cref="ARDB.Color" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToColor(Color)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using System.Windows.Media;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.System.Windows.Media;
    /// 
    /// DB.Color revitColor = color.ToColor();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("System.Windows")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from System.Windows.Media import Color
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.System.Windows.Media
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.System.Windows.Media)
    /// 
    /// revit_color = color.ToColor()	# type: DB.Color
    /// </code>
    /// 
    /// Using <see cref="ToColor(Color)" /> as static method:
    ///
    /// <code language="csharp">
    /// using System.Windows.Media;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.System.Windows.Media;
    /// 
    /// DB.Color revitColor = ColorConverter.ToColor(color)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("System.Windows")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// clr.AddReference("Eto")
    /// from System.Windows.Media import Color
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.System.Windows.Media.ColorConverter as CC
    /// 
    /// revit_color = CC.ToColor(color)	# type: DB.Color
    /// </code>
    ///
    /// </example>
    /// <param name="color">System color to convert.</param>
    /// <returns>Revit color that is equivalent to the provided System color.</returns>
    /// <since>1.0</since>
    public static ARDB.Color ToColor(this Color color)
    {
      return color.B == 0 && color.G == 0 && color.R == 0 && color.A == 0 ?
             ARDB.Color.InvalidColorValue :
             new ARDB.Color(color.R, color.G, color.B);
    }
  }
}

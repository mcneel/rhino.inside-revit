using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Eto.Drawing
{
  using global::Eto.Drawing;

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
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Eto.Drawing;
    /// 
    /// Color etoColor = revitColor.ToColor();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("Eto")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from Eto.Drawing import Color
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Eto.Drawing
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Eto.Drawing)
    /// 
    /// eto_color = revit_color.ToColor()	# type: Color
    /// </code>
    /// 
    /// Using <see cref="ToColor(ARDB.Color)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Eto.Drawing;
    /// 
    /// Color etoColor = ColorConverter.ToColor(revitColor)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("Eto")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from Eto.Drawing import Color
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Eto.Drawing.ColorConverter as CC
    /// 
    /// eto_color = CC.ToColor(revit_color)	# type: Color
    /// </code>
    ///
    /// </example>
    /// <param name="color">Revit color to convert.</param>
    /// <returns>Eto color that is equivalent to the provided Revit color.</returns>
    public static Color ToColor(this ARDB.Color color)
    {
      return color.IsValid ?
             Color.FromArgb(color.Red, color.Green, color.Blue, 0xFF) :
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
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Eto.Drawing;
    /// 
    /// DB.Color revitColor = etoColor.ToColor();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("Eto")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from Eto.Drawing import Color
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Eto.Drawing
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Eto.Drawing)
    /// 
    /// revit_color = eto_color.ToColor()	# type: DB.Color
    /// </code>
    /// 
    /// Using <see cref="ToColor(Color)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Eto.Drawing;
    /// 
    /// DB.Color revitColor = ColorConverter.ToColor(etoColor)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("Eto")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from Eto.Drawing import Color
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Eto.Drawing.ColorConverter as CC
    /// 
    /// revit_color = CC.ToColor(eto_color)	# type: DB.Color
    /// </code>
    ///
    /// </example>
    /// <param name="color">Eto color to convert.</param>
    /// <returns>Revit color that is equivalent to the provided Eto color.</returns>
    public static ARDB::Color ToColor(this Color color)
    {
      return color.ToArgb() == 0 ?
        ARDB::Color.InvalidColorValue :
        new ARDB::Color((byte) color.Rb, (byte) color.Gb, (byte) color.Bb);
    }
  }
}

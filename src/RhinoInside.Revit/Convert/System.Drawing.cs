using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.System.Drawing
{
  using global::System.Drawing;

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
    /// using System.Drawing;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.System.Drawing;
    /// 
    /// Color color = revitColor.ToColor();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("System.Drawing")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from System.Drawing import Color
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.System.Drawing
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.System.Drawing)
    /// 
    /// color = revit_color.ToColor()	# type: Color
    /// </code>
    /// 
    /// Using <see cref="ToColor(ARDB.Color)" /> as static method:
    ///
    /// <code language="csharp">
    /// using System.Drawing;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.System.Drawing;
    /// 
    /// Color color = ColorConverter.ToColor(revitColor)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("System.Drawing")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// clr.AddReference("Eto")
    /// from System.Drawing import Color
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.System.Drawing.ColorConverter as CC
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
    /// using System.Drawing;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.System.Drawing;
    /// 
    /// DB.Color revitColor = color.ToColor();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("System.Drawing")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from System.Drawing import Color
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.System.Drawing
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.System.Drawing)
    /// 
    /// revit_color = color.ToColor()	# type: DB.Color
    /// </code>
    /// 
    /// Using <see cref="ToColor(Color)" /> as static method:
    ///
    /// <code language="csharp">
    /// using System.Drawing;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.System.Drawing;
    /// 
    /// DB.Color revitColor = ColorConverter.ToColor(color)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("System.Drawing")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// clr.AddReference("Eto")
    /// from System.Drawing import Color
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.System.Drawing.ColorConverter as CC
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
      return color.ToArgb() == 0 ?
             ARDB.Color.InvalidColorValue :
             new ARDB.Color(color.R, color.G, color.B);
    }
  }

  /// <summary>
  /// Converter to convert <see cref="Color"/> values to and from <see cref="ARDB.ColorWithTransparency"/>.
  /// </summary>
  public static class ColorWithTransparencyConverter
  {
    /// <summary>
    /// Converts the specified <see cref="ARDB.Color" /> to an equivalent <see cref="Color" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToColor(ARDB.ColorWithTransparency)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using System.Drawing;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.System.Drawing;
    /// 
    /// Color color = revitColor.ToColor();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("System.Drawing")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from System.Drawing import Color
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.System.Drawing
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.System.Drawing)
    /// 
    /// color = revit_color.ToColor()	# type: Color
    /// </code>
    /// 
    /// Using <see cref="ToColor(ARDB.ColorWithTransparency)" /> as static method:
    ///
    /// <code language="csharp">
    /// using System.Drawing;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.System.Drawing;
    /// 
    /// Color color = ColorWithTransparencyConverter.ToColor(revitColor)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("System.Drawing")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// clr.AddReference("Eto")
    /// from System.Drawing import Color
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.System.Drawing.ColorWithTransparencyConverter as CC
    /// 
    /// color = CC.ToColor(revit_color)	# type: Color
    /// </code>
    ///
    /// </example>
    /// <param name="color">Revit color to convert.</param>
    /// <returns>System color that is equivalent to the provided Revit color.</returns>
    /// <since>1.1</since>
    public static Color ToColor(this ARDB.ColorWithTransparency color)
    {
      return color.IsValidObject ?
             Color.FromArgb(0xFF - (int) color.GetTransparency(), (int) color.GetRed(), (int) color.GetGreen(), (int) color.GetBlue()) :
             Color.FromArgb(0, 0, 0, 0);
    }

    /// <summary>
    /// Converts the specified <see cref="Color" /> to an equivalent <see cref="ARDB.ColorWithTransparency" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToColorWithTransparency(Color)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using System.Drawing;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.System.Drawing;
    /// 
    /// DB.ColorWithTransparency revitColor = color.ToColorWithTransparency();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("System.Drawing")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from System.Drawing import Color
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.System.Drawing
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.System.Drawing)
    /// 
    /// revit_color = color.ToColor()	# type: DB.ColorWithTransparency
    /// </code>
    /// 
    /// Using <see cref="ToColorWithTransparency(Color)" /> as static method:
    ///
    /// <code language="csharp">
    /// using System.Drawing;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.System.Drawing;
    /// 
    /// DB.ColorWithTransparency revitColor = ColorConverter.ToColorWithTransparency(color)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("System.Drawing")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// clr.AddReference("Eto")
    /// from System.Drawing import Color
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.System.Drawing.ColorConverter as CC
    /// 
    /// revit_color = CC.ToColorWithTransparency(color)	# type: DB.ColorWithTransparency
    /// </code>
    ///
    /// </example>
    /// <param name="color">System color to convert.</param>
    /// <returns>Revit color that is equivalent to the provided System color.</returns>
    /// <since>1.1</since>
    public static ARDB.ColorWithTransparency ToColorWithTransparency(this Color color)
    {
      return new ARDB.ColorWithTransparency(color.R, color.G, color.B, 0xFFu - color.A);
    }
  }

  /// <summary>
  /// Converter to convert <see cref="Rectangle"/> values to and from <see cref="ARDB.Rectangle"/>.
  /// </summary>
  public static class RectangleConverter
  {
    /// <summary>
    /// Converts the specified <see cref="ARDB.Rectangle" /> to an equivalent <see cref="Rectangle" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToRectangle(ARDB.Rectangle)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using System.Drawing;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.System.Drawing;
    /// 
    /// Rectangle rectangle = revitRectangle.ToRectangle();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("System.Drawing")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from System.Drawing import Rectangle
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.System.Drawing
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.System.Drawing)
    /// 
    /// rectangle = revit_rectangle.ToRectangle()	# type: Rectangle
    /// </code>
    /// 
    /// Using <see cref="ToRectangle(ARDB.Rectangle)" /> as static method:
    ///
    /// <code language="csharp">
    /// using System.Drawing;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.System.Drawing;
    /// 
    /// Rectangle rectangle = RectangleConverter.ToRectangle(revitRectangle)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("System.Drawing")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// clr.AddReference("Eto")
    /// from System.Drawing import Rectangle
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.System.Drawing.RectangleConverter as RC
    /// 
    /// rectangle = RC.ToRectangle(revit_rectangle)	# type: Rectangle
    /// </code>
    ///
    /// </example>
    /// <param name="rectangle">System rectangle to convert.</param>
    /// <returns>Revit rectangle that is equivalent to the provided System rectangle.</returns>
    /// <since>1.0</since>
    public static Rectangle ToRectangle(this ARDB.Rectangle rectangle)
    {
      return new Rectangle(rectangle.Left, rectangle.Top, rectangle.Right - rectangle.Left, rectangle.Bottom - rectangle.Top);
    }

    /// <summary>
    /// Converts the specified <see cref="Rectangle" /> to an equivalent <see cref="ARDB.Rectangle" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToRectangle(Rectangle)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using System.Drawing;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.System.Drawing;
    /// 
    /// DB.Rectangle revitRectangle = rectangle.ToRectangle();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("System.Drawing")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from System.Drawing import Rectangle
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.System.Drawing
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.System.Drawing)
    /// 
    /// revit_rectangle = rectangle.ToRectangle()	# type: DB.Rectangle
    /// </code>
    /// 
    /// Using <see cref="ToRectangle(Rectangle)" /> as static method:
    ///
    /// <code language="csharp">
    /// using System.Drawing;
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.System.Drawing;
    /// 
    /// DB.Rectangle revitRectangle = RectangleConverter.ToRectangle(rectangle)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("System.Drawing")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// clr.AddReference("Eto")
    /// from System.Drawing import Rectangle
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.System.Drawing.RectangleConverter as RC
    /// 
    /// revit_rectangle = RC.ToRectangle(rectangle)	# type: DB.Rectangle
    /// </code>
    ///
    /// </example>
    /// <param name="rectangle">System rectangle to convert.</param>
    /// <returns>Revit rectangle that is equivalent to the provided System rectangle.</returns>
    /// <since>1.0</since>
    public static ARDB.Rectangle ToRectangle(this Rectangle rectangle)
    {
      return new ARDB.Rectangle(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
    }
  }
}

using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ColorExtension
  {
    public static Color InvalidColorValue { get; } = Color.InvalidColorValue;
    public static Color Black { get; }  = new Color(0, 0, 0);
    public static Color White { get; }  = new Color(255, 255, 255);
    public static Color Red { get; }    = new Color(255, 0, 0);
    public static Color Green { get; }  = new Color(0, 255, 0);
    public static Color Blue { get; }   = new Color(0, 0, 255);

    public static bool IsEquivalent(this Color self, Color other)
    {
      if (ReferenceEquals(self, other)) return true;
      if (self is null || other is null) return false;
      if (!self.IsValid && self.IsValid == other.IsValid) return true;

      return self.Red == other.Red && self.Green == other.Green && self.Blue == other.Blue;
    }

    public static void Deconstruct
    (
      this Color value,
      out byte r, out byte g, out byte b
    )
    {
      r = value.Red;
      g = value.Green;
      b = value.Blue;
    }

    public static int ToBGR(this Color value)
    {
      return (value.Blue << 16) | (value.Green << 8) | (value.Red << 0);
    }

    public static void SetBGR(this Color self, int bgr)
    {
      self.Blue   = (byte) ((bgr >> 16) & byte.MaxValue);
      self.Green  = (byte) ((bgr >>  8) & byte.MaxValue);
      self.Red    = (byte) ((bgr >>  0) & byte.MaxValue);
    }
  }
}

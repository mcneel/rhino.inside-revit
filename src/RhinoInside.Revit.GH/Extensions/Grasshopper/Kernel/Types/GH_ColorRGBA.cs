using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GH_IO.Serialization;
using GH_IO.Types;
using Grasshopper.Special;

namespace Grasshopper.Kernel.Types
{
  [EditorBrowsable(EditorBrowsableState.Never)]
  public class GH_ColorRGBA : GH_Goo<Rhino.Display.ColorRGBA>, IGH_ItemDescription
  {
    public override bool IsValid => true;

    public override string TypeName => "RGBA Color";

    public override string TypeDescription => "A double-precision, RGBA color";

    #region IGH_ItemDescription
    public System.Drawing.Bitmap GetImage(System.Drawing.Size size) => default;

    public string Name => ToString();

    public string NickName => Value.A == 1.0 ?
      $"{Value.R:F2},{Value.G:F2},{Value.B:F2}":
      $"{Value.R:F2},{Value.G:F2},{Value.B:F2},{Value.A:F2}";

    public string Description => TypeDescription;
    #endregion

    public override IGH_Goo Duplicate() => MemberwiseClone() as IGH_Goo;

    public GH_ColorRGBA() { }
    public GH_ColorRGBA(Rhino.Display.ColorRGBA color) : base(color) { }

    public override string ToString()
    {
      var color = (System.Drawing.Color) Value;

      return color.A < 255 ?
        $"{color.R},{color.G},{color.B},{color.A}" :
        $"{color.R},{color.G},{color.B}";
    }

    struct ArgbComparer : IEqualityComparer<System.Drawing.Color>
    {
      public bool Equals(System.Drawing.Color x, System.Drawing.Color y) => x.ToArgb() == y.ToArgb();
      public int GetHashCode(System.Drawing.Color obj) => obj.ToArgb();
    }
    static readonly IReadOnlyDictionary<int, string> ColorNames = typeof(System.Drawing.Color).
      GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).
      Where(x => x.PropertyType == typeof(System.Drawing.Color)).Select(x => (System.Drawing.Color) x.GetValue(null)).
      Distinct(default(ArgbComparer)).
      ToDictionary(k => k.ToArgb(), v => v.Name);

    public static bool TryGetName(System.Drawing.Color color, out string name) =>
      ColorNames.TryGetValue(color.ToArgb(), out name);

    public override bool CastFrom(object source)
    {
      if (source is Rhino.Display.ColorRGBA value)
      {
        Value = value;
        return true;
      }

      var point = Rhino.Geometry.Point3d.Origin;
      if (GH_Convert.ToPoint3d(source, ref point, GH_Conversion.Both))
      {
        var x = Rhino.RhinoMath.Clamp(point.X, -1.0, +1.0);
        var y = Rhino.RhinoMath.Clamp(point.Y, -1.0, +1.0);
        var z = Rhino.RhinoMath.Clamp(point.Z, -1.0, +1.0);

        Value = new Rhino.Display.ColorRGBA
        (
          (double) ((x + 1.0) * 0.5),
          (double) ((y + 1.0) * 0.5),
          (double) ((z + 1.0) * 0.5),
          1.0f
        );
        return true;
      }

      if (GH_Convert.ToColor(source, out var color, GH_Conversion.Both))
      {
        Value = new Rhino.Display.ColorRGBA(color);
        return true;
      }

      return false;
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(GH_Colour)))
      {
        target = (Q) (object) new GH_Colour((System.Drawing.Color) Value);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Point)))
      {
        const double factor = 2.0;
        var point = new Rhino.Geometry.Point3d
        (
          (Value.R * factor) - 1.0,
          (Value.G * factor) - 1.0,
          (Value.B * factor) - 1.0
        );

        target = (Q) (object) new GH_Point(point);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Vector)))
      {
        const double factor = 2.0;
        var vector = new Rhino.Geometry.Vector3d
        (
          (Value.R * factor) - 1.0,
          (Value.G * factor) - 1.0,
          (Value.B * factor) - 1.0
        );

        target = (Q) (object) new GH_Vector(vector);
        return true;
      }

      return false;
    }

    public override bool Read(GH_IReader reader)
    {
      var point = reader.GetPoint4D("color");
      Value = new Rhino.Display.ColorRGBA
      (
        point.x,
        point.y,
        point.z,
        point.w
      );

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      var point = new GH_Point4D
      (
        Value.R,
        Value.G,
        Value.B,
        Value.A
      );
      writer.SetPoint4D("color", point);

      return true;
    }
  }
}

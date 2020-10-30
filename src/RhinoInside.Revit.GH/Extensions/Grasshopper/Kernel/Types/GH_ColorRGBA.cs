using System.ComponentModel;
using GH_IO.Serialization;
using GH_IO.Types;

namespace Grasshopper.Kernel.Types
{
  [EditorBrowsable(EditorBrowsableState.Never)]
  public class GH_ColorRGBA : GH_Goo<Rhino.Display.ColorRGBA>
  {
    public override bool IsValid => true;

    public override string TypeName => "Color";

    public override string TypeDescription => "Color defined by 4 floating point values";

    public override IGH_Goo Duplicate() => MemberwiseClone() as IGH_Goo;

    public GH_ColorRGBA() { }
    public GH_ColorRGBA(Rhino.Display.ColorRGBA color) : base(color) { }

    public override string ToString()
    {
      return GH_Format.FormatColour((System.Drawing.Color) Value);
    }

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

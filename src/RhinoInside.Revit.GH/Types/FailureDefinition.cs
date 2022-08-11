using System;
using System.Drawing;
using Autodesk.Revit.DB.Visual;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Special;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class FailureDefinition : GH_Goo<Guid>,
    IEquatable<FailureDefinition>,
    IGH_ItemDescription,
    IGH_QuickCast
  {
    public override bool IsValid => Value != Guid.Empty;

    public override string TypeName => "Failure Message";

    public override string TypeDescription => "Revit Failure Message";

    public FailureDefinition() { }
    public FailureDefinition(Guid guid) : base(guid) { }

    public override IGH_Goo Duplicate() => (FailureDefinition) MemberwiseClone();

    public override string ToString()
    {
      var id = new ARDB.FailureDefinitionId(Value);
      var accessor = Autodesk.Revit.ApplicationServices.Application.GetFailureDefinitionRegistry()?.FindFailureDefinition(id);
      return (accessor?.GetDescriptionText() ?? Value.ToString("B")).Split(new char[] {'.'}, StringSplitOptions.RemoveEmptyEntries)[0];
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (!IsValid) return false;

      if (typeof(Q).IsAssignableFrom(typeof(GH_Guid)))
      {
        target = (Q) (object) new GH_Guid(Value);
        return true;
      }

      return base.CastTo(ref target);
    }

    public override bool CastFrom(object source)
    {
      if (!GH_Convert.ToGUID(source, out var id, GH_Conversion.Both))
        return false;

      Value = id;
      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      writer.SetGuid("FailureDefinitionId", Value);
      return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
      Value = reader.GetGuid("FailureDefinitionId");
      return base.Read(reader);
    }

    #region IGH_QuickCast
    GH_QuickCastType IGH_QuickCast.QC_Type => GH_QuickCastType.text;

    double IGH_QuickCast.QC_Distance(IGH_QuickCast other)
    {
      switch (other.QC_Type)
      {
        case GH_QuickCastType.text:
          var dist0 = GH_StringMatcher.LevenshteinDistance(Value.ToString("B"), other.QC_Text());
          var dist1 = GH_StringMatcher.LevenshteinDistance(Value.ToString("B").ToUpperInvariant(), other.QC_Text().ToUpperInvariant());
          return 0.5 * (dist0 + dist1);
        default:
          return (this as IGH_QuickCast).QC_Distance(new GH_String(other.QC_Text()));
      }
    }

    int IGH_QuickCast.QC_Hash() => Value.GetHashCode();
    bool IGH_QuickCast.QC_Bool() => IsValid;
    int IGH_QuickCast.QC_Int() => throw new InvalidCastException($"{TypeName} cannot be cast to {nameof(Int32)}");
    double IGH_QuickCast.QC_Num() => throw new InvalidCastException($"{TypeName} cannot be cast to {nameof(Double)}");
    string IGH_QuickCast.QC_Text() => Value.ToString("B");
    Color IGH_QuickCast.QC_Col() => throw new InvalidCastException($"{TypeName} cannot be cast to {nameof(Color)}");
    Point3d IGH_QuickCast.QC_Pt() => throw new InvalidCastException($"{TypeName} cannot be cast to {nameof(Point3d)}");
    Vector3d IGH_QuickCast.QC_Vec() => throw new InvalidCastException($"{TypeName} cannot be cast to {nameof(Vector3d)}");
    Complex IGH_QuickCast.QC_Complex() => throw new InvalidCastException($"{TypeName} cannot be cast to {nameof(Complex)}");
    Matrix IGH_QuickCast.QC_Matrix() => throw new InvalidCastException($"{TypeName} cannot be cast to {nameof(Matrix)}");
    Interval IGH_QuickCast.QC_Interval() => throw new InvalidCastException($"{TypeName} cannot be cast to {nameof(Interval)}");
    int IGH_QuickCast.QC_CompareTo(IGH_QuickCast other)
    {
      if ((this as IGH_QuickCast).QC_Type != other.QC_Type) (this as IGH_QuickCast).QC_Type.CompareTo(other.QC_Type);
      return Value.ToString("B").CompareTo(other.QC_Text());
    }
    #endregion

    #region IEquatable
    public override int GetHashCode() => Value.GetHashCode();
    public override bool Equals(object obj) => Equals(obj as FailureDefinition);
    public bool Equals(FailureDefinition other) => Value == other?.Value;
    #endregion

    #region IGH_ItemDescription
    public string Name => ToString();

    public string NickName => Value.ToString("B");

    public string Description => TypeDescription;

    public Bitmap GetImage(Size size) => default;
    #endregion
  }
}

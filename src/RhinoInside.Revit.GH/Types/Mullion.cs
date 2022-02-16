using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Mullion")]
  public class Mullion : FamilyInstance
  {
    protected override Type ValueType => typeof(ARDB.Mullion);
    public static explicit operator ARDB.Mullion(Mullion value) => value?.Value;
    public new ARDB.Mullion Value => base.Value as ARDB.Mullion;

    public Mullion() { }
    public Mullion(ARDB.Mullion value) : base(value) { }

    public override Rhino.Geometry.Curve Curve => Value?.LocationCurve.ToCurve();
  }

  [Kernel.Attributes.Name("Mullion Position")]
  public class MullionPosition : ElementType
  {
    protected override Type ValueType => typeof(ARDB.ElementType);

    public MullionPosition() { }
    public MullionPosition(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }

    public override string Nomen
    {
      get
      {
        switch ((External.DB.BuiltInMullionPositionId) Id.IntegerValue)
        {
          case External.DB.BuiltInMullionPositionId.PerpendicularToFace: return "Perpendicular To Face";
          case External.DB.BuiltInMullionPositionId.ParallelToGround:    return "Parallel To Ground";
        }

        return base.Nomen;
      }
    }
  }

  [Kernel.Attributes.Name("Mullion Profile")]
  public class MullionProfile : ElementType
  {
    protected override Type ValueType => typeof(ARDB.ElementType);

    public MullionProfile() { }
    public MullionProfile(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }

    public override string Nomen
    {
      get
      {
        switch ((External.DB.BuiltInMullionProfileId) Id.IntegerValue)
        {
          case External.DB.BuiltInMullionProfileId.Rectangular: return "Rectangular";
          case External.DB.BuiltInMullionProfileId.Circular:    return "Circular";
        }

        return base.Nomen;
      }
    }
  }
}

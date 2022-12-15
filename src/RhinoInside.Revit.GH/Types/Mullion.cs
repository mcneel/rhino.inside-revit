using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Mullion")]
  public class Mullion : FamilyInstance
  {
    protected override Type ValueType => typeof(ARDB.Mullion);
    public new ARDB.Mullion Value => base.Value as ARDB.Mullion;

    public Mullion() { }
    public Mullion(ARDB.Mullion value) : base(value) { }

    public override Rhino.Geometry.Curve Curve => Value?.LocationCurve.ToCurve();
  }

  [Kernel.Attributes.Name("Mullion Type")]
  public class MullionType : FamilySymbol
  {
    protected override Type ValueType => typeof(ARDB.MullionType);
    public new ARDB.MullionType Value => base.Value as ARDB.MullionType;

    public MullionType() { }
    public MullionType(ARDB.MullionType value) : base(value) { }
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
        switch ((External.DB.BuiltInMullionPositionId) Id.ToValue())
        {
          case External.DB.BuiltInMullionPositionId.PerpendicularToFace: return "Perpendicular To Face";
          case External.DB.BuiltInMullionPositionId.ParallelToGround:    return "Parallel To Ground";
        }

        return base.Nomen;
      }
    }
  }

  [Kernel.Attributes.Name("Mullion Profile")]
  public class MullionProfile : FamilySymbol
  {
    protected override Type ValueType => typeof(ARDB.FamilySymbol);

    public MullionProfile() { }
    public MullionProfile(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }

    public override string Nomen
    {
      get
      {
        switch ((External.DB.BuiltInMullionProfileId) Id.ToValue())
        {
          case External.DB.BuiltInMullionProfileId.Rectangular: return "Rectangular";
          case External.DB.BuiltInMullionProfileId.Circular: return "Circular";
        }

        return base.Nomen;
      }
    }
  }
}

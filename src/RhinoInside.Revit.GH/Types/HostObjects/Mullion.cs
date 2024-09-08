using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

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
}

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  using ARDB_MullionPosition = ARDB.ElementType;

  [Kernel.Attributes.Name("Mullion Position")]
  public class MullionPosition : ElementType
  {
    protected override Type ValueType => typeof(ARDB_MullionPosition);
    public override bool IsValid => base.IsValid || (ReferenceDocument is object && Id.IsValid() && Enum.IsDefined(typeof(External.DB.BuiltInMullionPosition), Id.ToValue()));

    public MullionPosition() { }
    public MullionPosition(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }

    public override string Nomen
    {
      get
      {
        switch ((External.DB.BuiltInMullionPosition) Id.ToValue())
        {
          case External.DB.BuiltInMullionPosition.PerpendicularToFace: return "Perpendicular To Face";
          case External.DB.BuiltInMullionPosition.ParallelToGround:    return "Parallel To Ground";
        }

        return base.Nomen;
      }
    }
  }
}

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
  using ARDB_MullionProfile  = ARDB.FamilySymbol;

  [Kernel.Attributes.Name("Mullion Position")]
  public class MullionPosition : ElementType
  {
    protected override Type ValueType => typeof(ARDB_MullionPosition);

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
    protected override Type ValueType => typeof(ARDB_MullionProfile);
    public new ARDB_MullionProfile Value => base.Value as ARDB_MullionProfile;

    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static bool IsValidElement(ARDB.Element element)
    {
      return element is ARDB_MullionProfile &&
             element.Category?.Id.ToBuiltInCategory() == ARDB.BuiltInCategory.OST_ProfileFamilies;
    }

    public MullionProfile() { }
    public MullionProfile(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public MullionProfile(ARDB_MullionProfile profile) : base(profile)
    {
      if (!IsValidElement(profile))
        throw new ArgumentException("Invalid Element", nameof(profile));
    }

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

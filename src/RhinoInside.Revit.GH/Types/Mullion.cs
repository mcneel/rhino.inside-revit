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
  using ARDB_ProfileType  = ARDB.FamilySymbol;

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

  [Kernel.Attributes.Name("Profile Type")]
  public class ProfileType : FamilySymbol
  {
    protected override Type ValueType => typeof(ARDB_ProfileType);
    public new ARDB_ProfileType Value => base.Value as ARDB_ProfileType;

    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static bool IsValidElement(ARDB.Element element)
    {
      return element is ARDB_ProfileType &&
             element.Category?.Id.ToBuiltInCategory() == ARDB.BuiltInCategory.OST_ProfileFamilies;
    }

    public ProfileType() { }
    public ProfileType(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public ProfileType(ARDB_ProfileType profile) : base(profile)
    {
      if (!IsValidElement(profile))
        throw new ArgumentException("Invalid Element", nameof(profile));
    }

    public override string Nomen
    {
      get
      {
        switch ((External.DB.BuiltInProfileTypeId) Id.ToValue())
        {
          case External.DB.BuiltInProfileTypeId.Rectangular: return "Rectangular";
          case External.DB.BuiltInProfileTypeId.Circular: return "Circular";
        }

        return base.Nomen;
      }
    }
  }
}

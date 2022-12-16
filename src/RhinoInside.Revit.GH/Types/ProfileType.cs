using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  using ARDB_ProfileType  = ARDB.FamilySymbol;

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
        switch ((External.DB.BuiltInProfileType) Id.ToValue())
        {
          case External.DB.BuiltInProfileType.Rectangular: return "Rectangular";
          case External.DB.BuiltInProfileType.Circular: return "Circular";
        }

        return base.Nomen;
      }
    }
  }
}

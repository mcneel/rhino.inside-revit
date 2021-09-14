using System;
using System.Linq;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Family")]
  public interface IGH_Family : IGH_Element { }

  [Kernel.Attributes.Name("Family")]
  public class Family : Element, IGH_Family
  {
    protected override Type ValueType => typeof(DB.Family);
    public static explicit operator DB.Family(Family value) => value?.Value;
    public new DB.Family Value => base.Value as DB.Family;

    public Family() { }
    public Family(DB.Family family) : base(family) { }
    public override string DisplayName
    {
      get
      {
        if (Value is DB.Family family)
        {
          var familyCategory = family.FamilyCategory;
          if 
          (
            familyCategory is null &&
            family.GetFamilySymbolIds().FirstOrDefault() is DB.ElementId typeId &&
            family.Document.GetElement(typeId) is DB.ElementType type
          )
            familyCategory = type.Category;

          return $"{familyCategory?.Name} : {family.Name}";
        }

        return base.DisplayName;
      }
    }

    public override bool CastFrom(object source)
    {
      if (source is Document doc)
        return SetValue(doc.Value.OwnerFamily);

      return base.CastFrom(source);
    }
  }
}

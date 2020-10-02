using System;
using System.Linq;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public interface IGH_Family : IGH_Element
  {
    DB.Family APIFamily { get; }
  }

  public class Family : Element, IGH_Family
  {
    public override string TypeName => "Revit Family";
    public override string TypeDescription => "Represents a Revit family";
    protected override Type ScriptVariableType => typeof(DB.Family);
    public DB.Family APIFamily => IsValid ? Document.GetElement(Id) as DB.Family : default;
    public static explicit operator DB.Family(Family value) => value?.APIFamily;

    public Family() { }
    public Family(DB.Family family) : base(family) { }
    public override string DisplayName
    {
      get
      {
        if (APIFamily is DB.Family family)
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

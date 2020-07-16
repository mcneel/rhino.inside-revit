using System;
using System.Linq;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Family : Element
  {
    public override string TypeName => "Revit Family";
    public override string TypeDescription => "Represents a Revit family";
    protected override Type ScriptVariableType => typeof(DB.Family);
    public static explicit operator DB.Family(Family value) =>
      value?.IsValid == true ? value.Document.GetElement(value) as DB.Family : default;

    public Family() { }
    public Family(DB.Family family) : base(family) { }
    public override string DisplayName
    {
      get
      {
        var family = (DB.Family) this;
        if (family is object)
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

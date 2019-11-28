using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Family : Element
  {
    public override string TypeName => "Revit Family";
    public override string TypeDescription => "Represents a Revit family";
    protected override Type ScriptVariableType => typeof(DB.Family);
    public static explicit operator DB.Family(Family self) =>
      self.Document?.GetElement(self) as DB.Family;

    public Family() { }
    public Family(DB.Family family) : base(family) { }
    public override string Tooltip
    {
      get
      {
        var family = (DB.Family) this;
        if (family is object)
        {
          var tip = string.Empty;
          if (family.FamilyCategory is DB.Category familyCategory) tip += $"{familyCategory.Name} : ";
          else if
          (
            family.GetFamilySymbolIds().FirstOrDefault() is DB.ElementId typeId &&
            family.Document.GetElement(typeId) is DB.ElementType type
          )
            tip += $"{type.Category.Name} : ";

          return $"{tip}{family.Name}";
        }

        return base.Tooltip;
      }
    }
  }
}

namespace RhinoInside.Revit.GH.Parameters
{
  public class Family : ElementIdNonGeometryParam<Types.Family, Autodesk.Revit.DB.Family>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("3966ADD8-07C0-43E7-874B-6EFF95598EB0");

    public Family() : base("Family", "Family", "Represents a Revit document family.", "Params", "Revit") { }
  }
}

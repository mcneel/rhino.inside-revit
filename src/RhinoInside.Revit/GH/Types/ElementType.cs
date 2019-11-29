using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Types
{
  public class ElementType : Element
  {
    public override string TypeName => "Revit Element Type";
    public override string TypeDescription => "Represents a Revit element type";
    protected override Type ScriptVariableType => typeof(Autodesk.Revit.DB.ElementType);
    public static explicit operator Autodesk.Revit.DB.ElementType(ElementType self) =>
      self.Document?.GetElement(self) as Autodesk.Revit.DB.ElementType;

    public ElementType() { }
    public ElementType(Autodesk.Revit.DB.ElementType elementType) : base(elementType) { }
    public override string Tooltip
    {
      get
      {
        var elementType = (Autodesk.Revit.DB.ElementType) this;
        if (elementType != null)
        {
          var tip = string.Empty;
          if (elementType.Category is object)
            tip += $"{elementType.Category.Name} : ";

          if (!string.IsNullOrEmpty(elementType.FamilyName))
            tip += $"{elementType.FamilyName} : ";

          return $"{tip}{elementType.Name}";
        }

        return base.Tooltip;
      }
    }
  }
}

namespace RhinoInside.Revit.GH.Parameters
{
  public class ElementType : ElementIdNonGeometryParam<Types.ElementType, Autodesk.Revit.DB.ElementType>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("97DD546D-65C3-4D00-A609-3F5FBDA67142");

    public ElementType() : base("Element Type", "Element Type", "Represents a Revit document element type.", "Params", "Revit") { }
  }
}

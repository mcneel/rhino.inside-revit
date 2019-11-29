using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class ElementType : Element
  {
    public override string TypeName => "Revit Element Type";
    public override string TypeDescription => "Represents a Revit element type";
    protected override Type ScriptVariableType => typeof(DB.ElementType);
    public static explicit operator DB.ElementType(ElementType self) =>
      self.Document?.GetElement(self) as DB.ElementType;

    public ElementType() { }
    public ElementType(DB.ElementType elementType) : base(elementType) { }

    public override string DisplayName
    {
      get
      {
        var element = (DB.ElementType) this;
        if (element is object)
        {
          if (element.get_Parameter(DB.BuiltInParameter.ALL_MODEL_TYPE_MARK) is DB.Parameter parameter && parameter.HasValue)
          {
            var mark = parameter.AsString();
            if (!string.IsNullOrEmpty(mark))
              return $"{element.Category?.Name} : {element.FamilyName} : {element.Name} [{mark}]";
          }

          return $"{element.Category?.Name} : {element.FamilyName} : {element.Name}";
        }

        return base.DisplayName;
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

using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class ElementType : Element
  {
    public override string TypeDescription => "Represents a Revit element type";
    protected override Type ScriptVariableType => typeof(DB.ElementType);
    public static explicit operator DB.ElementType(ElementType self) =>
      self.Document?.GetElement(self) as DB.ElementType;

    public ElementType() { }
    protected ElementType(DB.Document doc, DB.ElementId id) : base(doc, id) { }

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

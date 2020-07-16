using System;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class ElementType : Element
  {
    public override string TypeDescription => "Represents a Revit element type";
    protected override Type ScriptVariableType => typeof(DB.ElementType);
    public static explicit operator DB.ElementType(ElementType value) =>
      value?.IsValid == true ? value.Document.GetElement(value) as DB.ElementType : default;

    public ElementType() { }
    protected ElementType(DB.Document doc, DB.ElementId id) : base(doc, id) { }

    public ElementType(DB.ElementType elementType) : base(elementType) { }

    public override string DisplayName
    {
      get
      {
        var elementType = (DB.ElementType) this;
        if (elementType is object)
           return $"{elementType.GetFamilyName()} : {elementType.Name}";

        return base.DisplayName;
      }
    }
  }
}

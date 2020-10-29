using System;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Element Type")]
  public interface IGH_ElementType : IGH_Element
  {
    string FamilyName { get; }
  }

  [Kernel.Attributes.Name("Element Type")]
  public class ElementType : Element, IGH_ElementType
  {
    protected override Type ScriptVariableType => typeof(DB.ElementType);
    public static explicit operator DB.ElementType(ElementType value) => value?.Value;
    public new DB.ElementType Value => base.Value as DB.ElementType;

    public ElementType() { }
    protected ElementType(DB.Document doc, DB.ElementId id) : base(doc, id) { }

    public ElementType(DB.ElementType elementType) : base(elementType) { }

    public override string DisplayName
    {
      get
      {
        if(Value is DB.ElementType elementType)
           return $"{elementType.GetFamilyName()} : {elementType.Name}";

        return base.DisplayName;
      }
    }

    public string FamilyName => Value?.GetFamilyName();
  }
}

using System;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public interface IGH_ElementType : IGH_Element
  {
    DB.ElementType APIElementType { get; }
  }

  public class ElementType : Element, IGH_ElementType
  {
    public override string TypeDescription => "Represents a Revit element type";
    protected override Type ScriptVariableType => typeof(DB.ElementType);
    public DB.ElementType APIElementType => ((IGH_Element)this).APIElement as DB.ElementType;
    public static explicit operator DB.ElementType(ElementType value) => value?.APIElementType;      

    public ElementType() { }
    protected ElementType(DB.Document doc, DB.ElementId id) : base(doc, id) { }

    public ElementType(DB.ElementType elementType) : base(elementType) { }

    public override string DisplayName
    {
      get
      {
        if(APIElementType is DB.ElementType elementType)
           return $"{elementType.GetFamilyName()} : {elementType.Name}";

        return base.DisplayName;
      }
    }
  }
}

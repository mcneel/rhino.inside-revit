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
        if (Value is DB.ElementType type)
        {
          var displayName = string.Empty;

          if (type.Category is DB.Category category)
            displayName += category.FullName();
          displayName += " : ";

          var familyName = type.GetFamilyName();
          if (!string.IsNullOrEmpty(familyName))
            displayName += familyName;
          displayName += " : ";

          displayName += type.Name;

          if
          (
            type.get_Parameter(DB.BuiltInParameter.ALL_MODEL_TYPE_MARK) is DB.Parameter parameter &&
            parameter.HasValue
          )
          {
            var mark = parameter.AsString();
            if (!string.IsNullOrEmpty(mark))
              displayName += $" [{mark}]";
          }

          return displayName;
        }

        return base.DisplayName;
      }
    }


    public string FamilyName => Value?.GetFamilyName();

    #region Identity Data
    public override string Mark
    {
      get => Value?.get_Parameter(DB.BuiltInParameter.ALL_MODEL_TYPE_MARK) is DB.Parameter parameter &&
        parameter.HasValue ?
        parameter.AsString() :
        default;
      set
      {
        if (value is object)
          Value?.get_Parameter(DB.BuiltInParameter.ALL_MODEL_TYPE_MARK)?.Set(value);
      }
    }
    #endregion
  }
}

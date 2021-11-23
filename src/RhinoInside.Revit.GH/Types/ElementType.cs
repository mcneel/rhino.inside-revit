using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Element Type")]
  public interface IGH_ElementType : IGH_Element
  {
    string FamilyName { get; }
  }

  [Kernel.Attributes.Name("Element Type")]
  public class ElementType : Element, IGH_ElementType
  {
    protected override Type ValueType => typeof(ARDB.ElementType);
    public static explicit operator ARDB.ElementType(ElementType value) => value?.Value;
    public new ARDB.ElementType Value => base.Value as ARDB.ElementType;

    public ElementType() { }
    protected ElementType(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }

    public ElementType(ARDB.ElementType elementType) : base(elementType) { }

    public override string DisplayName
    {
      get
      {
        if (Value is ARDB.ElementType type && type.GetFamilyName() is string familyName && familyName.Length > 0)
          return $"{familyName} : {Name}";

        return base.DisplayName;
      }
    }

    public string FamilyName => Value?.GetFamilyName();

    #region Identity Data
    public override string Mark
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_TYPE_MARK) is ARDB.Parameter parameter &&
        parameter.HasValue ?
        parameter.AsString() :
        default;
      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_TYPE_MARK)?.Update(value);
      }
    }
    #endregion
  }
}

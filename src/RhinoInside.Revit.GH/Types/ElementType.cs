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

    #region DocumentObject
    public override string DisplayName => Value is ARDB.ElementType type && type.GetFamilyName() is string familyName && familyName.Length > 0 ?
      $"{familyName} : {base.DisplayName}" :
      base.DisplayName;
    #endregion

    #region ModelContent
    protected override string ElementPath => FamilyName is string familyName && familyName.Length > 0 ?
      $"{familyName} : {base.ElementPath}" :
      base.ElementPath;
    #endregion

    #region Properties
    public string FamilyName => Value?.GetFamilyName();
    #endregion
  }
}

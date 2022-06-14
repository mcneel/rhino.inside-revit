using System;
using System.Linq;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Family")]
  public interface IGH_Family : IGH_Element { }

  [Kernel.Attributes.Name("Family")]
  public class Family : Element, IGH_Family
  {
    protected override Type ValueType => typeof(ARDB.Family);
    public static explicit operator ARDB.Family(Family value) => value?.Value;
    public new ARDB.Family Value => base.Value as ARDB.Family;

    public Family() { }
    public Family(ARDB.Family family) : base(family) { }
    public override string DisplayName
    {
      get
      {
        if (Value is ARDB.Family family)
        {
          var familyCategory = family.FamilyCategory;
          if 
          (
            familyCategory is null &&
            family.GetFamilySymbolIds().FirstOrDefault() is ARDB.ElementId typeId &&
            family.Document.GetElement(typeId) is ARDB.ElementType type
          )
            familyCategory = type.Category;

          return $"{familyCategory?.Name} : {family.Name}";
        }

        return base.DisplayName;
      }
    }

    public override bool CastFrom(object source)
    {
      if (source is Document doc)
        return SetValue(doc.Value.OwnerFamily);

      return base.CastFrom(source);
    }
  }

  [Kernel.Attributes.Name("Form Element")]
  public class CombinableElement : GeometricElement, IGH_InstanceElement
  {
    protected override Type ValueType => typeof(ARDB.CombinableElement);
    public new ARDB.CombinableElement Value => base.Value as ARDB.CombinableElement;

    public CombinableElement() { }
    public CombinableElement(ARDB.CombinableElement element) : base(element) { }

    #region Category
    public override Category Category
    {
      set
      {
        if (value is object && Value is ARDB.Element element)
        {
          using (var parameter = element.get_Parameter(ARDB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY))
          {
            if (parameter is null)
            {
              if (value.Id != ARDB.ElementId.InvalidElementId)
                throw new Exceptions.RuntimeErrorException($"{((IGH_Goo) this).TypeName} '{DisplayName}' does not support assignment of a Category.");
            }
            else
            {
              AssertValidDocument(value, nameof(Category));
              parameter.Update(value);
            }
          }
        }
      }
    }
    #endregion
  }
}

using System;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Combinable")]
  public class CombinableElement : GeometricElement, IGH_InstanceElement
  {
    protected override Type ValueType => typeof(ARDB.CombinableElement);
    public new ARDB.CombinableElement Value => base.Value as ARDB.CombinableElement;

    public CombinableElement() { }
    public CombinableElement(ARDB.CombinableElement element) : base(element) { }
  }

  [Kernel.Attributes.Name("Combination")]
  public class GeomCombination : CombinableElement
  {
    protected override Type ValueType => typeof(ARDB.GeomCombination);
    public new ARDB.GeomCombination Value => base.Value as ARDB.GeomCombination;

    public GeomCombination() { }
    public GeomCombination(ARDB.GeomCombination element) : base(element) { }
  }

  [Kernel.Attributes.Name("Form")]
  public class GenericForm : CombinableElement
  {
    protected override Type ValueType => typeof(ARDB.GenericForm);
    public new ARDB.GenericForm Value => base.Value as ARDB.GenericForm;

    public GenericForm() { }
    public GenericForm(ARDB.GenericForm element) : base(element) { }

    #region Category
    public override Category Subcategory
    {
      get
      {
        var paramId = ARDB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY;
        if (paramId != ARDB.BuiltInParameter.INVALID && Value is ARDB.Element element)
        {
          using (var parameter = element.get_Parameter(paramId))
          {
            if (parameter?.AsElementId() is ARDB.ElementId categoryId)
            {
              var category = new Category(Document, categoryId);
              return category.APIObject?.Parent is null ? new Category() : category;
            }
          }
        }

        return default;
      }

      set
      {
        var paramId = ARDB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY;
        if (value is object && Value is ARDB.Element element)
        {
          using (var parameter = element.get_Parameter(paramId))
          {
            if (parameter is null)
            {
              if (value.Id != ARDB.ElementId.InvalidElementId)
                throw new Exceptions.RuntimeErrorException($"{((IGH_Goo) this).TypeName} '{DisplayName}' does not support assignment of a Subcategory.");
            }
            else
            {
              AssertValidDocument(value, nameof(Subcategory));
              parameter.Update(value);
            }
          }
        }
      }
    }
    #endregion
  }
}

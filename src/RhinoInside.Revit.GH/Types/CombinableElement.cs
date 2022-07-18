using System;
using System.Linq;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Form")]
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

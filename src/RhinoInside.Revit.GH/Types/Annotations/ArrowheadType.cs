using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Arrowhead Type")]
  public class ArrowheadType : ElementType
  {
    public ArrowheadType() { }
    public ArrowheadType(ARDB.ElementType type) : base(type) { }

    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static bool IsValidElement(ARDB.Element elementType)
    {
      return elementType.Category is null && elementType.get_Parameter(ARDB.BuiltInParameter.ARROW_TYPE) is object;
    }
  }
}

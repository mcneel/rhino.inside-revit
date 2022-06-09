using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  /// <summary>
  /// Interface that represents any <see cref="ARDB.Element"/> that is a geometric element but is also in a category.
  /// </summary>
  [Kernel.Attributes.Name("Instance Element")]
  public interface IGH_InstanceElement : IGH_GeometricElement
  {
    Level Level { get; }
  }

  [Kernel.Attributes.Name("Instance Element")]
  public class InstanceElement : GeometricElement, IGH_InstanceElement
  {
    public InstanceElement() { }
    public InstanceElement(ARDB.Element element) : base(element) { }

    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      if (element?.Category is null)
        return false;

      return GeometricElement.IsValidElement(element);
    }
  }
}

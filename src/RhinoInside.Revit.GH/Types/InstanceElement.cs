using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  /// <summary>
  /// Interface that represents any <see cref="DB.Element"/> that is an instance of a <see cref="DB.ElementType"/>
  /// </summary>
  [Kernel.Attributes.Name("Instance")]
  public interface IGH_InstanceElement : IGH_GeometricElement
  {
    Level Level { get; }
  }

  [Kernel.Attributes.Name("Instance")]
  public class InstanceElement : GeometricElement, IGH_InstanceElement
  {
    public InstanceElement() { }
    public InstanceElement(DB.Element element) : base(element) { }

    protected override bool SetValue(DB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(DB.Element element)
    {
      if (element.Category is null)
        return false;

      return element.CanHaveTypeAssigned();
    }

    public override Level Level => (Value is DB.Element element) ?
      new Level(element.Document, element.LevelId) :
      default;
  }
}

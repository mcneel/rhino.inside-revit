using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  /// <summary>
  /// Interface that represents any <see cref="DB.Element"/> that is an instance of a <see cref="DB.ElementType"/>
  /// </summary>
  public interface IGH_InstanceElement : IGH_GeometricElement
  {
    ElementType ElementType { get; }
    Level Level { get; }
    View OwnerView { get; }
  }

  public class InstanceElement : GeometricElement, IGH_InstanceElement
  {
    public override string TypeDescription => "Represents a Revit Instance";

    public InstanceElement() { }
    public InstanceElement(DB.Element element) : base(element) { }

    protected override bool SetValue(DB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(DB.Element element)
    {
      // DB.ElementType CanHaveTypeAssigned return false.
      //if (element is DB.ElementType)
      //  return false;

      // DB.View do not have category.
      //if (element is DB.View)
      //  return false;

      if (element.Category is null)
        return false;

      return element.CanHaveTypeAssigned();
    }

    public ElementType ElementType =>
      ElementType.FromElementId(Document, (APIElement as DB.Element)?.GetTypeId()) as ElementType;

    public override Level Level =>
      Level.FromElementId(Document, (APIElement as DB.Element)?.LevelId) as Level;

    public View OwnerView =>
      View.FromElementId(Document, (APIElement as DB.Element)?.OwnerViewId) as View;
  }
}

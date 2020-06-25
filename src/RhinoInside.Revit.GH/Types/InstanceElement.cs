using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  /// <summary>
  /// Class that represents any <see cref="DB.Element"/> that is an instance of a <see cref="DB.ElementType"/>
  /// </summary>
  public class InstanceElement : GeometricElement
  {
    public override string TypeDescription => "Represents a Revit Instance";

    public InstanceElement() { }
    public InstanceElement(DB.Element element) : base(element) { }

    protected override bool SetValue(DB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(DB.Element element)
    {
      if (element is DB.ElementType)
        return false;

      if (element is DB.View)
        return false;

      return element.Category is object && element.CanHaveTypeAssigned();
    }

    public ElementType ElementType
    {
      get
      {
        var element = (DB.Element) this;
        return ElementType.FromElement(element.Document.GetElement(element.GetTypeId())) as ElementType;
      }
    }

    public override Level Level
    {
      get
      {
        var element = (DB.Element) this;
        return Level.FromElement(element.Document.GetElement(element.LevelId)) as Level;
      }
    }

    public View View
    {
      get
      {
        var element = (DB.Element) this;
        return View.FromElement(element.Document.GetElement(element.OwnerViewId)) as View;
      }
    }
  }
}

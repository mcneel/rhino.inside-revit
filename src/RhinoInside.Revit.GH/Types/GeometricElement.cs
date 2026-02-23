using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Geometric Element")]
  public interface IGH_GeometricElement : IGH_GraphicalElement { }

  [Kernel.Attributes.Name("Geometric Element")]
  public partial class GeometricElement : GraphicalElement,
    IGH_GeometricElement,
    IHostElementAccess,
    IGH_PreviewMeshData,
    Bake.IGH_BakeAwareElement
  {
    public GeometricElement() { }
    public GeometricElement(ARDB.Element element) : base(element) { }

    public static new bool IsValidElement(ARDB.Element element)
    {
      if (element.Category is null)
        return false;

      if (!GraphicalElement.IsValidElement(element))
        return false;

      return element.HasGeometry();
    }

    #region IHostElementAccess
    GraphicalElement IHostElementAccess.HostElement => HostElement;

    public virtual GraphicalElement HostElement => Value is ARDB.Element element ?
      GetElement<GraphicalElement>(element.LevelId) :
      default;
    #endregion
  }
}

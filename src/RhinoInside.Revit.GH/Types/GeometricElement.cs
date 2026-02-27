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

    public virtual GraphicalElement HostElement
    {
      get
      {
        if (Value is ARDB.Element element)
        {
          var hostId = default(ARDB.ElementId);

          if (element is ARDB.Structure.Rebar rebar) hostId = rebar.GetHostId();
          else if (element is ARDB.Structure.RebarInSystem rebarInSystem) hostId = rebarInSystem.GetHostId();
          else if (element is ARDB.Structure.RebarContainer rebarContainer) hostId = rebarContainer.GetHostId();
          else if (element is ARDB.Structure.AreaReinforcement areaReinforcement) hostId = areaReinforcement.GetHostId();
          else if (element is ARDB.Structure.PathReinforcement pathReinforcement) hostId = pathReinforcement.GetHostId();
          else if (element is ARDB.Structure.FabricSheet fabricSheet) hostId = fabricSheet.HostId;
          else if (element is ARDB.FabricationPart fabricationPart)
          {
            using (var hostedInfo = fabricationPart.GetHostedInfo())
              hostId = hostedInfo.HostId;
          }
          else hostId = element.get_Parameter(ARDB.BuiltInParameter.HOST_ID_PARAM)?.AsElementId();

          return GetElement<GraphicalElement>(hostId ?? LevelId);
        }

        return default;
      }
    }
    #endregion
  }
}

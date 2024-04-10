using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.HostObjects
{
  [ComponentVersion(introduced: "1.0", updated: "1.13")]
  public class ElementHost : Component
  {
    public override Guid ComponentGuid => new Guid("6723BEB1-DD99-40BE-8DA9-13B3812D6B46");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public ElementHost() : base
    (
      name: "Element Host",
      nickname: "Host",
      description: "Obtains the host of the specified element",
      category: "Revit",
      subCategory: "Architecture"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.GraphicalElement(), "Element", "E", "Element to query for its host", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GraphicalElement(), "Host", "H", "Element host object", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.GraphicalElement element, x => x.IsValid)) return;

      // Ask first to Element, maybe it knows its Host
      if (element is Types.IHostElementAccess access)
      {
        DA.SetData("Host", access.HostElement);
        return;
      }

      var hostId = default(ARDB.ElementId);

      if (element.Value is ARDB.Structure.Rebar rebar) hostId = rebar.GetHostId();
      else if (element.Value is ARDB.Structure.RebarInSystem rebarInSystem) hostId = rebarInSystem.GetHostId();
      else if (element.Value is ARDB.Structure.RebarContainer rebarContainer) hostId = rebarContainer.GetHostId();
      else if (element.Value is ARDB.Structure.AreaReinforcement areaReinforcement) hostId = areaReinforcement.GetHostId();
      else if (element.Value is ARDB.Structure.PathReinforcement pathReinforcement) hostId = pathReinforcement.GetHostId();
      else if (element.Value is ARDB.Structure.FabricSheet fabricSheet) hostId = fabricSheet.HostId;
      else if (element.Value is ARDB.FabricationPart fabricationPart)
      {
        using (var hostedInfo = fabricationPart.GetHostedInfo())
          hostId = hostedInfo.HostId;
      }
      else hostId = element.Value.get_Parameter(ARDB.BuiltInParameter.HOST_ID_PARAM)?.AsElementId();

      // Default to Level if hostId is null
      DA.SetData("Host", element.GetElement<Types.GraphicalElement>(hostId ?? element.LevelId));
    }
  }
}

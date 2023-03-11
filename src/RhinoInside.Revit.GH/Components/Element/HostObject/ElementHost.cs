using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.HostObjects
{
  using External.DB;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.13")]
  public class ElementHost : Component
  {
    public override Guid ComponentGuid => new Guid("6723BEB1-DD99-40BE-8DA9-13B3812D6B46");

    public ElementHost() : base
    (
      name: "Element Host",
      nickname: "Host",
      description: "Obtains the host of the specified element",
      category: "Revit",
      subCategory: "Host"
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
      if (element is Types.IHostElementAccess access && access.HostElement is Types.GraphicalElement hostElement)
      {
        DA.SetData("Host", hostElement);
        return;
      }

      // Special cases
      var host = element.Value.get_Parameter(ARDB.BuiltInParameter.HOST_ID_PARAM)?.AsElement();
      DA.SetData("Host", element.GetElement<Types.GraphicalElement>(host ?? element.Level?.Value));
    }
  }
}

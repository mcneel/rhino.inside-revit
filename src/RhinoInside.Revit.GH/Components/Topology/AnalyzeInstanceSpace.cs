using System;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Topology
{
  using External.DB.Extensions;
  using RhinoInside.Revit.Convert.Geometry;

  [ComponentVersion(introduced: "1.0", updated: "1.16")]
  public class AnalyzeInstanceSpace : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("6AC37380-D14F-46BF-835C-611DB8C38E3B");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AnalyzeInstanceSpace() : base
    (
      name: "Component Neighbours",
      nickname: "C-Nbours",
      description: "Query spatial elements surrounding the given component",
      category: "Revit",
      subCategory: "Topology"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.FamilyInstance>("Component", "C", "Component element"),
      ParamDefinition.Create<Parameters.Phase>("Phase", "P", "Phase to query surrounding spatial elements from", optional: true, relevance: ParamRelevance.Primary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.RoomElement>("From Room", "FR", "Room that given instance is originating from", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.RoomElement>("To Room", "TR", "Room that given instance is ending at", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.RoomElement>("Room", "R", "Room that spatially contains given instance", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.SpaceElement>("Space", "S", "Space that spatially contains given instance", relevance: ParamRelevance.Primary),
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Output<IGH_Param>("FromRoom") is IGH_Param fromRoom)
        fromRoom.Name = "From Room";

      if (Params.Output<IGH_Param>("To Room") is IGH_Param toRoom)
        toRoom.Name = "To Room";

      base.AddedToDocument(document);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Component", out Types.FamilyInstance element, x => x.IsValid)) return;
      if (element.Document.IsFamilyDocument) return;

      if (!Params.TryGetData(DA, "Phase", out Types.Phase phase, x => x.IsValid)) return;
      if (phase is object && !element.Document.IsEquivalent(phase.Document))
        throw new Exceptions.RuntimeArgumentException("Phase", $"Invalid document. {{{phase.Id}}}");

      phase = phase ?? Types.Phase.FromValue(element.Document.Phases.Cast<ARDB.Phase>().LastOrDefault()) as Types.Phase;
      var phaseStaus = element.Value.GetPhaseStatus(phase.Id);
      if (phaseStaus == ARDB.ElementOnPhaseStatus.Existing || phaseStaus == ARDB.ElementOnPhaseStatus.New)
      {
        Params.TrySetData(DA, "From Room", () => element.GetElement<Types.RoomElement>(element.Value.get_FromRoom(phase.Value)));
        Params.TrySetData(DA, "To Room", () => element.GetElement<Types.RoomElement>(element.Value.get_ToRoom(phase.Value)));
        Params.TrySetData(DA, "Room", () => element.GetElement<Types.RoomElement>(element.Value.get_Room(phase.Value)));
        Params.TrySetData(DA, "Space", () => element.GetElement<Types.SpaceElement>(element.Value.get_Space(phase.Value)));
      }
      else
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Instance element does not exist on phase '{phase.Nomen}'. {{{element.Id}}}");
      }
    }
  }
}

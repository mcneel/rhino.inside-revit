using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components.Elements
{
  using Convert.Geometry;
  using External.DB;
  using External.DB.Extensions;
  using GH.Exceptions;

  [ComponentVersion(introduced: "1.37")]
  public class ElementInterferences : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("6F9AB7AF-302C-478B-BD61-EB989ACBE630");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    static ARDB.ElementFilter ElementFilter => ElementFilters.Intersect
    (
      ElementFilters.ElementIsViewSpecificFilter(inverted: true),
      ElementFilters.ElementHasPhase
    );

    public ElementInterferences() : base
    (
      name: "Element Interferences",
      nickname: "E-Interferences",
      description: "Check for solid-solid interferences betwen elements.",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.GraphicalElement>("Element", "E", "Target element"),
      ParamDefinition.Create<Parameters.GraphicalElement>("Candidates", "C", "Candidate elements for interference checking", access: GH_ParamAccess.list),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Target element",
          DataMapping = GH_DataMapping.Graft
        },ParamRelevance.Primary
      ),
      ParamDefinition.Create<Parameters.GraphicalElement>("Interferences", "I", "Elements interferences list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.GraphicalElement element)) return;
      Params.TrySetData(DA, "Element", () => element);
      if (!Params.TryGetDataList(DA, "Candidates", out IList<Types.GraphicalElement> candidates)) return;

      var interferences = new List<Types.GraphicalElement>();

      candidates = candidates.Where(x => x?.IsValid is true && ElementFilter.PassesFilter(x.Value)).ToList();
      foreach (var candidateSet in candidates.GroupBy(x => x.Source))
      {
        var source = candidateSet.Key;
        if (element.AssertValidElementSource(source, acceptLinked: true))
        {
          var elementToWorld = element.Source is Types.RevitLinkInstance elementInstance ?
            Transform.ChangeBasis(Plane.WorldXY, elementInstance.Location) : Transform.Identity;
          elementToWorld.TryGetInverse(out var worldToElement);

          var worldToSource = source is Types.RevitLinkInstance sourceInstance ?
            Transform.ChangeBasis(Plane.WorldXY, sourceInstance.Location) : Transform.Identity;
          worldToSource.TryGetInverse(out var sourceToWorld);

          var xform = worldToElement * worldToSource;
          var bbox = element.GetBoundingBox(xform);
          if (!bbox.IsValid) return;

          var elements = candidateSet.Select(x => x.Value);
          elements = elements.WherePasses(ElementFilters.BoundingBoxIntersectsFilter(bbox.ToOutline(), GeometryTolerance.Internal.DefaultTolerance, inverted: false));
          if (element.Source.SourceDocument.Value.IsEquivalent(source.SourceDocument.Value))
            elements = elements.WherePasses(ElementFilters.ExclusionFilter(element.Id));

          {
            var createdPhaseId = element.Value.CreatedPhaseId;
            var demolishedPhaseId = element.Value.DemolishedPhaseId;

            {
              if (element.Source is Types.RevitLinkInstance elementSource)
              {
#if !REVIT_2019
              throw new RuntimeErrorException("Linked element intersection is only supported on Revit 2019 or above.");
#else
                var map = InverseMap(elementSource.Document, elementSource.SourceDocument.Value,
                                    (elementSource.Type.Value as ARDB.RevitLinkType).GetPhaseMap());

                createdPhaseId = GetPhaseIdAtHost(map, elementSource.SourceDocument.Value, createdPhaseId);
                demolishedPhaseId = GetPhaseIdAtHost(map, elementSource.SourceDocument.Value, demolishedPhaseId);
#endif
              }

              if (source is Types.RevitLinkInstance instanceSource)
              {
#if !REVIT_2019
              throw new RuntimeErrorException("Linked element intersection is only supported on Revit 2019 or above.");
#else
                var map = (instanceSource.Type.Value as ARDB.RevitLinkType).GetPhaseMap();
                if (!map.TryGetValue(createdPhaseId, out createdPhaseId)) createdPhaseId = ElementIdExtension.Invalid;
                if (!map.TryGetValue(demolishedPhaseId, out demolishedPhaseId)) demolishedPhaseId = ElementIdExtension.Invalid;
#endif
              }
            }

            elements = elements.WherePasses(ElementFilters.ElementIntersectsPhase(createdPhaseId, demolishedPhaseId));
          }

          if (element.Value.HasGeometry())
          {
            using (var options = new ARDB.Options() { DetailLevel = ARDB.ViewDetailLevel.Medium })
              elements = elements.WherePasses(ElementFilters.ElementIntersectsElementSolidsFilter(element.Value, options, xform.ToTransform()));
          }
          else AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Element has no geometry. {{{element.Id}}}");

          interferences.AddRange(elements.OfType<Types.GraphicalElement>().FromSource(source).TakeWhileIsNotEscapeKeyDown(this));
        }
      }

      DA.SetDataList("Interferences", interferences);
    }

    static KeyValuePair<ARDB.Phase, ARDB.Phase>[] InverseMap
    (
      ARDB.Document host, ARDB.Document link,
      IDictionary<ARDB.ElementId, ARDB.ElementId> map
    )
    {
      return map.
        Select(x => new KeyValuePair<ARDB.Phase, ARDB.Phase>(link.GetElement(x.Value) as ARDB.Phase, host.GetElement(x.Key) as ARDB.Phase)).
        OrderBy(x => GetPhaseIndex(x.Value)).
        ThenBy(x => GetPhaseIndex(x.Key)).
        ToArray();
    }

    static ARDB.ElementId GetPhaseIdAtHost
    (
      KeyValuePair<ARDB.Phase, ARDB.Phase>[] map,
      ARDB.Document link, ARDB.ElementId phaseId
    )
    {
      using (var phases = link.Phases)
      {
        do
        {
          if (TryGetPhaseAtHost(map, phaseId, out var hostId))
            return hostId;

          var phase = link.GetElement<ARDB.Phase>(phaseId);
          var next = phase is object ? GetPhase(phases, GetPhaseIndex(phase) + 1) : null;
          phaseId = next?.Id;

        } while (phaseId is object);
      }

      return phaseId ?? ElementIdExtension.Invalid;
    }

    static bool TryGetPhaseAtHost(KeyValuePair<ARDB.Phase, ARDB.Phase>[] phases, ARDB.ElementId key, out ARDB.ElementId value)
    {
      foreach (var item in phases)
      {
        if (item.Key.Id == key)
        {
          value = item.Value.Id;
          return true;
        }
      }

      value = default;
      return false;
    }

    static int GetPhaseIndex(ARDB.Phase phase) =>
      phase.get_Parameter(ARDB.BuiltInParameter.PHASE_SEQUENCE_NUMBER).AsInteger() - 1;

    static ARDB.Phase GetPhase(ARDB.PhaseArray phases, int index) =>
      index >= 0 && index < phases.Size ? phases.get_Item(index) : null;
  }
}

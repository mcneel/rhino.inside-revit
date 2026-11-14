using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Exceptions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Site
{
#if REVIT_2024
  [ComponentVersion(introduced: "1.37"), ComponentRevitAPIVersion(min: "2025.0")]
  public class ExcavateToposolid : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("A3AF16C8-F736-407D-A545-36BBD399EBD8");
    public override GH_Exposure Exposure => SDKCompliancy(GH_Exposure.tertiary);
    protected override string IconTag => string.Empty;

    public ExcavateToposolid()
    : base
    (
      name: "Excavate Toposolid",
      nickname: "Excavate",
      description: "Get-Set access component to excavated Elements.",
      category: "Revit",
      subCategory: "Site"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Toposolid()
        {
          Name = "Toposolid",
          NickName = "T",
          Description = "Target Toposolid",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Excavate",
          NickName = "E",
          Description = "Elements to excavate toposolid",
          Access = GH_ParamAccess.list,
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Unexcavate",
          NickName = "U",
          Description = "Elements to unexcavate from toposolid",
          Access = GH_ParamAccess.list,
          Optional = true
        }, ParamRelevance.Secondary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Toposolid()
        {
          Name = "Toposolid",
          NickName = "T",
          Description = "Target Toposolid",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Excavated",
          NickName = "E",
          Description = "Elements excavated from toposolid.",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Unexcavated",
          NickName = "U",
          Description = "Elements unexcavated from toposolid",
          Access = GH_ParamAccess.list,
          Optional = true
        }, ParamRelevance.Secondary
      ),
    };

    bool ReportFailure(string message)
    {
      if (string.IsNullOrEmpty(message))
        return false;

      if (FailureProcessingMode == ARDB.FailureProcessingResult.Continue)
      {
        AddContinuableFailure(message);
        throw new RuntimeException();
      }

      if (FailureProcessingMode != ARDB.FailureProcessingResult.ProceedWithCommit)
        throw new RuntimeException(message);

      AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);
      return true;
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
#if REVIT_2025
      if (!Params.GetData(DA, "Toposolid", out Types.Toposolid toposolid)) return;
      else DA.SetData("Toposolid", toposolid);
      if (!Params.TryGetDataList(DA, "Excavate", out IList<Types.GraphicalElement> add)) return;
      if (!Params.TryGetDataList(DA, "Unexcavate", out IList<Types.GraphicalElement> remove)) return;

      if (add is object || remove is object)
      {
        var added = new List<Types.GraphicalElement>(add?.Count ?? 0);
        var removed = new List<Types.GraphicalElement>(remove?.Count ?? 0);

        var excavated = new HashSet<ARDB.ElementId>
        (
          toposolid.Value.GetIntersectingElementData().
          Where(x => x.IntersectionType == ARDB.IntersectionType.Excavate).
          Select(x => x.IntersectingElementId),
          default(ElementIdEqualityComparer)
        );

        UpdateElement
        (
          toposolid.Value, () =>
          {
            foreach (var cutter in remove ?? Array.Empty<Types.GraphicalElement>())
            {
              if (!(cutter?.IsValid is true)) continue;

              var message = string.Empty;
              if (!excavated.Contains(cutter.Id))
              {
                if (FailureProcessingMode != ARDB.FailureProcessingResult.ProceedWithCommit)
                  message = $"The target toposolid is not excavated by the provied element. {{{cutter.Id}}}";
                else
                {
                  removed.Add(cutter);
                  continue;
                }
              }

              if (!ReportFailure(message))
              {
                toposolid.Value.RemoveExcavationBy(cutter.Id);
                toposolid.InvalidateGraphics();
                excavated.Remove(cutter.Id);
                removed.Add(cutter);
              }
            }

            foreach (var cutter in add ?? Array.Empty<Types.GraphicalElement>())
            {
              if (!(cutter?.IsValid is true)) continue;

              var message = string.Empty;
              if (!toposolid.Value.CanBeExcavatedBy(cutter.Id) || toposolid.Id == cutter.Id)
              {
                message = $"The toposolid can't be excavated by the specified element. {{{cutter.Id}}}";
              }
              else if (!excavated.Contains(cutter.Id))
              {
                toposolid.Value.ExcavateBy(cutter.Id);
                toposolid.InvalidateGraphics();
                added.Add(cutter);
                excavated.Add(cutter.Id);
              }

              ReportFailure(message);
            }
          }
        );

        Params.TrySetDataList(DA, "Excavated", () => added.Where(x => excavated.Contains(x.Id)));
        Params.TrySetDataList(DA, "Unexcavated", () => removed.Where(x => !excavated.Contains(x.Id)));
      }
      else
      {
        Params.TrySetDataList
        (
          DA, "Excavated",
          () => toposolid.Value.GetIntersectingElementData().
                Where(x => x.IntersectionType == ARDB.IntersectionType.Excavate).
                Select(x => x.IntersectingElementId).
                Select(x => toposolid.GetElement<Types.GraphicalElement>(x))
        );
        Params.TrySetDataList
        (
          DA, "Unexcavated", () => Array.Empty<Types.GraphicalElement>()
        );
      }
#endif
    }
  }
#endif
}

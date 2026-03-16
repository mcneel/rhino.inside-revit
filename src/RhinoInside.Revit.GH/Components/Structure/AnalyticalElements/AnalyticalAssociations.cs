using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Structure
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.36")]
  public class AnalyticalAssociations : TransactionalChainComponent, IGH_TaskCapableComponent
  {
    public override Guid ComponentGuid => new Guid("{6784ABA1-9109-45E7-8DBE-ABF48D48AD23}");
#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
#else
    public override GH_Exposure Exposure => GH_Exposure.hidden;
#endif

    protected override string IconTag => string.Empty;

    public AnalyticalAssociations() : base
    (
      name: "Analytical Associations",
      nickname: "A-Associations",
      description: "Get-Set associations between analytical and model elements",
      category: "Revit",
      subCategory: "Structure"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.AnalyticalElement()
        {
          Name = _AnalyticalElements_,
          NickName = "AE",
          Description = _AnalyticalElements_,
          Access = GH_ParamAccess.list,
          Optional = true,
          DataMapping = GH_DataMapping.Graft
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _PhysicalElements_,
          NickName = "ME",
          Description = _PhysicalElements_,
          Access = GH_ParamAccess.list,
          Optional = true,
          DataMapping = GH_DataMapping.Graft
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AnalyticalElement()
        {
          Name = _AnalyticalElements_,
          NickName = "AE",
          Description = "Analytical elements associated",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _PhysicalElements_,
          NickName = "ME",
          Description = "Model elements associated",
          Access = GH_ParamAccess.list
        }
      ),
    };

    const string _AnalyticalElements_ = "Analytical Elements";
    const string _PhysicalElements_ = "Model Elements";

    void IGH_TaskCapableComponent.RequestTaskCancellation() { }
    public bool UseTasks
    {
      get => Params.Input.All(x => x.SourceCount > 0);
      set { }
    }
    public bool InPreSolve { get; set; }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Params.TryGetDataList(DA, _AnalyticalElements_, out IList<Types.AnalyticalElement> analytical)) return;
      if (!Params.TryGetDataList(DA, _PhysicalElements_, out IList<Types.GraphicalElement> physical)) return;

      analytical ??= Array.Empty<Types.AnalyticalElement>();
      physical ??= Array.Empty<Types.GraphicalElement>();

      var analyticalSet = analytical.Where(x => x?.IsValid is true).ToHashSet();
      var physicalSet = physical.Where(x => x?.IsValid is true).ToHashSet();

      foreach (var element in physicalSet)
      {
        if (element.IsPhysicalElement) continue;

        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Input element is not a model analytical element.{{{element.Id}}}");
        return;
      }

      var document = default(ARDB.Document);
      foreach (var element in analyticalSet.Concat(physicalSet))
      {
        if (document is null) document = element.Document;
        if (document.IsEquivalent(element.Document)) continue;

        document = null;
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid document");
        return;
      }

      if (analyticalSet.Count == 0 && physicalSet.Count == 0)
        return;

      if (InPreSolve)
      {
        if (document is object)
        {
          StartTransaction(document);
          Types.AnalyticalElement.Associate(analyticalSet, physicalSet);
        }

        return;
      }
      else if (UseTasks)
      {
        physicalSet.Clear();
      }

      if (physicalSet.Count == 0)
      {
        var analyticalElement = analyticalSet.First();
        var physicalElements = analyticalElement.PhysicalElements;
        if (physicalElements.FirstOrDefault() is Types.GraphicalElement physicalElement)
        {
          if (analyticalSet.SetEquals(physicalElement.AnalyticalElements))
          {
            Params.TrySetDataList(DA, _AnalyticalElements_, () => analyticalSet);
            Params.TrySetDataList(DA, _PhysicalElements_, () => physicalElements);
          }
        }
      }
      else if (analyticalSet.Count == 0)
      {
        var physicalElement = physicalSet.First();
        var analyticalElements = physicalElement.AnalyticalElements;
        if (analyticalElements.FirstOrDefault() is Types.AnalyticalElement analyticalElement)
        {
          if (physicalSet.SetEquals(analyticalElement.PhysicalElements))
          {
            Params.TrySetDataList(DA, _AnalyticalElements_, () => analyticalElements);
            Params.TrySetDataList(DA, _PhysicalElements_, () => physicalSet);
          }
        }
      }
    }
  }
}

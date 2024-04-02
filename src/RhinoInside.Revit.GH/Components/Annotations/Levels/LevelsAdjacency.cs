using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Annotations.Levels
{
  using System.Collections;
  using System.Collections.Generic;
  using System.Linq;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.21")]
  public class LevelsAdjacency : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("EE672A3F-CF9F-4680-88DD-07FD58CCA8A5");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public LevelsAdjacency()
    : base("Levels Adjacency", "L-Adjacency", "Query closest levels to input elevation", "Revit", "Model")
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Level>("Levels", "L", access: GH_ParamAccess.list),
      ParamDefinition.Create<Parameters.ProjectElevation>("Elevations", "E", "Elevations", optional: true, access: GH_ParamAccess.list, relevance: ParamRelevance.Primary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Level>("Levels", "L", "Levels sorted by elevation", access: GH_ParamAccess.list, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Level>("Above", "+1", access: GH_ParamAccess.list, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Level>("Closest", "C", access: GH_ParamAccess.list, relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Parameters.Level>("Below", "-1", access: GH_ParamAccess.list, relevance: ParamRelevance.Primary),
    };

    struct ElevationComparer : IComparer, IEqualityComparer<Types.Level>
    {
      public int Compare(object x, object y)
      {
        var left  = (x is double xe) ? xe : (x is Types.Level xl) ? xl.Elevation : default(double?);
        var right = (y is double ye) ? ye : (y is Types.Level yl) ? yl.Elevation : default(double?);
        if (left < right) return -1;
        if (left > right) return +1;
        return 0;
      }

      public bool Equals(Types.Level x, Types.Level y) => x?.Elevation == y?.Elevation;
      public int GetHashCode(Types.Level obj) => obj.Elevation.GetHashCode();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetDataList(DA, "Levels", out IList<Types.Level> levels)) return;
      if (levels?.Count > 0)
      {
        if (!Params.TryGetDataList(DA, "Elevations", out IList<Types.ProjectElevation> elevations)) return;
        if (elevations is null) elevations = levels.Select(x => new Types.ProjectElevation(x)).ToArray();

        levels = levels.
          Where(x => x?.IsValid is true).
          OrderBy(x => x.Elevation).
          Distinct(default(ElevationComparer)).
          ToArray();

        Params.TrySetDataList(DA, "Levels", () => levels);

        var above = Params.IndexOfOutputParam("Above") >= 0 ? new List<Types.Level>(elevations.Count) : null;
        var closest = Params.IndexOfOutputParam("Closest") >= 0 ? new List<Types.Level>(elevations.Count) : null;
        var below = Params.IndexOfOutputParam("Below") >= 0 ? new List<Types.Level>(elevations.Count) : null;

        if (above is object || closest is object || below is object)
        {
          foreach (var elevation in elevations)
          {
            if (elevation?.IsElevation(out var z) is true)
            {
              var index = Array.BinarySearch(levels.ToArray(), z, default(ElevationComparer));
              if (index < 0) index = ~index;
              if (index >= levels.Count) index = levels.Count - 1;

              if (index > 0 && z - levels[index - 1].Elevation < levels[index].Elevation - z) index--;

              above?.Add(z < levels[index].Elevation ? levels[index] : index < levels.Count - 1 ? levels[index + 1] : new Types.Level());
              closest?.Add(levels[index]);
              below?.Add(z > levels[index].Elevation ? levels[index] : index > 0 ? levels[index - 1] : new Types.Level());
            }
          }

          Params.TrySetDataList(DA, "Above", () => above);
          Params.TrySetDataList(DA, "Closest", () => closest);
          Params.TrySetDataList(DA, "Below", () => below);
        }
      }
    }
  }
}

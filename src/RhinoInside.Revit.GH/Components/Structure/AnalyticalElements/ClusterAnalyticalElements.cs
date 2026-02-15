using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Structure
{
  [ComponentVersion(introduced: "1.27"), ComponentRevitAPIVersion(min: "2023.0")]
  public class ClusterAnalyticalElements : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("22C30D64-60CA-4EF9-9C2D-412B8D38A008");

#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
#else
    public override GH_Exposure Exposure => GH_Exposure.hidden;
#endif

    public ClusterAnalyticalElements() : base
    (
      name: "Cluster Analytical Elements (Role)",
      nickname: "C-Analytical",
      description: "Split a list of analytical elements into separate clusters by their structural role",
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
            Name = "Analytical Elements",
            NickName = "A",
            Description = "List of analytical elements to be split into branches based on their structural role",
            Access = GH_ParamAccess.list
          }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs = BuildOutputs();

    static ParamDefinition[] BuildOutputs()
    {
      var list = new List<ParamDefinition>();
#if REVIT_2023
      var values = Enum.GetValues(typeof(ARDB.Structure.AnalyticalStructuralRole)).
      Cast<ARDB.Structure.AnalyticalStructuralRole>().
      Select
      (
        x =>
        {
          var name = x.ToString();
          Types.AnalyticalStructuralRole.NamedValues.TryGetValue((int) x, out name);
          return (Name: name, ViewType: x);
        }
      ).Where(x => x.Name is object);

      foreach (var value in values.OrderBy(x => x.Name))
      {
        var param = default(IGH_Param);

        switch (value.ViewType)
        {
          case ARDB.Structure.AnalyticalStructuralRole.Unset:
            param = new Parameters.AnalyticalElement();
            break;
          case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleMember:
          case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleColumn:
          case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleBeam:
          case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleGirder:
            param = new Parameters.AnalyticalMember();
            break;
          case ARDB.Structure.AnalyticalStructuralRole.StructuralRolePanel:
          case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleFloor:
          case ARDB.Structure.AnalyticalStructuralRole.StructuralRoleWall:
            param = new Parameters.AnalyticalPanel();
            break;
        }

        param.Name = value.Name;
        param.NickName = value.Name.Substring(0, 1);
        param.Description = $"Analytical elements of role \"{value.Name}\"";
        param.Access = GH_ParamAccess.list;

        list.Add(new ParamDefinition(param, ParamRelevance.Primary));
      }
#endif
      return list.ToArray();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
#if REVIT_2023
      if (!Params.GetDataList(DA, "Analytical Elements", out IList<Types.AnalyticalElement> analyticalElements)) return;

      var roles = Enum.GetValues(typeof(ARDB.Structure.AnalyticalStructuralRole));
      var clusters = new Dictionary<ARDB.Structure.AnalyticalStructuralRole, List<Types.AnalyticalElement>>(roles.Length);

      foreach (var analyticalElement in analyticalElements)
      {
        if (analyticalElement?.Value is ARDB.Structure.AnalyticalElement value)
        {
          if (!clusters.TryGetValue(value.StructuralRole, out var cluster))
            clusters.Add(value.StructuralRole, cluster = new List<Types.AnalyticalElement>());
          cluster.Add(analyticalElement);
        }
      }

      foreach (var cluster in clusters)
      {
        Types.AnalyticalStructuralRole.NamedValues.TryGetValue((int) cluster.Key, out var name);
        Params.TrySetDataList(DA, name, () => cluster.Value);
      }
#endif
    }
  }
}


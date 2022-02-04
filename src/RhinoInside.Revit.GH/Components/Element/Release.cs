using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components
{
  [ComponentVersion(introduced: "1.5")]
  public class ElementRelease : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("8621421D-B642-4C26-A88B-1760B9D97C91");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    protected override string IconTag => "";

    public ElementRelease() : base
    (
      name: "Release Element",
      nickname: "Release",
      description: "Release elements on Revit document",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Elements to Release",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Released",
          NickName = "R",
          Optional = true
        },
        ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Elements Released",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Released",
          NickName = "R",
        },
        ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetDataList(DA, "Elements", out IList<Types.Element> elements)) return;

      if (Params.GetData(DA, "Released", out bool? released) && released == true)
      {
        var sets = elements.GroupBy(x => x.Document).ToArray();
        foreach (var set in sets)
        {
          StartTransaction(set.Key);

          foreach (var element in set.Where(x => x?.IsValid == true))
          {
            element.Pinned = false;
            ElementTracking.TrackedElementsDictionary.Remove(element.Value);
          }
        }
      }

      Params.TrySetDataList(DA, "Elements", () => elements);
      Params.TrySetDataList
      (
        DA, "Released", () =>
        elements.Select(x => x?.IsValid == true ? (bool?) !ElementTracking.TrackedElementsDictionary.ContainsKey(x.Value) : null)
      );
    }
  }
}

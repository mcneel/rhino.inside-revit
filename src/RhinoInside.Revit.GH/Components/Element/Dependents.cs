using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  [ComponentVersion(introduced: "1.0", updated: "1.4")]
  public class ElementDependents : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("97D71AA8-6987-45B9-8F25-B92671E20EF4");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    protected override string IconTag => "D";

    public ElementDependents() : base
    (
      name: "Element Dependents",
      nickname: "Dependents",
      description: "Queries for all elements that, from a logical point of view, are the children of Element",
      category: "Revit",
      subCategory: "Element")
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementFilter()
        {
          Name = "Filter",
          NickName = "F",
          Description = "Filter that will be applied to dependant elements",
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
          Name = "Element",
          NickName = "E",
          Description = "Input element",
          DataMapping = GH_DataMapping.Graft
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Dependents",
          NickName = "E",
          Description = "Set of elements that from a logical point of view, are the children of input Element",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Referentials",
          NickName = "R",
          Description = "Set of elements that directly or indirectly reference input Element",
          Access = GH_ParamAccess.list
        },
        ParamRelevance.Occasional
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element)) return;
      Params.GetData(DA, "Filter", out DB.ElementFilter filter);

      filter = CompoundElementFilter.Intersect
      (
        new DB.ExclusionFilter(new DB.ElementId[] { element.Id }),
        filter ?? CompoundElementFilter.Union
        (
          CompoundElementFilter.ElementIsNotInternalFilter(element.Document),
          new DB.ElementClassFilter(typeof(DB.ExtensibleStorage.DataStorage))
        )
      );

      Params.TrySetData(DA, "Element", () => element);
      if (element.Value is object)
      {
        if (Params.IndexOfOutputParam("Referentials") >= 0)
        {
          try
          {
            var dependents = element.Document.GetDependentElements
            (
              new DB.ElementId[] { element.Id },
              out var relateds,
              filter
            );

            DA.SetDataList("Dependents", dependents.Select(x => Types.Element.FromElementId(element.Document, x)));
            DA.SetDataList("Referentials", relateds.Select(x => Types.Element.FromElementId(element.Document, x)));

            return;
          }
          catch (Autodesk.Revit.Exceptions.ArgumentException e)
          { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{e.Message} {element.Id.IntegerValue}"); }
        }

        {
          var dependents = element.Value.GetDependentElements(filter);
          DA.SetDataList("Dependents", dependents.Select(x => Types.Element.FromElementId(element.Document, x)));
        }
      }
    }
  }
}

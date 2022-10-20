using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  public class ElementPropertyCategory : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("5AC48DE6-F706-4E88-A4AD-7A4439F1DAB5");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "C";

    public ElementPropertyCategory()
    : base
    (
      "Element Category",
      "ElemCat",
      "Element Category Property. Get-Set access component to Element Category property.",
      "Revit",
      "Element"
    )
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
          Description = "Element to access Category",
        }
      )
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
          Description = "Element to access Category",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Category",
          NickName = "C",
          Description = "Element Category",
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element, x => x.IsValid)) return;
      else DA.SetData("Element", element);

      DA.SetData("Category", element.Category);
    }
  }

  public class ElementPropertyType : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("FE427D04-1D8F-48BE-BFBA-EB28AD23FC03");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "T";

    public ElementPropertyType()
    : base
    (
      "Element Type",
      "ElemType",
      "Element Type Property. Get-Set access component to Element Type property.",
      "Revit",
      "Element"
    )
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
          Description = "Element to access Type",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Element Type",
          Optional = true,
          Access = GH_ParamAccess.list
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
          Description = "Element to access Type",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Element Type",
          Access = GH_ParamAccess.list
        },
        ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetDataList(DA, "Element", out IList<Types.Element> elements)) return;
      if (Params.GetDataList(DA, "Type", out IList<Types.ElementType> types))
      {
        var outputTypes = Params.IndexOfOutputParam("Type") < 0 ? default : new List<Types.ElementType>();
        var typesSets = new Dictionary<Types.ElementType, List<ARDB.ElementId>>();

        int index = 0;
        foreach (var element in elements)
        {
          if (element is object && types.ElementAtOrLast(index) is Types.ElementType type)
          {
            outputTypes?.Add(element is object ? type : default);

            if (!typesSets.TryGetValue(type, out var entry))
              typesSets.Add(type, new List<ARDB.ElementId> { element.Id });
            else
              entry.Add(element.Id);
          }
          else outputTypes?.Add(default);

          index++;
        }

        var map = new Dictionary<ARDB.ElementId, ARDB.ElementId>();
        foreach (var type in typesSets)
        {
          UpdateDocument
          (
            type.Key.Document, () =>
            {
              foreach (var entry in ARDB.Element.ChangeTypeId(type.Key.Document, type.Value, type.Key.Id))
              {
                if (map.ContainsKey(entry.Key)) map.Remove(entry.Key);
                map.Add(entry.Key, entry.Value);
              }
            }
          );
        }

        DA.SetDataList
        (
          "Element",
          elements.Select
          (
            x =>
            x is null ? null :
            map.TryGetValue(x.Id, out var newId) ?
            Types.Element.FromElementId(x.Document, newId) : x
          )
        );

        Params.TrySetDataList(DA, "Type", () => outputTypes);
      }
      else
      {
        DA.SetDataList("Element", elements);
        Params.TrySetDataList(DA, "Type", () => elements.Select(x => x?.Type));
      }
    }
  }
}

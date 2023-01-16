using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  using External.DB.Extensions;

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
        var typesSets = new Dictionary<Types.ElementType, List<ARDB.ElementId>>();

        foreach (var item in elements.ZipOrLast(types, (Element, Type) => (Element, Type)))
        {
          var element = item.Element;
          var type    = item.Type;

          if (element?.IsValid is true && type?.IsValid is true)
          {
            if (element.Type.Equals(type))
              continue;

            if (!element.Document.IsEquivalent(type.Document))
              type = Types.ElementType.FromElementId(element.Document, element.Document.LookupElement(type.Document, type.Id)) as Types.ElementType;

            if (type is object)
            {
              // Special case for ARDB.Panel
              if (element.Value is ARDB.Panel panel)
              {
                switch (type)
                {
                  case Types.HostObjectType hostType:
                    if (panel.FindHostPanel() is ARDB.ElementId hostPanelId && hostPanelId.IsValid())
                      element = Types.Element.FromElementId(element.Document, hostPanelId) ?? element;
                    break;
                }
              }

              // Special case for CurtainGrid based elements
              if (element is Types.ICurtainGridsAccess grids)
              {
                foreach (var grid in grids.CurtainGrids)
                {
                  StartTransaction(element.Document);
                  grid.DeleteGridLines(instance: false, type: true);
                  //grid.DeleteMullions(instance: false, type: true);
                }
              }

              if (!typesSets.TryGetValue(type, out var entry))
                typesSets.Add(type, new List<ARDB.ElementId> { element.Id });
              else
                entry.Add(element.Id);
            }
          }
        }

        foreach (var type in typesSets)
        {
          UpdateDocument
          (
            type.Key.Document, () => ARDB.Element.ChangeTypeId(type.Key.Document, type.Value, type.Key.Id)
          );
        }
      }

      DA.SetDataList("Element", elements);
      Params.TrySetDataList(DA, "Type", () => elements.Select(x => x?.Type));
    }
  }
}

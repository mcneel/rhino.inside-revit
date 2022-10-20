using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  using External.DB;
  using External.DB.Extensions;

  public class ElementPropertyName : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("01934AD1-F31B-43E5-ADD9-C196F4A2467E");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "N";

    public ElementPropertyName()
    : base
    (
      "Element Name",
      "ElemName",
      "Element Name Property. Get-Set accessor to Element Name property.",
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
          Description = "Element to access Name",
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Element Name",
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
          Description = "Element to access Name",
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Element Name",
        },
        ParamRelevance.Primary
      ),
    };

    Dictionary<Types.Element, string> renames;
    protected void ElementSetNomen(Types.Element element, string value)
    {
      if (string.IsNullOrEmpty(value))
        return;

      if (renames is null)
        renames = new Dictionary<Types.Element, string>();

      if (renames.TryGetValue(element, out var nomen))
      {
        if (nomen == value)
          return;

        renames.Remove(element);
      }
      else element.Nomen = Guid.NewGuid().ToString();

      renames.Add(element, value);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element, x => x.IsValid)) return;
      else DA.SetData("Element", element);

      if (Params.GetData(DA, "Name", out string name))
        UpdateElement(element.Value, () => ElementSetNomen(element, name));

      Params.TrySetData(DA, "Name", () => element.Nomen);
    }

    Dictionary<string, string> namesMap;
    public override void OnPrepare(IReadOnlyCollection<ARDB.Document> documents)
    {
      if (renames is object)
      {
        // Create a names map to remap the final output at 'Name'
        namesMap = Params.IndexOfOutputParam("Name") < 0 ? default : new Dictionary<string, string>();

        foreach (var rename in renames)
        {
          if (namesMap is object)
          {
            var nomen = rename.Key.Nomen;
            if (!namesMap.ContainsKey(nomen))
              namesMap.Add(rename.Key.Nomen, rename.Value);
          }

          // Update elements to the final names
          rename.Key.Nomen = rename.Value;
        }
      }
    }

    public override void OnDone(ARDB.TransactionStatus status)
    {
      if (status == ARDB.TransactionStatus.Committed && namesMap is object)
      {
        // Reconstruct output 'Name' with final values from `namesMap`.
        var _Name_ = Params.IndexOfOutputParam("Name");
        if (_Name_ >= 0)
        {
          var nameParam = Params.Output[_Name_];
          foreach (var item in nameParam.VolatileData.AllData(true))
          {
            if (item is GH_String text)
            {
              if (namesMap.TryGetValue(text.Value, out var nomen))
                text.Value = nomen;
              else
                text.Value = null;
            }
          }
        }
      }

      namesMap = default;
      renames = default;
    }
  }

  public class NamesakeElement : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("1FEE04EF-A3DA-44F4-B114-486724C92AB6");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override ARDB.ElementFilter ElementFilter => CompoundElementFilter.ElementIsElementTypeFilter(inverted: true);

    public NamesakeElement() : base
    (
      name: "Namesake Element",
      nickname: "Namesake",
      description: "Get namesake element on a diferent document",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document to query on",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Source Element",
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
          Description = "Namesake Element",
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Element Name",
        },
        ParamRelevance.Secondary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc)) return;

      if (!Params.GetData(DA, "Element", out Types.Element element, x => x.IsValid)) return;

      var namesake = Types.Element.FromElementId
      (
        doc,
        doc.LookupElement(element.Document, element.Id)
      );
      DA.SetData("Element", namesake);
      Params.TrySetData(DA, "Name", () => namesake.Nomen);
    }
  }
}

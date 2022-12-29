using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.11")]
  public class ElementGraphicOverrides : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("6F5E3619-4299-4FB5-8CAC-2C172A149142");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => "O";

    public ElementGraphicOverrides() : base
    (
      name: "Element Graphic Overrides",
      nickname: "EG-Overrides",
      description: "Get-Set element graphics overrides on the specified View",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to query element graphic overrides",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access graphic overrides",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Hidden",
          NickName = "H",
          Description = "Element hidden state",
          Access = GH_ParamAccess.list,
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.OverrideGraphicSettings()
        {
          Name = "Overrides",
          NickName = "O",
          Description = "Element graphic overrides",
          Access = GH_ParamAccess.list,
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to query element graphic overrides",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access graphics overrides",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Hidden",
          NickName = "H",
          Description = "Element hidden state",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.OverrideGraphicSettings()
        {
          Name = "Overrides",
          NickName = "O",
          Description = "Element graphic overrides",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      else Params.TrySetData(DA, "View", () => view);

      if (!Params.GetDataList(DA, "Element", out IList<Types.GraphicalElement> elements)) return;
      else Params.TrySetDataList(DA, "Element", () => elements);

      if (Params.GetDataList(DA, "Hidden", out IList<bool?> hidden) && hidden.Count > 0)
      {
        if (view.Value.AreGraphicsOverridesAllowed())
        {
          var elementsToHide = new HashSet<ARDB.ElementId>(elements.Count);
          var elementsToUnhide = new HashSet<ARDB.ElementId>(elements.Count);

          foreach (var pair in elements.ZipOrLast(hidden, (Element, Hidden) => (Element, Hidden)))
          {
            if (!pair.Hidden.HasValue) continue;
            if (!view.Document.IsEquivalent(pair.Element?.Document)) continue;
            if (pair.Element?.IsValid != true) continue;
            if (!pair.Element.Value.CanBeHidden(view.Value))
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Category '{pair.Element.Nomen}' can not be hidden on view '{view.Value.Title}'.");
              continue;
            }

            if (pair.Hidden.Value)
            {
              elementsToUnhide.Remove(pair.Element.Id);
              elementsToHide.Add(pair.Element.Id);
            }
            else
            {
              elementsToHide.Remove(pair.Element.Id);
              elementsToUnhide.Add(pair.Element.Id);
            }
          }

          StartTransaction(view.Document);

          if (elementsToHide.Count > 0) view.Value.HideElements(elementsToHide);
          if (elementsToUnhide.Count > 0) view.Value.UnhideElements(elementsToUnhide);
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Graphics Overrides are not allowed on View '{view.Value.Title}'");
      }

      Params.TrySetDataList
      (
        DA, "Hidden", () => elements.Select
        (
          x => view.Document.IsEquivalent(x?.Document) ?
               x?.Value?.IsHidden(view.Value) :
               default(bool?)
        )
      );

      if (Params.GetDataList(DA, "Overrides", out IList<Types.OverrideGraphicSettings> settings) && settings.Count > 0)
      {
        if (view.Value.AreGraphicsOverridesAllowed())
        {

          StartTransaction(view.Document);

          foreach (var pair in elements.ZipOrLast(settings, (Element, Settings) => (Element, Settings)))
          {
            if (pair.Settings?.Value is null) continue;
            if (!view.Document.IsEquivalent(pair.Element?.Document)) continue;
            if (pair.Element?.IsValid != true) continue;
            if (!pair.Element.Value.CanBeHidden(view.Value)) continue;

            view.Value.SetElementOverrides(pair.Element.Id, pair.Settings.Value);
          }
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Graphics Overrides are not allowed on View '{view.Value.Title}'");
      }

      Params.TrySetDataList
      (
        DA, "Overrides", () => elements.Select
        (
          x => view.Document.IsEquivalent(x?.Document) && x?.Id is ARDB.ElementId elementId &&
               view.Value.GetElementOverrides(elementId) is ARDB.OverrideGraphicSettings overrideSettings ?
               new Types.OverrideGraphicSettings(x.Document, overrideSettings) :
               default
        )
      );
    }
  }
}

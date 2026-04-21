using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  [ComponentVersion(introduced: "1.13", updated: "1.16")]
  public class ElementVisibility : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("8ED1490F-DA5D-40FA-8612-4F4B166ECE52");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public ElementVisibility() : base
    (
      name: "Element Visibility",
      nickname: "Visibility",
      description: "Check element visibility on a given View",
      category: "Revit",
      subCategory: "View"
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
          Description = "Element to access Visibility status",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View where to check element visibility",
        }
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
          Description = "Element to check visibility status",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Visible",
          NickName = "V",
          Description = "Element visibility status",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View where the element visibility has been checked",
        }, ParamRelevance.Secondary
      ),

    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetDataList(DA, "Element", out IList<Types.Element> elements)) return;
      else Params.TrySetDataList(DA, "Element", () => elements);

      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      else Params.TrySetData(DA, "View", () => view);

      Params.TrySetDataList
      (
        DA, "Visible", () =>
        {
          var visible = new bool[elements.Count];

          var viewValue = view.Value;
          if (!viewValue.IsTemplate)
          {
            var viewDocument = view.Document;

            // TODO : Test this, it may reduce the Outline.
            //using (viewDocument.NoSelectionScope())
            {
              // Build a list of ids that belong to `viewDocument`
              var ids = new HashSet<ARDB.ElementId>(elements.Count);
              {
                foreach (var element in elements)
                {
                  if (!viewDocument.IsEquivalent(element?.Document)) continue;
                  if (element?.Value is ARDB.Element elementValue)
                    ids.Add(element.Id);
                }
              }

              var visibleElements = viewValue.GetVisibleElements(ids);
              if (visibleElements.Count > 0)
              {
                for (int i = 0; i < elements.Count; i++)
                {
                  if (elements[i] is null) continue;
                  visible[i] = visibleElements.Contains(elements[i].Id);
                }
              }

              if (viewValue is ARDB.ViewSheet viewSheet)
              {
                foreach (var placedView in viewSheet.GetAllPlacedViews().Select(x => viewDocument.GetElement(x) as ARDB.View))
                {
                  ids.ExceptWith(visibleElements);

                  visibleElements = placedView.GetVisibleElements(ids);
                  if (visibleElements.Count > 0)
                  {
                    for (int i = 0; i < elements.Count; i++)
                    {
                      if (elements[i] is null) continue;
                      visible[i] |= visibleElements.Contains(elements[i].Id);
                    }
                  }
                }
              }
            }
          }

          return visible;
        }
      );
    }
  }
}

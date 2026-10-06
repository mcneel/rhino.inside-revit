using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.GH.Exceptions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  [ComponentVersion(introduced: "1.37")]
  public class ElementCut : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("985643E0-D04A-4EEB-9091-6DBE87B274CE");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    protected override string IconTag => "C";

    public ElementCut()
    : base
    (
      name: "Cut Element",
      nickname: "Cut",
      description: "Get-Set access component to Element cutters.",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access cutters",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Cut",
          NickName = "C",
          Description = "Cutter elements to add",
          Access = GH_ParamAccess.list,
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Uncut",
          NickName = "U",
          Description = "Cutter elements to remove",
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
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access cutters",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Cutters",
          NickName = "C",
          Description = "Cutter elements",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.GraphicalElement element)) return;
      else DA.SetData("Element", element);
      if (!Params.TryGetDataList(DA, "Cut", out IList<Types.GraphicalElement> add)) return;
      if (!Params.TryGetDataList(DA, "Uncut", out IList<Types.GraphicalElement> remove)) return;

      if (!ARDB.SolidSolidCutUtils.IsElementFromAppropriateContext(element.Value))
        throw new RuntimeErrorException("The target element is not valid for solid-solid cut");

      if (add is object || remove is object)
      {
        UpdateElement
        (
          element.Value, () =>
          {
            foreach (var cutter in remove ?? Array.Empty<Types.GraphicalElement>())
            {
              if (ARDB.SolidSolidCutUtils.CutExistsBetweenElements(cutter.Value, element.Value, out var canRemove) && canRemove)
                ARDB.SolidSolidCutUtils.RemoveCutBetweenSolids(element.Document, element.Value, cutter.Value);
            }

            foreach (var cutter in add ?? Array.Empty<Types.GraphicalElement>())
            {
              if (!cutter.IsValid) continue;
              if (ARDB.SolidSolidCutUtils.CanElementCutElement(cutter.Value, element.Value, out var reasson))
              {
                ARDB.SolidSolidCutUtils.AddCutBetweenSolids(element.Document, element.Value, cutter.Value);
              }
              else
              {
                var message = string.Empty;
                switch (reasson)
                {
                  case ARDB.CutFailureReason.CutAlreadyExists: break;
                  case ARDB.CutFailureReason.OppositeCutExists:
                    message = $"The target element has already cut the cutting element. {{{cutter.Id}}}"; break;
                  case ARDB.CutFailureReason.CutNotAppropriateForElements:
                    message = $"The cut is not appropriate for the two elements. {{{cutter.Id}}}"; break;
                }

                if (!string.IsNullOrEmpty(message))
                {
                  if (FailureProcessingMode == ARDB.FailureProcessingResult.Continue)
                  {
                    AddContinuableFailure(message);
                    throw new RuntimeException();
                  }

                  if (FailureProcessingMode != ARDB.FailureProcessingResult.ProceedWithCommit)
                    throw new RuntimeException(message);

                  AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);
                }
              }
            }
          }
        );
      }

      Params.TrySetDataList(DA, "Cutters", () => ARDB.SolidSolidCutUtils.GetCuttingSolids(element.Value).Select(x => element.GetElement<Types.GraphicalElement>(x)));
    }
  }
}

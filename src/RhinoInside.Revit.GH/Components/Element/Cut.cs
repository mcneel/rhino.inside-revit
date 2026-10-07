using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
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
          Description = "Target element",
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
        }, ParamRelevance.Secondary
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
          Description = "Target element",
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
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Cutting",
          NickName = "c",
          Description = "Elements being cut by target Element",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Secondary
      ),
    };

    static bool IsElementFromAppropriateContext(ARDB.Document document, ARDB.Element element)
    {
      return document.Equals(element.Document) && ARDB.SolidSolidCutUtils.IsElementFromAppropriateContext(element);
    }

    static bool CanElementCutElement(ARDB.Element cuttingElement, ARDB.Element cutElement, out ARDB.CutFailureReason reason)
    {
      if (ARDB.InstanceVoidCutUtils.IsVoidInstanceCuttingElement(cuttingElement))
      {
        reason = ARDB.CutFailureReason.CutAllowed;
        if (!ARDB.InstanceVoidCutUtils.CanBeCutWithVoid(cutElement) || cuttingElement.Id == cuttingElement.Id)
          reason = ARDB.CutFailureReason.CutNotAppropriateForElements;
        else if (ARDB.InstanceVoidCutUtils.InstanceVoidCutExists(cutElement, cuttingElement))
          reason = ARDB.CutFailureReason.CutAlreadyExists;
        else if (ARDB.InstanceVoidCutUtils.InstanceVoidCutExists(cuttingElement, cutElement))
          reason = ARDB.CutFailureReason.OppositeCutExists;

        return reason == ARDB.CutFailureReason.CutAllowed;
      }
      else return ARDB.SolidSolidCutUtils.CanElementCutElement(cuttingElement, cutElement, out reason);
    }

    static bool CutExistsBetweenElements(ARDB.Element first, ARDB.Element second, out bool firstCutsSecond)
    {
      if (ARDB.InstanceVoidCutUtils.IsVoidInstanceCuttingElement(first) && ARDB.InstanceVoidCutUtils.InstanceVoidCutExists(second, first))
      {
        firstCutsSecond = true;
        return true;
      }
      else if (ARDB.InstanceVoidCutUtils.IsVoidInstanceCuttingElement(second) && ARDB.InstanceVoidCutUtils.InstanceVoidCutExists(first, second))
      {
        firstCutsSecond = false;
        return true;
      }

      return ARDB.SolidSolidCutUtils.CutExistsBetweenElements(first, second, out firstCutsSecond);
    }

    static void AddCutBetweenElements(ARDB.Document document, ARDB.Element solidToBeCut, ARDB.Element cuttingSolid)
    {
      if (ARDB.InstanceVoidCutUtils.IsVoidInstanceCuttingElement(cuttingSolid))
        ARDB.InstanceVoidCutUtils.AddInstanceVoidCut(document, solidToBeCut, cuttingSolid);
      else
        ARDB.SolidSolidCutUtils.AddCutBetweenSolids(document, solidToBeCut, cuttingSolid, false);
    }

    static void RemoveCutBetweenElements(ARDB.Document document, ARDB.Element solidToBeCut, ARDB.Element cuttingSolid)
    {
      if (ARDB.InstanceVoidCutUtils.IsVoidInstanceCuttingElement(cuttingSolid))
        ARDB.InstanceVoidCutUtils.RemoveInstanceVoidCut(document, solidToBeCut, cuttingSolid);
      else
        ARDB.SolidSolidCutUtils.RemoveCutBetweenSolids(document, solidToBeCut, cuttingSolid);
    }

    static IEnumerable<ARDB.ElementId> GetCuttingElements(ARDB.Element element)
    {
      return ARDB.SolidSolidCutUtils.GetCuttingSolids(element).Concat(ARDB.InstanceVoidCutUtils.GetCuttingVoidInstances(element)).
             OrderBy(x => x.ToValue());
    }

    static IEnumerable<ARDB.ElementId> GetElementsBeingCut(ARDB.Element element)
    {
      return ARDB.SolidSolidCutUtils.GetSolidsBeingCut(element).Concat(ARDB.InstanceVoidCutUtils.GetElementsBeingCut(element)).
             OrderBy(x => x.ToValue());
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.GraphicalElement element)) return;
      else DA.SetData("Element", element);
      if (!Params.TryGetDataList(DA, "Cut", out IList<Types.GraphicalElement> add)) return;
      if (!Params.TryGetDataList(DA, "Uncut", out IList<Types.GraphicalElement> remove)) return;

      if (!IsElementFromAppropriateContext(element.Document, element.Value))
        throw new RuntimeErrorException($"The target element is not valid for solid-solid cut {{{element.Id}}}");

      if (add is object || remove is object)
      {
        UpdateElement
        (
          element.Value, () =>
          {
            foreach (var cutter in remove ?? Array.Empty<Types.GraphicalElement>())
            {
              if (!IsElementFromAppropriateContext(element.Document, cutter.Value))
                throw new RuntimeErrorException($"The cutter element is not valid for solid-solid cut {{{cutter.Id}}}");

              if (CutExistsBetweenElements(cutter.Value, element.Value, out var canRemove) && canRemove)
                RemoveCutBetweenElements(element.Document, element.Value, cutter.Value);
            }

            foreach (var cutter in add ?? Array.Empty<Types.GraphicalElement>())
            {
              if (!cutter.IsValid) continue;

              if (!IsElementFromAppropriateContext(element.Document, cutter.Value))
                throw new RuntimeErrorException($"The cutter element is not valid for solid-solid cut {{{cutter.Id}}}");

              if (CanElementCutElement(cutter.Value, element.Value, out var reasson))
              {
                AddCutBetweenElements(element.Document, element.Value, cutter.Value);
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

      Params.TrySetDataList(DA, "Cutters", () => GetCuttingElements(element.Value).Select(x => element.GetElement<Types.GraphicalElement>(x)));
      Params.TrySetDataList(DA, "Cutting", () => GetElementsBeingCut(element.Value).Select(x => element.GetElement<Types.GraphicalElement>(x)));
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.GH.Exceptions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  [ComponentVersion(introduced: "1.37")]
  public class ElementJoin : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("1E4E8E39-B0B0-42C1-B0F2-E99D596EE4C6");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    protected override string IconTag => "J";

    public ElementJoin()
    : base
    (
      name: "Join Element",
      nickname: "Join",
      description: "Get-Set access component to joined Elements.",
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
          Description = "Element to access other joined elements",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Join",
          NickName = "J",
          Description = "Elements to join",
          Access = GH_ParamAccess.list,
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Unjoin",
          NickName = "U",
          Description = "Elements to unjoin",
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
          Description = "Element to access other joined elements",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Joined",
          NickName = "J",
          Description = "Joined elements. (Cutting elements in the join)",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Joining",
          NickName = "j",
          Description = "Joining elements. (Elements being cut by target Element)",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Secondary
      ),
    };

    bool ReportFailure(string message)
    {
      if (string.IsNullOrEmpty(message))
        return false;

      if (FailureProcessingMode == ARDB.FailureProcessingResult.Continue)
      {
        AddContinuableFailure(message);
        throw new RuntimeException();
      }

      if (FailureProcessingMode != ARDB.FailureProcessingResult.ProceedWithCommit)
        throw new RuntimeException(message);

      AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);
      return true;
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.GraphicalElement element)) return;
      else DA.SetData("Element", element);
      if (!Params.TryGetDataList(DA, "Join", out IList<Types.GraphicalElement> add)) return;
      if (!Params.TryGetDataList(DA, "Unjoin", out IList<Types.GraphicalElement> remove)) return;

      if (element.Document.IsFamilyDocument)
        throw new RuntimeErrorException("Join functionality is not available for family documents.");

      if (add is object || remove is object)
      {
        UpdateElement
        (
          element.Value, () =>
          {
            foreach (var cutter in remove ?? Array.Empty<Types.GraphicalElement>())
            {
              var message = string.Empty;
              if (!ARDB.JoinGeometryUtils.AreElementsJoined(element.Document, element.Value, cutter.Value))
              {
                if (FailureProcessingMode != ARDB.FailureProcessingResult.ProceedWithCommit)
                  message = $"The elements are not joined. {{{cutter.Id}}}";
                else
                  continue;
              }
              else if (!ARDB.JoinGeometryUtils.IsCuttingElementInJoin(element.Document, cutter.Value, element.Value))
              {
                if (FailureProcessingMode != ARDB.FailureProcessingResult.ProceedWithCommit)
                  message = $"The target element is not joined the joining element but the other way around. {{{cutter.Id}}}";
              }

              if (!ReportFailure(message))
                ARDB.JoinGeometryUtils.UnjoinGeometry(element.Document, element.Value, cutter.Value);
            }

            foreach (var cutter in add ?? Array.Empty<Types.GraphicalElement>())
            {
              if (!cutter.IsValid) continue;

              var message = string.Empty;
              if (ARDB.JoinGeometryUtils.AreElementsJoined(element.Document, cutter.Value, element.Value))
              {
                if (ARDB.JoinGeometryUtils.IsCuttingElementInJoin(element.Document, element.Value, cutter.Value))
                {
                  if (FailureProcessingMode == ARDB.FailureProcessingResult.ProceedWithCommit)
                  {
                    ARDB.JoinGeometryUtils.SwitchJoinOrder(element.Document, element.Value, cutter.Value);
                    AddContinueFailure($"Join order switched. {{{cutter.Id}}}");
                  }
                  else
                    message = $"The target element is already joined the other way around to the joining element. {{{cutter.Id}}}";
                }
              }
              else
              {
                try
                {
                  ARDB.JoinGeometryUtils.JoinGeometry(element.Document, element.Value, cutter.Value);
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException) { message = $"The elements cannot be joined. {{{cutter.Id}}}"; }
                catch (Exception e) { message = e.Message; }
              }

              ReportFailure(message);
            }
          }
        );
      }

      Params.TrySetDataList
      (
        DA, "Joined",
        () => ARDB.JoinGeometryUtils.GetJoinedElements(element.Document, element.Value).
              Where(x => ARDB.JoinGeometryUtils.IsCuttingElementInJoin(element.Document, element.Document.GetElement(x), element.Value)).
              Select(x => element.GetElement<Types.GraphicalElement>(x))
      );
      Params.TrySetDataList
      (
        DA, "Joining",
        () => ARDB.JoinGeometryUtils.GetJoinedElements(element.Document, element.Value).
              Where(x => ARDB.JoinGeometryUtils.IsCuttingElementInJoin(element.Document, element.Value, element.Document.GetElement(x))).
              Select(x => element.GetElement<Types.GraphicalElement>(x))
      );
    }
  }
}

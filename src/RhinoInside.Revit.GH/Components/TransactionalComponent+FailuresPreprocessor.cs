using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using External.DB.Extensions;

  public abstract partial class TransactionalComponent
  {
    class FailuresPreprocessor : ARDB.IFailuresPreprocessor
    {
      readonly IGH_ActiveObject ActiveObject;
      readonly IEnumerable<ARDB.FailureDefinitionId> FailureDefinitionIdsToFix;
      readonly ARDB.FailureDefinitionId[] PendingFailureDefinitionIdsToFix;
      readonly ARDB.FailureProcessingResult FailureProcessingMode;

      public FailuresPreprocessor
      (
        IGH_ActiveObject activeObject,
        IEnumerable<ARDB.FailureDefinitionId> failureDefinitionIdsToFix,
        ARDB.FailureDefinitionId[] pendingFailureDefinitionIdsToFix,
        ARDB.FailureProcessingResult failureProcessingMode
      )
      {
        ActiveObject = activeObject;
        FailureDefinitionIdsToFix = failureDefinitionIdsToFix ?? Array.Empty<ARDB.FailureDefinitionId>();
        PendingFailureDefinitionIdsToFix = pendingFailureDefinitionIdsToFix ?? Array.Empty<ARDB.FailureDefinitionId>();
        FailureProcessingMode = failureProcessingMode;
      }

      static string GetDescriptionText(ARDB.FailureMessageAccessor message)
      {
        var description = message.GetDescriptionText();

        return string.IsNullOrWhiteSpace(description) ?
          $"{message.GetSeverity()} {{{message.GetFailureDefinitionId().Guid}}}" :
          description;
      }

      void AddRuntimeMessage(ARDB.FailureMessageAccessor message, bool? solved = null)
      {
        if (ActiveObject is IGH_ActiveObject activeObject)
        {
          var severity = message.GetSeverity();
          var failureId = message.GetFailureDefinitionId();

          if (failureId == ERDB.ExternalFailures.TransactionFailures.SimulatedTransaction)
          {
            // Simulation signal is already reflected in the canvas changing the component color,
            // So it's up to the component show relevant information about what 'simulation' means.
            // As an example Purge component shows a remarks that reads like 'No elements were deleted'.
            //AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, message.GetDescriptionText());

            return;
          }

          if (severity == ARDB.FailureSeverity.Warning && FailureDefinitionIdsToFix.Contains(failureId) is true)
            return;

          var level = GH_RuntimeMessageLevel.Remark;
          switch (severity)
          {
            case ARDB.FailureSeverity.None: level = GH_RuntimeMessageLevel.Remark; break;
            case ARDB.FailureSeverity.Warning: level = GH_RuntimeMessageLevel.Warning; break;
            case ARDB.FailureSeverity.Error: level = GH_RuntimeMessageLevel.Error; break;
            case ARDB.FailureSeverity.DocumentCorruption: level = GH_RuntimeMessageLevel.Error; break;
          }

          string solvedMark = string.Empty;
          if (message.GetSeverity() > ARDB.FailureSeverity.Warning)
          {
            switch (solved)
            {
              case null:
                solvedMark = FailureProcessingMode == ARDB.FailureProcessingResult.Continue ?
                  $"{TransactionalComponent.ComponentAttributes.IssuePrefix} " : string.Empty;
                break;
              case false: solvedMark = "❌ "; break;
              case true: solvedMark = "✔ "; break;
            }
          }

          var text = $"{solvedMark}{GetDescriptionText(message).Replace(". ", OS.NewLine)}";
          {
            int idsCount = 0;
            foreach (var id in message.GetFailingElementIds())
              text += idsCount++ == 0 ? $" {{{id.ToValue()}" : $", {id.ToValue()}";
            if (idsCount > 0) text += "} ";
          }

          activeObject.AddRuntimeMessage(level, text);
        }
      }

      void AddContinueMessage(ARDB.FailuresAccessor failures)
      {
        if (ActiveObject is TransactionalComponent activeObject)
        {
          var caption = string.Empty;
          var messages = failures.GetFailureMessages();
          if (messages.Count > 0)
          {
            caption = messages[0].GetDefaultResolutionCaption();
            for (int m = 1; m < messages.Count; ++m)
            {
              if (!messages[m].ShouldMergeWithMessage(messages[0]))
              {
                caption = string.Empty;
                break;
              }
            }
          }

          if (!string.IsNullOrEmpty(caption))
            activeObject.AddContinueFailure(caption);
        }
      }
      private bool Continuing => (ActiveObject?.Attributes as TransactionalComponent.ComponentAttributes)?.Pressed is true;

      ARDB.FailureProcessingResult FixFailures(ARDB.FailuresAccessor failures, IEnumerable<ARDB.FailureDefinitionId> failureIds, bool report = true)
      {
        foreach (var failureId in failureIds)
        {
          var solved = 0;
          foreach (var error in failures.GetFailureMessages().Where(x => x.GetFailureDefinitionId() == failureId))
          {
            if (!failures.IsFailureResolutionPermitted(error))
              continue;

            // Don't try to fix two times same issue
            if (failures.GetAttemptedResolutionTypes(error).Any())
              continue;

            if (report)
              AddRuntimeMessage(error, solved: true);

            failures.ResolveFailure(error);
            solved++;
          }

          if (solved > 0)
            return ARDB.FailureProcessingResult.ProceedWithCommit;
        }

        return ARDB.FailureProcessingResult.Continue;
      }

      public ARDB.FailureProcessingResult PreprocessFailures(ARDB.FailuresAccessor failures)
      {
#if DEBUG
        var tranasction = failures.GetTransactionName();
        var failureMessages = failures.GetFailureMessages().Select
        (
          failure =>
          (
            Severity: failure.GetSeverity(),
            Description: failure.GetDescriptionText(),
            FailingElements: failure.GetFailingElementIds().Select(x => failures.GetDocument().GetElement(x)).ToArray(),
            AdditionalElements: failure.GetAdditionalElementIds().Select(x => failures.GetDocument().GetElement(x)).ToArray(),

            Caption: failure.HasResolutions() ? failure.GetDefaultResolutionCaption() : string.Empty,
            CurrentResolution: failure.HasResolutions() ? failure.GetCurrentResolutionType() : ARDB.FailureResolutionType.Invalid,
            Resolutions: ((ARDB.FailureResolutionType[]) Enum.GetValues(typeof(ARDB.FailureResolutionType))).Where(x => failure.HasResolutionOfType(x)).ToArray()
          )
        ).ToArray();
#endif
        var severity = failures.GetSeverity();

        if
        (
          severity >= ARDB.FailureSeverity.Error &&
          FailureProcessingMode <= ARDB.FailureProcessingResult.ProceedWithCommit
        )
        {
          if (failures.IsTransactionBeingCommitted())
          {
            // Handled failures in order
            var result = FixFailures(failures, FailureDefinitionIdsToFix);
            if (result != ARDB.FailureProcessingResult.Continue)
              return result;
          }

          // Unhandled failures in incoming order
          var unhandledFailureDefinitionIds = Continuing ?
            PendingFailureDefinitionIdsToFix :
            failures.GetFailureMessages().GroupBy(x => x.GetFailureDefinitionId()).Select(x => x.Key);

          if (FailureProcessingMode == ARDB.FailureProcessingResult.ProceedWithCommit)
          {
            var result = FixFailures(failures, unhandledFailureDefinitionIds, !Continuing);
            if (result != ARDB.FailureProcessingResult.Continue)
              return result;
          }
          else if (FailureProcessingMode == ARDB.FailureProcessingResult.Continue)
          {
            AddContinueMessage(failures);
            if (ActiveObject is TransactionalComponent component) component.PendingFailureDefinitionIdsToFix = unhandledFailureDefinitionIds.ToArray();
          }
        }

        if (severity >= ARDB.FailureSeverity.Warning)
        {
          // Unsolved failures
          foreach (var error in failures.GetFailureMessages())
            AddRuntimeMessage(error);

          if (FailureProcessingMode != ARDB.FailureProcessingResult.WaitForUserInput)
            failures.DeleteAllWarnings();
        }

        if (FailureProcessingMode != ARDB.FailureProcessingResult.WaitForUserInput)
        {
          if (severity >= ARDB.FailureSeverity.Error)
            return ARDB.FailureProcessingResult.ProceedWithRollBack;
        }

        return ARDB.FailureProcessingResult.Continue;
      }
    }

    protected override bool AbortOnContinuableException => FailureProcessingMode > ARDB.FailureProcessingResult.ProceedWithCommit;

    ARDB.FailureProcessingResult _FailureProcessingMode = ARDB.FailureProcessingResult.Continue;
    public ARDB.FailureProcessingResult FailureProcessingMode
    {
      get => (Attributes as ComponentAttributes).Pressed ? ARDB.FailureProcessingResult.ProceedWithCommit : _FailureProcessingMode;
      set => _FailureProcessingMode = value;
    }

    /// <summary>
    /// Override to add handled failures to your component (Order is not important).
    /// </summary>
    protected virtual IEnumerable<ARDB.FailureDefinitionId> FailureDefinitionIdsToFix => null;
    protected internal ARDB.FailureDefinitionId[] PendingFailureDefinitionIdsToFix { get; internal set; } = Array.Empty<ARDB.FailureDefinitionId>();

    protected virtual ARDB.IFailuresPreprocessor CreateFailuresPreprocessor()
    {
      var pending = PendingFailureDefinitionIdsToFix;
      PendingFailureDefinitionIdsToFix = Array.Empty<ARDB.FailureDefinitionId>();
      return new FailuresPreprocessor(this, FailureDefinitionIdsToFix, pending, FailureProcessingMode);
    }
  }
}

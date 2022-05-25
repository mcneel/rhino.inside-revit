using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.UI.Events;
using GH_IO.Serialization;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using ElementTracking;
  using External.DB.Extensions;

  class TransactionalComponentFailuresPreprocessor : ARDB.IFailuresPreprocessor
  {
    readonly IGH_ActiveObject ActiveObject;
    readonly IEnumerable<ARDB.FailureDefinitionId> FailureDefinitionIdsToFix;
    readonly ARDB.FailureProcessingResult FailureProcessingMode;

    public TransactionalComponentFailuresPreprocessor
    (
      IGH_ActiveObject activeObject,
      IEnumerable<ARDB.FailureDefinitionId> failureDefinitionIdsToFix
    ) :
    this
    (
      activeObject,
      failureDefinitionIdsToFix,
      ARDB.FailureProcessingResult.ProceedWithRollBack
    )
    { }

    public TransactionalComponentFailuresPreprocessor
    (
      IGH_ActiveObject activeObject,
      IEnumerable<ARDB.FailureDefinitionId> failureDefinitionIdsToFix,
      ARDB.FailureProcessingResult failureProcessingMode
    )
    {
      ActiveObject = activeObject;
      FailureDefinitionIdsToFix = failureDefinitionIdsToFix;
      FailureProcessingMode = failureProcessingMode;
    }

    static string GetDescriptionMessage(ARDB.FailureMessageAccessor error)
    {
      var description = error.GetDescriptionText();
      if (string.IsNullOrWhiteSpace(description))
        return $"{error.GetSeverity()} {{{error.GetFailureDefinitionId().Guid}}}";

      return description;
    }

    void AddRuntimeMessage(ARDB.FailureMessageAccessor error, bool? solved = null)
    {
      if (error.GetFailureDefinitionId() == ERDB.ExternalFailures.TransactionFailures.SimulatedTransaction)
      {
        // Simulation signal is already reflected in the canvas changing the component color,
        // So it's up to the component show relevant information about what 'simulation' means.
        // As an example Purge component shows a remarks that reads like 'No elements were deleted'.
        //AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, error.GetDescriptionText());

        return;
      }

      var level = GH_RuntimeMessageLevel.Remark;
      switch (error.GetSeverity())
      {
        case ARDB.FailureSeverity.None:               level = GH_RuntimeMessageLevel.Remark;  break;
        case ARDB.FailureSeverity.Warning:            level = GH_RuntimeMessageLevel.Warning; break;
        case ARDB.FailureSeverity.Error:              level = GH_RuntimeMessageLevel.Error;   break;
        case ARDB.FailureSeverity.DocumentCorruption: level = GH_RuntimeMessageLevel.Error;   break;
      }

      string solvedMark = string.Empty;
      if (error.GetSeverity() > ARDB.FailureSeverity.Warning)
      {
        switch (solved)
        {
          case false: solvedMark = "❌ "; break;
          case true:  solvedMark = "✔ "; break;
        }
      }

      var description = GetDescriptionMessage(error);
      var message = $"{solvedMark}{description}";

      int idsCount = 0;
      foreach (var id in error.GetFailingElementIds())
        message += idsCount++ == 0 ? $" {{{id.IntegerValue}" : $", {id.IntegerValue}";
      if (idsCount > 0) message += "} ";

      ActiveObject?.AddRuntimeMessage(level, message);
    }

    ARDB.FailureProcessingResult FixFailures(ARDB.FailuresAccessor failuresAccessor, IEnumerable<ARDB.FailureDefinitionId> failureIds)
    {
      foreach (var failureId in failureIds)
      {
        var solved = 0;
        foreach (var error in failuresAccessor.GetFailureMessages().Where(x => x.GetFailureDefinitionId() == failureId))
        {
          if (!failuresAccessor.IsFailureResolutionPermitted(error))
            continue;

          // Don't try to fix two times same issue
          if (failuresAccessor.GetAttemptedResolutionTypes(error).Any())
            continue;

          AddRuntimeMessage(error, solved: true);

          failuresAccessor.ResolveFailure(error);
          solved++;
        }

        if (solved > 0)
          return ARDB.FailureProcessingResult.ProceedWithCommit;
      }

      return ARDB.FailureProcessingResult.Continue;
    }

    public ARDB.FailureProcessingResult PreprocessFailures(ARDB.FailuresAccessor failuresAccessor)
    {
#if DEBUG
      var tranasction = failuresAccessor.GetTransactionName();
      var failureMessages = failuresAccessor.GetFailureMessages().Select
      (
        error =>
        (
          Severity: error.GetSeverity(),
          Description: error.GetDescriptionText(),
          FailingElements: error.GetFailingElementIds().Select(x => failuresAccessor.GetDocument().GetElement(x)).ToArray(),
          AdditionalElements: error.GetAdditionalElementIds().Select(x => failuresAccessor.GetDocument().GetElement(x)).ToArray(),

          Caption: error.HasResolutions() ? error.GetDefaultResolutionCaption() : string.Empty,
          CurrentResolution: error.HasResolutions() ? error.GetCurrentResolutionType() : ARDB.FailureResolutionType.Invalid,
          Resolutions: ((ARDB.FailureResolutionType[]) Enum.GetValues(typeof(ARDB.FailureResolutionType))).Where(x => error.HasResolutionOfType(x)).ToArray()
        )
      ).ToArray();
#endif
      var severity = failuresAccessor.GetSeverity();

      if (failuresAccessor.IsTransactionBeingCommitted())
      {
        if
        (
          severity >= ARDB.FailureSeverity.Error &&
          FailureProcessingMode <= ARDB.FailureProcessingResult.ProceedWithCommit
        )
        {
          // Handled failures in order
          if (FailureDefinitionIdsToFix is IEnumerable<ARDB.FailureDefinitionId> failureDefinitionIdsToFix)
          {
            var result = FixFailures(failuresAccessor, failureDefinitionIdsToFix);
            if (result != ARDB.FailureProcessingResult.Continue)
              return result;
          }

          // Unhandled failures in incomming order
          {
            var unhandledFailureDefinitionIds = failuresAccessor.GetFailureMessages().GroupBy(x => x.GetFailureDefinitionId()).Select(x => x.Key);
            var result = FixFailures(failuresAccessor, unhandledFailureDefinitionIds);
            if (result != ARDB.FailureProcessingResult.Continue)
              return result;
          }
        }
      }

      if (severity >= ARDB.FailureSeverity.Warning)
      {
        // Unsolved failures or warnings
        foreach (var error in failuresAccessor.GetFailureMessages())
          AddRuntimeMessage(error);

        if (FailureProcessingMode != ARDB.FailureProcessingResult.WaitForUserInput)
          failuresAccessor.DeleteAllWarnings();
      }

      if (FailureProcessingMode != ARDB.FailureProcessingResult.WaitForUserInput)
      {
        if (severity >= ARDB.FailureSeverity.Error)
          return ARDB.FailureProcessingResult.ProceedWithRollBack;
      }

      return ARDB.FailureProcessingResult.Continue;
    }
  }

  [ComponentVersion(introduced: "1.0", updated: "1.5")]
  public abstract class TransactionalComponent : ZuiComponent, ARDB.ITransactionFinalizer
  {
    protected TransactionalComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    #region Transaction
    ARDB.TransactionStatus status = ARDB.TransactionStatus.Uninitialized;
    public ARDB.TransactionStatus Status
    {
      get => status;
      protected set => status = value;
    }

    protected ARDB.Transaction NewTransaction(ARDB.Document doc) => NewTransaction(doc, Name);
    protected ARDB.Transaction NewTransaction(ARDB.Document doc, string name)
    {
      var transaction = new ARDB.Transaction(doc, name);

      var options = transaction.GetFailureHandlingOptions();
      options = options.SetClearAfterRollback(true);
      options = options.SetDelayedMiniWarnings(false);
      options = options.SetForcedModalHandling(true);

      if(CreateFailuresPreprocessor() is ARDB.IFailuresPreprocessor preprocessor)
        options = options.SetFailuresPreprocessor(preprocessor);

      options = options.SetTransactionFinalizer(this);

      transaction.SetFailureHandlingOptions(options);

      return transaction;
    }

    protected ARDB.TransactionStatus CommitTransaction(ARDB.Document doc, ARDB.Transaction transaction)
    {
      // Disable Rhino UI if any warning-error dialog popup
      var uiApplication = Revit.ActiveUIApplication;
      External.UI.EditScope scope = null;
      EventHandler<DialogBoxShowingEventArgs> _ = null;
      try
      {
        uiApplication.DialogBoxShowing += _ = (sender, args) =>
        {
          if (scope is null)
            scope = new External.UI.EditScope(uiApplication);
        };

        if (transaction.GetStatus() == ARDB.TransactionStatus.Started)
        {
          return transaction.Commit();
        }
        else return transaction.RollBack();
      }
      finally
      {
        uiApplication.DialogBoxShowing -= _;

        if (scope is IDisposable disposable)
          disposable.Dispose();
      }
    }
    #endregion

    #region Attributes
    internal new class Attributes : ZuiAttributes
    {
      public Attributes(TransactionalComponent owner) : base(owner) { }

      protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
      {
        if (channel == GH_CanvasChannel.Objects && !Owner.Locked && Owner is TransactionalComponent component)
        {
          if (component.Status != ARDB.TransactionStatus.RolledBack && component.Status != ARDB.TransactionStatus.Uninitialized)
          {
            var palette = GH_CapsuleRenderEngine.GetImpliedPalette(Owner);
            if (palette == GH_Palette.Normal && !Owner.IsPreviewCapable)
              palette = GH_Palette.Hidden;

            // Errors and warnings should be refelected in Canvas
            if (palette == GH_Palette.Normal || palette == GH_Palette.Hidden)
            {
              var style = GH_CapsuleRenderEngine.GetImpliedStyle(palette, Selected, Owner.Locked, Owner.Hidden);
              var fill = style.Fill;
              var edge = style.Edge;
              var text = style.Text;

              switch (component.Status)
              {
                case ARDB.TransactionStatus.Uninitialized: palette = GH_Palette.Grey; break;
                case ARDB.TransactionStatus.Started: palette = GH_Palette.White; break;
                case ARDB.TransactionStatus.RolledBack:     /*palette = GH_Palette.Normal;*/ break;
                case ARDB.TransactionStatus.Committed: palette = GH_Palette.Black; break;
                case ARDB.TransactionStatus.Pending: palette = GH_Palette.Blue; break;
                case ARDB.TransactionStatus.Error: palette = GH_Palette.Pink; break;
                case ARDB.TransactionStatus.Proceed: palette = GH_Palette.Brown; break;
              }
              var replacement = GH_CapsuleRenderEngine.GetImpliedStyle(palette, Selected, Owner.Locked, Owner.Hidden);

              try
              {
                style.Edge = replacement.Edge;
                style.Fill = replacement.Fill;
                style.Text = replacement.Text;

                base.Render(canvas, graphics, channel);
              }
              finally
              {
                style.Fill = fill;
                style.Edge = edge;
                style.Text = text;
              }

              return;
            }
          }
        }

        base.Render(canvas, graphics, channel);
      }
    }

    public override void CreateAttributes() => m_attributes = new Attributes(this);
    #endregion

    // Setp 1.
    protected override void BeforeSolveInstance() => status = ARDB.TransactionStatus.Uninitialized;

    // Step 2.
    //protected override void TrySolveInstance(IGH_DataAccess DA) { }

    // Step 3.
    //protected override void AfterSolveInstance() {}

    #region IFailuresPreprocessor
    // Override to add handled failures to your component (Order is important).
    protected virtual IEnumerable<ARDB.FailureDefinitionId> FailureDefinitionIdsToFix => null;

    public ARDB.FailureProcessingResult FailureProcessingMode { get; set; } = ARDB.FailureProcessingResult.Continue;

    protected override bool AbortOnContinuableException => FailureProcessingMode > ARDB.FailureProcessingResult.ProceedWithCommit;

    protected virtual ARDB.IFailuresPreprocessor CreateFailuresPreprocessor()
    {
      return new TransactionalComponentFailuresPreprocessor(this, FailureDefinitionIdsToFix, FailureProcessingMode);
    }
    #endregion

    #region ITransactionFinalizer
    public virtual void OnCommitted(ARDB.Document document, string strTransactionName)
    {
      if (Status < ARDB.TransactionStatus.Pending)
        Status = ARDB.TransactionStatus.Committed;
    }

    public virtual void OnRolledBack(ARDB.Document document, string strTransactionName)
    {
      if (Status < ARDB.TransactionStatus.Pending)
        Status = ARDB.TransactionStatus.RolledBack;
    }
    #endregion

    #region IO
    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      int mode = (int) ARDB.FailureProcessingResult.Continue;
      reader.TryGetInt32("FailureProcessingMode", ref mode);
      FailureProcessingMode = (ARDB.FailureProcessingResult) mode;

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (FailureProcessingMode != ARDB.FailureProcessingResult.Continue)
        writer.SetInt32("FailureProcessingMode", (int) FailureProcessingMode);

      return true;
    }
    #endregion
  }

  internal enum TransactionExtent
  {
    Default,
    Component,
    Instance,
    Scope,
  }

  public abstract class TransactionalChainComponent : TransactionalComponent, ERDB.ITransactionNotification
  {
    protected TransactionalChainComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory)
    { }

    internal virtual TransactionExtent TransactionExtent => TransactionExtent.Component;

    public override bool RequiresFailed
    (
      IGH_DataAccess access, int index, object value,
      string message
    )
    {
      if (base.RequiresFailed(access, index, value, message)) return true;

      if (FailureProcessingMode >= ARDB.FailureProcessingResult.ProceedWithRollBack)
        access.AbortComponentSolution();

      return false;
    }

    ERDB.TransactionChain chain;

    public ARDB.TransactionStatus StartTransaction(ARDB.Document document) => chain.Start(document);

    protected ARDB.TransactionStatus CommitTransaction()
    {
      if (TransactionExtent != TransactionExtent.Scope)
        throw new InvalidOperationException();

      try
      {
        if (chain.HasStarted())
         Status = chain.Commit();
      }
      catch (Exception e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
      }

      return Status;
    }

    protected void RollBackTransaction()
    {
      if (TransactionExtent != TransactionExtent.Scope)
        throw new InvalidOperationException();

      try
      {
        if (chain.HasStarted())
          Status = chain.RollBack();
      }
      catch (Exception e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
      }
    }

    // Setp 1.
    protected override void BeforeSolveInstance()
    {
      base.BeforeSolveInstance();

      chain = new ERDB.TransactionChain
      (
        new ERDB.TransactionHandlingOptions
        {
          FailuresPreprocessor = CreateFailuresPreprocessor(),
          TransactionNotification = this,
          KeepFailuresAfterRollback = FailureProcessingMode == ARDB.FailureProcessingResult.WaitForUserInput
        },
        Name
      );
    }

    // Step 2.
    protected sealed override void SolveInstance(IGH_DataAccess DA)
    {
      base.SolveInstance(DA);

      if (TransactionExtent != TransactionExtent.Component)
      {
        try
        {
          Status = ARDB.TransactionStatus.Pending;
          Status = IsAborted ? chain.RollBack() : chain.Commit();
        }
        finally
        {
          switch (Status)
          {
            case ARDB.TransactionStatus.Uninitialized:
              break;

            case ARDB.TransactionStatus.Committed:
              break;

            default:
              break;
          }
        }
      }
    }

    protected override bool TryCatchException(IGH_DataAccess DA, Exception e)
    {
      if (FailureProcessingMode == ARDB.FailureProcessingResult.Continue)
      {
        for (int o = 0; o < Params.Output.Count; ++o)
        {
          switch (Params.Output[o].Access)
          {
            case GH_ParamAccess.item: DA.SetData    (o, default);                 break;
            case GH_ParamAccess.list: DA.SetDataList(o, default);                 break;
            case GH_ParamAccess.tree: DA.SetDataTree(o, default(IGH_Structure));  break;
          }
        }
      }

      if (base.TryCatchException(DA, e))
        return true;

      if (TransactionExtent != TransactionExtent.Component)
        Status = chain.RollBack();

      return false;
    }

    // Step 3.
    protected /*sealed*/ override void AfterSolveInstance()
    {
      using (chain)
      {
        if (chain.HasStarted())
        {
          try
          {
            Status = ARDB.TransactionStatus.Pending;
            Status = IsAborted ? chain.RollBack() : chain.Commit();
          }
          catch (Exception e)
          {
            Status = ARDB.TransactionStatus.Error;
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
            ResetData();
          }
          finally
          {
            switch (Status)
            {
              case ARDB.TransactionStatus.Uninitialized:
                break;

              case ARDB.TransactionStatus.Committed:
                ClearInvalidOutputElements();
                break;

              default:
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Transaction {Status} and aborted.");
                ResetData();
                break;
            }
          }
        }

        chain = default;
      }

      base.AfterSolveInstance();
    }

    protected void UpdateDocument(ARDB.Document document, Action solve)
    {
      StartTransaction(document);

      if (FailureProcessingMode <= ARDB.FailureProcessingResult.ProceedWithCommit)
      {
        using (var sub = new ARDB.SubTransaction(document))
        {
          sub.Start();
          solve();
          sub.Commit();
        }
      }
      else solve();
    }

    protected void UpdateElement(ARDB.Element element, Action solve) => UpdateDocument(element.Document, solve);

    /// <summary>
    /// Set to null those output elements that are invalid.
    /// </summary>
    void ClearInvalidOutputElements()
    {
      foreach (var output in Params.Output)
      {
        if (!typeof(Types.IGH_ElementId).IsAssignableFrom(output.Type))
          continue;

        var data = output.VolatileData;
        var pathCount = data.PathCount;
        for (int p = 0; p < pathCount; ++p)
        {
          var branch = data.get_Branch(p);

          var count = branch.Count;
          for (int e = 0; e < count; ++e)
          {
            if (branch[e] is Types.IGH_ElementId id && !id.IsValid)
              branch[e] = null;
          }
        }
      }
    }

    #region ERDB.ITransactionNotification
    External.UI.EditScope editScope = null;
    EventHandler<DialogBoxShowingEventArgs> dialogBoxShowing = null;

    // Step 2.1
    public virtual bool OnStart(ARDB.Document document) => true;

    // Step 2.2
    public virtual void OnStarted(ARDB.Document document) { }

    // Step 3.1
    public virtual void OnPrepare(IReadOnlyCollection<ARDB.Document> documents)
    {
      // Disable Rhino UI in case any warning-error dialog popups
      var activeApplication = Revit.ActiveUIApplication;
      activeApplication.DialogBoxShowing += dialogBoxShowing = (sender, args) =>
      {
        if (editScope is null)
          editScope = new External.UI.EditScope(activeApplication);
      };
    }

    // Step 3.2
    public virtual void OnDone(ARDB.TransactionStatus status)
    {
      Status = status;

      // Restore Rhino UI in case any warning-error dialog popups
      if(dialogBoxShowing != default)
      {
        Revit.ActiveUIApplication.DialogBoxShowing -= dialogBoxShowing;
        dialogBoxShowing = null;
        using (editScope) editScope = default;
      }
    }
    #endregion

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      {
        Menu_AppendSeparator(menu);
        var failures = Menu_AppendItem(menu, "Error Mode");

        var skip = Menu_AppendItem(failures.DropDown, "⏭ Skip", (s, a) => { FailureProcessingMode = ARDB.FailureProcessingResult.Continue; ExpireSolution(true); }, true, FailureProcessingMode == ARDB.FailureProcessingResult.Continue);
        skip.ToolTipText = $"Any failing element will be skipped.{Environment.NewLine}A null will be returned in its place.";

        var @continue = Menu_AppendItem(failures.DropDown, "⏯ Continue", (s, a) => { FailureProcessingMode = ARDB.FailureProcessingResult.ProceedWithCommit; ExpireSolution(true); }, true, FailureProcessingMode == ARDB.FailureProcessingResult.ProceedWithCommit);
        @continue.ToolTipText = $"If suitable, a default resolution will be applied.{Environment.NewLine}Otherwise, a null will be returned.";

        var cancel = Menu_AppendItem(failures.DropDown, "⏪ Cancel", (s, a) => { FailureProcessingMode = ARDB.FailureProcessingResult.ProceedWithRollBack; ExpireSolution(true); }, true, FailureProcessingMode == ARDB.FailureProcessingResult.ProceedWithRollBack);
        cancel.ToolTipText = $"A failing element will cancel the whole '{Name}' operation.{Environment.NewLine}Nothing will be returned in this case.";

        var custom = Menu_AppendItem(failures.DropDown, "⏸ Pause…", (s, a) => { FailureProcessingMode = ARDB.FailureProcessingResult.WaitForUserInput; ExpireSolution(true); }, true, FailureProcessingMode == ARDB.FailureProcessingResult.WaitForUserInput);
        custom.ToolTipText = $"Do a pause and let me decide.{Environment.NewLine}Shows Revit failures report dialog.";
      }
    }
    #endregion
  }

  public abstract class ElementTrackerComponent : TransactionalChainComponent, IGH_TrackingComponent
  {
    protected ElementTrackerComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory)
    { }

    protected virtual bool CurrentDocumentOnly
    {
      get
      {
        if(Params.Input<Parameters.Document>("Document") is IGH_Param document)
          return document.SourceCount == 0 && document.DataType == GH_ParamData.@void;

        return Inputs.Any(x => x.Param is Parameters.Document && x.Param.Name == "Document");
      }
    }

    protected override void BeforeSolveInstance()
    {
      base.BeforeSolveInstance();

      var currentDocument = CurrentDocumentOnly ? Revit.ActiveDBDocument : default;
      foreach (var output in Params.Output.OfType<IGH_TrackingParam>())
        output.OpenTrackingParam(currentDocument);
    }

    protected override void AfterSolveInstance()
    {
      foreach (var output in Params.Output.OfType<IGH_TrackingParam>())
        output.CloseTrackingParam();

      base.AfterSolveInstance();
    }

    protected T ReconstructElement<T>(ARDB.Document document, string parameterName, Func<T, T> func) where T : ARDB.Element
    {
      var output = default(T);

      if (Params.ReadTrackedElement(parameterName, document, out T input))
      {
        if (input?.DesignOption?.Id is ARDB.ElementId elementDesignOptionId)
        {
          var activeDesignOptionId = ARDB.DesignOption.GetActiveDesignOptionId(input.Document);

          if (elementDesignOptionId != activeDesignOptionId)
            input = null;
        }

        var graphical = input is object && Types.GraphicalElement.IsValidElement(input);
        var pinned = input?.Pinned != false;

        try
        {
          if (!graphical || pinned)
            UpdateDocument(document, () => output = func(input));
          else
          {
            if (graphical)
              AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Some elements were ignored because are unpinned.");

            output = input;
          }
        }
        catch(Exception e)
        {
          if (FailureProcessingMode <= ARDB.FailureProcessingResult.ProceedWithCommit)
            output = input;

          throw e;
        }
        finally
        {
          Params.WriteTrackedElement(parameterName, document, output);

          if (pinned && Types.GraphicalElement.IsValidElement(output))
          {
            // Reset CreatedPhaseId to last phase available
            if (!document.IsFamilyDocument && output.CreatedPhaseId != ARDB.ElementId.InvalidElementId && output.ArePhasesModifiable())
            {
              using (var phases = document.Phases)
              {
                if (!phases.IsEmpty)
                {
                  try
                  {
                    var createdPhaseId = document.Phases.Cast<ARDB.Phase>().Last().Id;
                    if (output.CreatedPhaseId != createdPhaseId && output.IsPhaseCreatedValid(createdPhaseId))
                    {
                      output.DemolishedPhaseId = ARDB.ElementId.InvalidElementId;
                      output.CreatedPhaseId = createdPhaseId;
                    }
                  }
                  catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }
                }
              }
            }

            // In case element is crated on this iteratrion we pin it here by default
            if (!output.Pinned)
            {
              try { output.Pinned = true; }
              catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }
            }
          }
        }
      }

      return output;
    }

    /// <summary>
    /// Check if there is a non tracked element with the desired name.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="paramName"></param>
    /// <param name="document"></param>
    /// <param name="elementNomen"></param>
    /// <param name="element"></param>
    /// <param name="categoryId"></param>
    /// <returns></returns>
    protected bool CanReconstruct<T>
    (
      string paramName,
      out bool untracked,
      ref T element,
      ARDB.Document document,
      string elementNomen,
      string parentNomen = default, ARDB.BuiltInCategory? categoryId = default
    )
      where T : ARDB.Element
    {
      return CanReconstruct
      (
        paramName,
        out untracked,
        ref element,
        document,
        elementNomen,
        (doc, name) =>
        {
          doc.TryGetElement(out T existing, name, parentNomen, categoryId);
          return existing;
        }
      );
    }

    protected internal bool CanReconstruct<T>
    (
      string paramName,
      out bool untracked,
      ref T element,
      ARDB.Document document,
      string elementNomen,
      Func<ARDB.Document, string, T> GetElement
    )
      where T : ARDB.Element
    {
      var nomenParameter = ARDB.BuiltInParameter.INVALID;
      if
      (
        !string.IsNullOrWhiteSpace(elementNomen) &&
        element?.GetElementNomen(out nomenParameter) != elementNomen
      )
      {
        // Query for an existing element.
        if (GetElement(document, elementNomen) is T existing)
        {
          Debug.Assert(existing.Id != element?.Id);

          if (Params.IsTrackedElement(paramName, existing))
          {
            // If existing is tracked and still pending to be processed
            // change its name to avoid collisions.
            existing.SetElementNomen(nomenParameter, existing.UniqueId);
          }
          else
          {
            untracked = true;
            return (element = PostNomenAlreadyInUse(existing)) is object;
          }
        }
      }

      untracked = false;
      return true;
    }

    protected T PostNomenAlreadyInUse<T>(T existing)
      where T: ARDB.Element
    {
      if (existing is object)
      {
        var nomen = existing.GetElementNomen(out var nomemParameter);
        var label = ((ERDB.Schemas.ParameterId) nomemParameter).Label;
        if (string.IsNullOrWhiteSpace(label)) label = "name";
        var message = $"The {label.ToLowerInvariant()} '{nomen}' is already in use.";

        if (FailureProcessingMode == ARDB.FailureProcessingResult.Continue)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, message);
          return null;
        }

        if (FailureProcessingMode == ARDB.FailureProcessingResult.ProceedWithCommit)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{message} Using existing.");
          return existing;
        }

        if (FailureProcessingMode == ARDB.FailureProcessingResult.WaitForUserInput)
        {
          var failureId = existing is ARDB.ViewSheet ?
            ARDB.BuiltInFailures.SheetFailures.SheetNumberDuplicated :
            ARDB.BuiltInFailures.GeneralFailures.NameNotUnique;

          using (var failure = new ARDB.FailureMessage(failureId))
          {
            failure.SetFailingElement(existing.Id);
            existing.Document.PostFailure(failure);
          }

          return null;
        }

        throw new Exceptions.RuntimeException(message);
      }

      throw new Exceptions.RuntimeException();
    }

    #region IGH_TrackingComponent
    TrackingMode IGH_TrackingComponent.TrackingMode => TrackingMode;
    internal TrackingMode TrackingMode { get; set; } = TrackingMode.Reconstruct;
    #endregion

    #region IO
    public override void AddedToDocument(GH_Document document)
    {
      if (ComponentVersion < new Version(0, 9, 0, 0))
        TrackingMode = TrackingMode.Reconstruct;

      base.AddedToDocument(document);
    }

    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      int mode = (int) TrackingMode.Disabled;
      reader.TryGetInt32("TrackingMode", ref mode);
      TrackingMode = (TrackingMode) mode;

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (TrackingMode != TrackingMode.Disabled)
        writer.SetInt32("TrackingMode", (int) TrackingMode);

      return true;
    }
    #endregion

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      if (TrackingMode != TrackingMode.NotApplicable)
      {
        if (Params.Output.OfType<IGH_TrackingParam>().FirstOrDefault() is IGH_Param output)
        {
          var tracking = Menu_AppendItem(menu, "Tracking Mode");

          var append = Menu_AppendItem(tracking.DropDown, "Disabled", (s, a) => { TrackingMode = TrackingMode.Disabled; ExpireSolution(true); }, true, TrackingMode == TrackingMode.Disabled);
          append.ToolTipText = $"No element tracking takes part in this mode, each solution will append a new {output.TypeName}." + Environment.NewLine +
                               $"The operation may fail if a {output.TypeName} with same name already exists.";

          var supersede = Menu_AppendItem(tracking.DropDown, "Enabled : Replace", (s, a) => { TrackingMode = TrackingMode.Supersede; ExpireSolution(true); }, true, TrackingMode == TrackingMode.Supersede);
          supersede.ToolTipText = $"A brand new {output.TypeName} will be created for each solution." + Environment.NewLine +
                                  $"{GH_Convert.ToPlural(output.TypeName)} created on previous iterations are deleted.";

          var reconstruct = Menu_AppendItem(tracking.DropDown, "Enabled : Update", (s, a) => { TrackingMode = TrackingMode.Reconstruct; ExpireSolution(true); }, true, TrackingMode == TrackingMode.Reconstruct);
          reconstruct.ToolTipText = $"If suitable, the previous solution {output.TypeName} will be updated from the input values;" + Environment.NewLine +
                                    "otherwise, a new one will be created.";
        }
      }
    }
    #endregion
  }
}

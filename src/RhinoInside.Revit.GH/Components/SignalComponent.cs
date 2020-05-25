using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Autodesk.Revit.UI.Events;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class SignalComponent : ZuiComponent
  {
    protected SignalComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    #region Signal
    protected static readonly string SignalParamName = "Signal";
    protected int SignalParamIndex => Params.IndexOfInputParam(SignalParamName);
    protected IGH_Param SignalParam => SignalParamIndex < 0 ? default : Params.Input[SignalParamIndex];

    protected static IGH_Param CreateSignalParam() => new Parameters.Param_Enum<Types.ComponentSignal>()
    {
      Name = SignalParamName,
      NickName = Grasshopper.CentralSettings.CanvasFullNames ? "Signal" : "S",
      Description = "Component signal",
      Access = GH_ParamAccess.tree,
      WireDisplay = GH_ParamWireDisplay.hidden
    };

    protected Kernel.ComponentSignal Signal { get; set; } = Kernel.ComponentSignal.Active;
    static Kernel.ComponentSignal? MaxSignal(IEnumerable<IGH_Goo> signals)
    {
      if (signals is object)
      {
        Kernel.ComponentSignal? max = default;
        foreach (var goo in signals)
        {
          if (goo is Types.ComponentSignal signal)
          {
            var value = signal.Value;
            if (!max.HasValue)
              max = value;

            if (value == Kernel.ComponentSignal.Frozen)
              continue;

            if (Math.Abs((int) value) > (int) max.Value)
              max = value;
          }
        }

        return max;
      }

      return default;
    }

    public override void ExpireSolution(bool recompute)
    {
      if (SignalParam is IGH_Param signal)
      {
        Phase = GH_SolutionPhase.Blank;

        if (signal.DataType == GH_ParamData.@void)
          Signal = Kernel.ComponentSignal.Frozen;

        OnSolutionExpired(recompute);
      }
      else
      {
        Signal = Kernel.ComponentSignal.Active;
        base.ExpireSolution(recompute);
      }
    }

    public override void CollectData()
    {
      if (Phase == GH_SolutionPhase.Collected)
        return;

      base.CollectData();

      var _Signal_ = Params.IndexOfInputParam(SignalParamName);
      if (_Signal_ >= 0)
      {
        var signal = Params.Input[_Signal_];
        Signal = MaxSignal(signal.VolatileData.AllData(false)).GetValueOrDefault();

        if (signal.DataType == GH_ParamData.@void)
          signal.NickName = SignalParamName;
        else
          signal.NickName = Signal.ToString();

        if (Signal != Kernel.ComponentSignal.Frozen)
        {
          if (OnPingDocument() is GH_Document doc)
          {
            doc.ScheduleSolution
            (
              0,
              x =>
              {
                base.ClearData();
                base.ExpireDownStreamObjects();

                // Mark it as Collected to avoid collect it again
                Phase = GH_SolutionPhase.Collected;
              }
            );
          }
        }

        Phase = GH_SolutionPhase.Computed;
      }
    }

    internal new class Attributes : ZuiComponent.Attributes
    {
      public Attributes(SignalComponent owner) : base(owner) { }

      protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
      {
        if (channel == GH_CanvasChannel.Objects && Owner is SignalComponent component && component.Signal != Kernel.ComponentSignal.Active)
        {
          var basePalette = Owner.Hidden || !Owner.IsPreviewCapable ? GH_Palette.Hidden : GH_Palette.Normal;
          var baseStyle = GH_CapsuleRenderEngine.GetImpliedStyle(basePalette, Selected, Owner.Locked, Owner.Hidden);

          var palette = GH_CapsuleRenderEngine.GetImpliedPalette(Owner);
          if (palette == GH_Palette.Normal && !Owner.IsPreviewCapable)
            palette = GH_Palette.Hidden;

          var style = GH_CapsuleRenderEngine.GetImpliedStyle(palette, Selected, Owner.Locked, Owner.Hidden);
          var fill = style.Fill;
          var edge = style.Edge;
          var text = style.Text;

          try
          {
            switch (component.Signal)
            {
              case Kernel.ComponentSignal.Frozen:

                style.Edge = Color.FromArgb(150, fill.R, fill.G, fill.B);
                if (Selected)
                  style.Fill = Color.FromArgb(GH_Skin.palette_trans_selected.Fill.A, baseStyle.Fill.R, baseStyle.Fill.G, baseStyle.Fill.B);
                else
                  style.Fill = Color.FromArgb(GH_Skin.palette_trans_standard.Fill.A, baseStyle.Fill.R, baseStyle.Fill.G, baseStyle.Fill.B);

                style.Text = baseStyle.Text;
                break;

                //case Kernel.ComponentSignal.Simulated:

                //  if (palette == GH_Palette.Normal || palette == GH_Palette.Hidden)
                //    style.Edge = style.Edge;
                //  else
                //    style.Edge = Color.FromArgb(150, fill.R, fill.G, fill.B);

                //  style.Fill = baseStyle.Fill;
                //  style.Text = baseStyle.Text;

                //  break;
            }

            base.Render(canvas, graphics, channel);
          }
          finally
          {
            style.Fill = fill;
            style.Edge = edge;
            style.Text = text;
          }
        }
        else base.Render(canvas, graphics, channel);
      }

      bool CanvasFullNames = Grasshopper.CentralSettings.CanvasFullNames;
      public override void ExpireLayout()
      {
        if (CanvasFullNames != Grasshopper.CentralSettings.CanvasFullNames)
        {
          if (Owner is IGH_VariableParameterComponent variableParameterComponent)
            variableParameterComponent.VariableParameterMaintenance();

          CanvasFullNames = Grasshopper.CentralSettings.CanvasFullNames;
        }

        base.ExpireLayout();
      }
    }

    public override void CreateAttributes() => m_attributes = new Attributes(this);
    #endregion
  }

  public abstract class TransactionalComponent :
    SignalComponent,
    DB.IFailuresPreprocessor,
    DB.ITransactionFinalizer
  {
    protected TransactionalComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    #region Transaction
    DB.TransactionStatus status = DB.TransactionStatus.Uninitialized;
    public DB.TransactionStatus Status
    {
      get => status;
      protected set
      {
        //if (status < value)
          status = value;
      }
    }

    internal new class Attributes : SignalComponent.Attributes
    {
      public Attributes(TransactionalComponent owner) : base(owner) { }

      protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
      {
        if (channel == GH_CanvasChannel.Objects && !Owner.Locked && Owner is TransactionalComponent component)
        {
          if (component.Status != DB.TransactionStatus.RolledBack)
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
                case DB.TransactionStatus.Uninitialized:  palette = GH_Palette.Grey; break;
                case DB.TransactionStatus.Started:        palette = GH_Palette.White; break;
                case DB.TransactionStatus.RolledBack:     /*palette = GH_Palette.Normal;*/ break;
                case DB.TransactionStatus.Committed:      palette = GH_Palette.Black; break;
                case DB.TransactionStatus.Pending:        palette = GH_Palette.Blue;  break;
                case DB.TransactionStatus.Error:          palette = GH_Palette.Pink;  break;
                case DB.TransactionStatus.Proceed:        palette = GH_Palette.Brown; break;
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

      bool CanvasFullNames = Grasshopper.CentralSettings.CanvasFullNames;
      public override void ExpireLayout()
      {
        if (CanvasFullNames != Grasshopper.CentralSettings.CanvasFullNames)
        {
          if (Owner is IGH_VariableParameterComponent variableParameterComponent)
            variableParameterComponent.VariableParameterMaintenance();

          CanvasFullNames = Grasshopper.CentralSettings.CanvasFullNames;
        }

        base.ExpireLayout();
      }
    }

    public override void CreateAttributes() => m_attributes = new Attributes(this);

    protected DB.Transaction NewTransaction(DB.Document doc) => NewTransaction(doc, Name);
    protected DB.Transaction NewTransaction(DB.Document doc, string name)
    {
      var transaction = new DB.Transaction(doc, name);

      var options = transaction.GetFailureHandlingOptions();
      options = options.SetClearAfterRollback(true);
      options = options.SetDelayedMiniWarnings(false);
      options = options.SetForcedModalHandling(true);

      options = options.SetFailuresPreprocessor(this);
      options = options.SetTransactionFinalizer(this);

      transaction.SetFailureHandlingOptions(options);

      return transaction;
    }

    protected DB.TransactionStatus CommitTransaction(DB.Document doc, DB.Transaction transaction)
    {
      // Disable Rhino UI if any warning-error dialog popup
      External.EditScope editScope = null;
      EventHandler<DialogBoxShowingEventArgs> _ = null;
      try
      {
        Revit.ApplicationUI.DialogBoxShowing += _ = (sender, args) =>
        {
          if (editScope is null)
            editScope = new External.EditScope();
        };

        if (transaction.GetStatus() == DB.TransactionStatus.Started)
        {
          OnBeforeCommit(doc, transaction.GetName());

          return transaction.Commit();
        }
        else return transaction.RollBack();
      }
      finally
      {
        Revit.ApplicationUI.DialogBoxShowing -= _;

        if (editScope is IDisposable disposable)
          disposable.Dispose();
      }
    }
    #endregion

    // Setp 1.
    protected override void BeforeSolveInstance() => status = DB.TransactionStatus.Uninitialized;

    // Step 2.
    protected virtual void OnAfterStart(DB.Document document, string strTransactionName) { }

    // Step 3.
    //protected override void TrySolveInstance(IGH_DataAccess DA) { }

    // Step 4.
    protected virtual void OnBeforeCommit(DB.Document document, string strTransactionName) { }

    // Step 5.
    //protected override void AfterSolveInstance() {}

    // Step 5.1
    #region IFailuresPreprocessor
    void AddRuntimeMessage(DB.FailureMessageAccessor error, bool? solved = null)
    {
      if (error.GetFailureDefinitionId() == DBX.ExternalFailures.TransactionFailures.SimulatedTransaction)
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
        case DB.FailureSeverity.Warning: level = GH_RuntimeMessageLevel.Warning; break;
        case DB.FailureSeverity.Error: level = GH_RuntimeMessageLevel.Error; break;
      }

      string solvedMark = string.Empty;
      if (error.GetSeverity() > DB.FailureSeverity.Warning)
      {
        switch (solved)
        {
          case false: solvedMark = "❌ "; break;
          case true: solvedMark = "✔ "; break;
        }
      }

      var description = error.GetDescriptionText();
      var text = string.IsNullOrEmpty(description) ?
        $"{solvedMark}{level} {{{error.GetFailureDefinitionId().Guid}}}" :
        $"{solvedMark}{description}";

      int idsCount = 0;
      foreach (var id in error.GetFailingElementIds())
        text += idsCount++ == 0 ? $" {{{id.IntegerValue}" : $", {id.IntegerValue}";
      if (idsCount > 0) text += "} ";

      AddRuntimeMessage(level, text);
    }

    // Override to add handled failures to your component (Order is important).
    protected virtual IEnumerable<DB.FailureDefinitionId> FailureDefinitionIdsToFix => null;

    DB.FailureProcessingResult FixFailures(DB.FailuresAccessor failuresAccessor, IEnumerable<DB.FailureDefinitionId> failureIds)
    {
      foreach (var failureId in failureIds)
      {
        int solvedErrors = 0;

        foreach (var error in failuresAccessor.GetFailureMessages().Where(x => x.GetFailureDefinitionId() == failureId))
        {
          if (!failuresAccessor.IsFailureResolutionPermitted(error))
            continue;

          // Don't try to fix two times same issue
          if (failuresAccessor.GetAttemptedResolutionTypes(error).Any())
            continue;

          AddRuntimeMessage(error, true);

          failuresAccessor.ResolveFailure(error);
          solvedErrors++;
        }

        if (solvedErrors > 0)
          return DB.FailureProcessingResult.ProceedWithCommit;
      }

      return DB.FailureProcessingResult.Continue;
    }

    DB.FailureProcessingResult DB.IFailuresPreprocessor.PreprocessFailures(DB.FailuresAccessor failuresAccessor)
    {
      if (!failuresAccessor.IsTransactionBeingCommitted())
        return DB.FailureProcessingResult.Continue;

      if (failuresAccessor.GetSeverity() >= DB.FailureSeverity.DocumentCorruption)
        return DB.FailureProcessingResult.ProceedWithRollBack;

      if (failuresAccessor.GetSeverity() >= DB.FailureSeverity.Error)
      {
        // Handled failures in order
        {
          var failureDefinitionIdsToFix = FailureDefinitionIdsToFix;
          if (failureDefinitionIdsToFix != null)
          {
            var result = FixFailures(failuresAccessor, failureDefinitionIdsToFix);
            if (result != DB.FailureProcessingResult.Continue)
              return result;
          }
        }

        // Unhandled failures in incomming order
        {
          var failureDefinitionIdsToFix = failuresAccessor.GetFailureMessages().GroupBy(x => x.GetFailureDefinitionId()).Select(x => x.Key);
          var result = FixFailures(failuresAccessor, failureDefinitionIdsToFix);
          if (result != DB.FailureProcessingResult.Continue)
            return result;
        }
      }

      if (failuresAccessor.GetSeverity() >= DB.FailureSeverity.Warning)
      {
        // Unsolved failures or warnings
        foreach (var error in failuresAccessor.GetFailureMessages().OrderBy(error => error.GetSeverity()))
          AddRuntimeMessage(error, false);

        failuresAccessor.DeleteAllWarnings();
      }

      if (failuresAccessor.GetSeverity() >= DB.FailureSeverity.Error)
        return DB.FailureProcessingResult.ProceedWithRollBack;

      return DB.FailureProcessingResult.Continue;
    }
    #endregion

    // Step 5.2
    #region ITransactionFinalizer
    // Step 5.2.A
    public virtual void OnCommitted(DB.Document document, string strTransactionName)
    {
      if(Status < DB.TransactionStatus.Committed)
        Status = DB.TransactionStatus.Committed;
    }

    // Step 5.2.B
    public virtual void OnRolledBack(DB.Document document, string strTransactionName)
    {
      foreach (var param in Params.Output)
        param.Phase = GH_SolutionPhase.Failed;

      if (Status < DB.TransactionStatus.RolledBack)
        Status = DB.TransactionStatus.RolledBack;
    }
    #endregion
  }

}

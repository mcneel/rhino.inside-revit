using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Autodesk.Revit.UI.Events;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.External.DB.Extensions;
using DBX = RhinoInside.Revit.External.DB;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using System.Drawing;
  using Exceptions;
  using Grasshopper.GUI.Canvas;
  using Grasshopper.Kernel.Attributes;
  using Kernel.Attributes;

  public abstract class TransactionalComponent :
    Component,
    DB.IFailuresPreprocessor,
    DB.ITransactionFinalizer
  {
    protected TransactionalComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    #region Signal
    protected static readonly string SignalParamName = "Signal";
    protected int SignalParamIndex => Params.IndexOfInputParam(SignalParamName);
    protected IGH_Param SignalParam => SignalParamIndex < 0 ? default : Params.Input[SignalParamIndex];
    protected DBX.TransactionSignal Signal = DBX.TransactionSignal.Effective;

    protected static IGH_Param CreateSignalParam() => new Parameters.Param_Enum<Types.TransactionSignal>()
    {
      Name = SignalParamName,
      NickName = Grasshopper.CentralSettings.CanvasFullNames ? "Signal" : "S",
      Description = "Transaction signal",
      Access = GH_ParamAccess.tree,
      WireDisplay = GH_ParamWireDisplay.hidden
    };

    static DBX.TransactionSignal? MaxSignal(IEnumerable<IGH_Goo> signals)
    {
      if (signals is object)
      {
        DBX.TransactionSignal? max = default;
        foreach (var goo in signals)
        {
          if (goo is Types.TransactionSignal signal)
          {
            var value = signal.Value;
            if (!max.HasValue)
              max = value;

            if (value == DBX.TransactionSignal.Frozen)
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
          Signal = DBX.TransactionSignal.Frozen;

        OnSolutionExpired(recompute);
      }
      else
      {
        Signal = DBX.TransactionSignal.Effective;
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

        if (Signal != DBX.TransactionSignal.Frozen)
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
    #endregion

    #region UI
    new class Attributes : GH_ComponentAttributes
    {
      public Attributes(TransactionalComponent owner) : base(owner) { }

      protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
      {
        if (channel == GH_CanvasChannel.Objects && Owner is TransactionalComponent component)
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
              case DBX.TransactionSignal.Frozen:

                style.Edge = Color.FromArgb(150, fill.R, fill.G, fill.B);
                if (Selected)
                  style.Fill = Color.FromArgb(GH_Skin.palette_trans_selected.Fill.A, baseStyle.Fill.R, baseStyle.Fill.G, baseStyle.Fill.B);
                else
                  style.Fill = Color.FromArgb(GH_Skin.palette_trans_standard.Fill.A, baseStyle.Fill.R, baseStyle.Fill.G, baseStyle.Fill.B);

                style.Text = baseStyle.Text;
                break;

              case DBX.TransactionSignal.Effective:

                if (!Owner.Locked)
                {
                  if (palette == GH_Palette.Normal || palette == GH_Palette.Hidden)
                  {
                    if (Selected)
                    {
                      style.Fill = GH_Skin.palette_black_selected.Fill;
                      style.Text = GH_Skin.palette_black_selected.Text;
                    }
                    else
                    {
                      style.Edge = Color.FromArgb(255, 80, 80, 80);
                      style.Fill = GH_Skin.palette_black_standard.Fill;
                      style.Text = GH_Skin.palette_black_standard.Text;
                    }
                  }
                }

                break;
              case DBX.TransactionSignal.Simulated:

                if (palette == GH_Palette.Normal || palette == GH_Palette.Hidden)
                  style.Edge = style.Edge;
                else
                  style.Edge = Color.FromArgb(150, fill.R, fill.G, fill.B);

                style.Fill = baseStyle.Fill;
                style.Text = baseStyle.Text;

                break;
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
      {
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
    }

    // Setp 1.
    // protected override void BeforeSolveInstance() { }

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

      if(failuresAccessor.GetSeverity() >= DB.FailureSeverity.Error)
        return DB.FailureProcessingResult.ProceedWithRollBack;

      return DB.FailureProcessingResult.Continue;
    }
    #endregion

    // Step 5.2
    #region ITransactionFinalizer
    // Step 5.2.A
    public virtual void OnCommitted(DB.Document document, string strTransactionName) { }

    // Step 5.2.B
    public virtual void OnRolledBack(DB.Document document, string strTransactionName)
    {
      foreach (var param in Params.Output)
        param.Phase = GH_SolutionPhase.Failed;
    }
    #endregion

    #region Solve Optional values
    protected static double LiteralLengthValue(double meters)
    {
      switch (Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem)
      {
        case Rhino.UnitSystem.None:
        case Rhino.UnitSystem.Inches:
        case Rhino.UnitSystem.Feet:
          return Math.Round(meters * Rhino.RhinoMath.UnitScale(Rhino.UnitSystem.Meters, Rhino.UnitSystem.Feet))
                 * Rhino.RhinoMath.UnitScale(Rhino.UnitSystem.Feet, Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem);
        default:
          return meters * Rhino.RhinoMath.UnitScale(Rhino.UnitSystem.Meters, Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem);
      }
    }

    protected static void ChangeElementTypeId<T>(ref T element, DB.ElementId elementTypeId) where T : DB.Element
    {
      if (element is object && elementTypeId != element.GetTypeId())
      {
        var doc = element.Document;
        if (element.IsValidType(elementTypeId))
        {
          var newElmentId = element.ChangeTypeId(elementTypeId);
          if (newElmentId != DB.ElementId.InvalidElementId)
            element = (T) doc.GetElement(newElmentId);
        }
        else element = null;
      }
    }

    protected static void ChangeElementType<E, T>(ref E element, Optional<T> elementType) where E : DB.Element where T : DB.ElementType
    {
      if (elementType.HasValue && element is object)
      {
        if (!element.Document.Equals(elementType.Value.Document))
          throw new ArgumentException($"{nameof(ChangeElementType)} failed to assign a type from a diferent document.", nameof(elementType));

        ChangeElementTypeId(ref element, elementType.Value.Id);
      }
    }

    protected static bool SolveOptionalCategory(ref Optional<DB.Category> category, DB.Document doc, DB.BuiltInCategory builtInCategory, string paramName)
    {
      bool wasMissing = category.IsMissing;

      if (wasMissing)
      {
        if (doc.IsFamilyDocument)
          category = doc.OwnerFamily.FamilyCategory;

        if(category.IsMissing)
        {
          category = Autodesk.Revit.DB.Category.GetCategory(doc, builtInCategory) ??
          throw new ArgumentException("No suitable Category has been found.", paramName);
        }
      }

      else if (category.Value == null)
        throw new ArgumentNullException(paramName);

      return wasMissing;
    }

    protected static bool SolveOptionalType<T>(ref Optional<T> type, DB.Document doc, DB.ElementTypeGroup group, string paramName) where T : DB.ElementType
    {
      return SolveOptionalType(ref type, doc, group, (document, name) => throw new ArgumentNullException(paramName), paramName);
    }

    protected static bool SolveOptionalType<T>(ref Optional<T> type, DB.Document doc, DB.ElementTypeGroup group, Func<DB.Document, string, T> recoveryAction, string paramName) where T : DB.ElementType
    {
      bool wasMissing = type.IsMissing;

      if (wasMissing)
        type = (T) doc.GetElement(doc.GetDefaultElementTypeId(group)) ??
        throw new ArgumentException($"No suitable {group} has been found.", paramName);

      else if (type.Value == null)
        type = (T) recoveryAction.Invoke(doc, paramName);

      return wasMissing;
    }

    protected static bool SolveOptionalType(ref Optional<DB.FamilySymbol> type, DB.Document doc, DB.BuiltInCategory category, string paramName)
    {
      bool wasMissing = type.IsMissing;

      if (wasMissing)
        type = doc.GetElement(doc.GetDefaultFamilyTypeId(new DB.ElementId(category))) as DB.FamilySymbol ??
               throw new ArgumentException("No suitable type has been found.", paramName);

      else if (type.Value == null)
        throw new ArgumentNullException(paramName);

      else if (!type.Value.Document.Equals(doc))
        throw new ArgumentException($"{nameof(SolveOptionalType)} failed to assign a type from a diferent document.", nameof(type));

      if (!type.Value.IsActive)
        type.Value.Activate();

      return wasMissing;
    }

    protected static bool SolveOptionalLevel(DB.Document doc, double elevation, ref Optional<DB.Level> level)
    {
      bool wasMissing = level.IsMissing;

      if (wasMissing)
        level = doc.FindLevelByElevation(elevation / Revit.ModelUnits) ??
                throw new ArgumentException("No suitable level has been found.", nameof(elevation));

      else if (level.Value == null)
        throw new ArgumentNullException(nameof(level));

      else if (!level.Value.Document.Equals(doc))
        throw new ArgumentException("Failed to assign a level from a diferent document.", nameof(level));

      return wasMissing;
    }

    protected static bool SolveOptionalLevel(DB.Document doc, Rhino.Geometry.Point3d point, ref Optional<DB.Level> level, out Rhino.Geometry.BoundingBox bbox)
    {
      bbox = new Rhino.Geometry.BoundingBox(point, point);
      return SolveOptionalLevel(doc, point.IsValid ? point.Z : double.NaN, ref level);
    }

    protected static bool SolveOptionalLevel(DB.Document doc, Rhino.Geometry.Line line, ref Optional<DB.Level> level, out Rhino.Geometry.BoundingBox bbox)
    {
      bbox = line.BoundingBox;
      return SolveOptionalLevel(doc, bbox.IsValid ? bbox.Min.Z : double.NaN, ref level);
    }

    protected static bool SolveOptionalLevel(DB.Document doc, Rhino.Geometry.GeometryBase geometry, ref Optional<DB.Level> level, out Rhino.Geometry.BoundingBox bbox)
    {
      bbox = geometry.GetBoundingBox(true);
      return SolveOptionalLevel(doc, bbox.IsValid ? bbox.Min.Z : double.NaN, ref level);
    }

    protected static bool SolveOptionalLevel(DB.Document doc, IEnumerable<Rhino.Geometry.GeometryBase> geometries, ref Optional<DB.Level> level, out Rhino.Geometry.BoundingBox bbox)
    {
      bbox = Rhino.Geometry.BoundingBox.Empty;
      foreach (var geometry in geometries)
        bbox.Union(geometry.GetBoundingBox(true));

      return SolveOptionalLevel(doc, bbox.IsValid ? bbox.Min.Z : double.NaN, ref level);
    }

    protected static void SolveOptionalLevelsFromBase(DB.Document doc, double elevation, ref Optional<DB.Level> baseLevel, ref Optional<DB.Level> topLevel)
    {
      if (baseLevel.IsMissing && topLevel.IsMissing)
      {
        var b = doc.FindBaseLevelByElevation(elevation / Revit.ModelUnits, out var t) ??
                t ?? throw new ArgumentException("No suitable base level has been found.", nameof(elevation));

        if (!baseLevel.HasValue)
          baseLevel = b;

        if (!topLevel.HasValue)
          topLevel = t ?? b;
      }

      else if (baseLevel.Value == null)
        throw new ArgumentNullException(nameof(baseLevel));

      else if (topLevel.Value == null)
        throw new ArgumentNullException(nameof(topLevel));

      else if (!baseLevel.Value.Document.Equals(doc))
        throw new ArgumentException("Failed to assign a level from a diferent document.", nameof(baseLevel));

      else if (!topLevel.Value.Document.Equals(doc))
        throw new ArgumentException("Failed to assign a level from a diferent document.", nameof(topLevel));
    }

    protected static void SolveOptionalLevelsFromTop(DB.Document doc, double elevation, ref Optional<DB.Level> baseLevel, ref Optional<DB.Level> topLevel)
    {
      if (baseLevel.IsMissing && topLevel.IsMissing)
      {
        var t = doc.FindTopLevelByElevation(elevation / Revit.ModelUnits, out var b) ??
                b ?? throw new ArgumentException("No suitable top level has been found.", nameof(elevation));

        if (!topLevel.HasValue)
          topLevel = t;

        if (!baseLevel.HasValue)
          baseLevel = b ?? t;
      }

      else if (baseLevel.Value == null)
        throw new ArgumentNullException(nameof(baseLevel));

      else if (topLevel.Value == null)
        throw new ArgumentNullException(nameof(topLevel));

      else if (!baseLevel.Value.Document.Equals(doc))
        throw new ArgumentException("Failed to assign a level from a diferent document.", nameof(baseLevel));

      else if (!topLevel.Value.Document.Equals(doc))
        throw new ArgumentException("Failed to assign a level from a diferent document.", nameof(topLevel));
    }

    protected static bool SolveOptionalLevels(DB.Document doc, Rhino.Geometry.Curve curve, ref Optional<DB.Level> baseLevel, ref Optional<DB.Level> topLevel)
    {
      bool result = true;

      result &= SolveOptionalLevel(doc, Math.Min(curve.PointAtStart.Z, curve.PointAtEnd.Z), ref baseLevel);
      result &= SolveOptionalLevel(doc, Math.Max(curve.PointAtStart.Z, curve.PointAtEnd.Z), ref baseLevel);

      return result;
    }
    #endregion
  }

  public abstract class TransactionComponent : TransactionalComponent
  {
    protected TransactionComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    #region Autodesk.Revit.DB.Transacion support
    protected enum TransactionStrategy
    {
      PerSolution,
      PerComponent
    }
    protected virtual TransactionStrategy TransactionalStrategy => TransactionStrategy.PerComponent;

    protected DB.Transaction CurrentTransaction;
    protected DB.TransactionStatus TransactionStatus => CurrentTransaction?.GetStatus() ?? DB.TransactionStatus.Uninitialized;

    [Obsolete("Superseded by 'StartTransaction' since 2020-05-21")]
    protected void BeginTransaction(DB.Document document) => StartTransaction(document);
    protected void StartTransaction(DB.Document document)
    {
      if (document is null)
        return;

      CurrentTransaction = NewTransaction(document, Name);
      if (CurrentTransaction.Start() != DB.TransactionStatus.Started)
      {
        CurrentTransaction.Dispose();
        CurrentTransaction = null;
        throw new InvalidOperationException($"Unable to start Transaction '{Name}'");
      }
    }

    protected DB.TransactionStatus CommitTransaction(DB.Document document)
    {
      try     { return base.CommitTransaction(document, CurrentTransaction); }
      finally { CurrentTransaction.Dispose(); CurrentTransaction = default; }
    }
    #endregion

    // Step 1.
    protected override void BeforeSolveInstance()
    {
      if (TransactionalStrategy != TransactionStrategy.PerComponent)
        return;

      if (Revit.ActiveDBDocument is DB.Document Document)
      {
        StartTransaction(Document);

        OnAfterStart(Document, CurrentTransaction.GetName());
      }
    }

    // Step 2.
    //protected override void OnAfterStart(Document document, string strTransactionName) { }

    // Step 3.
    //protected override void TrySolveInstance(IGH_DataAccess DA) { }

    // Step 4.
    //protected override void OnBeforeCommit(Document document, string strTransactionName) { }

    // Step 5.
    protected override void AfterSolveInstance()
    {
      if (TransactionalStrategy != TransactionStrategy.PerComponent)
        return;

      try
      {
        if (RunCount <= 0)
          return;

        if (TransactionStatus == DB.TransactionStatus.Uninitialized)
          return;

        if (Phase != GH_SolutionPhase.Failed)
        {
          if (Revit.ActiveDBDocument is DB.Document Document)
            CommitTransaction(Document);
        }
      }
      finally
      {
        switch (TransactionStatus)
        {
          case DB.TransactionStatus.Uninitialized:
          case DB.TransactionStatus.Started:
          case DB.TransactionStatus.Committed:
            break;
          default:
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Transaction {TransactionStatus} and aborted.");
            break;
        }

        CurrentTransaction?.Dispose();
        CurrentTransaction = null;
      }
    }
  }

  public abstract class TransactionsComponent : TransactionalComponent
  {
    protected TransactionsComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    #region Autodesk.Revit.DB.Transacion support
    protected enum TransactionStrategy
    {
      PerSolution,
      PerComponent
    }
    protected virtual TransactionStrategy TransactionalStrategy => TransactionStrategy.PerComponent;

    Dictionary<DB.Document, DB.Transaction> CurrentTransactions;

    [Obsolete("Superseded by 'StartTransaction' since 2020-05-21")]
    protected void BeginTransaction(DB.Document document) => StartTransaction(document);
    protected void StartTransaction(DB.Document document)
    {
      if (CurrentTransactions?.ContainsKey(document) != true)
      {
        var transaction = NewTransaction(document, Name);
        if (transaction.Start() != DB.TransactionStatus.Started)
        {
          transaction.Dispose();
          throw new InvalidOperationException($"Unable to start Transaction '{Name}'");
        }

        if (CurrentTransactions is null)
          CurrentTransactions = new Dictionary<DB.Document, DB.Transaction>();

        CurrentTransactions.Add(document, transaction);
      }
    }

    // Step 5.
    protected override void AfterSolveInstance()
    {
      if (TransactionalStrategy != TransactionStrategy.PerComponent)
        return;

      if (CurrentTransactions is null)
        return;

      try
      {
        if (RunCount <= 0)
          return;

        foreach (var transaction in CurrentTransactions)
        {
          try
          {
            if (Phase != GH_SolutionPhase.Failed && transaction.Value.GetStatus() != DB.TransactionStatus.Uninitialized)
            {
              CommitTransaction(transaction.Key, transaction.Value);
            }
          }
          finally
          {
            var transactionStatus = transaction.Value.GetStatus();
            switch (transactionStatus)
            {
              case DB.TransactionStatus.Uninitialized:
              case DB.TransactionStatus.Started:
              case DB.TransactionStatus.Committed:
                break;
              default:
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Transaction {transactionStatus} and aborted.");
                break;
            }
          }
        }
      }
      finally
      {
        foreach (var transaction in CurrentTransactions)
          transaction.Value.Dispose();

        CurrentTransactions = null;
      }
    }
    #endregion
  }

  public abstract class ReflectedComponent : TransactionComponent
  {
    protected ReflectedComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    #region Reflection
    static Dictionary<Type, Tuple<Type, Type>> ParamTypes = new Dictionary<Type, Tuple<Type, Type>>()
    {
      { typeof(bool),                           Tuple.Create(typeof(Param_Boolean),           typeof(GH_Boolean))         },
      { typeof(int),                            Tuple.Create(typeof(Param_Integer),           typeof(GH_Integer))         },
      { typeof(double),                         Tuple.Create(typeof(Param_Number),            typeof(GH_Number))          },
      { typeof(string),                         Tuple.Create(typeof(Param_String),            typeof(GH_String))          },
      { typeof(Guid),                           Tuple.Create(typeof(Param_Guid),              typeof(GH_Guid))            },
      { typeof(DateTime),                       Tuple.Create(typeof(Param_Time),              typeof(GH_Time))            },

      { typeof(Rhino.Geometry.Transform),       Tuple.Create(typeof(Param_Transform),         typeof(GH_Transform))       },
      { typeof(Rhino.Geometry.Point3d),         Tuple.Create(typeof(Param_Point),             typeof(GH_Point))           },
      { typeof(Rhino.Geometry.Vector3d),        Tuple.Create(typeof(Param_Vector),            typeof(GH_Vector))          },
      { typeof(Rhino.Geometry.Plane),           Tuple.Create(typeof(Param_Plane),             typeof(GH_Plane))          },
      { typeof(Rhino.Geometry.Line),            Tuple.Create(typeof(Param_Line),              typeof(GH_Line))            },
      { typeof(Rhino.Geometry.Arc),             Tuple.Create(typeof(Param_Arc),               typeof(GH_Arc))             },
      { typeof(Rhino.Geometry.Circle),          Tuple.Create(typeof(Param_Circle),            typeof(GH_Circle))          },
      { typeof(Rhino.Geometry.Curve),           Tuple.Create(typeof(Param_Curve),             typeof(GH_Curve))           },
      { typeof(Rhino.Geometry.Surface),         Tuple.Create(typeof(Param_Surface),           typeof(GH_Surface))         },
      { typeof(Rhino.Geometry.Brep),            Tuple.Create(typeof(Param_Brep),              typeof(GH_Brep))            },
//    { typeof(Rhino.Geometry.Extrusion),       Tuple.Create(typeof(Param_Extrusion),         typeof(GH_Extrusion))       },
      { typeof(Rhino.Geometry.Mesh),            Tuple.Create(typeof(Param_Mesh),              typeof(GH_Mesh))            },
      { typeof(Rhino.Geometry.SubD),            Tuple.Create(typeof(Param_SubD),              typeof(GH_SubD))            },

      { typeof(IGH_Goo),                        Tuple.Create(typeof(Param_GenericObject),     typeof(IGH_Goo))            },
      { typeof(IGH_GeometricGoo),               Tuple.Create(typeof(Param_Geometry),          typeof(IGH_GeometricGoo))   },

      { typeof(Autodesk.Revit.DB.Category),     Tuple.Create(typeof(Parameters.Category),     typeof(Types.Category))     },
      { typeof(Autodesk.Revit.DB.Element),      Tuple.Create(typeof(Parameters.Element),      typeof(Types.Element))      },
      { typeof(Autodesk.Revit.DB.ElementType),  Tuple.Create(typeof(Parameters.ElementType),  typeof(Types.ElementType))  },
      { typeof(Autodesk.Revit.DB.Material),     Tuple.Create(typeof(Parameters.Material),     typeof(Types.Material))     },
      { typeof(Autodesk.Revit.DB.SketchPlane),  Tuple.Create(typeof(Parameters.SketchPlane),  typeof(Types.SketchPlane))  },
      { typeof(Autodesk.Revit.DB.Level),        Tuple.Create(typeof(Parameters.Level),        typeof(Types.Level))        },
      { typeof(Autodesk.Revit.DB.Grid),         Tuple.Create(typeof(Parameters.Grid),         typeof(Types.Grid))         },
    };

    protected bool TryGetParamTypes(Type type, out Tuple<Type, Type> paramTypes)
    {
      if (type.IsEnum)
      {
        if (!Types.GH_Enumerate.TryGetParamTypes(type, out paramTypes))
          paramTypes = Tuple.Create(typeof(Param_Integer), typeof(GH_Integer));

        return true;
      }

      if (!ParamTypes.TryGetValue(type, out paramTypes))
      {
        if (typeof(DB.ElementType).IsAssignableFrom(type))
        {
          paramTypes = Tuple.Create(typeof(Parameters.ElementType), typeof(Types.ElementType));
          return true;
        }

        if (typeof(DB.Element).IsAssignableFrom(type))
        {
          paramTypes = Tuple.Create(typeof(Parameters.Element), typeof(Types.Element));
          return true;
        }

        return false;
      }

      return true;
    }

    IGH_Param CreateParam(Type argumentType)
    {
      if (!TryGetParamTypes(argumentType, out var paramTypes))
        return new Param_GenericObject();

      return (IGH_Param) Activator.CreateInstance(paramTypes.Item1);
    }

    IGH_Goo CreateGoo(Type argumentType, object value)
    {
      if (!TryGetParamTypes(argumentType, out var paramTypes))
        return null;

      return (IGH_Goo) Activator.CreateInstance(paramTypes.Item2, value);
    }

    protected Type GetParameterType(ParameterInfo parameter, out GH_ParamAccess access, out bool optional)
    {
      var parameterType = parameter.ParameterType;
      optional = parameter.IsDefined(typeof(OptionalAttribute), false);
      access = GH_ParamAccess.item;

      var genericType = parameterType.IsGenericType ? parameterType.GetGenericTypeDefinition() : null;

      if (genericType != null && genericType == typeof(Optional<>))
      {
        optional = true;
        parameterType = parameterType.GetGenericArguments()[0];
        genericType = parameterType.IsGenericType ? parameterType.GetGenericTypeDefinition() : null;
      }

      if (genericType != null && genericType == typeof(IList<>))
      {
        access = GH_ParamAccess.list;
        parameterType = parameterType.GetGenericArguments()[0];
        genericType = parameterType.IsGenericType ? parameterType.GetGenericTypeDefinition() : null;
      }

      return parameterType;
    }

    DB.ElementFilter elementFilter = null;
    protected override DB.ElementFilter ElementFilter => elementFilter;

    protected void RegisterInputParams(GH_InputParamManager manager, MethodInfo methodInfo)
    {
      var elementFilterClasses = new List<Type>();

      foreach (var parameter in methodInfo.GetParameters())
      {
        if (parameter.Position < 2)
          continue;

        if (parameter.IsOut || parameter.ParameterType.IsByRef)
          throw new NotImplementedException();

        var parameterType = GetParameterType(parameter, out var access, out var optional);
        var nickname = parameter.Name.First().ToString().ToUpperInvariant();
        var name = nickname + parameter.Name.Substring(1);

        foreach (var nickNameAttribte in parameter.GetCustomAttributes(typeof(NickNameAttribute), false).Cast<NickNameAttribute>())
          nickname = nickNameAttribte.NickName;

        var description = string.Empty;
        foreach (var descriptionAttribute in parameter.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>())
          description = (description.Length > 0) ? $"{description}\r\n{descriptionAttribute.Description}" : descriptionAttribute.Description;

        var param = manager[manager.AddParameter(CreateParam(parameterType), name, nickname, description, access)];

        param.Optional = optional;

        if (parameterType.IsEnum && param is Param_Integer integerParam)
        {
          foreach (var e in Enum.GetValues(parameterType))
            integerParam.AddNamedValue(Enum.GetName(parameterType, e), (int) e);
        }
        else if (parameterType == typeof(Autodesk.Revit.DB.Element) || parameterType.IsSubclassOf(typeof(Autodesk.Revit.DB.Element)))
        {
          elementFilterClasses.Add(parameterType);
        }
        else if (parameterType == typeof(Autodesk.Revit.DB.Category))
        {
          elementFilterClasses.Add(typeof(Autodesk.Revit.DB.Element));
        }
      }

      if (elementFilterClasses.Count > 0 && !elementFilterClasses.Contains(typeof(Autodesk.Revit.DB.Element)))
      {
        elementFilter = (elementFilterClasses.Count == 1) ?
         (DB.ElementFilter) new Autodesk.Revit.DB.ElementClassFilter(elementFilterClasses[0]) :
         (DB.ElementFilter) new Autodesk.Revit.DB.LogicalOrFilter(elementFilterClasses.Select(x => new Autodesk.Revit.DB.ElementClassFilter(x)).ToArray());
      }
    }

    bool GetInputOptionalData<T>(IGH_DataAccess DA, int index, out Optional<T> optional)
    {
      if (GetInputData(DA, index, out T value))
      {
        optional = new Optional<T>(value);
        return true;
      }

      optional = Optional.Missing;
      return false;
    }
    static readonly MethodInfo GetInputOptionalDataInfo = typeof(ReflectedComponent).GetMethod("GetInputOptionalData", BindingFlags.Instance | BindingFlags.NonPublic);

    protected bool GetInputData<T>(IGH_DataAccess DA, int index, out T value)
    {
      if (typeof(T).IsEnum)
      {
        int enumValue = 0;
        if (!DA.GetData(index, ref enumValue))
        {
          var param = Params.Input[index];

          if (param.Optional && param.SourceCount == 0)
          {
            value = default;
            return false;
          }

          throw new ArgumentNullException(param.Name);
        }

        if (!typeof(T).IsEnumDefined(enumValue))
        {
          var param = Params.Input[index];
          throw new System.ComponentModel.InvalidEnumArgumentException(param.Name, enumValue, typeof(T));
        }

        value = (T) Enum.ToObject(typeof(T), enumValue);
      }
      else if (typeof(T).IsGenericType && (typeof(T).GetGenericTypeDefinition() == typeof(Optional<>)))
      {
        var args = new object[] { DA, index, null };

        try { return (bool) GetInputOptionalDataInfo.MakeGenericMethod(typeof(T).GetGenericArguments()[0]).Invoke(this, args); }
        catch (TargetInvocationException e) { throw e.InnerException; }
        finally { value = args[2] != null ? (T) args[2] : default; }
      }
      else
      {
        value = default;
        if (!DA.GetData(index, ref value))
        {
          var param = Params.Input[index];
          if (param.Optional && param.SourceCount == 0)
            return false;

          throw new ArgumentNullException(param.Name);
        }
      }

      return true;
    }
    protected static readonly MethodInfo GetInputDataInfo = typeof(ReflectedComponent).GetMethod("GetInputData", BindingFlags.Instance | BindingFlags.NonPublic);

    protected bool GetInputDataList<T>(IGH_DataAccess DA, int index, out IList<T> value)
    {
      var list = new List<T>();
      if (DA.GetDataList(index, list))
      {
        value = list;
        return true;
      }
      else
      {
        value = default;
        return false;
      }
    }
    protected static readonly MethodInfo GetInputDataListInfo = typeof(ReflectedComponent).GetMethod("GetInputDataList", BindingFlags.Instance | BindingFlags.NonPublic);

    static string FirstCharUpper(string text)
    {
      if (char.IsUpper(text, 0))
        return text;

      var chars = text.ToCharArray();
      chars[0] = char.ToUpperInvariant(chars[0]);
      return new string(chars);
    }

    protected void ThrowArgumentNullException(string paramName, string description = null) => throw new ArgumentNullException(FirstCharUpper(paramName), description ?? string.Empty);

    protected void ThrowArgumentException(string paramName, string description = null) => throw new ArgumentException(description ?? "Invalid value.", FirstCharUpper(paramName));

    protected bool ThrowIfNotValid(string paramName, Rhino.Geometry.Point3d value)
    {
      if (!value.IsValid) ThrowArgumentException(paramName);
      return true;
    }

    protected bool ThrowIfNotValid(string paramName, Rhino.Geometry.GeometryBase value)
    {
      if (value is null) ThrowArgumentException(paramName);
      if (!value.IsValidWithLog(out var log)) ThrowArgumentException(paramName, log);
      return true;
    }
    #endregion
  }

  public abstract class ReconstructElementComponent :
    ReflectedComponent,
    Bake.IGH_ElementIdBakeAwareObject
  {
    protected IGH_Goo[] PreviousStructure;
    System.Collections.IEnumerator PreviousStructureEnumerator;

    protected ReconstructElementComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected override sealed void RegisterInputParams(GH_InputParamManager manager)
    {
      var type = GetType();
      var ReconstructInfo = type.GetMethod($"Reconstruct{type.Name}", BindingFlags.Instance | BindingFlags.NonPublic);
      RegisterInputParams(manager, ReconstructInfo);
    }

    protected static void ReplaceElement<T>(ref T previous, T next, ICollection<DB.BuiltInParameter> parametersMask = null) where T : DB.Element
    {
      next.CopyParametersFrom(previous, parametersMask);
      previous = next;
    }

    // Step 2.
    protected override void OnAfterStart(DB.Document document, string strTransactionName)
    {
      PreviousStructureEnumerator = PreviousStructure?.GetEnumerator();
    }

    // Step 3.
    protected override sealed void TrySolveInstance(IGH_DataAccess DA)
    {
      if (Revit.ActiveDBDocument is DB.Document Document)
        Iterate(DA, Document, (DB.Document doc, ref DB.Element current) => TrySolveInstance(DA, doc, ref current));
      else
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "There is no active Revit document");
    }

    delegate void CommitAction(DB.Document doc, ref DB.Element element);

    void Iterate(IGH_DataAccess DA, DB.Document doc, CommitAction action)
    {
      var element = PreviousStructureEnumerator?.MoveNext() ?? false ?
                    (
                      PreviousStructureEnumerator.Current is Types.Element x && doc.Equals(x.Document) ?
                      doc.GetElement(x.Id) :
                      null
                    ) :
                    null;

      if (element?.Pinned != false)
      {
        var previous = element;

        if (element?.DesignOption?.Id is DB.ElementId elementDesignOptionId)
        {
          var activeDesignOptionId = DB.DesignOption.GetActiveDesignOptionId(element.Document);

          if (elementDesignOptionId != activeDesignOptionId)
            element = null;
        }

        try
        {
          action(doc, ref element);
        }
        catch (CancelException e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
          element = null;
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException e)
        {
          var message = e.Message.Split("\r\n".ToCharArray()).First().Replace("Application.ShortCurveTolerance", "Revit.ShortCurveTolerance");
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {message}");
          element = null;
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
          element = null;
        }
        catch (System.ComponentModel.WarningException e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, e.Message);
          element = null;
        }
        catch (System.ArgumentNullException)
        {
          // Grasshopper components use to send a Null when they receive a Null without throwing any error
          element = null;
        }
        catch (System.ArgumentException e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.Source}: {e.Message}");
          element = null;
        }
        catch (System.Exception e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
          DA.AbortComponentSolution();
        }
        finally
        {
          if (previous is object && !ReferenceEquals(previous, element) && previous.IsValidObject)
            previous.Document.Delete(previous.Id);

          if (element?.IsValidObject == true)
            element.Pinned = true;
        }
      }

      DA.SetData(0, element);
    }

    void TrySolveInstance
    (
      IGH_DataAccess DA,
      DB.Document doc,
      ref DB.Element element
    )
    {
      var type = GetType();
      var ReconstructInfo = type.GetMethod($"Reconstruct{type.Name}", BindingFlags.Instance | BindingFlags.NonPublic);
      var parameters = ReconstructInfo.GetParameters();

      var arguments = new object[parameters.Length];
      try
      {
        arguments[0] = doc;
        arguments[1] = element;

        var args = new object[] { DA, null, null };
        foreach (var parameter in parameters)
        {
          var paramIndex = parameter.Position - 2;

          if (paramIndex < 0)
            continue;

          args[1] = paramIndex;

          try
          {
            switch (Params.Input[paramIndex].Access)
            {
              case GH_ParamAccess.item: GetInputDataInfo.MakeGenericMethod(parameter.ParameterType).Invoke(this, args); break;
              case GH_ParamAccess.list: GetInputDataListInfo.MakeGenericMethod(parameter.ParameterType.GetGenericArguments()[0]).Invoke(this, args); break;
              default: throw new NotImplementedException();
            }
          }
          catch (TargetInvocationException e) { throw e.InnerException; }
          finally { arguments[parameter.Position] = args[2]; args[2] = null; }
        }

        ReconstructInfo.Invoke(this, arguments);
      }
      catch (TargetInvocationException e) { throw e.InnerException; }
      finally { element = (DB.Element) arguments[1]; }
    }

    // Step 4.
    protected override void OnBeforeCommit(DB.Document document, string strTransactionName)
    {
      // Remove extra unused elements
      while (PreviousStructureEnumerator?.MoveNext() ?? false)
      {
        if (PreviousStructureEnumerator.Current is Types.Element elementId && document.Equals(elementId.Document))
        {
          if (document.GetElement(elementId.Id) is DB.Element element)
          {
            try { document.Delete(element.Id); }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { }
          }
        }
      }
    }

    // Step 5.
    protected override void AfterSolveInstance()
    {
      try { base.AfterSolveInstance(); }
      finally { PreviousStructureEnumerator = null; }
    }

    // Step 5.2.A
    public override void OnCommitted(DB.Document document, string strTransactionName)
    {
      // Update previous elements
      PreviousStructure = Params.Output[0].VolatileData.AllData(false).ToArray();
    }

    #region IGH_ElementIdBakeAwareObject
    bool Bake.IGH_ElementIdBakeAwareObject.CanBake(Bake.BakeOptions options) =>
      Params?.Output.OfType<Kernel.IGH_ElementIdParam>().
      Where
      (
        x =>
        x.VolatileData.AllData(true).
        OfType<Types.IGH_ElementId>().
        Where(goo => options.Document.Equals(goo.Document)).
        Any()
      ).
      Any() ?? false;

    bool Bake.IGH_ElementIdBakeAwareObject.Bake(Bake.BakeOptions options, out ICollection<DB.ElementId> ids)
    {
      using (var trans = new DB.Transaction(options.Document, "Bake"))
      {
        if (trans.Start() == DB.TransactionStatus.Started)
        {
          var list = new List<DB.ElementId>();
          var newStructure = (IGH_Goo[]) PreviousStructure.Clone();
          for (int g = 0; g < newStructure.Length; g++)
          {
            if (newStructure[g] is Types.IGH_ElementId id)
            {
              if
              (
                id.Document.Equals(options.Document) &&
                id.Document.GetElement(id.Id) is DB.Element element
              )
              {
                element.Pinned = false;
                list.Add(element.Id);
                newStructure[g] = default;
              }
            }
          }

          if (trans.Commit() == DB.TransactionStatus.Committed)
          {
            ids = list;
            PreviousStructure = newStructure;
            ExpireSolution(false);
            return true;
          }
        }
      }

      ids = default;
      return false;
    }
    #endregion
  }
}

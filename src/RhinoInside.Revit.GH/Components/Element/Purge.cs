using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ElementPurge : TransactionalComponent, IGH_VariableParameterComponent
  {
    public override Guid ComponentGuid => new Guid("05539772-7205-4D58-8093-1715DAF213AF");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    protected override string IconTag => "P";

    protected DBX.TransactionSignal Signal = DBX.TransactionSignal.Effective;

    enum ComponentCommand
    {
      Purge,
      Delete,
    }

    ComponentCommand Command = ComponentCommand.Purge;

    public ElementPurge() : base
    (
      name: "Purge Element",
      nickname: "Purge",
      description: "Purge unused elements from Revit document",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    [Flags]
    protected enum ParamRelevance
    {
      None      = 0,
      Mandatory = 1,
      Default   = 2,
      Binding   = 3
    }

    protected struct ParamDefinition
    {
      public ParamDefinition(IGH_Param param, ParamRelevance relevance)
      {
        Param = param;
        Relevance = relevance;
      }

      public readonly ParamRelevance Relevance;
      public readonly IGH_Param Param;
    }

    static protected void RegisterParams(GH_InputParamManager manager, IEnumerable<ParamDefinition> definitions)
    {
      foreach (var definition in definitions.Where(x => x.Relevance.HasFlag(ParamRelevance.Default)))
        manager.AddParameter(definition.Param);
    }

    static protected void RegisterParams(GH_OutputParamManager manager, IEnumerable<ParamDefinition> definitions)
    {
      foreach (var definition in definitions.Where(x => x.Relevance.HasFlag(ParamRelevance.Default)))
        manager.AddParameter(definition.Param);
    }

    static readonly ParamDefinition[] Inputs =
    {
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.TransactionSignal>()
        {
          Name = "Signal",
          NickName = "S",
          Description = "Transaction Signal",
          Access = GH_ParamAccess.tree,
          WireDisplay = GH_ParamWireDisplay.hidden
        },
        ParamRelevance.None
      ),
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Elements to Purge",
          Access = GH_ParamAccess.list,
          DataMapping = GH_DataMapping.Graft
        },
        ParamRelevance.Binding
      )
    };

    protected override void RegisterInputParams(GH_InputParamManager manager) => RegisterParams(manager, Inputs);

    static readonly ParamDefinition[] Outputs =
    {
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Succeeded",
          NickName = "S",
          Description = "Element is been deleted",
          Access = GH_ParamAccess.item
        },
        ParamRelevance.Binding
      ),
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Deleted",
          NickName = "D",
          Description = "Deleted elements. From a logical point of view, are the children of this Element",
          Access = GH_ParamAccess.list
        },
        ParamRelevance.Binding
      ),
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Modified",
          NickName = "M",
          Description = "Modified elements. Those elements reference Element but not depend on it",
          Access = GH_ParamAccess.list
        },
        ParamRelevance.Binding
      )
    };
    protected override void RegisterOutputParams(GH_OutputParamManager manager) => RegisterParams(manager, Outputs);

    class PurgeUpdater : DB.IUpdater, IDisposable
    {
      public string GetUpdaterName() => "Purge Updater";
      public string GetAdditionalInformation() => "N/A";
      public DB.ChangePriority GetChangePriority() => DB.ChangePriority.Annotations;
      public DB.UpdaterId GetUpdaterId() => UpdaterId;
      public static readonly DB.UpdaterId UpdaterId = new DB.UpdaterId
      (
        Addin.Id,
        new Guid("A50F0406-4E9A-4BE5-85CB-77C608AD8086")
      );

      public ICollection<DB.ElementId> AddedElementIds { get; private set; }
      public ICollection<DB.ElementId> DeletedElementIds { get; private set; }
      public ICollection<DB.ElementId> ModifiedElementIds { get; private set; }

      bool Delete;
      public PurgeUpdater(bool delete)
      {
        Delete = delete;
        DB.UpdaterRegistry.RegisterUpdater(this);

        var filter = new DB.ElementCategoryFilter(DB.BuiltInCategory.INVALID, true);
        DB.UpdaterRegistry.AddTrigger(UpdaterId, filter, DB.Element.GetChangeTypeAny());
        DB.UpdaterRegistry.AddTrigger(UpdaterId, filter, DB.Element.GetChangeTypeElementDeletion());
      }

      void IDisposable.Dispose()
      {
        DB.UpdaterRegistry.RemoveAllTriggers(UpdaterId);
        DB.UpdaterRegistry.UnregisterUpdater(UpdaterId);
      }

      public void Execute(DB.UpdaterData data)
      {
        AddedElementIds = data.GetAddedElementIds();
        DeletedElementIds = data.GetDeletedElementIds();
        ModifiedElementIds = data.GetModifiedElementIds();

        Debug.Assert(AddedElementIds.Count == 0);

        if (!Delete && ModifiedElementIds.Count > 0)
        {
          var message = new DB.FailureMessage(DBX.ExternalFailures.ElementFailures.FailedToPurgeElement);

          message.SetFailingElement(DeletedElementIds.First());
          message.SetAdditionalElements(ModifiedElementIds);

          data.GetDocument().PostFailure(message);
        }
      }
    }

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
      var _Signal_ = Params.IndexOfInputParam("Signal");
      if (_Signal_ >= 0)
      {
        Phase = GH_SolutionPhase.Blank;

        if (Params.Input[_Signal_].DataType == GH_ParamData.@void)
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

      Message = Command == ComponentCommand.Purge ? "Purge" : "Delete";

      var _Signal_ = Params.IndexOfInputParam("Signal");
      if (_Signal_ >= 0)
      {
        var signal = Params.Input[_Signal_];
        Signal = MaxSignal(signal.VolatileData.AllData(false)).GetValueOrDefault();

        if (signal.DataType == GH_ParamData.@void)
          signal.NickName = "Signal";
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

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var elementList = new List<Types.IGH_ElementId>();
      if (!DA.GetDataList("Elements", elementList))
        return;

      var elementGroups = elementList.
                          Where(x => x.IsValid).
                          GroupBy(x => x.Document).
                          ToArray();

      var transactionGroups = new Queue<DB.TransactionGroup>();

      try
      {
        var Deleted = new List<Types.Element>();
        var Modified = new List<Types.Element>();

        foreach (var elementGroup in elementGroups)
        {
          var doc = elementGroup.Key;
          if (Signal >= DBX.TransactionSignal.Effective)
          {
            var group = new DB.TransactionGroup(doc, $"{Command} Element");
            transactionGroups.Enqueue(group);
            group.Start();
          }

          using (var updater = new PurgeUpdater(Command == ComponentCommand.Delete))
          {
            using (var transaction = NewTransaction(doc, $"{Command} Element"))
            {
              transaction.Start();

              if (Signal == DBX.TransactionSignal.Simulated)
                doc.PostFailure(new DB.FailureMessage(DBX.ExternalFailures.TransactionFailures.SimulatedTransaction));

              var elementIdsSet = new HashSet<DB.ElementId>(elementGroup.Select(x => x.Id));
              var DeletedElementIds = elementGroup.Key.Delete(elementIdsSet);

              if (CommitTransaction(doc, transaction) == DB.TransactionStatus.Committed)
              {
                deletedElements += DeletedElementIds.Count;
              }
              else
              {
                if (Modified.Capacity < Modified.Count + updater.ModifiedElementIds.Count)
                  Modified.Capacity = Modified.Count + updater.ModifiedElementIds.Count;

                Modified.AddRange(updater.ModifiedElementIds.Select(x => Types.Element.FromElementId(doc, x)));

                if (Deleted.Capacity < Deleted.Count + updater.DeletedElementIds.Count)
                  Deleted.Capacity = Deleted.Count + updater.DeletedElementIds.Count;

                Deleted.AddRange(updater.DeletedElementIds.Select(x => Types.Element.FromElementId(doc, x)));
              }
            }
          }
        }

        if (Modified.Count == 0 || Command == ComponentCommand.Delete)
        {
          while (transactionGroups.Count > 0)
          using (var group = transactionGroups.Dequeue())
          {
            group.Assimilate();
          }

          DA.SetData("Succeeded", true);
        }
        else
        {
          DA.SetData("Succeeded", false);
        }

        DA.SetDataList("Deleted", Deleted);
        DA.SetDataList("Modified", Modified);
      }
      catch (Autodesk.Revit.Exceptions.ArgumentException)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or many of the elements cannot be deleted or are invalid.");
        DA.SetData("Succeeded", false);
      }

      foreach (var group in transactionGroups.Cast<DB.TransactionGroup>().Reverse())
      using (group)
      {
        group.RollBack();
      }
    }

    int deletedElements;
    protected override void BeforeSolveInstance()
    {
      base.BeforeSolveInstance();

      deletedElements = 0;
    }

    protected override void AfterSolveInstance()
    {
      if (RunCount > 0)
      {
        if (deletedElements == 0)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No elements were deleted");
        else
          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{deletedElements} elements were deleted.");
      }

      base.AfterSolveInstance();
    }

    #region UI
    new class Attributes : GH_ComponentAttributes
    {
      public Attributes(TransactionalComponent owner) : base(owner) { }

      protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
      {
        if (channel == GH_CanvasChannel.Objects && Owner is ElementPurge component)
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
            switch(component.Signal)
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
          // TODO : convert from short to long names
          CanvasFullNames = Grasshopper.CentralSettings.CanvasFullNames;
        }

        base.ExpireLayout();
      }
    }

    public override void CreateAttributes() => m_attributes = new Attributes(this);

    public bool CanInsertParameter(GH_ParameterSide side, int index)
    {
      if (side == GH_ParameterSide.Input)
      {
        if (index == 0)
        {
          var _Signal_ = Params.IndexOfInputParam("Signal");
          return _Signal_ < 0;
        }
      }

      return false;
    }

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      if (side == GH_ParameterSide.Input)
      {
        if (Params.IndexOfInputParam("Signal") < 0)
        {
          var signal = new Parameters.Param_Enum<Types.TransactionSignal>()
          {
            Name = "Signal",
            NickName = Grasshopper.CentralSettings.CanvasFullNames ? "Signal" : "S",
            Description = "Transaction signal",
            Access = GH_ParamAccess.tree,
            WireDisplay = GH_ParamWireDisplay.hidden
          };

          return signal;
        }
      }

      return default;
    }

    public bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      if (side == GH_ParameterSide.Input)
      {
        var param = Params.Input[index];
        var definition = Inputs.Where(x => x.Param.Name == param.Name).FirstOrDefault();

        if (Params.IndexOfInputParam("Signal") == index)
          return true;
      }

      return false;
    }

    public bool DestroyParameter(GH_ParameterSide side, int index) => CanRemoveParameter(side, index);

    public void VariableParameterMaintenance() { }

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var delete = Menu_AppendItem(menu, "Delete", Menu_CommandClicked, true, Command == ComponentCommand.Delete);
      delete.Tag = ComponentCommand.Delete;

      var purge = Menu_AppendItem(menu, "Purge", Menu_CommandClicked, true, Command == ComponentCommand.Purge);
      purge.Tag = ComponentCommand.Purge;
    }

    private void Menu_CommandClicked(object sender, EventArgs e)
    {
      if (sender is ToolStripMenuItem item)
      {
        if (item.Tag is ComponentCommand command)
        {
          RecordUndoEvent($"Set: {command}");
          Command = command;

          ExpireSolution(true);
        }
      }
    }
    #endregion

    #region IO
    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      int command = (int) default(ComponentCommand);
      reader.TryGetInt32("Command", ref command);
      Command = (ComponentCommand) command;

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (Command != default)
        writer.SetInt32("Command", (int) Command);

      return true;
    }
    #endregion

    //public static bool GetPurgableElements(DB.Document document, out ICollection<DB.ElementId> ids)
    //{
    //  var PurgeId = Guid.Parse("e8c63650-70b7-435a-9010-ec97660c1bda");

    //  try
    //  {
    //    using (var adviser = DB.PerformanceAdviser.GetPerformanceAdviser())
    //    {
    //      var rules = adviser.GetAllRuleIds().Where(x => x.Guid == PurgeId).ToList();
    //      if (rules.Count > 0)
    //      {
    //        var results = adviser.ExecuteRules(document, rules);
    //        if (results.Count > 0)
    //        {
    //          ids = results[0].GetFailingElements();
    //          return true;
    //        }
    //      }
    //    }
    //  }
    //  catch (Autodesk.Revit.Exceptions.InternalException) { }

    //  ids = default;
    //  return false;
    //}

    //protected override void SolveInstance(IGH_DataAccess DA)
    //{
    //  if (!DA.GetDataTree<Types.Element>("Elements", out var elementsTree))
    //    return;

    //  var elementsToDelete = Parameters.Element.
    //                         ToElementIds(elementsTree).
    //                         GroupBy(x => x.Document).
    //                         ToArray();

    //  foreach (var group in elementsToDelete)
    //  {
    //    DB.BuiltInFailures.ElementFailures.FailedToRemoveElement;
    //    BeginTransaction(group.Key);

    //    if (!GetPurgableElements(group.Key, out var purgableElements))
    //      continue;

    //    try
    //    {
    //      var elements = purgableElements.Intersect(group.Select(x => x.Id)).ToArray();
    //      var deletedElements = group.Key.Delete(elements);

    //      if (deletedElements.Count == 0)
    //        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No elements were deleted");
    //      else
    //        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{elementsToDelete.Length} elements and {deletedElements.Count - elementsToDelete.Length} dependant elements were deleted.");
    //    }
    //    catch (Autodesk.Revit.Exceptions.ArgumentException)
    //    {
    //      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or more of the elements cannot be deleted.");
    //    }
    //  }
    //}

    //protected override void SolveInstance(IGH_DataAccess DA)
    //{
    //  var element = default(Types.IGH_ElementId);
    //  if (!DA.GetData("Element", ref element))
    //    return;

    //  StartTransaction(element.Document);

    //  try
    //  {
    //    using (var sub = new DB.SubTransaction(element.Document))
    //    {
    //      sub.Start();

    //      ICollection<DB.ElementId> added = default, modified = default, deleted = default;
    //      var DocumentChanged = default(EventHandler<DB.Events.DocumentChangedEventArgs>);
    //      element.Document.Application.DocumentChanged += DocumentChanged = (sender, args) =>
    //      {
    //        added    = args.GetAddedElementIds();
    //        modified = args.GetModifiedElementIds();
    //        deleted  = args.GetDeletedElementIds();
    //      };
    //      var deletedElements = element.Document.Delete(element.Id);
    //      element.Document.Application.DocumentChanged -= DocumentChanged;

    //      if (deletedElements.Count > 1 || added.Count > 0 || modified.Count > 1 || deleted.Count > 1)
    //      {
    //        sub.RollBack();
    //        deletedElements = new DB.ElementId[0];
    //      }
    //      else sub.Commit();

    //      if (deletedElements.Count == 0)
    //        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No elements were deleted");
    //      else
    //        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{element} is referenced by {deletedElements.Count - 1} and will not be deleted.");
    //    }
    //  }
    //  catch (Autodesk.Revit.Exceptions.ArgumentException)
    //  {
    //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or more of the elements cannot be deleted.");
    //  }
    //}
  }
}

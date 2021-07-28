using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ElementDelete : TransactionalComponent
  {
    public override Guid ComponentGuid => new Guid("3FFC2CB2-48FF-4151-B5CB-511C964B487D");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    protected override string IconTag => "X";

    public ElementDelete() : base
    (
      name: "Delete Element",
      nickname: "Delete",
      description: "Deletes elements from Revit document",
      category: "Revit",
      subCategory: "Element"
    )
    {}

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Elements to Delete",
          Access = GH_ParamAccess.list
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Succeeded",
          NickName = "S",
          Description = "Element delete succeeded",
        }
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
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Modified",
          NickName = "M",
          Description = "Modified elements. Those elements reference Element but do not strictly depend on it",
          Access = GH_ParamAccess.list
        },
        ParamRelevance.Occasional
      )
    };

    bool Simulated;
    int deletedElements;

    /// <summary>
    /// Updater to collect changes on the Delete operation
    /// </summary>
    class Updater : DB.IUpdater, IDisposable
    {
      public string GetUpdaterName() => "Delete Updater";
      public string GetAdditionalInformation() => "N/A";
      public DB.ChangePriority GetChangePriority() => DB.ChangePriority.Annotations;
      public DB.UpdaterId GetUpdaterId() => UpdaterId;
      public static readonly DB.UpdaterId UpdaterId = new DB.UpdaterId
      (
        AddIn.Id,
        new Guid("9536C7C9-C58B-4D48-9103-5C8EBAA6F6C8")
      );

      public ICollection<DB.ElementId> AddedElementIds { get; private set; }
      public ICollection<DB.ElementId> DeletedElementIds { get; private set; }
      public ICollection<DB.ElementId> ModifiedElementIds { get; private set; }

      public Updater()
      {
        DB.UpdaterRegistry.RegisterUpdater(this, isOptional: true);

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
      }
    }

    int Delete
    (
      DB.Document document,
      ICollection<DB.ElementId> elementIds,
      List<Types.Element> deleted,
      List<Types.Element> modified
    )
    {
      var result = 0;
      if (elementIds.Count > 0)
      {
        using (var updater = deleted is object && modified is object ? new Updater() : default)
        {
          using (var transaction = NewTransaction(document))
          {
            transaction.Start();

            var DeletedElementIds = document.Delete(elementIds);

            if (CommitTransaction(document, transaction) == DB.TransactionStatus.Committed)
            {
              result = DeletedElementIds.Count;

              if (updater?.DeletedElementIds is object)
              {
                deleted.AddRange
                (
                  updater.DeletedElementIds.Select(x => new Types.Element(document, x)),
                  updater.DeletedElementIds.Count
                );
              }
            }
            else
            {
              result = -1;

              if (updater?.DeletedElementIds is object)
              {
                deleted.AddRange
                (
                  updater.DeletedElementIds.Select(x => Types.Element.FromElementId(document, x)),
                  updater.DeletedElementIds.Count
                );
              }
            }

            if (updater?.ModifiedElementIds is object)
            {
              modified.AddRange
              (
                updater.ModifiedElementIds.Select(x => Types.Element.FromElementId(document, x)),
                updater.ModifiedElementIds.Count
              );
            }
          }
        }
      }

      return result;
    }

    protected override void BeforeSolveInstance()
    {
      Message = string.Empty;
      base.BeforeSolveInstance();

      deletedElements = 0;
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var elementList = new List<Types.IGH_Element>();
      if (!DA.GetDataList("Elements", elementList))
        return;

      try
      {
        var elementGroups = elementList.
                            GroupBy(x => x.Document).
                            ToArray();

        if (elementGroups.Length > 0)
        {
          var transactionGroups = new Queue<DB.TransactionGroup>();

          try
          {
            var _Deleted_ = Params.IndexOfOutputParam("Deleted");
            var _Modified_ = Params.IndexOfOutputParam("Modified");

            var Succeeded = true;
            var Deleted  = _Deleted_  < 0 ? default : new List<Types.Element>();
            var Modified = _Modified_ < 0 ? default : new List<Types.Element>();

            foreach (var elementGroup in elementGroups)
            {
              var doc = elementGroup.Key;

              // Start a transaction Group to be able to rollback in case changes in different documents fails.
              {
                var group = new DB.TransactionGroup(doc, Name);
                if (group.Start() != DB.TransactionStatus.Started)
                {
                  group.Dispose();
                  continue;
                }
                transactionGroups.Enqueue(group);
              }

              var elements = new HashSet<DB.ElementId>(elementGroup.Select(x => x.Id));

              var result = Delete(doc, elements, Deleted, Modified);
              if (result < 0)
                Succeeded = false;
              else
                deletedElements += result;
            }

            if (Succeeded && !Simulated)
            {
              // Transactions in all documents succeded, it's fine to assimilate all changes as one.
              while (transactionGroups.Count > 0)
              {
                using (var group = transactionGroups.Dequeue())
                {
                  group.Assimilate();
                }
              }
            }
            else deletedElements = 0;

            DA.SetData("Succeeded", Succeeded);

            if(_Deleted_ >= 0)
              DA.SetDataList(_Deleted_, Deleted);

            if(_Modified_ >= 0)
              DA.SetDataList(_Modified_, Modified);
          }
          catch (Autodesk.Revit.Exceptions.ArgumentException)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or many of the elements cannot be deleted or are invalid.");
            DA.SetData("Succeeded", false);
          }
          finally
          {
            // In case we still have transaction groups here something bad happened
            foreach (var group in transactionGroups.Cast<DB.TransactionGroup>().Reverse())
            {
              using (group)
              {
                group.RollBack();
              }
            }
            transactionGroups.Clear();
          }
        }
      }
      catch (NullReferenceException)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or many of the elements are Null.");
      }
    }

    protected override void AfterSolveInstance()
    {
      if (RunCount > 0)
      {
        if (Simulated && Message == string.Empty)
          Message = "Simulated";

        if (Simulated)
        {
          Status = DB.TransactionStatus.RolledBack;
        }
        else
        {
          if (deletedElements == 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No elements were deleted");
          else
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{deletedElements} elements were deleted.");
        }
      }

      base.AfterSolveInstance();
    }

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

#if DEBUG
      // TODO : Keep thinking on Simulated Transactions feature
      Menu_AppendItem(menu, "Simulated", Menu_SimulatedClicked, true, Simulated);
#endif
    }

    private void Menu_SimulatedClicked(object sender, EventArgs e)
    {
      if (sender is ToolStripMenuItem item)
      {
        RecordUndoEvent($"Set: Simulated");
        Simulated = !Simulated;

        Message = Simulated ? "Simulated" : string.Empty;

        ClearData();
        ExpireDownStreamObjects();
        ExpireSolution(true);
      }
    }
    #endregion

    #region IO
    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      Simulated = default;
      reader.TryGetBoolean("Simulated", ref Simulated);

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (Simulated != default)
        writer.SetBoolean("Simulated", Simulated);

      return true;
    }
    #endregion
  }
}

namespace RhinoInside.Revit.GH.Components.Obsolete
{
  [Obsolete("Obsolete since 2020-05-21")]
  public class ElementDelete : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("213C1F14-A827-40E2-957E-BA079ECCE700");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.hidden;
    protected override string IconTag => "X";

    public ElementDelete()
    : base("Delete Element", "Delete", "Deletes elements from Revit document", "Revit", "Element")
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Elements to Delete",
          Access = GH_ParamAccess.tree
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs = { };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!DA.GetDataTree<Types.Element>("Elements", out var elements))
        return;

      var elementsToDelete = elements.AllData(true).
                             Cast<Types.IGH_Element>().
                             Where(x => x.IsValid).
                             GroupBy(x => x.Document).
                             ToList();

      var options = new External.DB.TransactionHandlingOptions
      {
        FailuresPreprocessor = new TransactionalComponentFailuresPreprocessor(this)
      };

      using (var chain = new External.DB.TransactionChain(options, Name))
      {
        foreach (var group in elementsToDelete)
        {
          chain.Start(group.Key);

          try
          {
            var deletedElements = group.Key.Delete(group.Select(x => x.Id).ToArray());

            if (deletedElements.Count == 0)
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No elements were deleted");
            else
              AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{elementsToDelete.Count} elements and {deletedElements.Count - elementsToDelete.Count} dependant elements were deleted.");
          }
          catch (Autodesk.Revit.Exceptions.ArgumentException)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or more of the elements cannot be deleted.");
          }
        }

        chain.Commit();
      }
    }
  }
}

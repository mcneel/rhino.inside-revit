using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ElementPurge : TransactionalComponent
  {
    public override Guid ComponentGuid => new Guid("05539772-7205-4D58-8093-1715DAF213AF");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    protected override string IconTag => "P";

    private bool Simulated;

    public ElementPurge() : base
    (
      name: "Purge Element",
      nickname: "Purge",
      description: "Purge unused elements from Revit document",
      category: "Revit",
      subCategory: "Element"
    )
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
          Description = "Elements to Purge",
          Access = GH_ParamAccess.list,
          DataMapping = GH_DataMapping.Graft
        },
        ParamVisibility.Binding
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
          Description = "Element purge succeeded",
          Access = GH_ParamAccess.item
        },
        ParamVisibility.Binding
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
        ParamVisibility.Binding
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
        ParamVisibility.Binding
      )
    };

    /// <summary>
    /// Updater to collect changes on the Purge operation
    /// </summary>
    class Updater : DB.IUpdater, IDisposable
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

      readonly ICollection<DB.ElementId> ElementIds;
      public Updater(ICollection<DB.ElementId> elementIds)
      {
        ElementIds = elementIds;

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
        if (AddedElementIds is null)
          AddedElementIds = data.GetAddedElementIds();
        else foreach (var item in data.GetAddedElementIds())
          AddedElementIds.Add(item);

        if(DeletedElementIds is null)
          DeletedElementIds = data.GetDeletedElementIds();
        else foreach (var item in data.GetDeletedElementIds())
          DeletedElementIds.Add(item);

        if(ModifiedElementIds is null)
          ModifiedElementIds = data.GetModifiedElementIds();
        else foreach (var item in data.GetModifiedElementIds())
          ModifiedElementIds.Add(item);

        Debug.Assert(AddedElementIds.Count == 0);

        if (ModifiedElementIds.Count > 0)
        {
          var message = new DB.FailureMessage(DBX.ExternalFailures.ElementFailures.FailedToPurgeElement);

          message.SetFailingElements(ElementIds);
          message.SetAdditionalElements(data.GetModifiedElementIds());

          data.GetDocument().PostFailure(message);
        }
      }
    }

    int deletedElements;
    protected override void BeforeSolveInstance()
    {
      Message = string.Empty;
      base.BeforeSolveInstance();

      deletedElements = 0;
    }

    static void ClassifyElementIds(IGrouping<DB.Document, Types.IGH_Element> group, out HashSet<DB.ElementId> elements, out HashSet<DB.ElementId> types)
    {
      elements = new HashSet<DB.ElementId>();
      types = new HashSet<DB.ElementId>();

      using (var typesFilter = new DB.ElementIsElementTypeFilter())
      {
        foreach (var item in group)
        {
          var id = item.Id;
          if (typesFilter.PassesFilter(group.Key, id))
            types.Add(id);
          else
            elements.Add(id);
        }
      }
    }

    bool Purge
    (
      DB.Document document,
      ICollection<DB.ElementId> elementIds,
      List<Types.Element> deleted,
      List<Types.Element> modified
    )
    {
      bool result = true;
      if (elementIds.Count > 0)
      {
        using (var updater = new Updater(elementIds))
        {
          using (var transaction = NewTransaction(document))
          {
            transaction.Start();

            var DeletedElementIds = document.Delete(elementIds);

            if (CommitTransaction(document, transaction) == DB.TransactionStatus.Committed)
            {
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
              result = false;

              if (updater.DeletedElementIds is object)
              {
                deleted.AddRange
                (
                  updater.DeletedElementIds.Select(x => Types.Element.FromElementId(document, x)),
                  updater.DeletedElementIds.Count
                );
              }
            }

            if (updater.ModifiedElementIds is object)
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
            var Succeeded = true;
            var Deleted = new List<Types.Element>();
            var Modified = new List<Types.Element>();

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

              ClassifyElementIds(elementGroup, out var elements, out var types);

              // Step 1. Purge non ElementTypes
              Succeeded &= Purge(doc, elements, Deleted, Modified);

              // Step 2. Check if ElementTypes to be purged are still in use in the model by any Instance
              if (types.Count > 0)
              {
                var typesInUse = new List<DB.ElementId>(types.Count);

                // Remove purgable ElementTypes from types
                if (doc.GetPurgableElementTypes(out var purgableTypes))
                {
                  foreach (var type in types)
                  {
                    if(!purgableTypes.Contains(type))
                      typesInUse.Add(type);
                  }
                }

                // Post a FailureMessage in case we have non purgable ElementTypes on types
                if (typesInUse.Count > 0)
                {
                  using (var transaction = NewTransaction(doc))
                  {
                    transaction.Start();

                    using (var message = new DB.FailureMessage(DBX.ExternalFailures.ElementFailures.FailedToPurgeElement))
                    {
                      message.SetFailingElements(typesInUse);
                      doc.PostFailure(message);
                    }

                    CommitTransaction(doc, transaction);
                  }

                  Succeeded &= false;
                }
              }

              // Step 3. Purge ElementTypes
              // Types include typesInUse in order to get information about Deleted and Modified elements.
              Succeeded &= Purge(doc, types, Deleted, Modified);
            }

            if (Succeeded && !Simulated)
            {
              deletedElements += Deleted.Count;

              // Transactions in all documents succeded, it's fine to assimilate all changes as one.
              while (transactionGroups.Count > 0)
              {
                using (var group = transactionGroups.Dequeue())
                {
                  group.Assimilate();
                }
              }
            }

            DA.SetData("Succeeded", Succeeded);
            DA.SetDataList("Deleted", Deleted);
            DA.SetDataList("Modified", Modified);
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

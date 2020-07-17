using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.InteropExtension;
using Autodesk.Revit.UI;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public abstract class ElementIdParam<T, R> :
  PersistentParam<Types.IGH_ElementId>,
  Kernel.IGH_ElementIdParam
  where T : class, Types.IGH_ElementId
  {
    public override sealed string TypeName => "Revit " + Name;
    protected ElementIdParam(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory)
    { }
    protected override Types.IGH_ElementId PreferredCast(object data) => data is R ? Types.Element.FromValue(data) : null;
    protected override Types.IGH_ElementId InstantiateT() => new Types.Element();

    protected T Current
    {
      get
      {
        var current = (SourceCount == 0 && PersistentDataCount == 1) ? PersistentData.get_FirstItem(true) : default;
        return (current?.LoadElement() == true ? current : default) as T;
      }
    }

    internal static IEnumerable<Types.IGH_ElementId> ToElementIds(IGH_Structure data) =>
      data.AllData(true).
      OfType<Types.IGH_ElementId>().
      Where(x => x.IsValid);

    [Flags]
    public enum DataGrouping
    {
      None = 0,
      Document = 1,
      Workset = 2,
      DesignOption = 4,
      Category = 8,
    };

    public DataGrouping Grouping { get; set; } = DataGrouping.None;

    public override sealed bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      int grouping = (int) DataGrouping.None;
      reader.TryGetInt32("Grouping", ref grouping);
      Grouping = (DataGrouping) grouping;

      return true;
    }
    public override sealed bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (Grouping != DataGrouping.None)
        writer.SetInt32("Grouping", (int) Grouping);

      return true;
    }

    public override void ClearData()
    {
      base.ClearData();

      if (PersistentDataCount == 0)
        return;

      foreach (var goo in PersistentData.OfType<T>())
        goo?.UnloadElement();
    }

    protected override void LoadVolatileData()
    {
      if (SourceCount == 0)
      {
        foreach (var branch in m_data.Branches)
        {
          for (int i = 0; i < branch.Count; i++)
          {
            var item = branch[i];
            if (item?.IsReferencedElement ?? false)
            {
              if (!item.LoadElement())
              {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"A referenced {item.TypeName} could not be found in the Revit document.");
                branch[i] = null;
              }
            }
          }
        }
      }
    }

    protected override void ProcessVolatileData()
    {
      if (Grouping != DataGrouping.None)
      {
        if (Kind == GH_ParamKind.floating)
        {
          if ((Grouping & DataGrouping.Document) != 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Grouped by Document");

          if ((Grouping & DataGrouping.Workset) != 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Grouped by Workset");

          if ((Grouping & DataGrouping.DesignOption) != 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Grouped by Design Option");

          if ((Grouping & DataGrouping.Category) != 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Grouped by Category");
        }

        var data = new GH_Structure<Types.IGH_ElementId>();
        var pathCount = m_data.PathCount;
        for (int p = 0; p < pathCount; ++p)
        {
          var path = m_data.Paths[p];
          var branch = m_data.get_Branch(path);
          foreach (var item in branch)
          {
            if (item is Types.IGH_ElementId value)
            {
              var group = path;

              if ((Grouping & DataGrouping.Document) != 0)
              {
                var docId = DocumentExtension.DocumentSessionId(value.DocumentGUID);
                group = group.AppendElement(docId);
              }

              if (Grouping > DataGrouping.Document)
              {
                var element = value.Document?.GetElement(value.Id);

                if ((Grouping & DataGrouping.Workset) != 0)
                {
                  var catId = element?.WorksetId?.IntegerValue ?? 0;
                  group = group.AppendElement(catId);
                }

                if ((Grouping & DataGrouping.DesignOption) != 0)
                {
                  var catId = element?.DesignOption?.Id.IntegerValue ?? 0;
                  group = group.AppendElement(catId);
                }

                if ((Grouping & DataGrouping.Category) != 0)
                {
                  var catId = element?.Category?.Id.IntegerValue ?? 0;
                  group = group.AppendElement(catId);
                }
              }

              data.Append((T) value, group);
            }
            else data.Append(null, path.AppendElement(int.MinValue));
          }
        }

        m_data = data;
      }

      base.ProcessVolatileData();
    }

    #region UI
    protected override void Menu_AppendPreProcessParameter(ToolStripDropDown menu)
    {
      base.Menu_AppendPreProcessParameter(menu);

      var Group = Menu_AppendItem(menu, "Group by") as ToolStripMenuItem;

      Group.Checked = Grouping != DataGrouping.None;
      Menu_AppendItem(Group.DropDown, "Document",      (s, a) => Menu_GroupBy(DataGrouping.Document),      true, (Grouping & DataGrouping.Document) != 0);
      Menu_AppendItem(Group.DropDown, "Workset",       (s, a) => Menu_GroupBy(DataGrouping.Workset),       true, (Grouping & DataGrouping.Workset) != 0);
      Menu_AppendItem(Group.DropDown, "Design Option", (s, a) => Menu_GroupBy(DataGrouping.DesignOption),  true, (Grouping & DataGrouping.DesignOption) != 0);
      Menu_AppendItem(Group.DropDown, "Category",      (s, a) => Menu_GroupBy(DataGrouping.Category),      true, (Grouping & DataGrouping.Category) != 0);
    }

    private void Menu_GroupBy(DataGrouping value)
    {
      RecordUndoEvent("Set: Grouping");

      if ((Grouping & value) != 0)
        Grouping &= ~value;
      else
        Grouping |= value;

      OnObjectChanged(GH_ObjectEventType.Options);

      if (Kind == GH_ParamKind.output)
        ExpireOwner();

      ExpireSolution(true);
    }

    protected override void PrepareForPrompt() { }
    protected override void RecoverFromPrompt() { }
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      Menu_AppendSeparator(menu);
      Menu_AppendActions(menu);
    }

    public virtual void Menu_AppendActions(ToolStripDropDown menu)
    {
      var doc = Revit.ActiveUIDocument?.Document;

      if (Kind == GH_ParamKind.output && Attributes.GetTopLevel.DocObject is Components.ReconstructElementComponent)
      {
        var pinned = ToElementIds(VolatileData).
                     Where(x => x.Document.Equals(doc)).
                     Select(x => x.Document.GetElement(x.Id)).
                     Where(x => x?.Pinned == true).Any();

        if (pinned)
          Menu_AppendItem(menu, $"Unpin {GH_Convert.ToPlural(TypeName)}", Menu_UnpinElements, DataType != GH_ParamData.remote, false);

        var unpinned = ToElementIds(VolatileData).
                     Where(x => x.Document.Equals(doc)).
                     Select(x => x.Document.GetElement(x.Id)).
                     Where(x => x?.Pinned == false).Any();

        if (unpinned)
          Menu_AppendItem(menu, $"Pin {GH_Convert.ToPlural(TypeName)}", Menu_PinElements, DataType != GH_ParamData.remote, false);
      }

      bool delete = ToElementIds(VolatileData).Where(x => x.Document.Equals(doc)).Any();

      Menu_AppendItem(menu, $"Delete {GH_Convert.ToPlural(TypeName)}", Menu_DeleteElements, delete, false);
    }

    void Menu_PinElements(object sender, EventArgs args)
    {
      var doc = Revit.ActiveUIDocument?.Document;
      var elements = ToElementIds(VolatileData).
                       Where(x => x.Document.Equals(doc)).
                       Select(x => x.Document.GetElement(x.Id)).
                       Where(x => x.Pinned == false);

      if (elements.Any())
      {
        try
        {
          using (var transaction = new DB.Transaction(doc, "Pin elements"))
          {
            transaction.Start();

            foreach (var element in elements)
              element.Pinned = true;

            transaction.Commit();
          }
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException)
        {
          TaskDialog.Show("Pin elements", $"One or more of the {TypeName} cannot be pinned.");
        }
      }
    }

    void Menu_UnpinElements(object sender, EventArgs args)
    {
      var doc = Revit.ActiveUIDocument?.Document;
      var elements = ToElementIds(VolatileData).
                       Where(x => x.Document.Equals(doc)).
                       Select(x => x.Document.GetElement(x.Id)).
                       Where(x => x.Pinned == true);

      if (elements.Any())
      {
        try
        {
          using (var transaction = new DB.Transaction(doc, "Unpin elements"))
          {
            transaction.Start();

            foreach (var element in elements)
              element.Pinned = false;

            transaction.Commit();
          }
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException)
        {
          TaskDialog.Show("Unpin elements", $"One or more of the {TypeName} cannot be unpinned.");
        }
      }
    }

    void Menu_DeleteElements(object sender, EventArgs args)
    {
      var doc = Revit.ActiveUIDocument?.Document;
      var elementIds = ToElementIds(VolatileData).
                       Where(x => x.Document.Equals(doc)).
                       Select(x => x.Id);

      if (elementIds.Any())
      {
        using (new External.EditScope())
        {
          using
          (
            var taskDialog = new TaskDialog(MethodBase.GetCurrentMethod().DeclaringType.FullName)
            {
              MainIcon = External.UI.TaskDialogIcons.IconWarning,
              TitleAutoPrefix = false,
              Title = "Delete Elements",
              MainInstruction = "Are you sure you want to delete those elements?",
              CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
              DefaultButton = TaskDialogResult.Yes,
              AllowCancellation = true,
#if REVIT_2020
              EnableMarqueeProgressBar = true
#endif
            }
          )
          {
            taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Show elements");
            taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Manage element collection");

            var result = TaskDialogResult.None;
            bool highlight = false;
            do
            {
              var elements = elementIds.ToArray();
              taskDialog.ExpandedContent = $"{elements.Length} elements and its depending elements will be deleted.";

              if (highlight)
                Revit.ActiveUIDocument?.Selection.SetElementIds(elements);

              switch (result = taskDialog.Show())
              {
                case TaskDialogResult.CommandLink1:
                  Revit.ActiveUIDocument?.ShowElements(elements);
                  highlight = true;
                  break;

                case TaskDialogResult.CommandLink2:
                  using (var dataManager = new GH_PersistentDataEditor())
                  {
                    var elementCollection = new GH_Structure<IGH_Goo>();
                    elementCollection.AppendRange(elementIds.Select(x => Types.Element.FromElementId(doc, x)));
                    dataManager.SetData(elementCollection, new Types.Element());

                    GH_WindowsFormUtil.CenterFormOnCursor(dataManager, true);
                    if (dataManager.ShowDialog(Revit.MainWindowHandle) == System.Windows.Forms.DialogResult.OK)
                      elementIds = dataManager.GetData<IGH_Goo>().AllData(true).OfType<Types.Element>().Select(x => x.Value);
                  }
                  break;

                case TaskDialogResult.Yes:
                  try
                  {
                    using (var transaction = new DB.Transaction(doc, "Delete elements"))
                    {
                      transaction.Start();
                      doc.Delete(elements);
                      transaction.Commit();
                    }

                    ClearData();
                    ExpireDownStreamObjects();
                    OnPingDocument().NewSolution(false);
                  }
                  catch (Autodesk.Revit.Exceptions.ArgumentException)
                  {
                    TaskDialog.Show("Delete elements", $"One or more of the {TypeName} cannot be deleted.");
                  }
                  break;
              }
            }
            while (result == TaskDialogResult.CommandLink1 || result == TaskDialogResult.CommandLink2);
          }
        }
      }
    }

    protected sealed override GH_GetterResult Prompt_Plural(ref List<Types.IGH_ElementId> values)
    {
      var list = new List<T>();
      var result = Prompt_Plural(ref list);
      values.AddRange(list);

      return result;
    }

    protected sealed override GH_GetterResult Prompt_Singular(ref Types.IGH_ElementId value)
    {
      T item = default;
      var result =  Prompt_Singular(ref item);
      value = item;

      return result;
    }

    protected virtual GH_GetterResult Prompt_Plural(ref List<T> values) => GH_GetterResult.cancel;
    protected virtual GH_GetterResult Prompt_Singular(ref T value) => GH_GetterResult.cancel;

    protected override bool Prompt_ManageCollection(GH_Structure<Types.IGH_ElementId> values)
    {
      foreach (var item in values.AllData(true))
      {
        if (item.IsValid)
          continue;

        if (item is Types.IGH_ElementId elementId)
        {
          if (elementId.IsReferencedElement)
            elementId.LoadElement();
        }
      }

      return base.Prompt_ManageCollection(values);
    }
    #endregion

    #region IGH_ElementIdParam
    bool Kernel.IGH_ElementIdParam.NeedsToBeExpired
    (
      DB.Document doc,
      ICollection<DB.ElementId> added,
      ICollection<DB.ElementId> deleted,
      ICollection<DB.ElementId> modified
    )
    {
      if (DataType == GH_ParamData.remote)
        return false;

      foreach (var data in VolatileData.AllData(true).OfType<Types.IGH_ElementId>())
      {
        if (!data.IsElementLoaded)
          continue;

        if (modified.Contains(data.Id))
          return true;

        if (deleted.Contains(data.Id))
          return true;
      }

      return false;
    }
    #endregion
  }

  public abstract class ElementIdWithoutPreviewParam<T, R> : ElementIdParam<T, R>
    where T : class, Types.IGH_ElementId
  {
    protected ElementIdWithoutPreviewParam(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory)
    { }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Plural(ref List<T> values) => GH_GetterResult.cancel;
    protected override GH_GetterResult Prompt_Singular(ref T value) => GH_GetterResult.cancel;
  }

  public abstract class ElementIdWithPreviewParam<X, R> : ElementIdParam<X, R>, IGH_PreviewObject
  where X : class, Types.IGH_ElementId, IGH_PreviewData
  {
    protected ElementIdWithPreviewParam(string name, string nickname, string description, string category, string subcategory) :
    base(name, nickname, description, category, subcategory)
    { }

    #region IGH_PreviewObject
    bool IGH_PreviewObject.Hidden { get; set; }
    bool IGH_PreviewObject.IsPreviewCapable => !VolatileData.IsEmpty;
    BoundingBox IGH_PreviewObject.ClippingBox => Preview_ComputeClippingBox();
    void IGH_PreviewObject.DrawViewportMeshes(IGH_PreviewArgs args) => Preview_DrawMeshes(args);
    void IGH_PreviewObject.DrawViewportWires(IGH_PreviewArgs args) => Preview_DrawWires(args);
    #endregion
  }
}

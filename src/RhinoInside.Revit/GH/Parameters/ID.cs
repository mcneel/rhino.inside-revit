using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Autodesk.Revit.UI;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public abstract class GH_PersistentParam<T> : Grasshopper.Kernel.GH_PersistentParam<T>
    where T : class, IGH_Goo
  {
    protected override sealed Bitmap Icon => ((Bitmap) Properties.Resources.ResourceManager.GetObject(typeof(T).Name)) ??
                                             ImageBuilder.BuildIcon(IconTag);

    protected virtual string IconTag => typeof(T).Name.Substring(0, 1);

    protected GH_PersistentParam(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory)
    { }
  }

  public interface IGH_ElementIdParam : IGH_Param
  {
    bool NeedsToBeExpired
    (
      DB.Document doc,
      ICollection<DB.ElementId> added,
      ICollection<DB.ElementId> deleted,
      ICollection<DB.ElementId> modified
    );
  }

  public abstract class ElementIdParam<T, R> : GH_PersistentParam<T>, IGH_ElementIdParam
    where T : class, Types.IGH_ElementId
  {
    public override sealed string TypeName => "Revit " + Name;
    protected ElementIdParam(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory)
    { }

    internal static IEnumerable<Types.IGH_ElementId> ToElementIds(IGH_Structure data) =>
      data.AllData(true).
      OfType<Types.IGH_ElementId>().
      Where(x => x.IsValid);

    public override void ClearData()
    {
      base.ClearData();

      if (PersistentDataCount == 0)
        return;

      foreach (var goo in PersistentData.OfType<T>())
        goo?.UnloadElement();
    }

    protected override void OnVolatileDataCollected()
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

      base.OnVolatileDataCollected();
    }

    protected override T PreferredCast(object data) => data is R ? (T) Activator.CreateInstance(typeof(T), data) : null;

    #region UI
    protected override void PrepareForPrompt() { }
    protected override void RecoverFromPrompt() { }
    public virtual void AppendAdditionalElementMenuItems(ToolStripDropDown menu) { }
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);
      Menu_AppendSeparator(menu);
      AppendAdditionalElementMenuItems(menu);

      var doc = Revit.ActiveUIDocument.Document;

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
      this.Menu_AppendConnect(menu, Menu_Connect);
    }

    void Menu_PinElements(object sender, EventArgs args)
    {
      var doc = Revit.ActiveUIDocument.Document;
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
      var doc = Revit.ActiveUIDocument.Document;
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
      var doc = Revit.ActiveUIDocument.Document;
      var elementIds = ToElementIds(VolatileData).
                       Where(x => x.Document.Equals(doc)).
                       Select(x => x.Id);

      if (elementIds.Any())
      {
        using (new ModalForm.EditScope())
        {
          using
          (
            var taskDialog = new TaskDialog(MethodBase.GetCurrentMethod().DeclaringType.FullName)
            {
              MainIcon = TaskDialogIcons.IconWarning,
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
                Revit.ActiveUIDocument.Selection.SetElementIds(elements);

              switch (result = taskDialog.Show())
              {
                case TaskDialogResult.CommandLink1:
                  Revit.ActiveUIDocument.ShowElements(elements);
                  highlight = true;
                  break;

                case TaskDialogResult.CommandLink2:
                  using (var dataManager = new GH_PersistentDataEditor())
                  {
                    var elementCollection = new GH_Structure<IGH_Goo>();
                    elementCollection.AppendRange(elementIds.Select(x => Types.Element.FromElementId(doc, x)));
                    dataManager.SetData(elementCollection, new Types.Element());

                    GH_WindowsFormUtil.CenterFormOnCursor(dataManager, true);
                    if (dataManager.ShowDialog(ModalForm.OwnerWindow) == System.Windows.Forms.DialogResult.OK)
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

    void Menu_Connect(object sender, EventArgs e)
    {
      if (sender is ToolStripMenuItem item && item.Tag is Guid componentGuid)
      {
        var obj = this.ConnectNewObject(componentGuid);
        if (obj is null)
          return;

        obj.ExpireSolution(true);
      }
    }

    protected override bool Prompt_ManageCollection(GH_Structure<T> values)
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
    bool IGH_ElementIdParam.NeedsToBeExpired
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

  public abstract class ElementIdNonGeometryParam<T, R> : ElementIdParam<T, R>
    where T : class, Types.IGH_ElementId
  {
    protected ElementIdNonGeometryParam(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory)
    { }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Plural(ref List<T> values) => GH_GetterResult.cancel;
    protected override GH_GetterResult Prompt_Singular(ref T value) => GH_GetterResult.cancel;
  }
}

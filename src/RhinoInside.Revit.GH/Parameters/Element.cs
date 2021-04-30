using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Interop;
using Autodesk.Revit.UI;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public abstract class Element<T, R> : ElementIdParam<T, R>
  where T : class, Types.IGH_Element
  {
    protected Element(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory)
    { }

    protected override T PreferredCast(object data) => data is R ? Types.Element.FromValue(data) as T : null;

    protected T Current
    {
      get
      {
        var current = (SourceCount == 0 && PersistentDataCount == 1) ? PersistentData.get_FirstItem(true) : default;

        return current is Types.IGH_ReferenceData reference && !reference.LoadReferencedData() ?
          default :
          current;
      }
    }

    internal static IEnumerable<Types.IGH_Element> ToElementIds(IGH_Structure data) =>
      data.AllData(true).
      OfType<Types.IGH_Element>().
      Where(x => x.IsValid);

    #region UI
    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Plural(ref List<T> values) => GH_GetterResult.cancel;
    protected override GH_GetterResult Prompt_Singular(ref T value) => GH_GetterResult.cancel;

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      Menu_AppendSeparator(menu);
      Menu_AppendActions(menu);
    }

    public virtual void Menu_AppendActions(ToolStripDropDown menu)
    {
      if (Revit.ActiveUIDocument?.Document is DB.Document doc)
      {
        if (Kind == GH_ParamKind.output && Attributes.GetTopLevel.DocObject is Components.ReconstructElementComponent)
        {
          var pinned = ToElementIds(VolatileData).
                       Where(x => doc.Equals(x.Document)).
                       Select(x => doc.GetElement(x.Id)).
                       Where(x => x?.Pinned == true).Any();

          if (pinned)
            Menu_AppendItem(menu, $"Unpin {GH_Convert.ToPlural(TypeName)}", Menu_UnpinElements, DataType != GH_ParamData.remote, false);

          var unpinned = ToElementIds(VolatileData).
                       Where(x => doc.Equals(x.Document)).
                       Select(x => doc.GetElement(x.Id)).
                       Where(x => x?.Pinned == false).Any();

          if (unpinned)
            Menu_AppendItem(menu, $"Pin {GH_Convert.ToPlural(TypeName)}", Menu_PinElements, DataType != GH_ParamData.remote, false);
        }

        {
          bool delete = ToElementIds(VolatileData).Any(x => doc.Equals(x.Document));

          Menu_AppendItem(menu, $"Delete {GH_Convert.ToPlural(TypeName)}", Menu_DeleteElements, delete, false);
        }
      }
    }

    void Menu_PinElements(object sender, EventArgs args)
    {
      var doc = Revit.ActiveUIDocument.Document;
      var elements = ToElementIds(VolatileData).
                       Where(x => doc.Equals(x.Document)).
                       Select(x => doc.GetElement(x.Id)).
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
                       Where(x => doc.Equals(x.Document)).
                       Select(x => doc.GetElement(x.Id)).
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
                       Where(x => doc.Equals(x.Document)).
                       Select(x => x.Id);

      if (elementIds.Any())
      {
        using (new External.UI.EditScope(Revit.ActiveUIApplication))
        {
          using
          (
            var taskDialog = new TaskDialog("Delete Elements")
            {
              Id = MethodBase.GetCurrentMethod().DeclaringType.FullName,
              MainIcon = External.UI.TaskDialogIcons.IconWarning,
              TitleAutoPrefix = false,
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
                      elementIds = dataManager.GetData<IGH_Goo>().AllData(true).OfType<Types.Element>().Select(x => x.Id);
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
    #endregion
  }

  public class Element : Element<Types.IGH_Element, object>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary;
    public override Guid ComponentGuid => new Guid("F3EA4A9C-B24F-4587-A358-6A7E6D8C028B");

    public Element() : base("Element", "Element", "Contains a collection of Revit elements", "Params", "Revit Primitives") { }

    protected override Types.IGH_Element InstantiateT() => new Types.Element();
  }
}

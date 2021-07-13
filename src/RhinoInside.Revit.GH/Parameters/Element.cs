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
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
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

    (bool Committed, IList<string> Messages) DeleteElements()
    {
      var committed = false;
      var messages = new List<string>();

      foreach (var document in ToElementIds(VolatileData).GroupBy(x => x.Document))
      {
        using (var group = new DB.TransactionGroup(document.Key, "Delete Elements") { IsFailureHandlingForcedModal = true })
        {
          var elementIds = new HashSet<DB.ElementId>
          (
            document.Where(x => x.IsValid && !x.Id.IsBuiltInId()).Select(x => x.Id),
            default(ElementIdEqualityComparer)
          );

          if (elementIds.Count > 0)
          {
            using (var tx = new DB.Transaction(document.Key, "Delete Elements"))
            {
              tx.SetFailureHandlingOptions(tx.GetFailureHandlingOptions().SetDelayedMiniWarnings(false));

              if (tx.Start() == DB.TransactionStatus.Started)
              {
                // Show feedback on Revit
                foreach (var elementId in elementIds)
                {
                  using (var message = new DB.FailureMessage(ExternalFailures.ElementFailures.ConfirmDeleteElement))
                  {
                    message.SetFailingElement(elementId);
                    document.Key.PostFailure(message);
                  }
                }

                if (tx.Commit() == DB.TransactionStatus.Committed)
                {
                  messages.Add($"Some elements were deleted from '{document.Key.Title.TripleDot(16)}'.");

                  if (elementIds.All(x => document.Key.GetElement(x) is object))
                  {
                    tx.Start();
                    document.Key.Delete(elementIds);
                    committed |= tx.Commit() == DB.TransactionStatus.Committed;
                  }
                  else committed = true;
                }
              }
            }
          }
        }
      }

      return (committed, messages);
    }

    void Menu_DeleteElements(object sender, EventArgs e)
    {
      bool enableSolutions = Guest.DocumentChangedEvent.EnableSolutions;
      Guest.DocumentChangedEvent.EnableSolutions = false;

      var (commited, messages) = DeleteElements();

      Guest.ShowEditor();

      if (commited)
      {
        // Show feedback on Grasshopper
        ClearRuntimeMessages();
        foreach (var message in messages)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);

        ReloadVolatileData();
        ExpireDownStreamObjects();
        OnPingDocument().NewSolution(false);
      }

      Guest.DocumentChangedEvent.EnableSolutions = enableSolutions;
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

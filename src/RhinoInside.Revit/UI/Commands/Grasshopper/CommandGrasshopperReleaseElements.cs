using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using RhinoInside.Revit.External.DB;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperReleaseElements : GrasshopperCommand
  {
    public static string CommandName => "Release\nElements";

    /// <summary>
    /// Available when there are tracked elements selected on Revit view.
    /// </summary>
    protected new class Availability : NeedsActiveDocument<GrasshopperCommand.Availability>
    {
      protected override bool IsCommandAvailable(UIApplication app, DB.CategorySet selectedCategories)
      {
        if (!base.IsCommandAvailable(app, selectedCategories))
          return false;

        var doc = app.ActiveUIDocument.Document;
        return app.ActiveUIDocument.Selection.GetElementIds().Any
        (
          x => GH.ElementTracking.TrackedElementsDictionary.ContainsKey(doc, x)
        );
      }
    }

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      // Create a push button to trigger a command add it to the ribbon panel.
      var buttonData = NewPushButtonData<CommandGrasshopperReleaseElements, NeedsActiveDocument<Availability>>
      (
        name: CommandName,
        iconName: "Ribbon.Grasshopper.ReleaseElements.png",
        tooltip: "Release elements created and tracked by Grasshopper",
        url: "guides/rir-grasshopper#element-tracking"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton)
      {
      }
    }

    bool ReleaseElements(IEnumerable<DB.Element> elements)
    {
      var committed = false;
      var messages = new List<string>();

      foreach (var document in elements.GroupBy(x => x.Document))
      {
        using (var tx = new DB.Transaction(document.Key, "Release Elements"))
        {
          tx.SetFailureHandlingOptions
          (
            tx.GetFailureHandlingOptions().SetForcedModalHandling(false)
          );

          if (tx.Start() == DB.TransactionStatus.Started)
          {
            var list = new List<DB.ElementId>();

            foreach (var element in document)
            {
              if (GH.ElementTracking.ElementStream.ReleaseElement(element))
              {
                element.Pinned = false;
                list.Add(element.Id);
              }
            }

            // Show feedback on Revit
            if (list.Count > 0)
            {
              using (var message = new DB.FailureMessage(ExternalFailures.ElementFailures.TrackedElementReleased))
              {
                message.SetFailingElements(list);
                document.Key.PostFailure(message);
              }

              committed |= tx.Commit() == DB.TransactionStatus.Committed;
            }
          }
        }
      }

      return committed;
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      var commited = ReleaseElements
      (
        data.Application.ActiveUIDocument.Selection.GetElementIds().
        Select(x => data.Application.ActiveUIDocument.Document.GetElement(x))
      );

      return commited ? Result.Succeeded : Result.Cancelled;
    }
  }
}

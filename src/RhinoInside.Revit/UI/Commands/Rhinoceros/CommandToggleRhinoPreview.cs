using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  public class CommandToggleRhinoPreview : RhinoCommand
  {
    public static string CommandName => "Toggle\nPreview";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
#if REVIT_2018
      var buttonData = NewPushButtonData<CommandToggleRhinoPreview, NeedsActiveDocument<Availability>>
      (
        name: CommandName,
        iconName: "Ribbon.Grasshopper.Preview_Off.png",
        tooltip: "Toggle Rhino model preview visibility",
        url : "reference/rir-interface#rhinoceros-panel"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        StoreButton(CommandName, pushButton);
        ButtonSetImages(false);
      }

      CommandStart.AddinStarted += CommandStart_AddinStarted;
#endif
    }

#if REVIT_2018
    private static void CommandStart_AddinStarted(object sender, CommandStart.AddinStartedArgs e)
    {
      DocumentPreviewServer.ActiveDocumentChanged += DocumentPreviewServer_ActiveDocumentChanged;
      CommandStart.AddinStarted -= CommandStart_AddinStarted;
    }
#endif

#if REVIT_2018
    static void ButtonSetImages(bool status)
    {
      if (RestoreButton(CommandName) is PushButton button)
      {
        if (status)
        {
          button.Image = ImageBuilder.LoadRibbonButtonImage("Ribbon.Rhinoceros.Preview_Shaded.png", true);
          button.LargeImage = ImageBuilder.LoadRibbonButtonImage("Ribbon.Rhinoceros.Preview_Shaded.png");
        }
        else
        {
          button.Image = ImageBuilder.LoadRibbonButtonImage("Ribbon.Grasshopper.Preview_Off.png", true);
          button.LargeImage = ImageBuilder.LoadRibbonButtonImage("Ribbon.Grasshopper.Preview_Off.png");
        }
      }
    }

    private static void DocumentPreviewServer_ActiveDocumentChanged(object sender, EventArgs e) =>
      ButtonSetImages(DocumentPreviewServer.ActiveDocument is object);
#endif

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
#if REVIT_2018
      DocumentPreviewServer.Toggle();
      return Result.Succeeded;
#else
      return Result.Failed;
#endif
    }
  }
}

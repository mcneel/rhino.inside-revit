using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  public class CommandRhinoPreview : RhinoCommand
  {
    public static void CreateUI(RibbonPanel ribbonPanel)
    {
#if REVIT_2018
      var buttonData = NewPushButtonData<CommandRhinoPreview, NeedsActiveDocument<Availability>>(
        "Preview",
        "Resources.Ribbon.Grasshopper.Preview_Off.png",
        "Toggle Rhino model preview visibility"
        );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        StoreButton("Preview", pushButton);
        DocumentPreviewServer.ActiveDocumentChanged += DocumentPreviewServer_ActiveDocumentChanged;
        ButtonSetImages(false);
        pushButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://github.com/mcneel/rhino.inside-revit/tree/master#sample-6"));
      }
#endif
    }

#if REVIT_2018
    static void ButtonSetImages(bool status)
    {
      if (RestoreButton("Preview") is PushButton button)
      {
        if (status)
        {
          button.Image = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Rhinoceros.Preview_Shaded.png", true);
          button.LargeImage = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Rhinoceros.Preview_Shaded.png");
        }
        else
        {
          button.Image = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Grasshopper.Preview_Off.png", true);
          button.LargeImage = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Grasshopper.Preview_Off.png");
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

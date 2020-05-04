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
      var buttonData = NewPushButtonData<CommandRhinoPreview, NeedsActiveDocument<Availability>>("Preview");

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        Button = pushButton;
        DocumentPreviewServer.ActiveDocumentChanged += DocumentPreviewServer_ActiveDocumentChanged;

        Button.ToolTip = "Toggle Rhino model preview visibility";
        ButtonSetImages(false);
        Button.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://github.com/mcneel/rhino.inside-revit/tree/master#sample-6"));
      }
#endif
    }

#if REVIT_2018
    static PushButton Button = default;

    static void ButtonSetImages(bool status)
    {
      if (status)
      {
        Button.Image = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Rhinoceros.Preview_Shaded.png", true);
        Button.LargeImage = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Rhinoceros.Preview_Shaded.png");
      }
      else
      {
        Button.Image = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Grasshopper.Preview_Off.png", true);
        Button.LargeImage = ImageBuilder.LoadBitmapImage("Resources.Ribbon.Grasshopper.Preview_Off.png");
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

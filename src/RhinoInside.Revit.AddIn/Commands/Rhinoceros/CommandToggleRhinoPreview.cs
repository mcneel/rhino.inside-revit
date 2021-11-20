using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.AddIn.Commands
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

      AssemblyResolver.References["RhinoCommon"].Activated += RhinoCommon_AssemblyActivated;
#endif
    }

#if REVIT_2018
    private static void RhinoCommon_AssemblyActivated(object sender, AssemblyLoadEventArgs args)
    {
      Rhinoceros.PreviewServer.ActiveDocumentChanged += DocumentPreviewServer_ActiveDocumentChanged;
    }

    static void ButtonSetImages(bool status)
    {
      if (RestoreButton(CommandName) is PushButton button)
      {
        if (status)
        {
          button.Image = LoadRibbonButtonImage("Ribbon.Rhinoceros.Preview_Shaded.png", true);
          button.LargeImage = LoadRibbonButtonImage("Ribbon.Rhinoceros.Preview_Shaded.png");
        }
        else
        {
          button.Image = LoadRibbonButtonImage("Ribbon.Grasshopper.Preview_Off.png", true);
          button.LargeImage = LoadRibbonButtonImage("Ribbon.Grasshopper.Preview_Off.png");
        }
      }
    }

    private static void DocumentPreviewServer_ActiveDocumentChanged(object sender, EventArgs e) =>
      ButtonSetImages(Rhinoceros.PreviewServer.ActiveDocument is object);
#endif

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
#if REVIT_2018
      Rhinoceros.PreviewServer.Toggle();
      return Result.Succeeded;
#else
      return Result.Failed;
#endif
    }
  }
}

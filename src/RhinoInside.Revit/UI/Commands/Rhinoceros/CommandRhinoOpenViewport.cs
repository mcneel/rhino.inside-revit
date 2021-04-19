using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandRhinoOpenViewport : RhinoCommand
  {
    public static string CommandName => "Open\nViewport";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandRhinoOpenViewport, AvailableWhenRhinoReady>
      (
        name: CommandName,
        iconName: "Ribbon.Rhinoceros.OpenViewport.png",
        tooltip: "Opens a perspective viewport",
        url: "reference/rir-interface#rhino-options"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.LongDescription = $"Opens a new perspective viewport (torn off from Rhino window)";
        StoreButton(CommandName, pushButton);
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      return Rhinoceros.RunScript(
        "-_NewFloatingViewport Enter\n" +
        "-_ViewportProperties Size 600 400\n" +
        "-_Zoom _Extents\n" +
        "_Enter",
        false) ? Result.Succeeded : Result.Failed;
    }
  }
}

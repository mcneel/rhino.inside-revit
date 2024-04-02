using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.AddIn.Commands
{
  abstract class CommandDebug : Command
  {
    internal static void CreateUI(RibbonPanel ribbonPanel)
    {
#if DEBUG
      var schema = NewPushButtonData<CommandTest, NeedsActiveDocument>
      (
        name: CommandTest.CommandName,
        iconName: "Options.png",
        tooltip: $"Debug {CommandTest.CommandName}"
      );

      if (ribbonPanel.AddItem(schema) is PushButton pushButton)
      {
      }
#endif
    }
  }

#if DEBUG
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandTest : Command
  {
    public static string CommandName = "Test";

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      return Result.Succeeded;
    }
  }
#endif
}

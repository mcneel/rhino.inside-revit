using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Eto.Forms;
using Rhino.PlugIns;
using System.Diagnostics;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandRhino : RhinoCommand
  {
    public static string CommandName => "Rhino";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandRhino, Availability>(
        CommandName,
        "Rhino.png",
        "Shows Rhino window"
        );
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.LongDescription = $"Use CTRL key to open only Rhino window without restoring other tool windows";
        pushButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://discourse.mcneel.com/"));
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      Rhinoceros.ShowAsync();
      return Result.Succeeded;
    }
  }
}

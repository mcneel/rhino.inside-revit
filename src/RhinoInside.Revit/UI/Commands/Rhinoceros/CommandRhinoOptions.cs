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
using RhinoInside.Revit.External.UI.Extensions;

using Eto.Forms;
using Rhino.PlugIns;
using System.Diagnostics;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandRhinoOptions : RhinoCommand
  {
    public static string CommandName => "RhinoOptions";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandRhinoOptions, Availability>(
        CommandName,
        "Rhino.png",
        "Shows Rhino Options window"
        );
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
        // set this button as the panel dialog-launcher (arrow-button at the corner of panel)
        ribbonPanel.SetButtonToDialogLauncher(Addin.AddinName, pushButton);
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      Rhinoceros.RunCommandOptions();
      return Result.Succeeded;
    }
  }
}

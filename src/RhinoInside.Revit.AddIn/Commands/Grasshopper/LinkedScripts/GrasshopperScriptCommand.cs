using System;
using System.Collections.Generic;
using System.Windows.Input;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.AddIn.Commands
{
  /// <summary>
  /// Base class for all the linked-script buttons in the UI. This class is dyanmically copied,
  /// extended, and configured to point to the script file and then is tied to the button on the UI
  /// </summary>
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  public abstract class GrasshopperScriptCommand : GrasshopperCommand
  {
    protected readonly string ScriptPath;

    /// <summary>
    /// Create new instance pointing to given script
    /// </summary>
    /// <param name="scriptPath">Full path of script file</param>
    protected GrasshopperScriptCommand(string scriptPath)
    {
      ScriptPath = scriptPath;
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      bool debugMode = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
      return debugMode ? Open() : Play(data, ref message);
    }

    private Result Play(ExternalCommandData data, ref string message) => CommandGrasshopperPlayer.Execute
    (
      data.Application,
      data.View,
      data.JournalData,
      ScriptPath,
      ref message
    );

    private Result Open() => GH.Guest.OpenDocument(ScriptPath) ? Result.Succeeded : Result.Cancelled;
  }
}

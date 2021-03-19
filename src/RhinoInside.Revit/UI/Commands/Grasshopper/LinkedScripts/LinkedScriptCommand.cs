using System;
using System.Collections.Generic;
using System.Windows.Input;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.UI
{
  /// <summary>
  /// Base class for all the linked-script buttons in the UI. This class is dyanmically copied,
  /// extended, and configured to point to the script file and then is tied to the button on the UI
  /// </summary>
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  public abstract class LinkedScriptCommand : Command
  {
    /* Note:
     * There is no default constructor. It is generated dyanmically when copying this base type
     */

    /// <summary>
    /// Configurations for the target script
    /// </summary>
    public class ScriptExecConfigs
    {
      public ScriptType ScriptType = ScriptType.GhFile;
      public string ScriptPath;
    }

    /// <summary>
    /// Script configurations for this instance
    /// </summary>
    public ScriptExecConfigs ExecCfgs;

    /// <summary>
    /// Create new instance pointing to given script
    /// </summary>
    /// <param name="scriptPath">Full path of script file</param>
    public LinkedScriptCommand(int scriptType, string scriptPath)
    {
      ExecCfgs = new ScriptExecConfigs
      {
        ScriptType = (ScriptType) scriptType,
        ScriptPath = scriptPath
      };
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      bool debugMode = (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
      switch (ExecCfgs.ScriptType)
      {
        case ScriptType.GhFile:
        case ScriptType.GhxFile:
          return debugMode ? OpenGH() : ExecuteGH(data, ref message);

        default: return Result.Succeeded;
      }
    }

    private Result ExecuteGH(ExternalCommandData data, ref string message)
    {
      // run definition with grasshopper player
      return CommandGrasshopperPlayer.Execute(
        data.Application,
        data.Application.ActiveUIDocument?.ActiveView,
        new Dictionary<string, string>(),
        ExecCfgs.ScriptPath,
        ref message
        );
    }

    private Result OpenGH() => GH.Guest.OpenDocument(ExecCfgs.ScriptPath) ? Result.Succeeded : Result.Cancelled;
  }
}

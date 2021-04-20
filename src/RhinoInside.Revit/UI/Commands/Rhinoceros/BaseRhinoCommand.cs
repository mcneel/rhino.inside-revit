using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.UI
{
  /// <summary>
  /// Base class for all Rhino.Inside Revit commands that call RhinoCommon
  /// </summary>
  public abstract class RhinoCommand : Command
  {
    public RhinoCommand()
    {
      if (Revit.OnStartup(AddIn.Host) != Result.Succeeded)
        throw new Exception("Failed to startup Rhino");
    }

    /// <summary>
    /// Available when no Rhino command is currently running
    /// </summary>
    protected class AvailableWhenRhinoReady : Command.Availability
    {
      public override bool IsCommandAvailable(UIApplication app, CategorySet selectedCategories) =>
        base.IsCommandAvailable(app, selectedCategories) &&
        AddIn.CurrentStatus == AddIn.Status.Ready &&
        // at this point addin is loaded and rhinocommon is available
        RhinoIsReady();

      bool RhinoIsReady()
      {
        // wrapping rhino test method to remove the rhinocommon
        // dependency from IsCommandAvailable method.
        return !Rhino.Commands.Command.InCommand();
      }
    }
  }
}

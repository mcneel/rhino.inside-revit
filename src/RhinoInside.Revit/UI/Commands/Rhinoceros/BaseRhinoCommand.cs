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
    protected new class Availability : Command.Availability
    {
      public override bool IsCommandAvailable(UIApplication app, CategorySet selectedCategories) =>
        base.IsCommandAvailable(app, selectedCategories) &&
        !Rhino.Commands.Command.InCommand();
    }
  }
}

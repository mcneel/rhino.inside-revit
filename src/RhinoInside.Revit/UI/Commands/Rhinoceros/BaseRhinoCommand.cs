using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.UI
{
  /// <summary>
  /// Base class for all Rhino.Inside Revit commands that call RhinoCommon.
  /// </summary>
  public abstract class RhinoCommand : Command
  {
    /// <summary>
    /// Available when no Rhino command is currently running.
    /// </summary>
    protected internal new class Availability : AvailableWhenReady
    {
      protected override bool IsCommandAvailable(UIApplication app, CategorySet selectedCategories) =>
        base.IsCommandAvailable(app, selectedCategories) &&
        !Rhino.Commands.Command.InCommand();
    }
  }
}

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
  /// <summary>
  /// Base class for all Rhino.Inside Revit commands that call RhinoCommon
  /// </summary>
  abstract public class RhinoCommand : Command
  {
    public RhinoCommand()
    {
      if (Revit.OnStartup(Revit.ApplicationUI) != Result.Succeeded)
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

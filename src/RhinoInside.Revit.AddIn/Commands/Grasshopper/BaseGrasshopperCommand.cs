using System;
using Rhino.PlugIns;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.AddIn.Commands
{
  /// <summary>
  /// Base class for all Rhino.Inside Revit commands that call Grasshopper API.
  /// </summary>
  public abstract class GrasshopperCommand : RhinoCommand
  {
    protected static readonly Guid PlugInId = new Guid(0xB45A29B1, 0x4343, 0x4035, 0x98, 0x9E, 0x04, 0x4E, 0x85, 0x80, 0xD9, 0xCF);

    public GrasshopperCommand()
    {
      if (!PlugIn.LoadPlugIn(PlugInId, true, true))
        throw new InvalidOperationException("Failed to load Grasshopper");
    }

    /// <summary>
    /// Available when Grasshopper Plugin is available in Rhino.
    /// </summary>
    protected internal new class Availability : RhinoCommand.Availability
    {
      static bool ready = false;
      public override bool IsRuntimeReady()
      {
        if (!base.IsRuntimeReady()) return false;
        return ready || (ready = AssemblyResolver.References["Grasshopper"].Assembly is object);
      }

      protected override bool IsCommandAvailable(ARUI.UIApplication app, ARDB.CategorySet selectedCategories) =>
        base.IsCommandAvailable(app, selectedCategories) &&
        (PlugIn.PlugInExists(PlugInId, out bool loaded, out bool loadProtected) & (loaded | !loadProtected));
    }
  }
}

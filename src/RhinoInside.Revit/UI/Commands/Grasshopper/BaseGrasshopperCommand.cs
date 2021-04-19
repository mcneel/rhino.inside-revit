using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Grasshopper;
using Grasshopper.Kernel;
using Microsoft.Win32.SafeHandles;
using Rhino.PlugIns;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.GH.Bake;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.UI
{
  /// <summary>
  /// Base class for all Rhino.Inside Revit commands that call Grasshopper API
  /// </summary>
  public abstract class GrasshopperCommand : RhinoCommand
  {
    protected static readonly Guid PluginId = new Guid(0xB45A29B1, 0x4343, 0x4035, 0x98, 0x9E, 0x04, 0x4E, 0x85, 0x80, 0xD9, 0xCF);

    public GrasshopperCommand()
    {
      if (!PlugIn.LoadPlugIn(PluginId, true, true))
        throw new InvalidOperationException("Failed to load Grasshopper");
    }

    /// <summary>
    /// Available when Grasshopper Plugin is available in Rhino
    /// </summary>
    protected class AvailableWhenGHReady : RhinoCommand.AvailableWhenRhinoReady
    {
      public override bool IsCommandAvailable(UIApplication _, DB.CategorySet selectedCategories) =>
        base.IsCommandAvailable(_, selectedCategories) &&
        AddIn.CurrentStatus == AddIn.Status.Ready &&
        // at this point addin is loaded and rhinocommon is available
        GHIsReady();

      bool GHIsReady()
      {
        // wrapping rhino test method to remove the rhinocommon
        // dependency from IsCommandAvailable method.
        return (PlugIn.PlugInExists(PluginId, out bool loaded, out bool loadProtected) & (loaded | !loadProtected));
      }
    }
  }
}

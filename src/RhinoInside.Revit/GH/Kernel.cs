using System.Collections.Generic;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Kernel
{
  public enum ComponentSignal
  {
    /// <summary>
    /// Component should not compute anything nor expire any output
    /// </summary>
    Frozen = 0,

    /// <summary>
    /// Component should execute normaly
    /// </summary>
    Active = 1,
  }

  /// <summary>
  /// Base interface for all Parameter types in RhinoInside.Revit.GH that reference Revit elements.
  /// </summary>
  /// <remarks>
  /// Do not implement this interface from scratch, derive from <c>RhinoInside.Revit.GH.Types.ElementIdParam</c> instead.
  /// </remarks>
  /// <seealso cref="RhinoInside.Revit.GH.Types.ElementIdParam"/>
  public interface IGH_ElementIdParam : IGH_Param
  {
    bool NeedsToBeExpired
    (
      DB.Document doc,
      ICollection<DB.ElementId> added,
      ICollection<DB.ElementId> deleted,
      ICollection<DB.ElementId> modified
    );
  }

  /// <summary>
  /// Base interface for all Component types in RhinoInside.Revit.GH that reference Revit elements.
  /// </summary>
  /// <remarks>
  /// Do not implement this interface from scratch, derive from <c>RhinoInside.Revit.GH.Components.Component</c> instead.
  /// </remarks>
  /// <seealso cref="RhinoInside.Revit.GH.Components.Component"/>
  public interface IGH_ElementIdComponent : IGH_Component
  {
    bool NeedsToBeExpired(DB.Events.DocumentChangedEventArgs args);
  }
}

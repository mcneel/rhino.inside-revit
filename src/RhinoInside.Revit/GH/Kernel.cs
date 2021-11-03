using System.Collections.Generic;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Kernel
{
  /// <summary>
  /// Base interface for all Parameter types in RhinoInside.Revit.GH that reference Revit elements.
  /// </summary>
  /// <remarks>
  /// Do not implement this interface from scratch, derive from <see cref="RhinoInside.Revit.GH.Types.ElementIdParam"/> instead.
  /// </remarks>
  /// <seealso cref="RhinoInside.Revit.GH.Types.ElementIdParam"/>
  internal interface IGH_ElementIdParam : IGH_Param
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
  /// Do not implement this interface from scratch, derive from <see cref="RhinoInside.Revit.GH.Components.Component"/> instead.
  /// </remarks>
  /// <seealso cref="RhinoInside.Revit.GH.Components.Component"/>
  internal interface IGH_ElementIdComponent : IGH_Component
  {
    bool NeedsToBeExpired
    (
      DB.Document doc,
      ICollection<DB.ElementId> added,
      ICollection<DB.ElementId> deleted,
      ICollection<DB.ElementId> modified
    );
  }
}

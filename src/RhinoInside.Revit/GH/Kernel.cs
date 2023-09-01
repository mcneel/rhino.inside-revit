using System.Collections.Generic;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Kernel
{
  /// <summary>
  /// Base interface for all Parameter types in RhinoInside.Revit.GH that reference Revit elements.
  /// </summary>
  internal interface IGH_ReferenceParam : IGH_Param
  {
    bool NeedsToBeExpired
    (
      ARDB.Document doc,
      ISet<ARDB.ElementId> added,
      ISet<ARDB.ElementId> deleted,
      ISet<ARDB.ElementId> modified
    );
  }

  /// <summary>
  /// Base interface for all Component types in RhinoInside.Revit.GH that reference Revit elements.
  /// </summary>
  internal interface IGH_ReferenceComponent : IGH_Component
  {
    bool NeedsToBeExpired
    (
      ARDB.Document doc,
      ISet<ARDB.ElementId> added,
      ISet<ARDB.ElementId> deleted,
      ISet<ARDB.ElementId> modified
    );
  }
}

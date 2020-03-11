using System;
using System.ComponentModel;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public interface IGH_ElementId : IGH_Goo
  {
    DB.Document Document { get; }
    DB.ElementId Id { get; }

    Guid DocumentGUID { get; }
    string UniqueID { get; }

    bool IsReferencedElement { get; }
    bool IsElementLoaded { get; }
    bool LoadElement();
    void UnloadElement();
  }
}

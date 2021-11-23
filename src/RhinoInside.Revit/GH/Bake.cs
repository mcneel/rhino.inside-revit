using System.Collections.Generic;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Bake
{
  internal class BakeOptions
  {
    public ARDB.Document Document;
    public ARDB.View     View;
    public ARDB.Category Category;
    public ARDB.Workset  Workset;
    public ARDB.Material Material;
  }

  internal interface IGH_ElementIdBakeAwareObject
  {
    bool CanBake(BakeOptions options);
    bool Bake(BakeOptions options, out ICollection<ARDB.ElementId> ids);
  }

  internal interface IGH_ElementIdBakeAwareData
  {
    bool Bake(BakeOptions options, out ARDB.ElementId id);
  }
}

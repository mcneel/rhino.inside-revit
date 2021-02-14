using System;
using System.Collections.Generic;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Bake
{
  public class BakeOptions
  {
    public DB.Document Document;
    public DB.View     View;
    public DB.Category Category;
    public DB.Workset Workset;
    public DB.Material Material;
  }

  public interface IGH_ElementIdBakeAwareObject
  {
    bool CanBake(BakeOptions options);
    bool Bake(BakeOptions options, out ICollection<DB.ElementId> ids);
  }

  public interface IGH_ElementIdBakeAwareData
  {
    bool Bake(BakeOptions options, out DB.ElementId id);
  }
}

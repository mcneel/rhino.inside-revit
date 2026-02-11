using System;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Element Source")]
  public interface IGH_ModelInstance : IGH_Goo
  {
    public RevitLinkInstance ModelInstance { get; }
    public Document ModelDocument { get; }
  }
}

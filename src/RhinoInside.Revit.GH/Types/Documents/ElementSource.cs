using System;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Element Source")]
  public interface IGH_ElementSource : IGH_Goo
  {
    public RevitLinkInstance SourceInstance { get; }
    public Document SourceDocument { get; }
  }
}

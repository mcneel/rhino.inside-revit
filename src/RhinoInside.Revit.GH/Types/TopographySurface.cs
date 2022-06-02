using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Topography")]
  public class TopographySurface : GeometricElement
  {
    protected override Type ValueType => typeof(ARDB.Architecture.TopographySurface);
    public new ARDB.Architecture.TopographySurface Value => base.Value as ARDB.Architecture.TopographySurface;

    public TopographySurface() { }
    public TopographySurface(ARDB.Architecture.TopographySurface element) : base(element) { }
  }
}

using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Area Scheme")]
  public class AreaScheme : Element
  {
    protected override Type ValueType => typeof(ARDB.AreaScheme);
    public new ARDB.AreaScheme Value => base.Value as ARDB.AreaScheme;

    public AreaScheme() { }
    public AreaScheme(ARDB.AreaScheme element) : base(element) { }
  }
}

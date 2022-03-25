using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("3D View")]
  public class View3D : View
  {
    protected override Type ValueType => typeof(ARDB.View3D);
    public new ARDB.View3D Value => base.Value as ARDB.View3D;

    public View3D() { }
    public View3D(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public View3D(ARDB.View3D view) : base(view) { }
  }
}

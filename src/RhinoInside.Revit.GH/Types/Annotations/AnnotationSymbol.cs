using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Symbol")]
  public class AnnotationSymbol : FamilyInstance
  {
    protected override Type ValueType => typeof(ARDB.AnnotationSymbol);
    public new ARDB.AnnotationSymbol Value => base.Value as ARDB.AnnotationSymbol;

    public AnnotationSymbol() { }
    public AnnotationSymbol(ARDB.AnnotationSymbol element) : base(element) { }
  }
}

using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Symbol")]
  public sealed class AnnotationSymbol : FamilyInstance
  {
    protected override Type ValueType => typeof(ARDB.AnnotationSymbol);
    public new ARDB.AnnotationSymbol Value => base.Value as ARDB.AnnotationSymbol;

    public AnnotationSymbol() { }
    public AnnotationSymbol(ARDB.AnnotationSymbol element) : base(element) { }
  }
}

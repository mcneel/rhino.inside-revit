using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Fill Pattern")]
  public class FillPatternElement : Element
  {
    protected override Type ValueType => typeof(ARDB.FillPatternElement);
    public new ARDB.FillPatternElement Value => base.Value as ARDB.FillPatternElement;
    public static explicit operator ARDB.FillPatternElement(FillPatternElement value) => value?.Value;

    public FillPatternElement() { }
    public FillPatternElement(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public FillPatternElement(ARDB.FillPatternElement value) : base(value) { }
  }
}

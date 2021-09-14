using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Fill Pattern")]
  public class FillPatternElement : Element
  {
    protected override Type ValueType => typeof(DB.FillPatternElement);
    public new DB.FillPatternElement Value => base.Value as DB.FillPatternElement;
    public static explicit operator DB.FillPatternElement(FillPatternElement value) => value?.Value;

    public FillPatternElement() { }
    public FillPatternElement(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public FillPatternElement(DB.FillPatternElement value) : base(value) { }
  }
}

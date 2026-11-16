using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Structural Settings")]
  public sealed class StructuralSettings : Element
  {
    protected sealed override Type ValueType => typeof(ARDB.Structure.StructuralSettings);
    public new ARDB.Structure.StructuralSettings Value => base.Value as ARDB.Structure.StructuralSettings;

    public StructuralSettings() { }
    public StructuralSettings(ARDB.Structure.StructuralSettings element) : base(element) { }
  }
}

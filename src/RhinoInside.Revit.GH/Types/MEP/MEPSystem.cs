using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("MEP System")]
  public class MEPSystem : InstanceElement
  {
    protected override Type ValueType => typeof(ARDB.MEPSystem);
    public new ARDB.MEPSystem Value => base.Value as ARDB.MEPSystem;

    public MEPSystem() { }
    public MEPSystem(ARDB.MEPSystem value) : base(value) { }
  }

  [Kernel.Attributes.Name("MEP System Type")]
  public class MEPSystemType : ElementType
  {
    protected override Type ValueType => typeof(ARDB.MEPSystemType);
    public new ARDB.MEPSystemType Value => base.Value as ARDB.MEPSystemType;

    public MEPSystemType() { }
    public MEPSystemType(ARDB.MEPSystemType value) : base(value) { }
  }
}

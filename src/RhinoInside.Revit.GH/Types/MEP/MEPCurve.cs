using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("MEP Curve")]
  public class MEPCurve : HostObject
  {
    protected override Type ValueType => typeof(ARDB.MEPCurve);
    public new ARDB.MEPCurve Value => base.Value as ARDB.MEPCurve;

    public MEPCurve() { }
    public MEPCurve(ARDB.MEPCurve value) : base(value) { }
  }

  [Kernel.Attributes.Name("MEP Curve Type")]
  public class MEPCurveType : HostObjectType
  {
    protected override Type ValueType => typeof(ARDB.MEPCurveType);
    public new ARDB.MEPCurveType Value => base.Value as ARDB.MEPCurveType;

    public MEPCurveType() { }
    public MEPCurveType(ARDB.MEPCurveType value) : base(value) { }
  }
}

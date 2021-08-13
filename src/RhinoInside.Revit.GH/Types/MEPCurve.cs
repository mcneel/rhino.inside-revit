using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("MEP Curve")]
  public class MEPCurve : HostObject
  {
    protected override Type ScriptVariableType => typeof(DB.MEPCurve);
    public new DB.MEPCurve Value => base.Value as DB.MEPCurve;

    public MEPCurve() { }
    public MEPCurve(DB.MEPCurve value) : base(value) { }
  }

  [Kernel.Attributes.Name("MEP Curve Type")]
  public class MEPCurveType : HostObjectType
  {
    protected override Type ScriptVariableType => typeof(DB.MEPCurveType);
    public new DB.MEPCurveType Value => base.Value as DB.MEPCurveType;

    public MEPCurveType() { }
    public MEPCurveType(DB.MEPCurveType value) : base(value) { }
  }
}

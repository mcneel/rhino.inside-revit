using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Truss")]
  public class Truss : InstanceElement
  {
    protected override Type ValueType => typeof(ARDB.Structure.Truss);
    public new ARDB.Structure.Truss Value => base.Value as ARDB.Structure.Truss;

    public Truss() { }
    public Truss(ARDB.Structure.Truss truss) : base(truss) { }

    public double? LevelOffset =>
      Value?.get_Parameter(ARDB.BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM).AsDouble() * Revit.ModelUnits;
  }
}

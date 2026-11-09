using System;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
#if REVIT_2024
  [Kernel.Attributes.Name("Toposolid")]
  public class Toposolid : CeilingAndFloor
  {
    protected override Type ValueType => typeof(ARDB.Toposolid);
    public new ARDB.Toposolid Value => base.Value as ARDB.Toposolid;

    public Toposolid() { }
    public Toposolid(ARDB.Toposolid element) : base(element) { }

    #region IHostElementAccess
    public override GraphicalElement HostElement =>
      Value is ARDB.Toposolid solid ?
          solid.HostTopoId.IsValid() ?
          GetElement<GraphicalElement>(solid.HostTopoId) :
          new GraphicalElement():
        default;
    #endregion

    public override double? LevelOffset =>
      Value?.get_Parameter(ARDB.BuiltInParameter.TOPOSOLID_HEIGHTABOVELEVEL_PARAM).AsDouble() * Revit.ModelUnits;
  }
#endif
}

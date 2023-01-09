using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using Rhino;

  [Kernel.Attributes.Name("Curtain System")]
  public class CurtainSystem : HostObject, ICurtainGridsAccess
  {
    protected override Type ValueType => typeof(ARDB.CurtainSystem);
    public new ARDB.CurtainSystem Value => base.Value as ARDB.CurtainSystem;

    public CurtainSystem() { }
    public CurtainSystem(ARDB.CurtainSystem system) : base(system) { }

    #region ICurtainGridsAccess
    public IList<CurtainGrid> CurtainGrids => Value is ARDB.CurtainSystem system ?
      system.CurtainGrids is ARDB.CurtainGridSet gridSet ?
      gridSet.Cast<ARDB.CurtainGrid>().Select((x, i) => new CurtainGrid(this, x, i)).ToArray() :
      new CurtainGrid[] { } :
      default;
    #endregion

    #region Properties
    protected override void ResetValue()
    {
      using (_PolySurface) _PolySurface = null;
      using (_Mesh) _Mesh = null;

      base.ResetValue();
    }
    public override Plane Location => base.Location;

    Brep _PolySurface;
    public override Brep PolySurface => _PolySurface ?? (_PolySurface = Brep.MergeBreps(CurtainGrids.Select(x => x.PolySurface), RhinoMath.UnsetValue));

    Mesh _Mesh;
    public override Mesh Mesh
    {
      get
      {
        if (_Mesh is null && CurtainGrids is IList<CurtainGrid> curtainGrids)
        {
          _Mesh = new Mesh();
          foreach (var curtainGrid in curtainGrids)
            _Mesh.Append(curtainGrid.Mesh);
        }
        return _Mesh; 
      }
    }
    #endregion
  }
}

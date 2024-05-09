using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Roof")]
  public class Roof : HostObject, ISketchAccess, ICurtainGridsAccess
  {
    protected override Type ValueType => typeof(ARDB.RoofBase);
    public new ARDB.RoofBase Value => base.Value as ARDB.RoofBase;

    public Roof() { }
    public Roof(ARDB.RoofBase roof) : base(roof) { }

    public override ARDB.ElementId LevelId
    {
      get
      {
        switch (Value)
        {
          case ARDB.ExtrusionRoof extrusionRoof:
            return extrusionRoof.get_Parameter(ARDB.BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM).AsElementId();
        }

        return base.LevelId;
      }
    }

    public double? LevelOffset
    {
      get
      {
        switch (Value)
        {
          case ARDB.ExtrusionRoof extrusionRoof:
            return extrusionRoof.get_Parameter(ARDB.BuiltInParameter.ROOF_CONSTRAINT_OFFSET_PARAM).AsDouble() * Revit.ModelUnits;

          case ARDB.FootPrintRoof footPrintRoof:
            return footPrintRoof.get_Parameter(ARDB.BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM).AsDouble() * Revit.ModelUnits;
        }

        return default;
      }
    }

    #region Location
    public override Plane Location
    {
      get
      {
        if(Sketch is Sketch sketch)
        {
          var plane = sketch.ProfilesPlane;

          var center = plane.Origin;
          center.Z = Level.Elevation + LevelOffset.Value;

          var xAxis = plane.XAxis;
          var yAxis = plane.YAxis;

          if (Value is ARDB.ExtrusionRoof)
            yAxis = plane.ZAxis;

          return new Plane(center, xAxis, yAxis);
        }

        return base.Location;
      }
    }
    #endregion

    #region ISketchAccess
    public Sketch Sketch => GetElement<Sketch>(Value?.GetSketchId());
    #endregion

    #region ICurtainGridsAccess
    public IList<CurtainGrid> CurtainGrids
    {
      get
      {
        switch (Value)
        {
          case ARDB.ExtrusionRoof extrusionRoof:
            return extrusionRoof.CurtainGrids is ARDB.CurtainGridSet extrusionGrids ?
              extrusionGrids.Cast<ARDB.CurtainGrid>().Select((x, i) => new CurtainGrid(this, x, i)).ToArray() :
              new CurtainGrid[] { };

          case ARDB.FootPrintRoof footPrintRoof:
            return footPrintRoof.CurtainGrids is ARDB.CurtainGridSet footPrintGrids ?
              footPrintGrids.Cast<ARDB.CurtainGrid>().Select((x, i) => new CurtainGrid(this, x, i)).ToArray() :
              new CurtainGrid[] { };
        }

        return default;
      }
    }
    #endregion
  }
}

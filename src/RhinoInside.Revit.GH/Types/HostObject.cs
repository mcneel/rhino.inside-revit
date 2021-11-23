using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Host")]
  public interface IGH_HostObject : IGH_InstanceElement { }

  [Kernel.Attributes.Name("Host")]
  public class HostObject : InstanceElement, IGH_HostObject
  {
    protected override Type ValueType => typeof(ARDB.HostObject);
    public new ARDB.HostObject Value => base.Value as ARDB.HostObject;

    public HostObject() { }
    protected internal HostObject(ARDB.HostObject host) : base(host) { }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.HostObject host && !(host.Location is ARDB.LocationPoint) && !(host.Location is ARDB.LocationCurve))
        {
          if (host.GetSketch() is ARDB.Sketch sketch)
          {
            var center = Point3d.Origin;
            var count = 0;
            foreach (var curveArray in sketch.Profile.Cast<ARDB.CurveArray>())
            {
              foreach (var curve in curveArray.Cast<ARDB.Curve>())
              {
                count++;
                center += curve.Evaluate(0.0, normalized: true).ToPoint3d();
                count++;
                center += curve.Evaluate(1.0, normalized: true).ToPoint3d();
              }
            }
            center /= count;

            var hostLevelId = host.LevelId;
            if (hostLevelId == ARDB.ElementId.InvalidElementId)
              hostLevelId = host.get_Parameter(ARDB.BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM)?.AsElementId() ?? hostLevelId;

            if (host.Document.GetElement(hostLevelId) is ARDB.Level level)
              center.Z = level.GetHeight() * Revit.ModelUnits;

            var plane = sketch.SketchPlane.GetPlane().ToPlane();
            var origin = center;
            var xAxis = plane.XAxis;
            var yAxis = plane.YAxis;

            if (host is ARDB.Wall)
            {
              xAxis = -plane.XAxis;
              yAxis = plane.ZAxis;
            }

            if (host is ARDB.FootPrintRoof)
              origin.Z += host.get_Parameter(ARDB.BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM).AsDouble() * Revit.ModelUnits;

            if (host is ARDB.ExtrusionRoof)
            {
              origin.Z += host.get_Parameter(ARDB.BuiltInParameter.ROOF_CONSTRAINT_OFFSET_PARAM).AsDouble() * Revit.ModelUnits;
              yAxis = -plane.ZAxis;
            }

            return new Plane(origin, xAxis, yAxis);
          }
        }

        return base.Location;
      }
    }

    public IList<CurtainGrid> CurtainGrids
    {
      get
      {
        var grids = default(IEnumerable<ARDB.CurtainGrid>);
        switch (Value)
        {
          case ARDB.CurtainSystem curtainSystem: grids = curtainSystem.CurtainGrids?.Cast<ARDB.CurtainGrid>(); break;
          case ARDB.ExtrusionRoof extrusionRoof: grids = extrusionRoof.CurtainGrids?.Cast<ARDB.CurtainGrid>(); break;
          case ARDB.FootPrintRoof footPrintRoof: grids = footPrintRoof.CurtainGrids?.Cast<ARDB.CurtainGrid>(); break;
          case ARDB.Wall wall: grids = wall.CurtainGrid is null ? null : Enumerable.Repeat(wall.CurtainGrid, 1); break;
          default: return new CurtainGrid[0];
        }

        return grids.Select(x => new CurtainGrid(Value, x)).ToArray();
      }
    }
  }

  [Kernel.Attributes.Name("Host Type")]
  public interface IGH_HostObjectType : IGH_ElementType { }

  [Kernel.Attributes.Name("Host Type")]
  public class HostObjectType : ElementType, IGH_HostObjectType
  {
    protected override Type ValueType => typeof(ARDB.HostObjAttributes);
    public new ARDB.HostObjAttributes Value => base.Value as ARDB.HostObjAttributes;

    public HostObjectType() { }
    protected internal HostObjectType(ARDB.HostObjAttributes type) : base(type) { }

    public CompoundStructure CompoundStructure
    {
      get => Value is ARDB.HostObjAttributes type ? new CompoundStructure(Document, type.GetCompoundStructure()) : default;
      set
      {
        if (value is object && Value is ARDB.HostObjAttributes type)
          type.SetCompoundStructure(value.Value);
      }
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Host")]
  public interface IGH_HostObject : IGH_InstanceElement { }

  [Kernel.Attributes.Name("Host")]
  public class HostObject : InstanceElement, IGH_HostObject
  {
    protected override Type ValueType => typeof(DB.HostObject);
    public new DB.HostObject Value => base.Value as DB.HostObject;

    public HostObject() { }
    protected internal HostObject(DB.HostObject host) : base(host) { }

    public override Plane Location
    {
      get
      {
        if (Value is DB.HostObject host && !(host.Location is DB.LocationPoint) && !(host.Location is DB.LocationCurve))
        {
          if (host.GetSketch() is DB.Sketch sketch)
          {
            var center = Point3d.Origin;
            var count = 0;
            foreach (var curveArray in sketch.Profile.Cast<DB.CurveArray>())
            {
              foreach (var curve in curveArray.Cast<DB.Curve>())
              {
                count++;
                center += curve.Evaluate(0.0, normalized: true).ToPoint3d();
                count++;
                center += curve.Evaluate(1.0, normalized: true).ToPoint3d();
              }
            }
            center /= count;

            var hostLevelId = host.LevelId;
            if (hostLevelId == DB.ElementId.InvalidElementId)
              hostLevelId = host.get_Parameter(DB.BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM)?.AsElementId() ?? hostLevelId;

            if (host.Document.GetElement(hostLevelId) is DB.Level level)
              center.Z = level.GetHeight() * Revit.ModelUnits;

            var plane = sketch.SketchPlane.GetPlane().ToPlane();
            var origin = center;
            var xAxis = plane.XAxis;
            var yAxis = plane.YAxis;

            if (host is DB.Wall)
            {
              xAxis = -plane.XAxis;
              yAxis = plane.ZAxis;
            }

            if (host is DB.FootPrintRoof)
              origin.Z += host.get_Parameter(DB.BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM).AsDouble() * Revit.ModelUnits;

            if (host is DB.ExtrusionRoof)
            {
              origin.Z += host.get_Parameter(DB.BuiltInParameter.ROOF_CONSTRAINT_OFFSET_PARAM).AsDouble() * Revit.ModelUnits;
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
        var grids = default(IEnumerable<DB.CurtainGrid>);
        switch (Value)
        {
          case DB.CurtainSystem curtainSystem: grids = curtainSystem.CurtainGrids?.Cast<DB.CurtainGrid>(); break;
          case DB.ExtrusionRoof extrusionRoof: grids = extrusionRoof.CurtainGrids?.Cast<DB.CurtainGrid>(); break;
          case DB.FootPrintRoof footPrintRoof: grids = footPrintRoof.CurtainGrids?.Cast<DB.CurtainGrid>(); break;
          case DB.Wall wall: grids = wall.CurtainGrid is null ? null : Enumerable.Repeat(wall.CurtainGrid, 1); break;
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
    protected override Type ValueType => typeof(DB.HostObjAttributes);
    public new DB.HostObjAttributes Value => base.Value as DB.HostObjAttributes;

    public HostObjectType() { }
    protected internal HostObjectType(DB.HostObjAttributes type) : base(type) { }

    public CompoundStructure CompoundStructure
    {
      get => Value is DB.HostObjAttributes type ? new CompoundStructure(Document, type.GetCompoundStructure()) : default;
      set
      {
        if (value is object && Value is DB.HostObjAttributes type)
          type.SetCompoundStructure(value.Value);
      }
    }
  }
}

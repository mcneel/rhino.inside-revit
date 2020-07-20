using System;
using System.Linq;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public interface IGH_HostObject : IGH_InstanceElement
  {
    DB.HostObject APIHostObject { get; }
  }

  public class HostObject : InstanceElement, IGH_HostObject
  {
    public override string TypeDescription => "Represents a Revit host element";
    protected override Type ScriptVariableType => typeof(DB.HostObject);
    public DB.HostObject APIHostObject => IsValid ? Document.GetElement(Value) as DB.HostObject : default;
    public static explicit operator DB.HostObject(HostObject value) => value?.APIHostObject;

    public HostObject() { }
    public HostObject(DB.HostObject host) : base(host) { }

    public override Plane Location
    {
      get
      {
        var host = APIHostObject;

        if (!(host.Location is DB.LocationPoint) && !(host.Location is DB.LocationCurve))
        {
          if (host.GetFirstDependent<DB.Sketch>() is DB.Sketch sketch)
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
              center.Z = level.Elevation * Revit.ModelUnits;

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

    public override Vector3d Orientation => base.Orientation;
  }

  public interface IGH_HostObjectType : IGH_ElementType
  {
    DB.HostObjAttributes HostObjAttributes { get; }
  }

  public class HostObjectType : ElementType, IGH_HostObjectType
  {
    public override string TypeDescription => "Represents a Revit host element type";
    protected override Type ScriptVariableType => typeof(DB.HostObjAttributes);
    public DB.HostObjAttributes HostObjAttributes => IsValid ? Document.GetElement(Value) as DB.HostObjAttributes : default;
    public static explicit operator DB.HostObjAttributes(HostObjectType value) => value?.HostObjAttributes;

    public HostObjectType() { }
    public HostObjectType(DB.HostObjAttributes type) : base(type) { }
  }
}

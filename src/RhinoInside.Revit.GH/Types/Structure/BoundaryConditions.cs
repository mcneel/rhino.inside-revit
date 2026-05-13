using System;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Boundary Conditions")]
  public class BoundaryConditions : GeometricElement
  {
    protected override Type ValueType => typeof(ARDB.Structure.BoundaryConditions);
    protected virtual string State => default;

    public new ARDB.Structure.BoundaryConditions Value => base.Value as ARDB.Structure.BoundaryConditions;

    public BoundaryConditions() { }
    public BoundaryConditions(ARDB.Structure.BoundaryConditions boundaryConditions) : base(boundaryConditions) { }

    public override Plane Location
    {
      get
      {
        if (Value?.GetDegreesOfFreedomCoordinateSystem() is ARDB.Transform coordSystem)
          return new Plane(BoundingBox.Center, coordSystem.BasisX.ToVector3d(), coordSystem.BasisY.ToVector3d());

        return NaN.Plane;
      }
    }

    protected Rhino.Display.PointStyle GetPointStyle()
    {
      switch (State)
      {
        default:
        case "Fixed": return Rhino.Display.PointStyle.Square;
        case "Pinned": return Rhino.Display.PointStyle.Clover;
        case "Roller": return Rhino.Display.PointStyle.Circle;
        case "User": return Rhino.Display.PointStyle.Asterisk;
      }
    }

    protected Rhino.Display.PointStyle GetPointStyle(Rhino.Display.RhinoViewport viewport, out double spacing)
    {
      viewport.GetWorldToScreenScale(BoundingBox.Center, out var pixelsPerUnit);
      spacing = ARDB.Structure.StructuralSettings.GetStructuralSettings(Document).BoundaryConditionAreaAndLineSymbolSpacing * Revit.ModelUnits;
      spacing *= 500.0 / pixelsPerUnit;

      return GetPointStyle();
    }
  }

  [Kernel.Attributes.Name("Point Boundary Conditions")]
  public class PointBoundaryConditions : BoundaryConditions
  {
    protected override Type ValueType => typeof(PointBoundaryConditions);
    protected override string State => Value.get_Parameter(ARDB.BuiltInParameter.BOUNDARY_PARAM_PRESET).AsValueString();
    public PointBoundaryConditions() { }
    public PointBoundaryConditions(ARDB.Structure.BoundaryConditions boundaryConditions) : base(boundaryConditions) { }

    #region Location
    public override Point3d Position => Value.Point.ToPoint3d();
    #endregion

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value.Point.ToPoint3d() is Point3d position)
        args.Pipeline.DrawPoint(position, GetPointStyle(), CentralSettings.PreviewPointRadius, args.Color);
    }
    #endregion
  }

  [Kernel.Attributes.Name("Line Boundary Conditions")]
  public class LineBoundaryConditions : BoundaryConditions
  {
    protected override Type ValueType => typeof(LineBoundaryConditions);
    public LineBoundaryConditions() { }
    public LineBoundaryConditions(ARDB.Structure.BoundaryConditions boundaryConditions) : base(boundaryConditions) { }
    protected override string State => Value.get_Parameter(ARDB.BuiltInParameter.BOUNDARY_PARAM_PRESET_LINEAR).AsValueString();

    #region Location
    public override Curve Curve => Value?.GetCurve().ToCurve();
    #endregion

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.Structure.BoundaryConditions conditions)
      {
        var style = GetPointStyle(args.Viewport, out var spacing);

        if (conditions.GetCurve().ToCurve() is Curve curve)
        {
          var segments = (int) Math.Ceiling(curve.GetLength() / spacing);
          if (curve.DivideByCount(Math.Min(512, segments), true, out var points) is object)
            args.Pipeline.DrawPoints(points.Skip(points.Length > 2 ? 1 : 0), style, CentralSettings.PreviewPointRadius, args.Color);
        }
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Area Boundary Conditions")]
  public class AreaBoundaryConditions : BoundaryConditions
  {
    protected override Type ValueType => typeof(AreaBoundaryConditions);
    public AreaBoundaryConditions() { }
    public AreaBoundaryConditions(ARDB.Structure.BoundaryConditions boundaryConditions) : base(boundaryConditions) { }
    protected override string State => Value.get_Parameter(ARDB.BuiltInParameter.BOUNDARY_PARAM_PRESET_AREA).AsValueString();

    #region Location
    public override Brep TrimmedSurface
    {
      get
      {
        var loops = Value.GetLoops().Select(GeometryDecoder.ToPolyCurve).ToArray();
        var plane = Location;
        if (loops.Length > 0)
        {
          var loopsBox = BoundingBox.Empty;
          foreach (var loop in loops)
          {
            if (loop.ClosedCurveOrientation(plane) == CurveOrientation.Clockwise)
              loop.Reverse();

            loopsBox.Union(loop.GetBoundingBox(plane));
          }

          var planeSurface = new PlaneSurface
          (
            plane,
            new Interval(loopsBox.Min.X, loopsBox.Max.X),
            new Interval(loopsBox.Min.Y, loopsBox.Max.Y)
          );

          return planeSurface.CreateTrimmedSurface(loops, GeometryTolerance.Model.VertexTolerance);
        }

        return null;
      }
    }
    #endregion

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.Structure.BoundaryConditions conditions)
      {
        var style = GetPointStyle(args.Viewport, out var spacing);

        foreach (var curve in conditions.GetLoops().SelectMany(GeometryDecoder.ToCurveMany))
        {
          var segments = (int) Math.Ceiling(curve.GetLength() / spacing);
          if (curve.DivideByCount(Math.Min(512, segments), true, out var points) is object)
            args.Pipeline.DrawPoints(points.Skip(points.Length > 2 ? 1 : 0), style, CentralSettings.PreviewPointRadius, args.Color);
        }
      }
    }
    #endregion
  }
}

using System;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
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
      if (Value?.GetCurve().ToCurve() is Curve curve)
      {
        var segments = (int) Math.Ceiling(curve.GetLength() / (ARDB.Structure.StructuralSettings.GetStructuralSettings(Document).BoundaryConditionAreaAndLineSymbolSpacing * Revit.ModelUnits));
        curve.DivideByCount(segments, true, out Point3d[] points);

        if (points != null)
          args.Pipeline.DrawPoints(points, GetPointStyle(), CentralSettings.PreviewPointRadius, args.Color);
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
        var loops = Value.GetLoops().First().Select(x => x.ToCurve());
        return Brep.CreateEdgeSurface(loops);
      }
    }
    #endregion

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (GeometryDecoder.ToCurve(Value?.GetLoops().First()) is Curve curve)
      {
        var segments = (int) Math.Ceiling(curve.GetLength() / (ARDB.Structure.StructuralSettings.GetStructuralSettings(Document).BoundaryConditionAreaAndLineSymbolSpacing * Revit.ModelUnits));
        curve.DivideByCount(segments, true, out var points);

        if (points != null)
          args.Pipeline.DrawPoints(points, GetPointStyle(), CentralSettings.PreviewPointRadius, args.Color);
      }
    }
    #endregion
  }
}

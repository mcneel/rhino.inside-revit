using System;
using Grasshopper.Kernel;
using Grasshopper;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using RhinoInside.Revit.Convert.Geometry;
using System.Linq;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Boundary Conditions")]
  public class BoundaryConditions : GeometricElement
  {
    protected override Type ValueType => typeof(ARDB.Structure.BoundaryConditions);
    public ARDB.Structure.BoundaryConditionsState BoundaryConditionsState
    {
      get
      {
        switch (Value?.GetBoundaryConditionsType())
        {
          default:
          case ARDB.Structure.BoundaryConditionsType.Point:
          return (ARDB.Structure.BoundaryConditionsState) Value.get_Parameter(ARDB.BuiltInParameter.BOUNDARY_PARAM_PRESET).AsInteger();
          case ARDB.Structure.BoundaryConditionsType.Line:
          return (ARDB.Structure.BoundaryConditionsState) Value.get_Parameter(ARDB.BuiltInParameter.BOUNDARY_PARAM_PRESET_LINEAR).AsInteger();
          case ARDB.Structure.BoundaryConditionsType.Area:
          return (ARDB.Structure.BoundaryConditionsState) Value.get_Parameter(ARDB.BuiltInParameter.BOUNDARY_PARAM_PRESET_AREA).AsInteger();
        }
      }
    }
    public new ARDB.Structure.BoundaryConditions Value => base.Value as ARDB.Structure.BoundaryConditions;

    public BoundaryConditions() { }
    public BoundaryConditions(ARDB.Structure.BoundaryConditions boundaryConditions) : base(boundaryConditions) { }

    public Rhino.Display.PointStyle GetPointStyle()
    {
      switch (BoundaryConditionsState)
      {
        default:
        case ARDB.Structure.BoundaryConditionsState.Fixed: return Rhino.Display.PointStyle.Square;
        case ARDB.Structure.BoundaryConditionsState.Pinned: return Rhino.Display.PointStyle.Clover;
        case ARDB.Structure.BoundaryConditionsState.Roller: return Rhino.Display.PointStyle.Circle;
        case ARDB.Structure.BoundaryConditionsState.User: return Rhino.Display.PointStyle.Asterisk;
      }
    }

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      base.DrawViewportWires(args);
      var structuralSettings = ARDB.Structure.StructuralSettings.GetStructuralSettings(Revit.ActiveUIApplication.ActiveUIDocument.Document);
      double spacing = structuralSettings.BoundaryConditionAreaAndLineSymbolSpacing * Revit.ModelUnits;
      var boundaryType = Value.GetBoundaryConditionsType();
      Point3d[] points = null;

      switch (boundaryType)
      {
        case ARDB.Structure.BoundaryConditionsType.Point:
          if (Value.Point.ToPoint3d() is Point3d position)
            args.Pipeline.DrawPoint(position,this.GetPointStyle(),CentralSettings.PreviewPointRadius, args.Color);
          return;

        case ARDB.Structure.BoundaryConditionsType.Line:
          {
            if (Value?.GetCurve().ToCurve() is Curve curve)
              curve.DivideByCount((int) Math.Ceiling(curve.GetLength() / spacing), true, out points);
            break;
          }

        case ARDB.Structure.BoundaryConditionsType.Area:
          {
            if (GeometryDecoder.ToCurve(Value?.GetLoops().First()) is Curve curve)
              curve.DivideByCount((int) Math.Ceiling(curve.GetLength() / spacing), true, out points);
            break;
          }
      }

      if (points != null && points.Length > 0)
        args.Pipeline.DrawPoints(points, this.GetPointStyle(), CentralSettings.PreviewPointRadius, args.Color);
    }
    #endregion
  }

  //[Kernel.Attributes.Name("Point Boundary Conditions")]
  //public class PointBoundaryConditions : BoundaryConditions
  //{
  //  protected override Type ValueType => typeof(PointBoundaryConditions);
  //  public override ARDB.Structure.BoundaryConditionsState BoundaryConditionsState =>
  //    (ARDB.Structure.BoundaryConditionsState) Value.get_Parameter(ARDB.BuiltInParameter.BOUNDARY_PARAM_PRESET).AsInteger();
  //  public PointBoundaryConditions() { }
  //  public PointBoundaryConditions(ARDB.Structure.BoundaryConditions boundaryConditions) : base(boundaryConditions) { }

  //  #region IGH_PreviewData
  //  protected override void DrawViewportWires(GH_PreviewWireArgs args)
  //  {
  //    if (Value.Point.ToPoint3d() is Point3d position)
  //      args.Pipeline.DrawPoint(position, this.GetPointStyle(), CentralSettings.PreviewPointRadius, args.Color);
  //  }
  //  #endregion
  //}

  //[Kernel.Attributes.Name("Line Boundary Conditions")]
  //public class LineBoundaryConditions : BoundaryConditions
  //{
  //  protected override Type ValueType => typeof(LineBoundaryConditions);
  //  public LineBoundaryConditions() { }
  //  public LineBoundaryConditions(ARDB.Structure.BoundaryConditions boundaryConditions) : base(boundaryConditions) { }
  //  public override ARDB.Structure.BoundaryConditionsState BoundaryConditionsState => (ARDB.Structure.BoundaryConditionsState) Value.get_Parameter(ARDB.BuiltInParameter.BOUNDARY_PARAM_PRESET_LINEAR).AsInteger();

  //  #region IGH_PreviewData
  //  protected override void DrawViewportWires(GH_PreviewWireArgs args)
  //  {
  //    if (Value?.GetCurve().ToCurve() is Curve curve)
  //    {
  //      var segments = (int)Math.Ceiling(curve.GetLength() /(ARDB.Structure.StructuralSettings.GetStructuralSettings(Revit.ActiveUIApplication.ActiveUIDocument.Document).BoundaryConditionAreaAndLineSymbolSpacing * Revit.ModelUnits));
  //      curve.DivideByCount(segments, true, out Point3d[] points);

  //      if (points != null) 
  //        args.Pipeline.DrawPoints(points, this.GetPointStyle(), CentralSettings.PreviewPointRadius, args.Color);
  //    }
  //  }
  //  #endregion
  //}

  //[Kernel.Attributes.Name("Area Boundary Conditions")]
  //public class AreaBoundaryConditions : BoundaryConditions
  //{
  //  protected override Type ValueType => typeof(AreaBoundaryConditions);
  //  public AreaBoundaryConditions() { }
  //  public AreaBoundaryConditions(ARDB.Structure.BoundaryConditions boundaryConditions) : base(boundaryConditions) { }
  //  public override ARDB.Structure.BoundaryConditionsState BoundaryConditionsState => (ARDB.Structure.BoundaryConditionsState) Value.get_Parameter(ARDB.BuiltInParameter.BOUNDARY_PARAM_PRESET_AREA).AsInteger();

  //  #region IGH_PreviewData
  //  protected override void DrawViewportWires(GH_PreviewWireArgs args)
  //  {
  //    if (GeometryDecoder.ToCurve(Value?.GetLoops().First()) is Curve curve)
  //    {
  //      var segments = (int) Math.Ceiling(curve.GetLength() / (ARDB.Structure.StructuralSettings.GetStructuralSettings(Revit.ActiveUIApplication.ActiveUIDocument.Document).BoundaryConditionAreaAndLineSymbolSpacing * Revit.ModelUnits));
  //      curve.DivideByCount(segments, true, out Point3d[] points);

  //      if (points != null)
  //        args.Pipeline.DrawPoints(points, this.GetPointStyle(), CentralSettings.PreviewPointRadius, args.Color);
  //    }
  //  }
  //  #endregion
  //}
}


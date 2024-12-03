using System;
using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Load Element")]
  public class LoadElement : GeometricElement
  {
    protected override Type ValueType => typeof(ARDB.Structure.LoadBase);
    public new ARDB.Structure.LoadBase Value => base.Value as ARDB.Structure.LoadBase;

    public LoadElement() { }
    public LoadElement(ARDB.Structure.LoadBase element) : base(element) { }
  }
}

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Point Load")]
  public class PointLoad : LoadElement
  {
    protected override Type ValueType => typeof(PointLoad);
    public new ARDB.Structure.PointLoad Value => base.Value as ARDB.Structure.PointLoad;

    public PointLoad() { }
    public PointLoad(ARDB.Structure.PointLoad element) : base(element) { }

    #region Location
    public override Point3d Position => Value?.Point.ToPoint3d() ?? NaN.Point3d;
    #endregion

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var position = Position;
      if (position.IsValid)
        args.Pipeline.DrawPoint(position, CentralSettings.PreviewPointStyle, CentralSettings.PreviewPointRadius, args.Color);
    }
    #endregion
  }
}

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Line Load")]
  public class LineLoad : LoadElement
  {
    protected override Type ValueType => typeof(LineLoad);
    public new ARDB.Structure.LineLoad Value => base.Value as ARDB.Structure.LineLoad;

    public LineLoad() { }
    public LineLoad(ARDB.Structure.LineLoad element) : base(element) { }

    #region Location
    public override Curve Curve => Value?.GetCurve().ToCurve() ?? default;
    #endregion

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Curve is Curve curve)
        args.Pipeline.DrawCurve(curve,  args.Color, args.Thickness);
    }
    #endregion
  }
}

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Area Load")]
  public class AreaLoad : LoadElement
  {
    protected override Type ValueType => typeof(AreaLoad);
    public new ARDB.Structure.AreaLoad Value => base.Value as ARDB.Structure.AreaLoad;

    public AreaLoad() { }
    public AreaLoad(ARDB.Structure.AreaLoad element) : base(element) { }
  }
}

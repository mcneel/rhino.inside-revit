using System;
using System.Linq;
using Rhino.Geometry;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;
  using Convert.Geometry;

#if REVIT_2023
  using ARDB_Structure_AnalyticalElement = ARDB.Structure.AnalyticalElement;
#else
  using ARDB_Structure_AnalyticalElement = ARDB.Structure.AnalyticalModel;
#endif

  [Kernel.Attributes.Name("Analytical Element")]
  public class AnalyticalElement : GeometricElement
  {
    protected override Type ValueType => typeof(ARDB_Structure_AnalyticalElement);
    public new ARDB_Structure_AnalyticalElement Value => base.Value as ARDB_Structure_AnalyticalElement;

    public AnalyticalElement() { }
    public AnalyticalElement(ARDB_Structure_AnalyticalElement element) : base(element) { }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB_Structure_AnalyticalElement element)
        {
          var (origin, basisX, basisY) = element.GetLocation();
          return new Plane(origin.ToPoint3d(), basisX.Direction.ToVector3d(), basisY.Direction.ToVector3d());
        }

        return NaN.Plane;
      }
    }

    public override Curve Curve
    {
      get => Value?.IsSingleCurve() is true ? Value.GetCurve().ToCurve() : default;
      set => throw new InvalidOperationException("Curve can not be set for this element.");
    }

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      base.DrawViewportWires(args);

      if (args.Color == System.Drawing.Color.FromArgb(args.Color.A, GH_Document.DefaultSelectedPreviewColour))
      {
        var location = Location;
        if (location.IsValid)
        {
          args.Pipeline.DrawDirectionArrow(location.Origin, location.XAxis, System.Drawing.Color.DarkRed);
          args.Pipeline.DrawDirectionArrow(location.Origin, location.YAxis, System.Drawing.Color.DarkGreen);
          args.Pipeline.DrawDirectionArrow(location.Origin, location.ZAxis, System.Drawing.Color.DarkBlue);
        }
      }
    }
    #endregion
  }
}

namespace RhinoInside.Revit.GH.Types
{
#if REVIT_2023
  using ARDB_Structure_AnalyticalMember = ARDB.Structure.AnalyticalMember;
#else
  using ARDB_Structure_AnalyticalMember = ARDB.Structure.AnalyticalModelStick;
#endif

  [Kernel.Attributes.Name("Analytical Member")]
  public class AnalyticalMember : AnalyticalElement
  {
    protected override Type ValueType => typeof(ARDB_Structure_AnalyticalMember);
    public new ARDB_Structure_AnalyticalMember Value => base.Value as ARDB_Structure_AnalyticalMember;

    public AnalyticalMember() { }
    public AnalyticalMember(ARDB_Structure_AnalyticalMember element) : base(element) { }
  }
}

namespace RhinoInside.Revit.GH.Types
{
#if REVIT_2023
  using ARDB_Structure_AnalyticalSurfaceBase = ARDB.Structure.AnalyticalSurfaceBase;
#else
  using ARDB_Structure_AnalyticalSurfaceBase = ARDB.Structure.AnalyticalModelSurface;
#endif

  [Kernel.Attributes.Name("Analytical Surface")]
  public class AnalyticalSurface : AnalyticalElement
  {
    protected override Type ValueType => typeof(ARDB_Structure_AnalyticalSurfaceBase);
    public new ARDB_Structure_AnalyticalSurfaceBase Value => base.Value as ARDB_Structure_AnalyticalSurfaceBase;

    public AnalyticalSurface() { }
    public AnalyticalSurface(ARDB_Structure_AnalyticalSurfaceBase element) : base(element) { }

    protected static ARDB.CurveLoop GetOuterContour(ARDB_Structure_AnalyticalSurfaceBase surface)
    {
#if REVIT_2023
      return surface?.GetOuterContour();
#else
      return surface?.GetLoops(ARDB.Structure.AnalyticalLoopType.External).FirstOrDefault();
#endif
    }

    public override Brep TrimmedSurface
    {
      get
      {
        if (Value is ARDB_Structure_AnalyticalSurfaceBase)
        {
          using (var options = new ARDB.Options())
          {
            var geometry = Value.get_Geometry(options);
            return geometry.OfType<ARDB.Solid>().FirstOrDefault().ToBrep();
          }
        }

        return null;
      }
    }
  }
}

namespace RhinoInside.Revit.GH.Types
{
#if REVIT_2023
  using ARDB_Structure_AnalyticalPanel = ARDB.Structure.AnalyticalPanel;
#else
  using ARDB_Structure_AnalyticalPanel = ARDB.Structure.AnalyticalModelSurface;
#endif

  [Kernel.Attributes.Name("Analytical Panel")]
  public class AnalyticalPanel : AnalyticalSurface
  {
    protected override Type ValueType => typeof(ARDB_Structure_AnalyticalPanel);
    public new ARDB_Structure_AnalyticalPanel Value => base.Value as ARDB_Structure_AnalyticalPanel;

    public AnalyticalPanel() { }
    public AnalyticalPanel(ARDB_Structure_AnalyticalPanel element) : base(element) { }
  }
}

namespace RhinoInside.Revit.GH.Types
{
#if REVIT_2023
  using ARDB_Structure_AnalyticalOpening = ARDB.Structure.AnalyticalOpening;
#else
  using ARDB_Structure_AnalyticalOpening = ARDB.Structure.AnalyticalModelSurface;
#endif

  [Kernel.Attributes.Name("Analytical Opening")]
  public class AnalyticalOpening : AnalyticalSurface
  {
    protected override Type ValueType => typeof(ARDB_Structure_AnalyticalOpening);
    public new ARDB_Structure_AnalyticalOpening Value => base.Value as ARDB_Structure_AnalyticalOpening;

    public AnalyticalOpening() { }
    public AnalyticalOpening(ARDB_Structure_AnalyticalOpening element) : base(element) { }

    public override Brep TrimmedSurface
    {
      get
      {
        if (Value is ARDB_Structure_AnalyticalOpening)
        {
          var loops = new Curve[] { GetOuterContour(Value).ToPolyCurve() };
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
        }

        return null;
      }
    }
  }
}

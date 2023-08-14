using System;
using Rhino.Geometry;
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
      get => Value.IsSingleCurve() == true ? Value.GetCurve().ToCurve() : default;
      set => throw new InvalidOperationException("Curve can not be set for this element.");
    }
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
  }
}

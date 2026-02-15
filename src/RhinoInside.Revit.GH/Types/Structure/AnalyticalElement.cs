using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
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

#if REVIT_2023
    public bool IsEnabled => true;
#else
    public bool IsEnabled => Value?.IsEnabled() is true;
#endif

    public override Plane Location
    {
      get
      {
        if (Value is ARDB_Structure_AnalyticalElement element)
        {
          var (origin, basisX, basisY) = element.GetLocation();
          return new Plane
          (
            origin?.ToPoint3d() ?? NaN.Point3d,
            basisX.Direction?.ToVector3d() ?? NaN.Vector3d,
            basisY.Direction?.ToVector3d() ?? NaN.Vector3d
          );
        }

        return NaN.Plane;
      }
    }

    public override Point3d Position =>
#if !REVIT_2023
      Value?.IsSinglePoint() is true ? Value.GetPoint().ToPoint3d() :
#endif
      Location.Origin;

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

    #region Properties
    public ARDB.Structure.AnalyzeAs? AnalyzeAs
    {
#if REVIT_2023
      get => Value?.AnalyzeAs;
      set
      {
        if(value is object && Value is ARDB_Structure_AnalyticalElement element && element.AnalyzeAs != value.Value)
          element.AnalyzeAs = value.Value;
      }
#else
      get => Value?.GetAnalyzeAs();
      set
      {
        if (value is object && value != AnalyzeAs)
          Value?.SetAnalyzeAs(value.Value);
      }
#endif
    }

    public ARDB.Structure.AnalyticalStructuralRole? StructuralRole
    {
#if REVIT_2023
      get => Value?.StructuralRole;
      set
      {
        if (value is object && Value is ARDB_Structure_AnalyticalElement element && element.StructuralRole != value.Value)
          element.StructuralRole = value.Value;
      }
#else
      get => IsValid ? ARDB.Structure.AnalyticalStructuralRole.Unset : default;
      set => throw new Exceptions.RuntimeErrorException($"'{DisplayName}' does not support assignment of a user-specified structural role.");
#endif
    }
    #endregion

    public GraphicalElement[] PhysicalElements
    {
      get
      {
        if (!IsValid) return null;

#if REVIT_2023
        if (ARDB.Structure.AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(Document) is ARDB.Structure.AnalyticalToPhysicalAssociationManager manager)
        {
#if REVIT_2024
          return manager.GetAssociatedElementIds(Id).Select(GetElement<GraphicalElement>).ToArray();
#else
          var id = manager.GetAssociatedElementId(Id);
          if (id.IsValid())
            return new GraphicalElement[] { GetElement<GraphicalElement>(id) };
#endif
        }
#else
        var id = Value.GetElementId();
        if (id.IsValid())
          return new GraphicalElement[] { GetElement<GraphicalElement>(id) };
#endif

        return Array.Empty<GraphicalElement>();
      }
    }

    internal static void Associate(ISet<AnalyticalElement> analyticalElements, ISet<GraphicalElement> physicalElements)
    {
      var documents = physicalElements.Concat(analyticalElements).Select(x => x.Document).Distinct().ToArray();
      if (documents.Length == 0) return;
      if (documents.Length == 1)
      {
#if REVIT_2023
        if (ARDB.Structure.AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(documents[0]) is ARDB.Structure.AnalyticalToPhysicalAssociationManager manager)
        {
#if !REVIT_2024
          if (analyticalElements.Count > 1 || physicalElements.Count > 1)
            throw new Exceptions.RuntimeErrorException("Analytical elements do not support assignment of multiple model elements.");
#endif
          foreach (var element in analyticalElements.Concat(physicalElements))
          {
            if (manager.HasAssociation(element.Id))
              manager.RemoveAssociation(element.Id);
          }

          if (analyticalElements.Count > 0 && physicalElements.Count > 0)
          {
#if REVIT_2024
            manager.AddAssociation(analyticalElements.Select(x => x.Id).ToHashSet(), physicalElements.Select(x => x.Id).ToHashSet());
#else
            manager.AddAssociation(analyticalElements.First().Id, physicalElements.First().Id);
#endif
          }
        }
#else
        throw new Exceptions.RuntimeErrorException("Analytical elements do not support assignment of user-specified model elements.");
#endif
      }
      else throw new Exceptions.RuntimeErrorException("Invalid document");
    }
  }
}

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

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

    #region Location
#if REVIT_2023
    public override void SetCurve(Curve curve, bool keepJoins = false)
    {
      if (curve is object && Value is ARDB_Structure_AnalyticalMember member)
      {
        var newCurve = curve.ToCurve();
        if (!member.GetCurve().AlmostEquals(newCurve, GeometryTolerance.Internal.VertexTolerance))
        {
          member.SetCurve(newCurve);
          InvalidateGraphics();
        }
      }
    }
#endif
#endregion
  }
}

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

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
  using Convert.Geometry;

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

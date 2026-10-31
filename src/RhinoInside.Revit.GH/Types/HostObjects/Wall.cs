using System;
using System.Collections.Generic;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using RhinoInside.Revit.External.DB;

  [Kernel.Attributes.Name("Wall")]
  public sealed class Wall : HostObject,
    ISketchAccess,
    ICurtainGridsAccess
  {
    protected override Type ValueType => typeof(ARDB.Wall);
    public new ARDB.Wall Value => base.Value as ARDB.Wall;

    public Wall() { }
    public Wall(ARDB.Wall wall) : base(wall) { }

    #region Location
    public override Plane Location
    {
      get
      {
        if (Value?.Location is ARDB.LocationCurve locationCurve && locationCurve.Curve is ARDB.Curve curve)
        {
          var start = curve.GetEndPoint(CurveEnd.Start).ToPoint3d();
          var end = curve.GetEndPoint(CurveEnd.End).ToPoint3d();
          var axis = end - start;
          var origin = start + (axis * 0.5);
          var perp = axis.RightDirection(GeometryDecoder.Tolerance.DefaultTolerance);
          return new Plane(origin, axis, perp);
        }

        return base.Location;
      }
    }

    public override Vector3d FacingOrientation => Value?.Flipped == true ? -Location.YAxis : Location.YAxis;

    public double SlantAngle
    {
      get
      {
        if (Value is ARDB.Wall wall)
        {
#if REVIT_2021
          switch (wall.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).AsEnum<ARDB.WallCrossSection>())
          {
            case ARDB.WallCrossSection.Vertical: return 0.0;
            case ARDB.WallCrossSection.SingleSlanted: return wall.get_Parameter(ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL).AsDouble();
            case ARDB.WallCrossSection.Tapered: return 0.0;
          }
#else
          return 0.0;
#endif
        }

        return double.NaN;
      }
      set
      {
#if REVIT_2021
        if (Value is ARDB.Wall wall)
        {
          wall.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).Update((int) ARDB.WallCrossSection.SingleSlanted);
          wall.get_Parameter(ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL).Update(value);
        }
#endif
      }
    }

    public double TapperInteriorAngle
    {
      get
      {
        if (Value is ARDB.Wall wall)
        {
#if REVIT_2021
          switch (wall.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).AsEnum<ARDB.WallCrossSection>())
          {
            case ARDB.WallCrossSection.Vertical: return 0.0;
            case ARDB.WallCrossSection.SingleSlanted: return wall.get_Parameter(ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL).AsDouble();
#if REVIT_2022
            case ARDB.WallCrossSection.Tapered: return wall.get_Parameter(ARDB.BuiltInParameter.WALL_TAPERED_INTERIOR_INWARD_ANGLE).AsDouble();
#endif
          }
#else
          return 0.0;
#endif
        }

        return double.NaN;
      }
      set
      {
#if REVIT_2021
        if (Value is ARDB.Wall wall)
        {
          wall.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).Update((int) ARDB.WallCrossSection.Tapered);
#if REVIT_2022
          wall.get_Parameter(ARDB.BuiltInParameter.WALL_TAPERED_INTERIOR_INWARD_ANGLE).Update(value);
#endif
        }
#endif
      }
    }

    public double TapperExteriorAngle
    {
      get
      {
        if (Value is ARDB.Wall wall)
        {
#if REVIT_2021
          switch (wall.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).AsEnum<ARDB.WallCrossSection>())
          {
            case ARDB.WallCrossSection.Vertical: return 0.0;
            case ARDB.WallCrossSection.SingleSlanted: return wall.get_Parameter(ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL).AsDouble();
#if REVIT_2022
            case ARDB.WallCrossSection.Tapered: return wall.get_Parameter(ARDB.BuiltInParameter.WALL_TAPERED_EXTERIOR_INWARD_ANGLE).AsDouble();
#endif
          }
#else
          return 0.0;
#endif
        }
        return double.NaN;
      }
      set
      {
#if REVIT_2021
        if (Value is ARDB.Wall wall)
        {
          wall.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).Update((int) ARDB.WallCrossSection.Tapered);
#if REVIT_2022
          wall.get_Parameter(ARDB.BuiltInParameter.WALL_TAPERED_EXTERIOR_INWARD_ANGLE).Update(value);
#endif
        }
#endif
      }
    }

    public static bool IsValidCurve(Curve curve, out string log)
    {
      if (!curve.IsValidWithLog(out log)) return false;

      var tol = GeometryTolerance.Model;
#if REVIT_2020
      if
      (
        !(curve.IsLinear(tol.VertexTolerance) || curve.IsArc(tol.VertexTolerance) || curve.IsEllipse(tol.VertexTolerance)) ||
        !curve.TryGetPlane(out var axisPlane, tol.VertexTolerance) ||
        axisPlane.ZAxis.IsParallelTo(Vector3d.ZAxis) == 0
      )
      {
        log = "Curve should be a horizontal line, arc or ellipse curve.";
        return false;
      }
#else
      if
      (
        !(curve.IsLinear(tol.VertexTolerance) || curve.IsArc(tol.VertexTolerance)) ||
        !curve.TryGetPlane(out var axisPlane, tol.VertexTolerance) ||
        axisPlane.ZAxis.IsParallelTo(Vector3d.ZAxis) == 0
      )
      {
        log = "Curve should be a horizontal line or arc curve.";
        return false;
      }
#endif

      return true;
    }

    public override void SetCurve(Curve curve, bool keepJoins = false)
    {
      if (Value is ARDB.Wall wall && curve is object)
      {
        if (wall.Location is ARDB.LocationCurve locationCurve)
        {
          if (!IsValidCurve(curve, out var log))
            throw new Exceptions.RuntimeArgumentException(nameof(curve), log, curve);

          var tol = GeometryTolerance.Model;
          var newCurve = default(ARDB.Curve);
          switch (Curve)
          {
            case LineCurve _:
              if (curve.TryGetLine(out var valueLine, tol.VertexTolerance))
                newCurve = valueLine.ToLine();
              else
                throw new Exceptions.RuntimeArgumentException(nameof(curve), "Curve should be a line like curve.", curve);
              break;

            case ArcCurve _:
              if (curve.TryGetArc(out var valueArc, tol.VertexTolerance))
                newCurve = valueArc.ToArc();
              else
                throw new Exceptions.RuntimeArgumentException(nameof(curve), "Curve should be an arc like curve.", curve);
              break;

            case Curve _:
              if (curve.TryGetEllipse(out var _, tol.VertexTolerance))
                newCurve = curve.ToCurve();
              else
                throw new Exceptions.RuntimeArgumentException(nameof(curve), "Curve should be an ellipse like curve.", curve);
              break;
          }

          if (!locationCurve.Curve.AlmostEquals(newCurve, GeometryTolerance.Internal.VertexTolerance))
          {
            using (!keepJoins ? ElementJoins.DisableJoinsScope(wall) : default)
              locationCurve.Curve = newCurve;

            InvalidateGraphics();
          }
        }
        else base.SetCurve(curve, keepJoins);
      }
    }

    public override Surface Surface
    {
      get
      {
        if (Curve is Curve axis)
        {
          var location = Location;
          var origin = location.Origin;
          var domain = Domain;

          var axis0 = axis.DuplicateCurve();
          axis0.Translate(new Vector3d(0.0, 0.0, domain.T0 - origin.Z));

          var axis1 = axis.DuplicateCurve();
          axis1.Translate(new Vector3d(0.0, 0.0, domain.T1 - origin.Z));

#if REVIT_2021
          if (Value.get_Parameter(ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL) is ARDB.Parameter slantAngle)
          {
            var angle = slantAngle.AsDouble();
            if (angle != 0.0)
            {
              var offset0 = (domain.T0 - origin.Z) * Math.Tan(angle);
              var offset1 = (domain.T1 - origin.Z) * Math.Tan(angle);

              // We need to use `ARDB.Curve.CreateOffset` to obtain same kind of "offset"
              // else the resulting surface is not parameterized like Revit.
              // This is important to evaluate rectangular Opening and WallSweep on that surface.
              var o0 = axis0.ToCurve().CreateOffset(GeometryEncoder.ToInternalLength(offset0), ERDB.UnitXYZ.BasisZ);
              var o1 = axis1.ToCurve().CreateOffset(GeometryEncoder.ToInternalLength(offset1), ERDB.UnitXYZ.BasisZ);

              axis0 = o0.ToCurve();
              axis1 = o1.ToCurve();
            }
          }
#endif

          if (NurbsSurface.CreateRuledSurface(axis0, axis1) is Surface surface)
          {
            surface.SetDomain(0, axis.Domain);
            surface.SetDomain(1, new Interval(domain.T0 - origin.Z, domain.T1 - origin.Z));
            return surface;
          }
        }

        return null;
      }
    }

    //public override Brep PolySurface
    //{
    //  get
    //  {
    //    if (Value?.CurtainGrid is ARDB.CurtainGrid grid && Surface is Surface surface)
    //    {
    //      var loops = grid.GetCurtainCells().SelectMany(x => x.CurveLoops.ToCurveMany()).ToArray();
    //      return surface.CreateTrimmedSurface(loops, GeometryObjectTolerance.Model.VertexTolerance);
    //    }

    //    return base.PolySurface;
    //  }
    //}
    #endregion

    #region IHostElementAccess
    public override GraphicalElement HostElement
    {
      get
      {
        if (Value is ARDB.Wall wall)
        {
          if (wall.IsStackedWallMember) return GetElement<GraphicalElement>(wall.StackedWallOwnerId);

          // Search geometrically for an `ARDB.HostObject`
          if (wall.GetOutline() is ARDB.Outline outline)
          {
            using (var collector = new ARDB.FilteredElementCollector(Document))
            {
              // The wall may be a panel on any `HostObject` that support curtain grids.
              var elementCollector = collector.OfClass(typeof(ARDB.HostObject));

              // Element should be at the same Design Option
              if (wall.DesignOption is ARDB.DesignOption designOption)
                elementCollector = elementCollector.WherePasses(new ARDB.ElementDesignOptionFilter(designOption.Id));
              else
                elementCollector = elementCollector.WherePasses(new ARDB.ElementDesignOptionFilter(ARDB.ElementId.InvalidElementId));

              if (wall.Category?.Parent is ARDB.Category hostCategory)
                elementCollector = elementCollector.OfCategoryId(hostCategory.Id);

              var bboxFilter = new ARDB.BoundingBoxIntersectsFilter(outline, wall.Document.Application.VertexTolerance);
              elementCollector = elementCollector.WherePasses(bboxFilter);

              using (var includesFilter = ElementFilters.InclusionFilter(wall))
              {
                foreach (ARDB.HostObject hostObject in elementCollector)
                {
                  if (hostObject.Id == wall.Id)
                    continue;

                  // Necessary to found Panel walls in a Curtain Wall
                  if (hostObject.GetDependentElements(includesFilter).Count > 0)
                    return GetElement<GraphicalElement>(hostObject);

                  // Necessary to found Walls embeded in an other Wall
                  if (hostObject.FindInserts(true, false, true, false).Contains(wall.Id))
                    return GetElement<GraphicalElement>(hostObject);
                }
              }
            }
          }
        }

        return base.HostElement;
      }
    }
    #endregion

    #region ISketchAccess
    public Sketch Sketch => GetElement<Sketch>(Value?.GetSketchId());

    public override Plane SketchPlane
    {
      get
      {
        var plane = Location;
        var axis = plane.XAxis;
        plane = new Plane(plane.Origin, -axis, plane.ZAxis);
        plane.Rotate(SlantAngle, axis);
        return plane;
      }
    }
    #endregion

    #region ICurtainGridsAccess
    public IList<CurtainGrid> CurtainGrids => Value is ARDB.Wall wall ?
      wall.CurtainGrid is ARDB.CurtainGrid grid ?
      new CurtainGrid[] { new CurtainGrid(this, grid, 0) } :
      new CurtainGrid[] { } :
      default;
    #endregion

    #region Joins
    public bool? IsJoinAllowedAtStart
    {
      get =>  Value is ARDB.Wall wall ?
        ARDB.WallUtils.IsWallJoinAllowedAtEnd(wall, 0) :
        default;

      set
      {
        if (value is object && Value is ARDB.Wall wall && value != IsJoinAllowedAtEnd)
        {
          InvalidateGraphics();

          if (value == true)
            ARDB.WallUtils.AllowWallJoinAtEnd(wall, 0);
          else
            ARDB.WallUtils.DisallowWallJoinAtEnd(wall, 0);
        }
      }
    }

    public bool? IsJoinAllowedAtEnd
    {
      get => Value is ARDB.Wall wall ?
        ARDB.WallUtils.IsWallJoinAllowedAtEnd(wall, 1) :
        default;

      set
      {
        if (value is object && Value is ARDB.Wall wall && value != IsJoinAllowedAtEnd)
        {
          InvalidateGraphics();

          if (value == true)
            ARDB.WallUtils.AllowWallJoinAtEnd(wall, 1);
          else
            ARDB.WallUtils.DisallowWallJoinAtEnd(wall, 1);
        }
      }
    }
    #endregion

    #region Structure
    public override bool Structural => Value?.get_Parameter(ARDB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).AsBoolean() is true;
    #endregion
  }
}

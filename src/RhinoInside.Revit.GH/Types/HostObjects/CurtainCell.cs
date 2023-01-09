using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Curtain Cell")]
  public class CurtainCell : DocumentObject,
    IGH_PreviewData
  {
    public new ARDB.CurtainCell Value => base.Value as ARDB.CurtainCell;
    public Panel Panel { get; private set; }

    public CurtainCell() : base() { }
    public CurtainCell(Panel panel, ARDB.CurtainCell value) : base(panel.Document, value)
    {
      Panel = panel;
    }

    #region DocumentObject
    public override string DisplayName =>  "Curtain Cell";
    #endregion

    #region IGH_Goo
    public override bool CastTo<Q>(out Q target)
    {
      target = default;

      if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
      {
        target = (Q) (object) new GH_Plane(Location);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
      {
        var mesh = Mesh;
        if (mesh is null) return false;

        target = (Q) (object) new GH_Mesh(mesh);
        return target is object;
      }

      //if (typeof(Q).IsAssignableFrom(typeof(GH_Surface)))
      //{
      //  var trimmedSurface = TrimmedSurface;
      //  if (trimmedSurface is null) return false;

      //  target = (Q) (object) new GH_Surface(trimmedSurface);
      //  return target is object;
      //}

      if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
      {
        var polySurface = PolySurface;
        if (polySurface is null) return false;

        target = (Q) (object) new GH_Brep(polySurface);
        return target is object;
      }

      return base.CastTo(out target);
    }
    #endregion

    #region IGH_PreviewData
    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      foreach (var curve in PlanarizedCurveLoops ?? EmptyCurves)
        args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
    }

    void IGH_PreviewData.DrawViewportMeshes(GH_PreviewMeshArgs args)
    {
      if (Mesh is Mesh mesh)
        args.Pipeline.DrawMeshShaded(mesh, args.Material);
    }

    private BoundingBox? _ClippingBox;
    BoundingBox IGH_PreviewData.ClippingBox
    {
      get
      {
        if (!_ClippingBox.HasValue)
        {
          _ClippingBox = BoundingBox.Empty;
          foreach (var curve in PlanarizedCurveLoops)
            _ClippingBox.Value.Union(curve.GetBoundingBox(false));
        }

        return _ClippingBox.Value;
      }
    }
    #endregion

    #region Geometry
    /// <summary>
    /// <see cref="Rhino.Geometry.Plane"/> where this element is located.
    /// </summary>
    public Plane Location
    {
      get
      {
        if (Panel is InstanceElement panel)
        {
          var location = panel.Location;
          location = new Plane(location.Origin, location.XAxis, Vector3d.CrossProduct(location.XAxis, location.YAxis));
          return location;
        }

        return NaN.Plane;
      }
    }

    static readonly PolyCurve[] EmptyCurves = new PolyCurve[0];

    PolyCurve[] _PlanarizedCurveLoops;
    public PolyCurve[] PlanarizedCurveLoops
    {
      get
      {
        if (_PlanarizedCurveLoops is null && Value is ARDB.CurtainCell cell)
        {
          try { _PlanarizedCurveLoops = cell.PlanarizedCurveLoops.ToArray(GeometryDecoder.ToPolyCurve); }
          catch { _PlanarizedCurveLoops = EmptyCurves; }
        }

        return _PlanarizedCurveLoops;
      }
    }

    PolyCurve[] _CurveLoops;
    public PolyCurve[] CurveLoops
    {
      get
      {
        if (_CurveLoops is null && Value is ARDB.CurtainCell cell)
        {
          try { _CurveLoops = cell.CurveLoops.ToArray(GeometryDecoder.ToPolyCurve); }
          catch { _CurveLoops = EmptyCurves; }
        }

        return _CurveLoops;
      }
    }

    public Surface Surface
    {
      get
      {
        if (PlanarizedCurveLoops is PolyCurve[] planarCurves)
        {
          var plane = Location;
          var bbox = BoundingBox.Empty;
          var curves = new List<Curve>();
          foreach (var loop in planarCurves)
          {
            var curve = Curve.ProjectToPlane(loop, plane);
            bbox.Union(curve.GetBoundingBox(plane));
            curves.Add(curve);
          }

          if (bbox.IsValid)
            return new PlaneSurface(plane, new Interval(bbox.Min.X, bbox.Max.X), new Interval(bbox.Min.Y, bbox.Max.Y));
        }

        return default;
      }
    }

    public Brep TrimmedSurface => Brep.CreateFromSurface(Surface);

    public Brep PolySurface
    {
      get
      {
        if (PlanarizedCurveLoops is PolyCurve[] planarCurves && Surface is Surface surface)
          return surface.CreateTrimmedSurface(planarCurves, GeometryDecoder.Tolerance.VertexTolerance);

        return default;
      }
    }

    public Mesh Mesh
    {
      get
      {
        if (PlanarizedCurveLoops is Curve[] planarCurves && planarCurves.Length > 0)
        {
          using (var mp = new MeshingParameters(0.0, GeometryTolerance.Model.ShortCurveTolerance)
          {
            Tolerance = GeometryTolerance.Model.VertexTolerance,
            SimplePlanes = true,
            JaggedSeams = true,
            RefineGrid = false,
            GridMinCount = 1,
            GridMaxCount = 1,
            GridAspectRatio = 0
          })
          {
            var mesh = new Mesh();
            var breps = Brep.CreatePlanarBreps(planarCurves, GeometryTolerance.Model.VertexTolerance);
            mesh.Append(breps.SelectMany
            (
              x =>
              {
                var faces = Mesh.CreateFromBrep(x, mp);
                x.Dispose();

                foreach(var face in faces)
                  face.MergeAllCoplanarFaces(GeometryTolerance.Model.VertexTolerance, GeometryTolerance.Model.AngleTolerance);

                return faces;
              })
            );

            return mesh;
          }
        }

        return default;
      }
    }
    #endregion
  }
}

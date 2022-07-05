using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Curtain Grid")]
  public class CurtainGrid : DocumentObject,
    IGH_GeometricGoo,
    IGH_PreviewData
  {
    public CurtainGrid() : base() { }
    public CurtainGrid(ARDB.HostObject host, ARDB.CurtainGrid value) : base(host.Document, value)
    { }

    #region DocumentObject
    public override string DisplayName
    {
      get
      {
        if (Value is ARDB.CurtainGrid grid)
          return $"Curtain Grid [{grid.NumVLines + 1} x {grid.NumULines + 1}]";

        return "Curtain Grid";
      }
    }

    protected override void ResetValue()
    {
      clippingBox = default;
      curves = default;

      base.ResetValue();
    }
    #endregion

    #region IGH_Goo
    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
      {
        target = (Q) (object) (Mesh is Mesh mesh ? new GH_Mesh(mesh) : default);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
      {
        target = (Q) (object) (Brep is Brep brep ? new GH_Brep(brep) : default);
        return true;
      }

      target = default;
      return false;
    }
    #endregion

    #region IGH_GeometricGoo
    BoundingBox IGH_GeometricGoo.Boundingbox => GetBoundingBox(Transform.Identity);
    Guid IGH_GeometricGoo.ReferenceID
    {
      get => Guid.Empty;
      set { if (value != Guid.Empty) throw new InvalidOperationException(); }
    }

    bool IGH_GeometricGoo.IsReferencedGeometry => false;
    bool IGH_GeometricGoo.IsGeometryLoaded => Value is object;
    IGH_GeometricGoo IGH_GeometricGoo.DuplicateGeometry() => default;
    public BoundingBox GetBoundingBox(Transform xform)
    {
      var bbox = BoundingBox.Empty;
      foreach (var curve in Curves)
        bbox.Union(curve.GetBoundingBox(xform));

      return bbox;
    }

    IGH_GeometricGoo IGH_GeometricGoo.Transform(Transform xform) => default;
    IGH_GeometricGoo IGH_GeometricGoo.Morph(SpaceMorph xmorph) => default;
    bool IGH_GeometricGoo.LoadGeometry() => false;
    bool IGH_GeometricGoo.LoadGeometry(RhinoDoc doc) => false;
    void IGH_GeometricGoo.ClearCaches() => ResetValue();
    #endregion

    #region IGH_PreviewData
    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      foreach (var curve in Curves)
        args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
    }

    void IGH_PreviewData.DrawViewportMeshes(GH_PreviewMeshArgs args) { }

    private BoundingBox? clippingBox;
    BoundingBox IGH_PreviewData.ClippingBox
    {
      get
      {
        if (!clippingBox.HasValue)
        {
          clippingBox = BoundingBox.Empty;
          foreach (var curve in Curves)
            clippingBox.Value.Union(curve.GetBoundingBox(false));
        }

        return clippingBox.Value;
      }
    }
    #endregion

    #region Implementation
    static IEnumerable<ARDB.CurtainGrid> HostCurtainGrids(ARDB.HostObject host)
    {
      var grids = default(IEnumerable<ARDB.CurtainGrid>);
      switch (host)
      {
        case ARDB.CurtainSystem curtainSystem: grids = curtainSystem.CurtainGrids?.Cast<ARDB.CurtainGrid>(); break;
        case ARDB.ExtrusionRoof extrusionRoof: grids = extrusionRoof.CurtainGrids?.Cast<ARDB.CurtainGrid>(); break;
        case ARDB.FootPrintRoof footPrintRoof: grids = footPrintRoof.CurtainGrids?.Cast<ARDB.CurtainGrid>(); break;
        case ARDB.Wall wall: grids = wall.CurtainGrid is null ? null : Enumerable.Repeat(wall.CurtainGrid, 1); break;
      }

      return grids;
    }

    static IList<ARDB.Reference> GetFaceReferences(ARDB.HostObject host)
    {
      var references = new List<ARDB.Reference>();

      try { references.AddRange(ARDB.HostObjectUtils.GetBottomFaces(host)); }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }
      try { references.AddRange(ARDB.HostObjectUtils.GetTopFaces(host)); }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }
      try { references.AddRange(ARDB.HostObjectUtils.GetSideFaces(host, ARDB.ShellLayerType.Interior)); }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }
      try { references.AddRange(ARDB.HostObjectUtils.GetSideFaces(host, ARDB.ShellLayerType.Exterior)); }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }

      return references;
    }

    static bool IsCurtainGridOnFace(ICollection<ARDB.CurtainCell> cells, ARDB.Face face)
    {
      var result = cells.Count > 0;

      var tol = GeometryTolerance.Internal;
      foreach (var cell in cells)
      {
        foreach (var loop in cell.CurveLoops.Cast<ARDB.CurveArray>())
        {
          foreach (var curve in loop.Cast<ARDB.Curve>())
          {
            var center = curve.Evaluate(0.5, true);
            var distance = face.Project(center).Distance;
            if (distance > tol.VertexTolerance)
              return false;
          }
        }
      }

      return result;
    }

    static ARDB.Reference FindReference(ARDB.HostObject host, ARDB.CurtainGrid value)
    {
      if (host is ARDB.Wall wall)
        return new ARDB.Reference(wall);

      var cells = value.GetCurtainCells();
      foreach (var reference in GetFaceReferences(host))
      {
        if (host.GetGeometryObjectFromReference(reference) is ARDB.Face face && IsCurtainGridOnFace(cells, face))
          return reference;
      }

      return default;
    }

    ARDB.CurtainGrid FindCurtainGrid(ARDB.HostObject host, ARDB.Reference reference)
    {
      if (host is ARDB.Wall wall)
      {
        return wall.CurtainGrid;
      }
      else
      {
        if
        (
          reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_SURFACE &&
          host.GetGeometryObjectFromReference(reference) is ARDB.Face face &&
          HostCurtainGrids(host) is IEnumerable<ARDB.CurtainGrid> grids
        )
        {
          foreach (var grid in grids)
          {
            if (IsCurtainGridOnFace(grid.GetCurtainCells(), face))
              return grid;
          }
        }
      }

      return default;
    }
    #endregion

    #region Properties
    static readonly PolyCurve[] EmptyCurves = new PolyCurve[0];

    PolyCurve[] planarCurves;
    public PolyCurve[] PlanarCurves
    {
      get
      {
        if (planarCurves is null)
        {
          planarCurves = Value is ARDB.CurtainGrid grid ?
            planarCurves = grid.GetCurtainCells().SelectMany
            (
              x =>
              {
                try { return x.PlanarizedCurveLoops.ToArray(GeometryDecoder.ToPolyCurve); }
                catch { return EmptyCurves; }
              }
            ).
            ToArray() :
            EmptyCurves;
        }

        return planarCurves;
      }
    }

    public Mesh Mesh
    {
      get
      {
        if (Value is ARDB.CurtainGrid)
        {
          var mp = new MeshingParameters(0.0, GeometryTolerance.Model.ShortCurveTolerance)
          {
            Tolerance = GeometryTolerance.Model.VertexTolerance,
            SimplePlanes = true,
            JaggedSeams = true,
            RefineGrid = false,
            GridMinCount = 1,
            GridMaxCount = 1,
            GridAspectRatio = 0
          };

          var mesh = new Mesh();
          mesh.Append(PlanarCurves.Select
          (
            x =>
            {
              var m = Mesh.CreateFromPlanarBoundary(x, mp, mp.Tolerance);
              m.MergeAllCoplanarFaces(GeometryTolerance.Model.VertexTolerance, GeometryTolerance.Model.AngleTolerance);
              return m;
            })
          );
          return mesh;
        }

        return default;
      }
    }

    PolyCurve[] curves;
    public PolyCurve[] Curves
    {
      get
      {
        if (curves is null)
        {
          curves = Value is ARDB.CurtainGrid grid ?
            grid.GetCurtainCells().SelectMany
            (
              x =>
              {
                try { return x.CurveLoops.ToArray(GeometryDecoder.ToPolyCurve); }
                catch { return EmptyCurves; }
              }
            ).
            ToArray():
            EmptyCurves;
        }

        return curves;
      }
    }

    public Brep Brep
    {
      get
      {
        if (Value is ARDB.CurtainGrid)
        {
          var brep = Brep.MergeBreps
          (
            PlanarCurves.SelectMany
            (
              x =>
              Brep.CreatePlanarBreps(x, GeometryTolerance.Model.VertexTolerance)
            ),
            RhinoMath.UnsetValue
          );
          if (brep?.IsValid == false)
            brep.Repair(GeometryTolerance.Model.VertexTolerance);

          return brep;
        }

        return default;
      }
    }

    public IEnumerable<Curve> GridLineU => Value is ARDB.CurtainGrid grid ?
      grid.GetUGridLineIds().Select(x => (Document.GetElement(x) as ARDB.CurtainGridLine).FullCurve.ToCurve()) :
      default;

    public IEnumerable<Curve> GridLineV => Value is ARDB.CurtainGrid grid ?
      grid.GetVGridLineIds().Select(x => (Document.GetElement(x) as ARDB.CurtainGridLine).FullCurve.ToCurve()) :
      default;
    #endregion
  }

  [Kernel.Attributes.Name("Curtain Cell")]
  public class CurtainCell : DocumentObject
  {
    public CurtainCell() : base() { }
    public CurtainCell(ARDB.Document doc, ARDB.CurtainCell value) : base(doc, value)
    { }

    #region DocumentObject
    public override string DisplayName =>  "Curtain Cell";

    protected override void ResetValue()
    {
      base.ResetValue();
    }
    #endregion

  }
}

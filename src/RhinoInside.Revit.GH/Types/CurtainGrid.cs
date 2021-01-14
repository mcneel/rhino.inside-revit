using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Curtain Grid")]
  public class CurtainGrid : ReferenceObject, IGH_GeometricGoo, IGH_PreviewData
  {
    #region DocumentObject
    public override string DisplayName
    {
      get
      {
        if (Document.GetElement(Id) is DB.Element element)
          return $"{element.Name} [Curtain Grid]";

        return "[Curtain Grid]";
      }
    }

    protected override void ResetValue()
    {
      clippingBox = default;
      curves = default;

      base.ResetValue();
    }
    #endregion

    #region ReferenceObject
    readonly DB.ElementId id;
    public override DB.ElementId Id => id;
    #endregion

    #region IGH_Goo
    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
      {
        var mesh = new Mesh();
        foreach (var curve in Curves)
        {
          if (curve.SegmentCount == 4)
          {
            var face = new MeshFace
            (
              mesh.Vertices.Add(curve.SegmentCurve(0).PointAtStart),
              mesh.Vertices.Add(curve.SegmentCurve(1).PointAtStart),
              mesh.Vertices.Add(curve.SegmentCurve(2).PointAtStart),
              mesh.Vertices.Add(curve.SegmentCurve(3).PointAtStart)
            );
            mesh.Faces.AddFace(face);
          }
        }

        target = (Q) (object) new GH_Mesh(mesh);
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

    public CurtainGrid() : base() { }
    public CurtainGrid(DB.HostObject host, DB.CurtainGrid value) : base(host.Document, value)
    {
      id = host.Id;
    }

    #region Implementation
    static IEnumerable<DB.CurtainGrid> HostCurtainGrids(DB.HostObject host)
    {
      var grids = default(IEnumerable<DB.CurtainGrid>);
      switch (host)
      {
        case DB.CurtainSystem curtainSystem: grids = curtainSystem.CurtainGrids?.Cast<DB.CurtainGrid>(); break;
        case DB.ExtrusionRoof extrusionRoof: grids = extrusionRoof.CurtainGrids?.Cast<DB.CurtainGrid>(); break;
        case DB.FootPrintRoof footPrintRoof: grids = footPrintRoof.CurtainGrids?.Cast<DB.CurtainGrid>(); break;
        case DB.Wall wall: grids = wall.CurtainGrid is null ? null : Enumerable.Repeat(wall.CurtainGrid, 1); break;
      }

      return grids;
    }

    static IList<DB.Reference> GetFaceReferences(DB.HostObject host)
    {
      var references = new List<DB.Reference>();

      try { references.AddRange(DB.HostObjectUtils.GetBottomFaces(host)); }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }
      try { references.AddRange(DB.HostObjectUtils.GetTopFaces(host)); }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }
      try { references.AddRange(DB.HostObjectUtils.GetSideFaces(host, DB.ShellLayerType.Interior)); }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }
      try { references.AddRange(DB.HostObjectUtils.GetSideFaces(host, DB.ShellLayerType.Exterior)); }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }

      return references;
    }

    static bool IsCurtainGridOnFace(ICollection<DB.CurtainCell> cells, DB.Face face)
    {
      var result = cells.Count > 0;

      foreach (var cell in cells)
      {
        foreach (var loop in cell.CurveLoops.Cast<DB.CurveArray>())
        {
          foreach (var curve in loop.Cast<DB.Curve>())
          {
            var center = curve.Evaluate(0.5, true);
            var distance = face.Project(center).Distance;
            if (distance > Revit.VertexTolerance)
              return false;
          }
        }
      }

      return result;
    }

    static DB.Reference FindReference(DB.HostObject host, DB.CurtainGrid value)
    {
      if (host is DB.Wall wall)
        return new DB.Reference(wall);

      var cells = value.GetCurtainCells();
      foreach (var reference in GetFaceReferences(host))
      {
        if (host.GetGeometryObjectFromReference(reference) is DB.Face face && IsCurtainGridOnFace(cells, face))
          return reference;
      }

      return default;
    }

    DB.CurtainGrid FindCurtainGrid(DB.HostObject host, DB.Reference reference)
    {
      if (host is DB.Wall wall)
      {
        return wall.CurtainGrid;
      }
      else
      {
        if
        (
          reference.ElementReferenceType == DB.ElementReferenceType.REFERENCE_TYPE_SURFACE &&
          host.GetGeometryObjectFromReference(reference) is DB.Face face &&
          HostCurtainGrids(host) is IEnumerable<DB.CurtainGrid> grids
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
    PolyCurve[] curves;
    static readonly PolyCurve[] EmptyCurves = new PolyCurve[0];
    public PolyCurve[] Curves
    {
      get
      {
        if (curves is null)
        {
          if (Value is DB.CurtainGrid grid)
            curves = grid.GetCurtainCells().Cast<DB.CurtainCell>().SelectMany
            (
              x =>
              {
                try { return x.CurveLoops.ToPolyCurves(); }
                catch { return EmptyCurves; }
              }
            ).ToArray();
          else curves = EmptyCurves;
        }

        return curves;
      }
    }
    #endregion
  }
}

using System;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  using ARDB_Viewer = ARDB.Element;

  [Kernel.Attributes.Name("Viewer")]
  public class Viewer : GraphicalElement, ISketchAccess
  {
    protected override Type ValueType => typeof(ARDB_Viewer);
    public new ARDB_Viewer Value => base.Value as ARDB_Viewer;

    protected override bool SetValue(ARDB_Viewer element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB_Viewer element)
    {
      if (element.GetType() != typeof(ARDB_Viewer))
        return false;

      return ViewExtension.IsViewer(element);
    }

    public Viewer() { }
    public Viewer(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public Viewer(ARDB_Viewer element) : base(element)
    {
      if (!IsValidElement(element))
        throw new ArgumentException("Invalid Element", nameof(element));
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo(out target))
        return true;

      if (typeof(Q).IsAssignableFrom(typeof(View)))
      {
        target = (Q) (object) View;
        return true;
      }

      return false;
    }

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB_Viewer viewer)
      {
        var forceShowCrops = args.Thickness > 1 && viewer.get_Parameter(ARDB.BuiltInParameter.VIEWER_CROP_REGION_DISABLED)?.AsBoolean() is true;
        {
          if
          (
            (forceShowCrops || viewer.get_Parameter(ARDB.BuiltInParameter.VIEWER_CROP_REGION_VISIBLE)?.AsBoolean() is true) &&
            CropShape is Curve[] cropShape
          )
          {
            foreach (var curve in cropShape)
              args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
          }

          if
          (
            (args.Thickness > 1 && viewer.get_Parameter(ARDB.BuiltInParameter.VIEWER_ANNOTATION_CROP_ACTIVE)?.AsBoolean() is true) &&
            AnnotationCropShape is Polyline annotationCropShape
          )
          {
            args.Pipeline.DrawPatternedPolyline
            (
              annotationCropShape,
              args.Color,
              0x000070F0,
              args.Thickness,
              close: true
            );
          }
        }

        //(View.GetViewFrame() as IGH_PreviewData).DrawViewportWires(args);
      }
    }
    protected override void DrawViewportMeshes(GH_PreviewMeshArgs args)
    {
      if (Mesh is Mesh mesh)
        args.Pipeline.DrawMeshShaded(mesh, args.Material);
    }
    #endregion

    #region GraphicalElement
    protected override void SubInvalidateGraphics()
    {
      _View = default;
      _CropShape = default;
      _AnnotationCropShape = default;
      _Surface = default;
      _PolySurface = default;
      _Mesh = default;

      base.SubInvalidateGraphics();
    }

    public override BoundingBox GetBoundingBox(Transform xform) => View?.Box.GetBoundingBox(xform) ?? NaN.BoundingBox;

    public override Plane Location => View?.Location ?? NaN.Plane;

    public Plane CropPlane => Sketch?.ProfilesPlane ?? Location;

    (bool HasValue, Curve[] Value) _CropShape;
    public Curve[] CropShape
    {
      get
      {
        if (!_CropShape.HasValue)
        {
          using (var shape = View.Value.GetCropRegionShapeManager())
          {
            if (shape.CanHaveShape)
              _CropShape.Value = shape.GetCropShape().Select(GeometryDecoder.ToPolyCurve).ToArray();
          }

          _CropShape.HasValue = true;
        }

        return _CropShape.Value;
      }
    }

    (bool HasValue, Polyline Value) _AnnotationCropShape;
    public Polyline AnnotationCropShape
    {
      get
      {
        if (!_AnnotationCropShape.HasValue)
        {
          using (var shape = View.Value.GetCropRegionShapeManager())
          {
            if (shape.CanHaveAnnotationCrop)
            {
              using (var annotationCropShape = shape.GetAnnotationCropShape())
                _AnnotationCropShape.Value = new Polyline(annotationCropShape.OfType<ARDB.Curve>().Select(x => x.GetEndPoint(0).ToPoint3d()));
            }
          }

          _AnnotationCropShape.HasValue = true;
        }

        return _AnnotationCropShape.Value;
      }
    }

    (bool HasValue, Surface Value) _Surface;
    public override Surface Surface
    {
      get
      {
        if (!_Surface.HasValue && View is View view)
        {
          if (view.CropBoxActive is true)
          {
            var nearOffset = view is View3D view3D && view3D.Value.IsPerspective ?
              0.1 * Revit.ModelUnits : 0.0;

            var box = view.Box;
            var boxPlane = box.Plane;
            var plane = new Plane
            (
              boxPlane.Origin + nearOffset * boxPlane.ZAxis,
              boxPlane.XAxis,
              boxPlane.YAxis
            );

            _Surface.Value = new PlaneSurface(plane, box.X, box.Y);
          }
          else _Surface.Value = view.Surface;

          _Surface.HasValue = true;
        }

        return _Surface.Value;
      }
    }

    (bool HasValue, Brep Value) _PolySurface;
    public override Brep PolySurface
    {
      get
      {
        if (!_PolySurface.HasValue && View is View view)
        {
          if (view.CropBoxActive is true && CropShape is Curve[] cropShape)
            _PolySurface.Value = Surface.CreateTrimmedSurface(cropShape, GeometryTolerance.Model.VertexTolerance);
          else
            _PolySurface.Value = TrimmedSurface;

          _PolySurface.HasValue = true;
        }

        return _PolySurface.Value;
      }
    }

    (bool HasValue, Mesh Value) _Mesh;
    public override Mesh Mesh
    {
      get
      {
        if (!_Mesh.HasValue && Surface is Surface surface)
        {
          _Mesh.Value = Mesh.CreateFromSurface(surface, MeshingParameters.FastRenderMesh);
          _Mesh.Value.Normals.ComputeNormals();
          _Mesh.HasValue = true;
        }

        return _Mesh.Value;
      }
    }
    #endregion

    #region ISketchAccess
    Sketch _Sketch;
    public Sketch Sketch => _Sketch ??= GetElement<Sketch>(Value?.GetSketchId());
    #endregion

    #region Properties
    View _View;
    public View View => _View ??= GetElement<View>(Value?.GetFirstDependent<ARDB.View>());
    #endregion
  }
}

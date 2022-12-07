using System;
using Rhino.Geometry;
using Rhino.Display;
using Rhino.DocObjects;
using ARDB = Autodesk.Revit.DB;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;
  using Convert.Geometry;

#if REVIT_2020
  using ARDB_ImageInstance = ARDB.ImageInstance;
#else
  using ARDB_ImageInstance = ARDB.Element;
#endif

  [Kernel.Attributes.Name("Raster Image")]
  public class ImageInstance : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB_ImageInstance);
    public new ARDB_ImageInstance Value => base.Value as ARDB_ImageInstance;

    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      return element is ARDB_ImageInstance &&
             element.Category?.Id.ToBuiltInCategory() == ARDB.BuiltInCategory.OST_RasterImages;
    }

    public ImageInstance() { }
    public ImageInstance(ARDB_ImageInstance image) : base(image)
    {
#if !REVIT_2020
      if (!IsValidElement(image))
        throw new ArgumentException("Invalid Element", nameof(image));
#endif
    }

    protected override void ResetValue()
    {
      using (_DisplayTexture) _DisplayTexture = null;
      using (_Mesh)           _Mesh = default;

      base.ResetValue();
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (xform.IsIdentity)
        return base.GetBoundingBox(xform);

      var box = Box;
      return box.IsValid ? box.GetBoundingBox(xform) : NaN.BoundingBox;
    }

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Mesh is Mesh mesh)
      {
        if (DisplayTexture is Texture texture)
        {
          var material = new DisplayMaterial(System.Drawing.Color.White, transparency: 0.0);
          {
            if (args.Thickness > 1)
              material.Emission = args.Color;

            material.SetBitmapTexture(texture, front: true);
            args.Pipeline.DrawMeshShaded(mesh, material);
          }
        }

        args.Pipeline.DrawMeshWires(mesh, args.Color, args.Thickness);
      }
    }
    #endregion

    #region Properties
    public override Box Box
    {
      get
      {
#if REVIT_2020
        if (Value is ARDB_ImageInstance image)
        {
          var points = new Point3d[]
          {
            image.GetLocation(ARDB.BoxPlacement.Center).ToPoint3d(),
            image.GetLocation(ARDB.BoxPlacement.BottomLeft).ToPoint3d(),
            image.GetLocation(ARDB.BoxPlacement.BottomRight).ToPoint3d(),
            image.GetLocation(ARDB.BoxPlacement.TopRight).ToPoint3d(),
            image.GetLocation(ARDB.BoxPlacement.TopLeft).ToPoint3d(),
          };

          var plane = new Plane
          (
            points[0],
            points[2] - points[1],
            points[4] - points[1]
          );

          plane.ClosestParameter(points[1], out var minU, out var minV);
          plane.ClosestParameter(points[3], out var maxU, out var maxV);

          return new Box
          (
            plane,
            new Interval(minU, maxU),
            new Interval(minV, maxV),
            new Interval(0.0, 0.0)
          );
        }
#else
        var location = Location;
        if (location.IsValid)
        {
          var width  = Width.Value;
          var height = Height.Value;

          return new Box
          (
            Location,
            new Interval(-width  * 0.5, +width  * 0.5),
            new Interval(-height * 0.5, +height * 0.5),
            new Interval(0.0, 0.0)
          );
        }
#endif
        return NaN.Box;
      }
    }

    public override Plane Location
    {
      get
      {
#if REVIT_2020
        return Box.Plane;
#else
        if (OwnerView is View view)
        {
          var viewLocation = view.Location;
          return new Plane
          (
            BoundingBox.Center,
            viewLocation.XAxis,
            viewLocation.YAxis
          );
        }

        return NaN.Plane;
#endif
      }
    }

    public double? Width
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.RASTER_SHEETWIDTH).AsDouble() * Revit.ModelUnits;
    }

    public double? Height
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.RASTER_SHEETHEIGHT).AsDouble() * Revit.ModelUnits;
    }

    public override Surface Surface
    {
      get
      {
        var box = Box;
        if (box.IsValid)
        {
          return new PlaneSurface
          (
            plane: box.Plane,
            xExtents: box.X,
            yExtents: box.Y
          );
        }

        return null;
      }
    }

    Mesh _Mesh;
    public override Mesh Mesh
    {
      get
      {
        if (_Mesh is null)
        {
          var box = Box;
          if (box.IsValid)
          {
            _Mesh = new Mesh();
            var vertices = _Mesh.Vertices;
            vertices.Add(box.Plane.Origin + (box.Plane.XAxis * box.X.T0) + (box.Plane.YAxis * box.Y.T0));
            vertices.Add(box.Plane.Origin + (box.Plane.XAxis * box.X.T1) + (box.Plane.YAxis * box.Y.T0));
            vertices.Add(box.Plane.Origin + (box.Plane.XAxis * box.X.T1) + (box.Plane.YAxis * box.Y.T1));
            vertices.Add(box.Plane.Origin + (box.Plane.XAxis * box.X.T0) + (box.Plane.YAxis * box.Y.T1));

            var coordinates = _Mesh.TextureCoordinates;
            coordinates.Add(0.0, 0.0);
            coordinates.Add(1.0, 0.0);
            coordinates.Add(1.0, 1.0);
            coordinates.Add(0.0, 1.0);

            _Mesh.Faces.AddFace(new MeshFace(0, 1, 2, 3));
            _Mesh.Normals.ComputeNormals();
          }
        }
        return _Mesh;
      }
    }

    Texture _DisplayTexture;
    Texture DisplayTexture
    {
      get
      {
        if (_DisplayTexture is null)
        {
          if (Type.Value.get_Parameter(ARDB.BuiltInParameter.RASTER_SYMBOL_FILENAME)?.AsString() is string imageFilePath)
          {
            if (System.IO.File.Exists(imageFilePath))
              _DisplayTexture = new Texture() { FileName = imageFilePath };
          }
        }

        return _DisplayTexture;
      }
    }
    #endregion
  }
}

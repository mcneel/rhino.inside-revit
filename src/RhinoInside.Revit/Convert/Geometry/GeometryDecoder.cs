using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  using Raw;
  using RhinoInside.Revit.External.DB.Extensions;

  /// <summary>
  /// Converts a Revit geometry type to an equivalent Rhino geometry type.
  /// </summary>
  public static class GeometryDecoder
  {
    #region Context
    internal sealed class Context : State<Context>
    {
      public ARDB.Element Element = default;
      public ARDB.Visibility Visibility = ARDB.Visibility.Invisible;
      public ARDB.Category Category = default;
      public ARDB.Material Material = default;
      public ARDB.ElementId[] FaceMaterialId;
    }
    #endregion

    #region Points and Vectors
    /// <summary>
    /// Converts the specified <see cref="ARDB.UV" /> to an equivalent <see cref="Rhino.Geometry.Point2d" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToPoint2d(ARDB.UV)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Point2d rhinoPoint2d = revitUvPoint.ToPoint2d();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_point2d = revit_uvpoint.ToPoint2d() # type: RG.Point2d
    /// </code>
    /// 
    /// Using <see cref="ToPoint2d(ARDB.UV)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Point2d rhinoPoint2d = GeometryEncoder.ToPoint2d(revitUvPoint)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_point2d = GD.ToPoint2d(revit_uvpoint) # type: RG.Point2d
    /// </code>
    ///
    /// </example>
    /// <param name="uvPoint">Revit uvPoint to convert.</param>
    /// <returns>Rhino point that is equivalent to the provided Revit uvPoint.</returns>
    public static Point2d ToPoint2d(this ARDB.UV uvPoint)
    { var rhino = RawDecoder.AsPoint2d(uvPoint); UnitConvertible.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified <see cref="ARDB.UV" /> to an equivalent <see cref="Rhino.Geometry.Vector2d" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToVector2d(ARDB.UV)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Vector2d rhinoVector2d = revitUvPoint.ToVector2d();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_vector2d = revit_uvpoint.ToVector2d() # type: RG.Vector2d
    /// </code>
    /// 
    /// Using <see cref="ToVector2d(ARDB.UV)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Vector2d rhinoVector2d = GeometryEncoder.ToVector2d(revitUvPoint)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_vector2d = GD.ToVector2d(revit_uvpoint) # type: RG.Point2d
    /// </code>
    ///
    /// </example>
    /// <param name="uvPoint">Revit uvPoint to convert.</param>
    /// <returns>Rhino vector that is equivalent to the provided Revit uvPoint.</returns>
    public static Vector2d ToVector2d(this ARDB.UV uvPoint)
    { return new Vector2d(uvPoint.U, uvPoint.V); }

    /// <summary>
    /// Converts the specified <see cref="ARDB.XYZ" /> to an equivalent <see cref="Rhino.Geometry.Point3d" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToPoint3d(ARDB.XYZ)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Point3d rhinoPoint3d = revitPoint.ToPoint3d();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_point3d = revit_point.ToPoint3d() # type: RG.Point3d
    /// </code>
    /// 
    /// Using <see cref="ToPoint3d(ARDB.XYZ)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Point3d rhinoPoint3d = GeometryEncoder.ToPoint3d(revitPoint)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_point3d = GD.ToPoint3d(revit_point) # type: RG.Point3d
    /// </code>
    ///
    /// </example>
    /// <param name="point">Revit point to convert.</param>
    /// <returns>Rhino point that is equivalent to the provided Revit point.</returns>
    public static Point3d ToPoint3d(this ARDB.XYZ point)
    { var rhino = RawDecoder.AsPoint3d(point); UnitConvertible.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified <see cref="ARDB.XYZ" /> to an equivalent <see cref="Rhino.Geometry.Vector3d" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToVector3d(ARDB.XYZ)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Vector3d rhinoVector3d = revitPoint.ToVector3d();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_vector3d = revit_point.ToVector3d() # type: RG.Vector3d
    /// </code>
    /// 
    /// Using <see cref="ToVector3d(ARDB.XYZ)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Vector3d rhinoVector3d = GeometryEncoder.ToVector3d(revitPoint)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_vector3d = GD.ToVector3d(revit_point) # type: RG.Vector3d
    /// </code>
    ///
    /// </example>
    /// <param name="point">Revit point to convert.</param>
    /// <returns>Rhino vector that is equivalent to the provided Revit point.</returns>
    public static Vector3d ToVector3d(this ARDB.XYZ point)
    { return new Vector3d(point.X, point.Y, point.Z); }

    /// <summary>
    /// Converts the specified <see cref="ARDB.Plane" /> to an equivalent <see cref="Rhino.Geometry.Plane" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToPlane(ARDB.Plane)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Plane rhinoPlane = revitPlane.ToPlane();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_plane = revit_plane.ToPlane() # type: RG.Plane
    /// </code>
    /// 
    /// Using <see cref="ToPlane(ARDB.Plane)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Plane rhinoPlane = GeometryEncoder.ToPlane(revitPlane)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_plane = GD.ToPlane(revit_plane) # type: RG.Plane
    /// </code>
    ///
    /// </example>
    /// <param name="plane">Revit point to convert.</param>
    /// <returns>Rhino plane that is equivalent to the provided Revit plane.</returns>
    public static Plane ToPlane(this ARDB.Plane plane)
    { var rhino = RawDecoder.AsPlane(plane); UnitConvertible.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified <see cref="ARDB.Transform" /> to an equivalent <see cref="Rhino.Geometry.Transform" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToTransform(ARDB.Transform)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Transform rhinoTransform = revitTransform.ToTransform();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_transform = revit_transform.ToTransform() # type: RG.Transform
    /// </code>
    /// 
    /// Using <see cref="ToTransform(ARDB.Transform)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Transform rhinoTransform = GeometryEncoder.ToTransform(revitTransform)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_transform = GD.ToTransform(revit_transform) # type: RG.Transform
    /// </code>
    ///
    /// </example>
    /// <param name="transform">Revit transform to convert.</param>
    /// <returns>Rhino transform that is equivalent to the provided Revit transform.</returns>
    public static Transform ToTransform(this ARDB.Transform transform)
    { var rhino = RawDecoder.AsTransform(transform); UnitConvertible.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified <see cref="ARDB.BoundingBoxXYZ" /> to an equivalent <see cref="Rhino.Geometry.BoundingBox" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToBoundingBox(ARDB.BoundingBoxXYZ)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// BoundingBox rhinoBBox = revitBBox.ToBoundingBox();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_bbox = revit_bbox.ToBoundingBox() # type: RG.BoundingBox
    /// </code>
    /// 
    /// Using <see cref="ToBoundingBox(ARDB.BoundingBoxXYZ)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// BoundingBox rhinoBBox = GeometryEncoder.ToBoundingBox(revitBBox)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_bbox = GD.ToBoundingBox(revit_bbox) # type: RG.BoundingBox
    /// </code>
    ///
    /// </example>
    /// <param name="boundingBox">Revit boundingBox to convert.</param>
    /// <returns>Rhino boundingBox that is equivalent to the provided Revit boundingBox.</returns>
    public static BoundingBox ToBoundingBox(this ARDB.BoundingBoxXYZ boundingBox)
    { var rhino = RawDecoder.AsBoundingBox(boundingBox); UnitConvertible.Scale(ref rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified <see cref="ARDB.BoundingBoxXYZ" /> to an equivalent <see cref="Rhino.Geometry.BoundingBox" /> and outputs the conversion transform.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToBoundingBox(ARDB.BoundingBoxXYZ)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// BoundingBox rhinoBBox = revitBBox.ToBoundingBox(out Transform transform);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_bbox, transform = revit_bbox.ToBoundingBox() # type: (BoundingBox, RG.Transform)
    /// </code>
    /// 
    /// Using <see cref="ToBoundingBox(ARDB.BoundingBoxXYZ)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// BoundingBox rhinoBBox = GeometryEncoder.ToBoundingBox(revitBBox, out Transform transform)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_bbox, transform = GD.ToBoundingBox(revit_bbox) # type: (BoundingBox, RG.Transform)
    /// </code>
    ///
    /// </example>
    /// <param name="boundingBox">Revit boundingBox to convert.</param>
    /// <param name="transform">Conversion transform as output.</param>
    /// <returns>Rhino boundingBox that is equivalent to the provided Revit boundingBox.</returns>
    public static BoundingBox ToBoundingBox(this ARDB.BoundingBoxXYZ boundingBox, out Transform transform)
    {
      var rhino = RawDecoder.AsBoundingBox(boundingBox, out transform);
      UnitConvertible.Scale(ref rhino, UnitConverter.ToRhinoUnits);
      UnitConvertible.Scale(ref transform, UnitConverter.ToRhinoUnits);
      return rhino;
    }

    internal static Interval[] ToIntervals(ARDB.BoundingBoxUV value)
    {
      return !value.IsUnset() ?
      new Interval[]
      {
        new Interval(value.Min.U, value.Max.U),
        new Interval(value.Min.V, value.Max.V)
      } :
      new Interval[]
      {
        NaN.Interval,
        NaN.Interval
      };
    }

    /// <summary>
    /// Converts the specified <see cref="ARDB.Outline" /> to an equivalent <see cref="Rhino.Geometry.BoundingBox" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToBoundingBox(ARDB.Outline)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// BoundingBox rhinoBBox = revitOutline.ToBoundingBox();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_bbox = revit_outline.ToBoundingBox() # type: RG.BoundingBox
    /// </code>
    /// 
    /// Using <see cref="ToBoundingBox(ARDB.Outline)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// BoundingBox rhinoBBox = GeometryEncoder.ToBoundingBox(revitOutline)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_bbox = GD.ToBoundingBox(revit_outline) # type: RG.BoundingBox
    /// </code>
    ///
    /// </example>
    /// <param name="outline">Revit outline to convert.</param>
    /// <returns>Rhino boundingBox that is equivalent to the provided Revit outline.</returns>
    public static BoundingBox ToBoundingBox(this ARDB.Outline outline)
    {
      return new BoundingBox(outline.MinimumPoint.ToPoint3d(), outline.MaximumPoint.ToPoint3d());
    }

    /// <summary>
    /// Converts the specified <see cref="ARDB.BoundingBoxXYZ" /> to an equivalent <see cref="Rhino.Geometry.Box" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToBox(ARDB.BoundingBoxXYZ)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Box rhinoBox = revitBBox.ToBox();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_box = revit_bbox.ToBox() # type: RG.Box
    /// </code>
    /// 
    /// Using <see cref="ToBox(ARDB.BoundingBoxXYZ)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Box rhinoBox = GeometryEncoder.ToBox(revitBBox)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_box = GD.ToBox(revit_bbox) # type: RG.Box
    /// </code>
    ///
    /// </example>
    /// <param name="boundingBox">Revit boundingBox to convert.</param>
    /// <returns>Rhino box that is equivalent to the provided Revit boundingBox.</returns>
    public static Box ToBox(this ARDB.BoundingBoxXYZ boundingBox)
    {
      var rhino = RawDecoder.AsBoundingBox(boundingBox, out var transform);
      UnitConvertible.Scale(ref rhino, UnitConverter.ToRhinoUnits);
      UnitConvertible.Scale(ref transform, UnitConverter.ToRhinoUnits);

      return new Box
      (
        new Plane
        (
          origin: new Point3d(transform.M03, transform.M13, transform.M23),
          xDirection: new Vector3d(transform.M00, transform.M10, transform.M20),
          yDirection: new Vector3d(transform.M01, transform.M11, transform.M21)
        ),
        xSize: new Interval(rhino.Min.X, rhino.Max.X),
        ySize: new Interval(rhino.Min.Y, rhino.Max.Y),
        zSize: new Interval(rhino.Min.Z, rhino.Max.Z)
      );
    }
    #endregion

    #region GeometryBase
    /// <summary>
    /// Converts the specified <see cref="ARDB.Point" /> to an equivalent <see cref="Rhino.Geometry.Point" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToPoint(ARDB.Point)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Point rhinoPoint = revitPoint.ToPoint();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_point = revit_point.ToPoint() # type: RG.Point
    /// </code>
    /// 
    /// Using <see cref="ToPoint(ARDB.Point)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Point rhinoPoint = GeometryEncoder.ToPoint(revitPoint)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_point = GD.ToPoint(revit_point) # type: RG.Point
    /// </code>
    ///
    /// </example>
    /// <param name="point">Revit point to convert.</param>
    /// <returns>Rhino point that is equivalent to the provided Revit point.</returns>
    public static Point ToPoint(this ARDB.Point point)
    { var rhino = RawDecoder.ToRhino(point); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified <see cref="ARDB.Line" /> to an equivalent <see cref="Rhino.Geometry.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurve(ARDB.Line)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve rhinoCurve = revitLine.ToCurve();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_curve = revit_line.ToCurve() # type: RG.Curve
    /// </code>
    /// 
    /// Using <see cref="ToCurve(ARDB.Line)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve rhinoCurve = GeometryEncoder.ToCurve(revitLine)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_curve = GD.ToCurve(revit_line) # type: RG.Curve
    /// </code>
    ///
    /// </example>
    /// <param name="line">Revit line to convert.</param>
    /// <returns>Rhino curve that is equivalent to the provided Revit line.</returns>
    public static Curve ToCurve(this ARDB.Line line)
    { var rhino = RawDecoder.ToRhino(line); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified <see cref="ARDB.Arc" /> to an equivalent <see cref="Rhino.Geometry.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurve(ARDB.Arc)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve rhinoCurve = revitArc.ToCurve();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_curve = revit_arc.ToCurve() # type: RG.Curve
    /// </code>
    /// 
    /// Using <see cref="ToCurve(ARDB.Arc)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve rhinoCurve = GeometryEncoder.ToCurve(revitArc)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_curve = GD.ToCurve(revit_arc) # type: RG.Curve
    /// </code>
    ///
    /// </example>
    /// <param name="arc">Revit arc to convert.</param>
    /// <returns>Rhino curve that is equivalent to the provided Revit arc.</returns>
    public static Curve ToCurve(this ARDB.Arc arc)
    { var rhino = RawDecoder.ToRhino(arc); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified <see cref="ARDB.Ellipse" /> to an equivalent <see cref="Rhino.Geometry.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurve(ARDB.Ellipse)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve rhinoCurve = revitEllipse.ToCurve();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_curve = revit_ellipse.ToCurve() # type: RG.Curve
    /// </code>
    /// 
    /// Using <see cref="ToCurve(ARDB.Ellipse)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve rhinoCurve = GeometryEncoder.ToCurve(revitEllipse)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_curve = GD.ToCurve(revit_ellipse) # type: RG.Curve
    /// </code>
    ///
    /// </example>
    /// <param name="ellipse">Revit ellipse to convert.</param>
    /// <returns>Rhino curve that is equivalent to the provided Revit ellipse.</returns>
    public static Curve ToCurve(this ARDB.Ellipse ellipse)
    { var rhino = RawDecoder.ToRhino(ellipse); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified <see cref="ARDB.NurbSpline" /> to an equivalent <see cref="Rhino.Geometry.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurve(ARDB.NurbSpline)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve rhinoCurve = revitNurbSpline.ToCurve();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_curve = revit_nurbsspline.ToCurve() # type: RG.Curve
    /// </code>
    /// 
    /// Using <see cref="ToCurve(ARDB.NurbSpline)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve rhinoCurve = GeometryEncoder.ToCurve(revitNurbSpline)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_curve = GD.ToCurve(revit_nurbsspline) # type: RG.Curve
    /// </code>
    ///
    /// </example>
    /// <param name="nurbSpline">Revit helix to convert.</param>
    /// <returns>Rhino curve that is equivalent to the provided Revit hermiteSpline.</returns>
    public static Curve ToCurve(this ARDB.NurbSpline nurbSpline)
    { var rhino = RawDecoder.ToRhino(nurbSpline); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified <see cref="ARDB.HermiteSpline" /> to an equivalent <see cref="Rhino.Geometry.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurve(ARDB.HermiteSpline)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve rhinoCurve = revitHermiteSpline.ToCurve();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_curve = revit_hermitespline.ToCurve() # type: RG.Curve
    /// </code>
    /// 
    /// Using <see cref="ToCurve(ARDB.HermiteSpline)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve rhinoCurve = GeometryEncoder.ToCurve(revitHermiteSpline)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_curve = GD.ToCurve(revit_hermitespline) # type: RG.Curve
    /// </code>
    ///
    /// </example>
    /// <param name="hermiteSpline">Revit helix to convert.</param>
    /// <returns>Rhino curve that is equivalent to the provided Revit hermiteSpline.</returns>
    public static Curve ToCurve(this ARDB.HermiteSpline hermiteSpline)
    { var rhino = RawDecoder.ToRhino(hermiteSpline); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified <see cref="ARDB.CylindricalHelix" /> to an equivalent <see cref="Rhino.Geometry.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurve(ARDB.CylindricalHelix)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve rhinoCurve = revitHelix.ToCurve();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_curve = revit_helix.ToCurve() # type: RG.Curve
    /// </code>
    /// 
    /// Using <see cref="ToCurve(ARDB.CylindricalHelix)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve rhinoCurve = GeometryEncoder.ToCurve(revitHelix)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_curve = GD.ToCurve(revit_helix) # type: RG.Curve
    /// </code>
    ///
    /// </example>
    /// <param name="helix">Revit helix to convert.</param>
    /// <returns>Rhino curve that is equivalent to the provided Revit helix.</returns>
    public static Curve ToCurve(this ARDB.CylindricalHelix helix)
    { var rhino = RawDecoder.ToRhino(helix); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified <see cref="ARDB.Curve" /> to an equivalent <see cref="Rhino.Geometry.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurve(ARDB.Curve)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve rhinoCurve = revitCurve.ToCurve();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_curve = revit_curve.ToCurve() # type: RG.Curve
    /// </code>
    /// 
    /// Using <see cref="ToCurve(ARDB.Curve)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve rhinoCurve = GeometryEncoder.ToCurve(revitCurve)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_curve = GD.ToCurve(revit_curve) # type: RG.Curve
    /// </code>
    ///
    /// </example>
    /// <param name="curve">Revit curve to convert.</param>
    /// <returns>Rhino curve that is equivalent to the provided Revit curve.</returns>
    public static Curve ToCurve(this ARDB.Curve curve)
    { var rhino = RawDecoder.ToRhino(curve); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified <see cref="ARDB.PolyLine" /> to an equivalent <see cref="Rhino.Geometry.PolylineCurve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToPolylineCurve(ARDB.PolyLine)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// PolylineCurve rhinoPolylineCurve = revitPolyLine.ToPolylineCurve();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_polylinecurve = revit_polyline.ToPolylineCurve() # type: RG.PolylineCurve
    /// </code>
    /// 
    /// Using <see cref="ToPolylineCurve(ARDB.PolyLine)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// PolylineCurve rhinoPolylineCurve = GeometryEncoder.ToPolylineCurve(revitPolyLine)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_polylinecurve = GD.ToPolylineCurve(revit_polyline) # type: RG.PolylineCurve
    /// </code>
    ///
    /// </example>
    /// <param name="polyLine">Revit polyLine to convert.</param>
    /// <returns>Rhino polyLineCurve that is equivalent to the provided Revit polyLine.</returns>
    public static PolylineCurve ToPolylineCurve(this ARDB.PolyLine polyLine)
    { var rhino = RawDecoder.ToRhino(polyLine); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified <see cref="ARDB.Face" /> to an equivalent <see cref="Rhino.Geometry.Brep" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToBrep(ARDB.Face)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Brep rhinoBrep = revitFace.ToBrep();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_brep = revit_face.ToBrep() # type: RG.Brep
    /// </code>
    /// 
    /// Using <see cref="ToBrep(ARDB.Face)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Brep rhinoBrep = GeometryEncoder.ToBrep(revitFace)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_brep = GD.ToBrep(revit_face) # type: RG.Brep
    /// </code>
    ///
    /// </example>
    /// <param name="face">Revit face to convert.</param>
    /// <returns>Rhino brep that is equivalent to the provided Revit face.</returns>
    public static Brep ToBrep(this ARDB.Face face)
    { var rhino = RawDecoder.ToRhino(face); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified <see cref="ARDB.Solid" /> to an equivalent <see cref="Rhino.Geometry.Brep" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToBrep(ARDB.Solid)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Brep rhinoBrep = revitSolid.ToBrep();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_brep = revit_solid.ToBrep() # type: RG.Brep
    /// </code>
    /// 
    /// Using <see cref="ToBrep(ARDB.Solid)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Brep rhinoBrep = GeometryEncoder.ToBrep(revitSolid)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_brep = GD.ToBrep(revit_solid) # type: RG.Brep
    /// </code>
    ///
    /// </example>
    /// <param name="solid">Revit solid to convert.</param>
    /// <returns>Rhino brep that is equivalent to the provided Revit solid.</returns>
    public static Brep ToBrep(this ARDB.Solid solid)
    { var rhino = RawDecoder.ToRhino(solid); UnitConvertible.Scale(rhino, UnitConverter.ToRhinoUnits); return rhino; }

    /// <summary>
    /// Converts the specified <see cref="ARDB.Mesh" /> to an equivalent <see cref="Rhino.Geometry.Mesh" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToMesh(ARDB.Mesh)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Mesh rhinoMesh = revitMesh.ToMesh();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_mesh = revit_mesh.ToMesh() # type: RG.Mesh
    /// </code>
    /// 
    /// Using <see cref="ToMesh(ARDB.Mesh)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Mesh rhinoMesh = GeometryEncoder.ToMesh(revitMesh)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_mesh = GD.ToMesh(revit_mesh) # type: RG.Mesh
    /// </code>
    ///
    /// </example>
    /// <param name="mesh">Revit mesh to convert.</param>
    /// <returns>Rhino mesh that is equivalent to the provided Revit mesh.</returns>
    public static Mesh ToMesh(this ARDB.Mesh mesh) =>
      MeshDecoder.FromRawMesh(MeshDecoder.ToRhino(mesh), UnitConverter.ToRhinoUnits);
    #endregion

    /// <summary>
    /// Converts the specified <see cref="ARDB.CurveLoop" /> to an equivalent <see cref="Rhino.Geometry.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurve(ARDB.CurveLoop)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve rhinoCurve = revitCurveLoop.ToCurve();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_curve = revit_curveloop.ToCurve() # type: RG.Curve
    /// </code>
    /// 
    /// Using <see cref="ToCurve(ARDB.CurveLoop)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve rhinoCurve = GeometryEncoder.ToCurve(revitCurveLoop)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_curve = GD.ToCurve(revit_curveloop) # type: RG.Curve
    /// </code>
    ///
    /// </example>
    /// <param name="curveLoop">Revit curveLoop to convert.</param>
    /// <returns>Rhino curve that is equivalent to the provided Revit curveLoop.</returns>
    public static Curve ToCurve(this ARDB.CurveLoop curveLoop)
    {
      if (curveLoop.NumberOfCurves() == 1)
        return curveLoop.First().ToCurve();

      var polycurve = new PolyCurve();

      foreach (var curve in curveLoop)
        polycurve.AppendSegment(curve.ToCurve());

      if (!curveLoop.IsOpen())
        polycurve.MakeClosed(Revit.VertexTolerance * Revit.ModelUnits);

      return polycurve;
    }

    /// <summary>
    /// Converts the specified <see cref="ARDB.CurveArray" /> to an equivalent array of <see cref="Rhino.Geometry.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurves(ARDB.CurveArray)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve[] rhinoCurves = revitCurveArray.ToCurves();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from System import Array
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_curves = revit_curvearray.ToCurves() # type: RG.Array[Curve]
    /// </code>
    /// 
    /// Using <see cref="ToCurves(ARDB.CurveArray)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// Curve[] rhinoCurves = GeometryEncoder.ToCurves(revitCurveArray)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from System import Array
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_curve = GD.ToCurves(revit_curvearray) # type: RG.Array[Curve]
    /// </code>
    ///
    /// </example>
    /// <param name="curveArray">Revit curveArray to convert.</param>
    /// <returns>Rhino curve array that is equivalent to the provided Revit curveArray.</returns>
    public static Curve[] ToCurves(this ARDB.CurveArray curveArray)
    {
      var count = curveArray.Size;
      var curves = new Curve[count];

      int index = 0;
      foreach (var curve in curveArray.Cast<ARDB.Curve>())
        curves[index++] = curve.ToCurve();

      return curves;
    }

    /// <summary>
    /// Converts the specified <see cref="ARDB.CurveArrArray" /> to an equivalent array of <see cref="Rhino.Geometry.PolyCurve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToPolyCurves(ARDB.CurveArrArray)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// PolyCurve[] rhinoCurves = revitCurveArray.ToPolyCurves();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from System import Array
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// rhino_curves = revit_curvearray.ToPolyCurves() # type: RG.Array[PolyCurve]
    /// </code>
    /// 
    /// Using <see cref="ToPolyCurves(ARDB.CurveArrArray)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// PolyCurve[] rhinoCurves = GeometryEncoder.ToPolyCurves(revitCurveArray)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RhinoCommon")
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from System import Array
    /// import Rhino.Geometry as RG
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryDecoder as GD
    /// 
    /// rhino_curve = GD.ToPolyCurves(revit_curvearray) # type: RG.Array[PolyCurve]
    /// </code>
    ///
    /// </example>
    /// <param name="curveArrays">Revit curveArray to convert.</param>
    /// <returns>Rhino polyCurve array that is equivalent to the provided Revit curveArrArray.</returns>
    public static PolyCurve[] ToPolyCurves(this ARDB.CurveArrArray curveArrays)
    {
      var count = curveArrays.Size;
      var list = new PolyCurve[count];

      int index = 0;
      foreach (var curveArray in curveArrays.Cast<ARDB.CurveArray>())
      {
        var polycurve = new PolyCurve();

        foreach (var curve in curveArray.Cast<ARDB.Curve>())
          polycurve.AppendSegment(curve.ToCurve());

        polycurve.MakeClosed(Revit.VertexTolerance * Revit.ModelUnits);

        list[index++] = polycurve;
      }

      return list;
    }

    /// <summary>
    /// Update Context from <see cref="ARDB.GeometryObject"/> <paramref name="geometryObject"/>
    /// </summary>
    /// <param name="geometryObject"></param>
    internal static void UpdateGraphicAttributes(ARDB.GeometryObject geometryObject)
    {
      if (geometryObject is object)
      {
        var context = Context.Peek;
        if (context.Element is ARDB.Element element)
        {
          context.Visibility = geometryObject.Visibility;

          if (geometryObject is ARDB.GeometryInstance instance)
          {
            context.Element = instance.Symbol;
            context.Category = instance.Symbol.Category ?? context.Category;
            context.Material = context.Category?.Material;
          }
          else if (geometryObject is ARDB.GeometryElement geometry)
          {
            context.Material = geometry.MaterialElement;
          }
          else if (geometryObject is ARDB.Solid solid)
          {
            if (!solid.Faces.IsEmpty)
            {
              var faces = solid.Faces;
              var count = faces.Size;

              context.FaceMaterialId = new ARDB.ElementId[count];
              for (int f = 0; f < count; ++f)
                context.FaceMaterialId[f] = faces.get_Item(f).MaterialElementId;
            }
          }
          else if (geometryObject is ARDB.Mesh mesh)
          {
            context.FaceMaterialId = new ARDB.ElementId[] { mesh.MaterialElementId };
          }

          if (geometryObject.GraphicsStyleId != ARDB.ElementId.InvalidElementId)
            context.Category = (element.Document.GetElement(geometryObject.GraphicsStyleId) as ARDB.GraphicsStyle).GraphicsStyleCategory;
        }
      }
    }

    /// <summary>
    /// Set graphic attributes to <see cref="GeometryBase"/> <paramref name="geometry"/> from Context
    /// </summary>
    /// <param name="geometry"></param>
    /// <returns><paramref name="geometry"/> with graphic attributes</returns>
    static GeometryBase SetGraphicAttributes(GeometryBase geometry)
    {
      if (geometry is object)
      {
        var context = Context.Peek;
        if (context.Element is object)
        {
          if (context.Category is ARDB.Category category)
            geometry.TrySetUserString(ARDB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY.ToString(), category.Id);

          if (context.Material is ARDB.Material material)
            geometry.TrySetUserString(ARDB.BuiltInParameter.MATERIAL_ID_PARAM.ToString(), material.Id);
        }
      }

      return geometry;
    }

    internal static IEnumerable<GeometryBase> ToGeometryBaseMany(this ARDB.GeometryObject geometry) =>
      ToGeometryBaseMany(geometry, _ => true);

    internal static IEnumerable<GeometryBase> ToGeometryBaseMany(this ARDB.GeometryObject geometry, Func<ARDB.GeometryObject, bool> visitor)
    {
      UpdateGraphicAttributes(geometry);

      if (visitor(geometry))
      {
        switch (geometry)
        {
          case ARDB.GeometryElement element:
            foreach (var g in element.SelectMany(x => x.ToGeometryBaseMany(visitor)))
              yield return g;
            yield break;

          case ARDB.GeometryInstance instance:
            foreach (var g in instance.GetInstanceGeometry().ToGeometryBaseMany(visitor))
              yield return g;
            yield break;

          case ARDB.Mesh mesh:
            yield return SetGraphicAttributes(mesh.ToMesh());
            yield break;

          case ARDB.Solid solid:
            yield return SetGraphicAttributes(solid.ToBrep());
            yield break;

          case ARDB.Curve curve:
            yield return SetGraphicAttributes(curve.ToCurve());
            yield break;

          case ARDB.PolyLine polyline:
            yield return SetGraphicAttributes(polyline.ToPolylineCurve());
            yield break;
        }
      }
    }
  }
}

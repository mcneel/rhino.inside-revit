using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  using External.DB.Extensions;

  /// <summary>
  /// Converts a Rhino geometry type to an equivalent Revit geometry type.
  /// </summary>
  public static class GeometryEncoder
  {
    #region Context
    internal delegate void RuntimeMessage(int severity, string message, GeometryBase geometry);

    [DebuggerTypeProxy(typeof(DebugView))]
    internal sealed class Context : State<Context>
    {
      public static Context Push(ARDB.Document document)
      {
        var ctx = Push();
        if (!ctx.Document.IsEquivalent(document))
        {
          ctx.GraphicsStyleId = ARDB.ElementId.InvalidElementId;
          ctx.MaterialId = ARDB.ElementId.InvalidElementId;
          ctx.FaceMaterialId = default;
        }
        ctx.Document = document;
        ctx.Element = default;
        return ctx;
      }

      public static Context Push(ARDB.Element element)
      {
        var ctx = Push(element?.Document);
        ctx.Element = element;
        return ctx;
      }

      public ARDB.Document Document { get; private set; } = default;
      public ARDB.Element Element { get; private set; } = default;

      public ARDB.ElementId GraphicsStyleId = ARDB.ElementId.InvalidElementId;
      public ARDB.ElementId MaterialId = ARDB.ElementId.InvalidElementId;
      public IReadOnlyList<ARDB.ElementId> FaceMaterialId;
      public RuntimeMessage RuntimeMessage = NullRuntimeMessage;

      static void NullRuntimeMessage(int severity, string message, GeometryBase geometry) { }

      class DebugView
      {
        readonly Context context;
        public DebugView(Context value) => context = value;
        public ARDB.Document Document => context.Document;
        public ARDB.Element Element => context.Element;

        public ARDB.GraphicsStyle GraphicsStyle => context.Document?.GetElement(context.GraphicsStyleId) as ARDB.GraphicsStyle;
        public ARDB.Material Material => context.Document?.GetElement(context.MaterialId) as ARDB.Material;
        public IEnumerable<ARDB.Material> FaceMaterials
        {
          get
          {
            if (context.Document is null || context.FaceMaterialId is null) return default;
            return context.FaceMaterialId.Select(x => context.Document.GetElement(x) as ARDB.Material);
          }
        }
      }
    }
    #endregion

    #region Points and Vectors
    /// <summary>
    /// Converts the specified <see cref="Point2f" /> to an equivalent <see cref="ARDB.UV" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToUV(Point2f)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// DB.UV revitUVpoint = rhinoPoint2f.ToUV();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    /// 
    /// revit_uvpoint: DB.UV = rhino_point2f.ToUV()
    /// </code>
    /// 
    /// Using <see cref="ToUV(Point2f)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// DB.UV revitUVpoint = GeometryEncoder.ToUV(rhinoPoint2f)
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    /// 
    /// revit_uvpoint: DB.UV = ge.ToUV(rhino_point2f)
    /// </code>
    ///
    /// </example>
    /// <param name="point">Rhino point to convert.</param>
    /// <returns>Revit point that is equivalent to the provided Rhino point.</returns>
    public static ARDB::UV ToUV(this Point2f point)
    {
      double factor = UnitConverter.ToHostUnits;
      return new ARDB::UV(point.X * factor, point.Y * factor);
    }
    internal static ARDB::UV ToUV(this Point2f point, double factor)
    {
      return factor == 1.0 ?
        new ARDB::UV(point.X, point.Y) :
        new ARDB::UV(point.X * factor, point.Y * factor);
    }

    /// <summary>
    /// Converts the specified <see cref="Point2d" /> to an equivalent <see cref="ARDB.UV" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToUV(Point2d)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.UV revitUVpoint = rhinoPoint2d.ToUV();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_uvpoint: DB.UV = rhino_point2d.ToUV()
    /// </code>
    /// 
    /// Using <see cref="ToUV(Point2d)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.UV revitUVpoint = GeometryEncoder.ToUV(rhinoPoint2d);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_uvpoint: DB.UV = ge.ToUV(rhino_point2d)
    /// </code>
    ///
    /// </example>
    /// <param name="point">Rhino point to convert.</param>
    /// <returns>Revit point that is equivalent to the provided Rhino point.</returns>
    public static ARDB::UV ToUV(this Point2d point)
    {
      double factor = UnitConverter.ToHostUnits;
      return new ARDB::UV(point.X * factor, point.Y * factor);
    }
    internal static ARDB::UV ToUV(this Point2d point, double factor)
    {
      return factor == 1.0 ?
        new ARDB::UV(point.X, point.Y) :
        new ARDB::UV(point.X * factor, point.Y * factor);
    }

    /// <summary>
    /// Converts the specified <see cref="Vector2f" /> to an equivalent <see cref="ARDB.UV" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToUV(Vector2f)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.UV revitUVpoint = rhinoVector2f.ToUV();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_uvpoint: DB.UV = rhino_vector2f.ToUV()
    /// </code>
    /// 
    /// Using <see cref="ToUV(Vector2f)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.UV revitUVpoint = GeometryEncoder.ToUV(rhinoVector2f);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_uvpoint: DB.UV = ge.ToUV(rhino_vector2f)
    /// </code>
    ///
    /// </example>
    /// <param name="vector">Rhino vector to convert.</param>
    /// <returns>Revit point that is equivalent to the provided Rhino vector.</returns>
    public static ARDB::UV ToUV(this Vector2f vector)
    {
      return new ARDB::UV(vector.X, vector.Y);
    }
    internal static ARDB::UV ToUV(this Vector2f vector, double factor)
    {
      return factor == 1.0 ?
        new ARDB::UV(vector.X, vector.Y) :
        new ARDB::UV(vector.X * factor, vector.Y * factor);
    }

    /// <summary>
    /// Converts the specified <see cref="Vector2d" /> to an equivalent <see cref="ARDB.UV" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToUV(Vector2d)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.UV revitUVpoint = rhinoVector2d.ToUV();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_uvpoint: DB.UV = rhino_vector2d.ToUV()
    /// </code>
    /// 
    /// Using <see cref="ToUV(Vector2d)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.UV revitUVpoint = GeometryEncoder.ToUV(rhinoVector2d);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_uvpoint: DB.UV = ge.ToUV(rhino_vector2d)
    /// </code>
    ///
    /// </example>
    /// <param name="vector">Rhino vector to convert.</param>
    /// <returns>Revit point that is equivalent to the provided Rhino vector.</returns>
    public static ARDB::UV ToUV(this Vector2d vector)
    {
      return new ARDB::UV(vector.X, vector.Y);
    }
    internal static ARDB::UV ToUV(this Vector2d vector, double factor)
    {
      return factor == 1.0 ?
        new ARDB::UV(vector.X, vector.Y) :
        new ARDB::UV(vector.X * factor, vector.Y * factor);
    }

    /// <summary>
    /// Converts the specified <see cref="Point3f" /> to an equivalent <see cref="ARDB.XYZ" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToXYZ(Point3f)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.XYZ revitXYZPoint = rhinoPoint3f.ToXYZ();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_xyzpoint: DB.XYZ = rhino_point3f.ToXYZ()
    /// </code>
    /// 
    /// Using <see cref="ToXYZ(Point3f)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.XYZ revitXYZPoint = GeometryEncoder.ToXYZ(rhinoPoint3f);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_xyzpoint: DB.XYZ = ge.ToXYZ(rhino_point3f)
    /// </code>
    ///
    /// </example>
    /// <param name="point">Rhino point to convert.</param>
    /// <returns>Revit point that is equivalent to the provided Rhino point.</returns>
    public static ARDB::XYZ ToXYZ(this Point3f point)
    {
      double factor = UnitConverter.ToHostUnits;
      return new ARDB::XYZ(point.X * factor, point.Y * factor, point.Z * factor);
    }
    internal static ARDB::XYZ ToXYZ(this Point3f point, double factor)
    {
      return factor == 1.0 ?
        new ARDB::XYZ(point.X, point.Y, point.Z) :
        new ARDB::XYZ(point.X * factor, point.Y * factor, point.Z * factor);
    }

    /// <summary>
    /// Converts the specified <see cref="Point3d" /> to an equivalent <see cref="ARDB.XYZ" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToXYZ(Point3d)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.XYZ revitXYZPoint = rhinoPoint3d.ToXYZ();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_xyzpoint: DB.XYZ = rhino_point3d.ToXYZ()
    /// </code>
    /// 
    /// Using <see cref="ToXYZ(Point3d)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.XYZ revitXYZPoint = GeometryEncoder.ToXYZ(rhinoPoint3d);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_xyzpoint: DB.XYZ = ge.ToXYZ(rhino_point3d)
    /// </code>
    ///
    /// </example>
    /// <param name="point">Rhino point to convert.</param>
    /// <returns>Revit point that is equivalent to the provided Rhino point.</returns>
    public static ARDB::XYZ ToXYZ(this Point3d point)
    {
      double factor = UnitConverter.ToHostUnits;
      return new ARDB::XYZ(point.X * factor, point.Y * factor, point.Z * factor);
    }
    internal static ARDB::XYZ ToXYZ(this Point3d point, double factor)
    {
      return factor == 1.0 ?
        new ARDB::XYZ(point.X, point.Y, point.Z) :
        new ARDB::XYZ(point.X * factor, point.Y * factor, point.Z * factor);
    }

    /// <summary>
    /// Converts the specified <see cref="Vector3f" /> to an equivalent <see cref="ARDB.XYZ" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToXYZ(Vector3f)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.XYZ revitXYZPoint = rhinoVector3f.ToXYZ();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_xyzpoint: DB.XYZ = rhino_vector3f.ToXYZ()
    /// </code>
    /// 
    /// Using <see cref="ToXYZ(Vector3f)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.XYZ revitXYZPoint = GeometryEncoder.ToXYZ(rhinoVector3f);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_xyzpoint: DB.XYZ = ge.ToXYZ(rhino_vector3f)
    /// </code>
    ///
    /// </example>
    /// <param name="vector">Rhino vector to convert.</param>
    /// <returns>Revit point that is equivalent to the provided Rhino vector.</returns>
    public static ARDB::XYZ ToXYZ(this Vector3f vector)
    {
      return new ARDB::XYZ(vector.X, vector.Y, vector.Z);
    }
    internal static ARDB::XYZ ToXYZ(this Vector3f vector, double factor)
    {
      return factor == 1.0 ?
        new ARDB::XYZ(vector.X, vector.Y, vector.Z) :
        new ARDB::XYZ(vector.X * factor, vector.Y * factor, vector.Z * factor);
    }

    /// <summary>
    /// Converts the specified <see cref="Vector3d" /> to an equivalent <see cref="ARDB.XYZ" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToXYZ(Vector3d)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.XYZ revitXYZPoint = rhinoVector3d.ToXYZ();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_xyzpoint: DB.XYZ = rhino_vector3d.ToXYZ()
    /// </code>
    /// 
    /// Using <see cref="ToXYZ(Vector3d)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.XYZ revitXYZPoint = GeometryEncoder.ToXYZ(rhinoVector3d);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_xyzpoint: DB.XYZ = ge.ToXYZ(rhino_vector3d)
    /// </code>
    ///
    /// </example>
    /// <param name="vector">Rhino vector to convert.</param>
    /// <returns>Revit point that is equivalent to the provided Rhino vector.</returns>
    public static ARDB::XYZ ToXYZ(this Vector3d vector)
    {
      return new ARDB::XYZ(vector.X, vector.Y, vector.Z);
    }
    internal static ARDB::XYZ ToXYZ(this Vector3d vector, double factor)
    {
      return factor == 1.0 ?
        new ARDB::XYZ(vector.X, vector.Y, vector.Z) :
        new ARDB::XYZ(vector.X * factor, vector.Y * factor, vector.Z * factor);
    }

    /// <summary>
    /// Converts the specified <see cref="Rhino.Geometry.Plane" /> to an equivalent <see cref="ARDB.Plane" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToPlane(Rhino.Geometry.Plane)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Plane revitPlane = rhinoPlane.ToPlane();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_plane: DB.Plane = rhino_plane.ToPlane()
    /// </code>
    /// 
    /// Using <see cref="ToPlane(Rhino.Geometry.Plane)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Plane revitPlane = GeometryEncoder.ToPlane(rhinoPlane);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_plane: DB.Plane = ge.ToPlane(rhino_plane)
    /// </code>
    ///
    /// </example>
    /// <param name="plane">Rhino plane to convert.</param>
    /// <returns>Revit Plane that is equivalent to the provided Rhino plane.</returns>
    public static ARDB.Plane ToPlane(this Plane plane) => ToPlane(plane, UnitConverter.ToHostUnits);
    internal static ARDB.Plane ToPlane(this Plane plane, double factor)
    {
      return ARDB.Plane.CreateByOriginAndBasis(plane.Origin.ToXYZ(factor), plane.XAxis.ToXYZ(), plane.YAxis.ToXYZ());
    }

    /// <summary>
    /// Converts the specified <see cref="Transform" /> to an equivalent <see cref="ARDB.Transform" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToTransform(Transform)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Transform revitTransform = rhinoTransform.ToTransform();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_transform: DB.Transform = rhino_transform.ToTransform()
    /// </code>
    /// 
    /// Using <see cref="ToTransform(Transform)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Transform revitTransform = GeometryEncoder.ToTransform(rhinoTransform);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_transform: DB.Transform = ge.ToTransform(rhino_transform)
    /// </code>
    ///
    /// </example>
    /// <param name="transform">Rhino transform to convert.</param>
    /// <returns>Revit transfrom that is equivalent to the provided Rhino transform.</returns>
    public static ARDB.Transform ToTransform(this Transform transform) => ToTransform(transform, UnitConverter.ToHostUnits);
    internal static ARDB.Transform ToTransform(this Transform transform, double factor)
    {
      Debug.Assert(transform.IsAffine);

      var result = factor == 1.0 ?
        ARDB.Transform.CreateTranslation(new ARDB.XYZ(transform.M03, transform.M13, transform.M23)) :
        ARDB.Transform.CreateTranslation(new ARDB.XYZ(transform.M03 * factor, transform.M13 * factor, transform.M23 * factor));

      result.BasisX = new ARDB.XYZ(transform.M00, transform.M10, transform.M20);
      result.BasisY = new ARDB.XYZ(transform.M01, transform.M11, transform.M21);
      result.BasisZ = new ARDB.XYZ(transform.M02, transform.M12, transform.M22);
      return result;
    }

    /// <summary>
    /// Converts the specified <see cref="BoundingBox" /> to an equivalent <see cref="ARDB.BoundingBoxXYZ" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToBoundingBoxXYZ(BoundingBox)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.BoundingBoxXYZ revitBBox = rhinoBBox.ToBoundingBoxXYZ();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_bbox: DB.BoundingBoxXYZ = rhino_bbox.ToBoundingBoxXYZ()
    /// </code>
    /// 
    /// Using <see cref="ToBoundingBoxXYZ(BoundingBox)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.BoundingBoxXYZ revitBBox = GeometryEncoder.ToBoundingBoxXYZ(rhinoBBox);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_bbox: DB.BoundingBoxXYZ = ge.ToBoundingBoxXYZ(rhino_bbox)
    /// </code>
    /// 
    /// </example>
    /// <param name="boundingBox">Rhino bounding box to convert.</param>
    /// <returns>Revit bounding box that is equivalent to the provided Rhino bounding box.</returns>
    public static ARDB.BoundingBoxXYZ ToBoundingBoxXYZ(this BoundingBox boundingBox) => ToBoundingBoxXYZ(boundingBox, UnitConverter.ToHostUnits);
    internal static ARDB.BoundingBoxXYZ ToBoundingBoxXYZ(this BoundingBox boundingBox, double factor)
    {
      return new ARDB.BoundingBoxXYZ
      {
        Min = boundingBox.Min.ToXYZ(factor),
        Max = boundingBox.Min.ToXYZ(factor),
        Enabled = boundingBox.IsValid
      };
    }

    /// <summary>
    /// Converts the specified <see cref="Rhino.Geometry.Box" /> to an equivalent <see cref="ARDB.BoundingBoxXYZ" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToBoundingBoxXYZ(Rhino.Geometry.Box)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.BoundingBoxXYZ revitBBox = rhinoBox.ToBoundingBoxXYZ();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_bbox: DB.BoundingBoxXYZ = rhino_box.ToBoundingBoxXYZ()
    /// </code>
    /// 
    /// Using <see cref="ToBoundingBoxXYZ(Rhino.Geometry.Box)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.BoundingBoxXYZ revitBBox = GeometryEncoder.ToBoundingBoxXYZ(rhinoBox);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_bbox: DB.BoundingBoxXYZ = ge.ToBoundingBoxXYZ(rhino_box)
    /// </code>
    /// 
    /// </example>
    /// <param name="box">Rhino box to convert.</param>
    /// <returns>Revit bounding box that is equivalent to the provided Rhino box.</returns>
    public static ARDB.BoundingBoxXYZ ToBoundingBoxXYZ(this Box box) => ToBoundingBoxXYZ(box, UnitConverter.ToHostUnits);
    internal static ARDB.BoundingBoxXYZ ToBoundingBoxXYZ(this Box box, double factor)
    {
      return new ARDB.BoundingBoxXYZ
      {
        Transform = Transform.PlaneToPlane(Plane.WorldXY, box.Plane).ToTransform(factor),
        Min = new ARDB.XYZ(box.X.Min * factor, box.Y.Min * factor, box.Z.Min * factor),
        Max = new ARDB.XYZ(box.X.Max * factor, box.Y.Max * factor, box.Z.Max * factor),
        Enabled = box.IsValid
      };
    }

    /// <summary>
    /// Converts the specified <see cref="BoundingBox" /> to an equivalent <see cref="ARDB.Outline" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToOutline(BoundingBox)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Outline revitOutline = rhinoBBox.ToOutline();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_outline: DB.Outline = rhino_bbox.ToOutline()
    /// </code>
    /// 
    /// Using <see cref="ToOutline(BoundingBox)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Outline revitOutline = GeometryEncoder.ToOutline(rhinoBBox);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_outline: DB.Outline = ge.ToOutline(rhino_bbox)
    /// </code>
    /// 
    /// </example>
    /// <param name="boundingBox">Rhino bounding box to convert.</param>
    /// <returns>Revit outline that is equivalent to the provided Rhino bounding box.</returns>
    public static ARDB.Outline ToOutline(this BoundingBox boundingBox) => ToOutline(boundingBox, UnitConverter.ToHostUnits);
    internal static ARDB.Outline ToOutline(this BoundingBox boundingBox, double factor)
    {
      return new ARDB.Outline(boundingBox.Min.ToXYZ(factor), boundingBox.Max.ToXYZ(factor));
    }
    #endregion

    #region Curves
    /// <summary>
    /// Converts the specified <see cref="Line" /> to an equivalent <see cref="ARDB.Line" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToLine(Line)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Line revitLine = rhinoLine.ToLine();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_line: DB.Line = rhino_line.ToLine()
    /// </code>
    /// 
    /// Using <see cref="ToLine(Line)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Line revitLine = GeometryEncoder.ToLine(rhinoLine);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_line: DB.Line = ge.ToLine(rhino_line)
    /// </code>
    /// 
    /// </example>
    /// <param name="line">Rhino line to convert.</param>
    /// <returns>Revit line that is equivalent to the provided Rhino line.</returns>
    public static ARDB.Line ToLine(this Line line) => line.ToLine(UnitConverter.ToHostUnits);
    internal static ARDB.Line ToLine(this Line line, double factor)
    {
      return ARDB.Line.CreateBound(line.From.ToXYZ(factor), line.To.ToXYZ(factor));
    }

    /// <summary>
    /// Converts the specified <see cref="Polyline" /> to an equivalent array of <see cref="ARDB.Line" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToLines(Polyline)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Line[] revitLines = rhinoPolyLine.ToLines();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from System import Array
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_lines: Array[DB.Line] = rhino_polyline.ToLines()
    /// </code>
    /// 
    /// Using <see cref="ToLines(Polyline)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Line[] revitLines = GeometryEncoder.ToLines(rhinoPolyLine);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from System import Array
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_lines: Array[DB.Line] = ge.ToLines(rhino_polyline)
    /// </code>
    /// 
    /// </example>
    /// <param name="polyline">Rhino polyline to convert.</param>
    /// <returns>A Revit line array that is equivalent to the provided Rhino polyline.</returns>
    public static ARDB.Line[] ToLines(this Polyline polyline) => polyline.ToLines(UnitConverter.ToHostUnits);
    internal static ARDB.Line[] ToLines(this Polyline polyline, double factor)
    {
      polyline = polyline.Duplicate();
      polyline.DeleteShortSegments(Revit.ShortCurveTolerance / factor);

      int count = polyline.Count;
      var list = new ARDB.Line[Math.Max(0, count - 1)];
      if (count > 1)
      {
        var point = polyline[0];
        ARDB.XYZ end, start = new ARDB.XYZ(point.X * factor, point.Y * factor, point.Z * factor);
        for (int p = 1; p < count; start = end, ++p)
        {
          point = polyline[p];
          end = new ARDB.XYZ(point.X * factor, point.Y * factor, point.Z * factor);
          list[p - 1] = ARDB.Line.CreateBound(start, end);
        }
      }

      return list;
    }

    /// <summary>
    /// Converts the specified <see cref="Polyline" /> to an equivalent of <see cref="ARDB.PolyLine" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToPolyLine(Polyline)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.PolyLine revitPolyLine = rhinoPolyLine.ToPolyLine();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_polyline: DB.PolyLine = rhino_polyline.ToPolyLine()
    /// </code>
    /// 
    /// Using <see cref="ToPolyLine(Polyline)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.PolyLine revitPolyLine = GeometryEncoder.ToPolyLine(rhinoPolyLine);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_polyline: DB.PolyLine = ge.ToPolyLine(rhino_polyline)
    /// </code>
    /// 
    /// </example>
    /// <param name="polyline">Rhino polyline to convert.</param>
    /// <returns>Revit polyline that is equivalent to the provided Rhino polyline.</returns>
    public static ARDB.PolyLine ToPolyLine(this Polyline polyline) => polyline.ToPolyLine(UnitConverter.ToHostUnits);
    internal static ARDB.PolyLine ToPolyLine(this Polyline polyline, double factor)
    {
      int count = polyline.Count;
      var points = new ARDB.XYZ[count];

      if (factor == 1.0)
      {
        for (int p = 0; p < count; ++p)
        {
          var point = polyline[p];
          points[p] = new ARDB.XYZ(point.X, point.Y, point.Z);
        }
      }
      else
      {
        for (int p = 0; p < count; ++p)
        {
          var point = polyline[p];
          points[p] = new ARDB.XYZ(point.X * factor, point.Y * factor, point.Z * factor);
        }
      }

      return ARDB.PolyLine.Create(points);
    }

    /// <summary>
    /// Converts the specified <see cref="Arc" /> to an equivalent of <see cref="ARDB.Arc" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToArc(Arc)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Arc revitArc = rhinoArc.ToArc();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_arc: DB.Arc = rhino_arc.ToArc()
    /// </code>
    /// 
    /// Using <see cref="ToArc(Arc)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Arc revitArc = GeometryEncoder.ToArc(rhinoArc);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_arc: DB.Arc = ge.ToArc(rhino_arc)
    /// </code>
    /// 
    /// </example>
    /// <param name="arc">Rhino arc to convert.</param>
    /// <returns>Revit arc that is equivalent to the provided Rhino arc.</returns>
    public static ARDB.Arc ToArc(this Arc arc) => arc.ToArc(UnitConverter.ToHostUnits);
    internal static ARDB.Arc ToArc(this Arc arc, double factor)
    {
      if (arc.IsCircle)
        return ARDB.Arc.Create(arc.Plane.ToPlane(factor), arc.Radius * factor, 0.0, 2.0 * Math.PI);
      else
        return ARDB.Arc.Create(arc.StartPoint.ToXYZ(factor), arc.EndPoint.ToXYZ(factor), arc.MidPoint.ToXYZ(factor));
    }

    /// <summary>
    /// Converts the specified <see cref="Circle" /> to an equivalent of <see cref="ARDB.Arc" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToArc(Circle)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Arc revitArc = rhinoCircle.ToArc();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_arc: DB.Arc = rhino_circle.ToArc()
    /// </code>
    /// 
    /// Using <see cref="ToArc(Circle)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Arc revitArc = GeometryEncoder.ToArc(rhinoCircle);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_arc: DB.Arc = ge.ToArc(rhino_circle)
    /// </code>
    /// 
    /// </example>
    /// <param name="circle">Rhino circle to convert.</param>
    /// <returns>Revit arc that is equivalent to the provided Rhino circle.</returns>
    public static ARDB.Arc ToArc(this Circle circle) => circle.ToArc(UnitConverter.ToHostUnits);
    internal static ARDB.Arc ToArc(this Circle circle, double factor)
    {
      return ARDB.Arc.Create(circle.Plane.ToPlane(factor), circle.Radius * factor, 0.0, 2.0 * Math.PI);
    }

    /// <summary>
    /// Converts the specified <see cref="Ellipse" /> to an equivalent of <see cref="ARDB.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurve(Ellipse)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Curve revitCurve = rhinoEllipse.ToCurve();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_curve: DB.Curve = rhino_ellipse.ToCurve()
    /// </code>
    /// 
    /// Using <see cref="ToCurve(Ellipse)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Curve revitCurve = GeometryEncoder.ToCurve(rhinoEllipse);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_curve: DB.Curve = ge.ToCurve(rhino_ellipse)
    /// </code>
    /// 
    /// </example>
    /// <param name="ellipse">Rhino ellipse to convert.</param>
    /// <returns>Revit curve that is equivalent to the provided Rhino ellipse.</returns>
    public static ARDB.Curve ToCurve(this Ellipse ellipse) => ellipse.ToCurve(new Interval(0.0, 2.0 * Math.PI), UnitConverter.ToHostUnits);
    internal static ARDB.Curve ToCurve(this Ellipse ellipse, double factor) => ellipse.ToCurve(new Interval(0.0, 2.0 * Math.PI), factor);

    /// <summary>
    /// Converts the specified <see cref="Ellipse" /> within the given <see cref="Interval" />to an equivalent of <see cref="ARDB.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurve(Ellipse, Interval)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// var interval = new Interval(0.0, 0.5);
    /// DB.Curve revitCurve = rhinoEllipse.ToCurve(interval);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// interval = Interval(0.0, 0.5);
    /// revit_curve: DB.Curve = rhino_ellipse.ToCurve(interval)
    /// </code>
    /// 
    /// Using <see cref="ToCurve(Ellipse, Interval)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// var interval = new Interval(0.0, 0.5);
    /// DB.Curve revitCurve = GeometryEncoder.ToCurve(rhinoEllipse, interval);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// interval = Interval(0.0, 0.5)
    /// revit_curve: DB.Curve = ge.ToCurve(rhino_ellipse, interval)
    /// </code>
    /// 
    /// </example>
    /// <param name="ellipse">Rhino ellipse to convert.</param>
    /// <param name="interval">Interval where the ellipse is defined.</param>
    /// <returns>Revit curve that is equivalent to the provided Rhino ellipse within the given interval</returns>
    public static ARDB.Curve ToCurve(this Ellipse ellipse, Interval interval) => ellipse.ToCurve(interval, UnitConverter.ToHostUnits);
    internal static ARDB.Curve ToCurve(this Ellipse ellipse, Interval interval, double factor)
    {
#if REVIT_2018
      return ARDB.Ellipse.CreateCurve(ellipse.Plane.Origin.ToXYZ(factor), ellipse.Radius1 * factor, ellipse.Radius2 * factor, ellipse.Plane.XAxis.ToXYZ(), ellipse.Plane.YAxis.ToXYZ(), interval.Min, interval.Max);
#else
      return DB.Ellipse.Create(ellipse.Plane.Origin.ToXYZ(factor), ellipse.Radius1 * factor, ellipse.Radius2 * factor, ellipse.Plane.XAxis.ToXYZ(), ellipse.Plane.YAxis.ToXYZ(), interval.Min, interval.Max);
#endif
    }
    #endregion

    #region GeometryBase

    #region Points
    /// <summary>
    /// Converts the specified <see cref="Point" /> to an equivalent of <see cref="ARDB.Point" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToPoint(Point)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Point revitPoint = rhinoPoint.ToPoint();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_point: DB.Point = rhino_point.ToPoint()
    /// </code>
    /// 
    /// Using <see cref="ToPoint(Point)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Point revitPoint = GeometryEncoder.ToPoint(rhinoPoint);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_point: DB.Point = ge.ToPoint(rhino_point)
    /// </code>
    /// 
    /// </example>
    /// <param name="point">Rhino point to convert.</param>
    /// <returns>Revit Point that is equivalent to the provided Rhino point.</returns>
    public static ARDB.Point ToPoint(this Point point) => point.ToPoint(UnitConverter.ToHostUnits);
    internal static ARDB.Point ToPoint(this Point point, double factor)
    {
      return ARDB.Point.Create(point.Location.ToXYZ(factor));
    }

    /// <summary>
    /// Converts the specified <see cref="PointCloud" /> to an equivalent array of <see cref="ARDB.Point" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToPoints(PointCloud)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Point[] revitPoints = rhinoPointCloud.ToPoints();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from System import Array
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_points: Array[DB.Point] = rhino_pointcloud.ToPoints()
    /// </code>
    /// 
    /// Using <see cref="ToPoints(PointCloud)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Point[] revitPoints = GeometryEncoder.ToPoints(rhinoPointCloud);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// from System import Array
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_points: Array[DB.Point] = ge.ToPoints(rhino_pointcloud)
    /// </code>
    /// 
    /// </example>
    /// <param name="pointCloud">Rhino pointcloud to convert.</param>
    /// <returns>Revit point array that is equivalent to the provided Rhino pointcloud.</returns>
    public static ARDB.Point[] ToPoints(this PointCloud pointCloud) => pointCloud.ToPoints(UnitConverter.ToHostUnits);
    internal static ARDB.Point[] ToPoints(this PointCloud pointCloud, double factor)
    {
      var array = new ARDB.Point[pointCloud.Count];
      int index = 0;
      if (factor == 1.0)
      {
        foreach (var point in pointCloud)
        {
          var location = point.Location;
          array[index++] = ARDB.Point.Create(new ARDB::XYZ(location.X, location.Y, location.Z));
        }
      }
      else
      {
        foreach (var point in pointCloud)
        {
          var location = point.Location;
          array[index++] = ARDB.Point.Create(new ARDB::XYZ(location.X * factor, location.Y * factor, location.Z * factor));
        }
      }

      return array;
    }
    #endregion

    #region Curves
    /// <summary>
    /// Converts the specified <see cref="LineCurve" /> to an equivalent of <see cref="ARDB.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurve(LineCurve)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Curve revitCurve = rhinoLineCurve.ToCurve();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_curve: DB.Curve = rhino_linecurve.ToCurve()
    /// </code>
    /// 
    /// Using <see cref="ToCurve(LineCurve)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Curve revitCurve = GeometryEncoder.ToCurve(rhinoLineCurve);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_curve: DB.Curve = ge.ToCurve(rhino_linecurve)
    /// </code>
    /// 
    /// </example>
    /// <param name="lineCurve">Rhino lineCurve to convert.</param>
    /// <returns>Revit curve that is equivalent to the provided Rhino lineCurve.</returns>
    public static ARDB.Curve ToCurve(this LineCurve lineCurve) => lineCurve.Line.ToLine(UnitConverter.ToHostUnits);
    internal static ARDB.Curve ToCurve(this LineCurve lineCurve, double factor) => lineCurve.Line.ToLine(factor);

    /// <summary>
    /// Converts the specified <see cref="PolylineCurve" /> to an equivalent of <see cref="ARDB.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurve(PolylineCurve)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Curve revitCurve = rhinoPolylineCurve.ToCurve();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_curve: DB.Curve = rhino_polylinecurve.ToCurve()
    /// </code>
    /// 
    /// Using <see cref="ToCurve(PolylineCurve)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Curve revitCurve = GeometryEncoder.ToCurve(rhinoPolylineCurve);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_curve: DB.Curve = ge.ToCurve(rhino_polylinecurve)
    /// </code>
    /// 
    /// </example>
    /// <param name="polylineCurve">Rhino polylineCurve to convert.</param>
    /// <returns>Revit curve that is equivalent to the provided Rhino polylineCurve.</returns>
    public static ARDB.Curve ToCurve(this PolylineCurve polylineCurve) => ToCurve(polylineCurve, UnitConverter.ToHostUnits);
    internal static ARDB.Curve ToCurve(this PolylineCurve polylineCurve, double factor)
    {
      if (polylineCurve.TryGetLine(out var line, Revit.VertexTolerance * factor))
        return line.ToLine(factor);

      throw new ConversionException("Failed to convert non G1 continuous curve.");
    }

    /// <summary>
    /// Converts the specified <see cref="ArcCurve" /> to an equivalent of <see cref="ARDB.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurve(ArcCurve)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Curve revitCurve = rhinoArcCurve.ToCurve();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_curve: DB.Curve = rhino_arccurve.ToCurve()
    /// </code>
    /// 
    /// Using <see cref="ToCurve(ArcCurve)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Curve revitCurve = GeometryEncoder.ToCurve(rhinoArcCurve);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_curve: DB.Curve = ge.ToCurve(rhino_arccurve)
    /// </code>
    /// 
    /// </example>
    /// <param name="arcCurve">Rhino arcCurve to convert.</param>
    /// <returns>Revit curve that is equivalent to the provided Rhino arcCurve.</returns>
    public static ARDB.Curve ToCurve(this ArcCurve arcCurve) => arcCurve.Arc.ToArc(UnitConverter.ToHostUnits);
    internal static ARDB.Curve ToCurve(this ArcCurve arcCurve, double factor) => arcCurve.Arc.ToArc(factor);

    /// <summary>
    /// Converts the specified <see cref="NurbsCurve" /> to an equivalent of <see cref="ARDB.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurve(NurbsCurve)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Curve revitCurve = rhinoNurbsCurve.ToCurve();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_curve: DB.Curve = rhino_nurbscurve.ToCurve()
    /// </code>
    /// 
    /// Using <see cref="ToCurve(NurbsCurve)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Curve revitCurve = GeometryEncoder.ToCurve(rhinoNurbsCurve);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_curve: DB.Curve = ge.ToCurve(rhino_nurbscurve)
    /// </code>
    /// 
    /// </example>
    /// <param name="nurbsCurve">Rhino nurbsCurve to convert.</param>
    /// <returns>Revit curve that is equivalent to the provided Rhino nurbsCurve.</returns>
    public static ARDB.Curve ToCurve(this NurbsCurve nurbsCurve) => nurbsCurve.ToCurve(UnitConverter.ToHostUnits);
    internal static ARDB.Curve ToCurve(this NurbsCurve nurbsCurve, double factor)
    {
      if (nurbsCurve.TryGetEllipse(out var ellipse, out var interval, Revit.VertexTolerance * factor))
        return ellipse.ToCurve(interval, factor);

      var gap = Revit.ShortCurveTolerance * 1.01 / factor;
      if (nurbsCurve.IsClosed(gap))
      {
        var length = nurbsCurve.GetLength();
        if
        (
          length > gap &&
          nurbsCurve.LengthParameter((gap / 2.0), out var t0) &&
          nurbsCurve.LengthParameter(length - (gap / 2.0), out var t1)
        )
        {
          var segments = nurbsCurve.Split(new double[] { t0, t1 });
          nurbsCurve = segments[0] as NurbsCurve ?? nurbsCurve;
        }
        else throw new ConversionException($"Failed to Split closed NurbsCurve, Length = {length}");
      }

      if (nurbsCurve.Degree < 3 && nurbsCurve.SpanCount > 1)
      {
        nurbsCurve = nurbsCurve.DuplicateCurve() as NurbsCurve;
        nurbsCurve.IncreaseDegree(3);
      }

      return NurbsSplineEncoder.ToNurbsSpline(nurbsCurve, factor);
    }

    /// <summary>
    /// Converts the specified <see cref="PolyCurve" /> to an equivalent of <see cref="ARDB.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurve(PolyCurve)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Curve revitCurve = rhinoPolyCurve.ToCurve();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_curve: DB.Curve = rhino_polycurve.ToCurve()
    /// </code>
    /// 
    /// Using <see cref="ToCurve(PolyCurve)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Curve revitCurve = GeometryEncoder.ToCurve(rhinoPolyCurve);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_curve: DB.Curve = ge.ToCurve(rhino_polycurve)
    /// </code>
    /// 
    /// </example>
    /// <param name="polyCurve">Rhino polyCurve to convert.</param>
    /// <returns>Revit curve that is equivalent to the provided Rhino polyCurve.</returns>
    public static ARDB.Curve ToCurve(this PolyCurve polyCurve) => ToCurve(polyCurve, UnitConverter.ToHostUnits);
    internal static ARDB.Curve ToCurve(this PolyCurve polyCurve, double factor)
    {
      var curve = polyCurve.Simplify
      (
        CurveSimplifyOptions.AdjustG1 |
        CurveSimplifyOptions.Merge,
        Revit.VertexTolerance * factor,
        Revit.AngleTolerance
      )
      ?? polyCurve;

      if (curve is PolyCurve)
        return curve.ToNurbsCurve().ToCurve(factor);
      else
        return curve.ToCurve(factor);
    }

    /// <summary>
    /// Converts the specified <see cref="Curve" /> to an equivalent of <see cref="ARDB.Curve" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurve(Curve)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Curve revitCurve = rhinoCurve.ToCurve();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_curve: DB.Curve = rhino_curve.ToCurve()
    /// </code>
    /// 
    /// Using <see cref="ToCurve(Curve)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Curve revitCurve = GeometryEncoder.ToCurve(rhinoCurve);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_curve: DB.Curve = ge.ToCurve(rhino_curve)
    /// </code>
    /// 
    /// </example>
    /// <param name="curve">Rhino curve to convert.</param>
    /// <returns>Revit curve that is equivalent to the provided Rhino curve.</returns>
    public static ARDB.Curve ToCurve(this Curve curve) => curve.ToCurve(UnitConverter.ToHostUnits);
    internal static ARDB.Curve ToCurve(this Curve curve, double factor)
    {
      switch (curve)
      {
        case LineCurve line:
          return line.Line.ToLine(factor);

        case ArcCurve arc:
          return arc.Arc.ToArc(factor);

        case PolylineCurve polyline:
          return polyline.ToCurve(factor);

        case PolyCurve polyCurve:
          return polyCurve.ToCurve(factor);

        case NurbsCurve nurbsCurve:
          return nurbsCurve.ToCurve(factor);

        default:
          return curve.ToNurbsCurve().ToCurve(factor);
      }
    }

    /// <summary>
    /// Converts the specified <see cref="Curve" /> to an equivalent of <see cref="ARDB.CurveLoop" /> containing consecutive segments of the input curve.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurveLoop(Curve)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.CurveLoop revitCurveLoop = rhinoCurve.ToCurveLoop();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_curveloop: DB.CurveLoop = rhino_curve.ToCurveLoop()
    /// </code>
    /// 
    /// Using <see cref="ToCurveLoop(Curve)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.CurveLoop revitCurveLoop = GeometryEncoder.ToCurveLoop(rhinoCurve);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_curveloop: DB.CurveLoop = ge.ToCurveLoop(rhino_curve)
    /// </code>
    /// 
    /// </example>
    /// <param name="curve">Rhino curve to convert.</param>
    /// <returns>Revit curveLoop that contains consecutive segments of provided Rhino curve.</returns>
    public static ARDB.CurveLoop ToCurveLoop(this Curve curve)
    {
      curve = curve.InOtherUnits(UnitConverter.ToHostUnits);
      curve.CombineShortSegments(Revit.ShortCurveTolerance);

      return ARDB.CurveLoop.Create(curve.ToCurveMany(UnitConverter.NoScale).SelectMany(x => x.ToBoundedCurves()).ToList());
    }

    /// <summary>
    /// Converts the specified <see cref="Curve" /> to an equivalent of <see cref="ARDB.CurveArray" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToCurveArray(Curve)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.CurveArray revitCurveArray = rhinoCurve.ToCurveArray();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_curvearray: DB.CurveArray = rhino_curve.ToCurveArray()
    /// </code>
    /// 
    /// Using <see cref="ToCurveArray(Curve)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.CurveArray revitCurveArray = GeometryEncoder.ToCurveArray(rhinoCurve);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_curvearray: DB.CurveArray = ge.ToCurveArray(rhino_curve)
    /// </code>
    /// 
    /// </example>
    /// <param name="curve">Rhino curve to convert.</param>
    /// <returns>Revit curveArray that contains consecutive segments of provided Rhino curve.</returns>
    public static ARDB.CurveArray ToCurveArray(this Curve curve)
    {
      curve = curve.InOtherUnits(UnitConverter.ToHostUnits);
      curve.CombineShortSegments(Revit.ShortCurveTolerance);

      return curve.ToCurveMany(UnitConverter.NoScale).SelectMany(x => x.ToBoundedCurves()).ToCurveArray();
    }

    internal static ARDB.CurveArrArray ToCurveArrArray(this IList<Curve> curves)
    {
      var curveArrayArray = new ARDB.CurveArrArray();
      foreach (var curve in curves)
        curveArrayArray.Append(curve.ToCurveArray());

      return curveArrayArray;
    }
    #endregion

    #region Solids
    /// <summary>
    /// Converts the specified <see cref="Brep" /> to an equivalent of <see cref="ARDB.Solid" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToSolid(Brep)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Solid revitSolid = rhinoBrep.ToSolid();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_solid: DB.Solid = rhino_brep.ToSolid()
    /// </code>
    /// 
    /// Using <see cref="ToSolid(Brep)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Solid revitSolid = GeometryEncoder.ToSolid(rhinoBrep);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_solid: DB.Solid = ge.ToSolid(rhino_brep)
    /// </code>
    /// 
    /// </example>
    /// <param name="brep">Rhino brep to convert.</param>
    /// <returns>Revit solid that is equivalent to the provided Rhino brep.</returns>
    public static ARDB.Solid ToSolid(this Brep brep) => BrepEncoder.ToSolid(brep, UnitConverter.ToHostUnits);
    internal static ARDB.Solid ToSolid(this Brep brep, double factor) => BrepEncoder.ToSolid(brep, factor);

    /// <summary>
    /// Converts the specified <see cref="Extrusion" /> to an equivalent of <see cref="ARDB.Solid" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToSolid(Extrusion)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Solid revitSolid = rhinoExtrusion.ToSolid();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_solid: DB.Solid = rhino_extrusion.ToSolid()
    /// </code>
    /// 
    /// Using <see cref="ToSolid(Extrusion)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Solid revitSolid = GeometryEncoder.ToSolid(rhinoExtrusion);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_solid: DB.Solid = ge.ToSolid(rhino_extrusion)
    /// </code>
    /// 
    /// </example>
    /// <param name="extrusion">Rhino extrusion to convert.</param>
    /// <returns>Revit solid that is equivalent to the provided Rhino extrusion.</returns>
    public static ARDB.Solid ToSolid(this Extrusion extrusion) => ExtrusionEncoder.ToSolid(extrusion, UnitConverter.ToHostUnits);
    internal static ARDB.Solid ToSolid(this Extrusion extrusion, double factor) => ExtrusionEncoder.ToSolid(extrusion, factor);

    /// <summary>
    /// Converts the specified <see cref="SubD" /> to an equivalent of <see cref="ARDB.Solid" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToSolid(SubD)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Solid revitSolid = rhinoSubD.ToSolid();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_solid: DB.Solid = rhino_subd.ToSolid()
    /// </code>
    /// 
    /// Using <see cref="ToSolid(SubD)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Solid revitSolid = GeometryEncoder.ToSolid(rhinoSubD);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_solid: DB.Solid = ge.ToSolid(rhino_subd)
    /// </code>
    /// 
    /// </example>
    /// <param name="subd">Rhino subd to convert.</param>
    /// <returns>Revit solid that is equivalent to the provided Rhino subd.</returns>
    public static ARDB.Solid ToSolid(this SubD subd) => SubDEncoder.ToSolid(subd, UnitConverter.ToHostUnits);
    internal static ARDB.Solid ToSolid(this SubD subd, double factor) => SubDEncoder.ToSolid(subd, factor);

    /// <summary>
    /// Converts the specified <see cref="Mesh" /> to an equivalent of <see cref="ARDB.Solid" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToSolid(Mesh)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Solid revitSolid = rhinoMesh.ToSolid();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_solid: DB.Solid = rhino_mesh.ToSolid()
    /// </code>
    /// 
    /// Using <see cref="ToSolid(Mesh)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Solid revitSolid = GeometryEncoder.ToSolid(rhinoMesh);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_solid: DB.Solid = ge.ToSolid(rhino_mesh)
    /// </code>
    /// 
    /// </example>
    /// <param name="mesh">Rhino mesh to convert.</param>
    /// <returns>Revit solid that is equivalent to the provided Rhino mesh.</returns>
    public static ARDB.Solid ToSolid(this Mesh mesh) => Raw.RawEncoder.ToHost(MeshEncoder.ToRawBrep(mesh, UnitConverter.ToHostUnits));
    internal static ARDB.Solid ToSolid(this Mesh mesh, double factor) => Raw.RawEncoder.ToHost(MeshEncoder.ToRawBrep(mesh, factor));
    #endregion

    #region Meshes
    /// <summary>
    /// Converts the specified <see cref="Brep" /> to an equivalent of <see cref="ARDB.Mesh" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToMesh(Brep)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Mesh revitMesh = rhinoBrep.ToMesh();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_mesh: DB.Mesh = rhino_brep.ToMesh()
    /// </code>
    /// 
    /// Using <see cref="ToMesh(Brep)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Mesh revitMesh = GeometryEncoder.ToMesh(rhinoBrep);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_mesh: DB.Mesh = ge.ToMesh(rhino_brep)
    /// </code>
    /// 
    /// </example>
    /// <param name="brep">Rhino brep to convert.</param>
    /// <returns>Revit mesh that is equivalent to the provided Rhino brep.</returns>
    public static ARDB.Mesh ToMesh(this Brep brep) => BrepEncoder.ToMesh(brep, UnitConverter.NoScale);
    internal static ARDB.Mesh ToMesh(this Brep brep, double factor) => BrepEncoder.ToMesh(brep, factor);

    /// <summary>
    /// Converts the specified <see cref="Extrusion" /> to an equivalent of <see cref="ARDB.Mesh" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToMesh(Extrusion)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Mesh revitMesh = rhinoExtrusion.ToMesh();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_mesh: DB.Mesh = rhino_extrusion.ToMesh()
    /// </code>
    /// 
    /// Using <see cref="ToMesh(Extrusion)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Mesh revitMesh = GeometryEncoder.ToMesh(rhinoExtrusion);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_mesh: DB.Mesh = ge.ToMesh(rhino_extrusion)
    /// </code>
    /// 
    /// </example>
    /// <param name="extrusion">Rhino extrusion to convert.</param>
    /// <returns>Revit mesh that is equivalent to the provided Rhino extrusion.</returns>
    public static ARDB.Mesh ToMesh(this Extrusion extrusion) => ExtrusionEncoder.ToMesh(extrusion, UnitConverter.NoScale);
    internal static ARDB.Mesh ToMesh(this Extrusion extrusion, double factor) => ExtrusionEncoder.ToMesh(extrusion, factor);

    /// <summary>
    /// Converts the specified <see cref="SubD" /> to an equivalent of <see cref="ARDB.Mesh" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToMesh(SubD)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Mesh revitMesh = rhinoSubD.ToMesh();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_mesh: DB.Mesh = rhino_subd.ToMesh()
    /// </code>
    /// 
    /// Using <see cref="ToMesh(SubD)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Mesh revitMesh = GeometryEncoder.ToMesh(rhinoSubD);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_mesh: DB.Mesh = ge.ToMesh(rhino_subd)
    /// </code>
    /// 
    /// </example>
    /// <param name="subd">Rhino subd to convert.</param>
    /// <returns>Revit mesh that is equivalent to the provided Rhino subd.</returns>
    public static ARDB.Mesh ToMesh(this SubD subd) => SubDEncoder.ToMesh(subd, UnitConverter.NoScale);
    internal static ARDB.Mesh ToMesh(this SubD subd, double factor) => SubDEncoder.ToMesh(subd, factor);

    /// <summary>
    /// Converts the specified <see cref="Mesh" /> to an equivalent of <see cref="ARDB.Mesh" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToMesh(Mesh)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Mesh revitMesh = rhinoMesh.ToMesh();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_mesh: DB.Mesh = rhino_mesh.ToMesh()
    /// </code>
    /// 
    /// Using <see cref="ToMesh(Mesh)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.Mesh revitMesh = GeometryEncoder.ToMesh(rhinoMesh);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_mesh: DB.Mesh = ge.ToMesh(rhino_mesh)
    /// </code>
    /// 
    /// </example>
    /// <param name="mesh">Rhino mesh to convert.</param>
    /// <returns>Revit mesh that is equivalent to the provided Rhino mesh.</returns>
    public static ARDB.Mesh ToMesh(this Mesh mesh) => MeshEncoder.ToMesh(MeshEncoder.ToRawMesh(mesh, UnitConverter.ToHostUnits));
    internal static ARDB.Mesh ToMesh(this Mesh mesh, double factor) => MeshEncoder.ToMesh(MeshEncoder.ToRawMesh(mesh, factor));
    #endregion

    /// <summary>
    /// Converts the specified <see cref="GeometryBase" /> to an equivalent of <see cref="ARDB.GeometryObject" />.
    /// </summary>
    /// <example>
    /// 
    /// Using <see cref="ToGeometryObject(GeometryBase)" /> as extension method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.GeometryObject revitGeomObj = rhinoGeom.ToGeometryObject();
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry
    /// clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)
    ///
    /// revit_geomobj: DB.GeometryObject = rhino_geom.ToGeometryObject()
    /// </code>
    /// 
    /// Using <see cref="ToGeometryObject(GeometryBase)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.GeometryObject revitGeomObj = GeometryEncoder.ToGeometryObject(rhinoGeom);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as ge
    ///
    /// revit_geomobj: DB.GeometryObject = ge.ToGeometryObject(rhino_geom)
    /// </code>
    /// 
    /// </example>
    /// <param name="geombase">Rhino geometryBase to convert.</param>
    /// <returns>Revit geometryObject that is equivalent to the provided Rhino geometryBase.</returns>
    public static ARDB.GeometryObject ToGeometryObject(this GeometryBase geombase) => ToGeometryObject(geombase, UnitConverter.ToHostUnits);
    internal static ARDB.GeometryObject ToGeometryObject(this GeometryBase geombase, double scaleFactor)
    {
      switch (geombase)
      {
        case Point point: return point.ToPoint(scaleFactor);
        case Curve curve: return curve.ToCurve(scaleFactor);
        case Brep brep: return brep.ToSolid(scaleFactor);
        case Extrusion extrusion: return extrusion.ToSolid(scaleFactor);
        case SubD subD: return subD.ToSolid(scaleFactor);
        case Mesh mesh: return mesh.ToMesh(scaleFactor);

        default:
          if (geombase.HasBrepForm)
          {
            var brepForm = Brep.TryConvertBrep(geombase);
            if (BrepEncoder.EncodeRaw(ref brepForm, scaleFactor))
              return BrepEncoder.ToSolid(brepForm);
          }
          break;
      }

      throw new ConversionException($"Unable to convert {geombase} to Autodesk.Revit.DB.GeometryObject");
    }
    #endregion

    internal static IEnumerable<ARDB.Point> ToPointMany(this PointCloud pointCloud) => pointCloud.ToPointMany(UnitConverter.ToHostUnits);
    internal static IEnumerable<ARDB.Point> ToPointMany(this PointCloud pointCloud, double factor)
    {
      if (factor == 1.0)
      {
        foreach (var point in pointCloud)
        {
          var location = point.Location;
          yield return ARDB.Point.Create(new ARDB::XYZ(location.X, location.Y, location.Z));
        }
      }
      else
      {
        foreach (var point in pointCloud)
        {
          var location = point.Location;
          yield return ARDB.Point.Create(new ARDB::XYZ(location.X * factor, location.Y * factor, location.Z * factor));
        }
      }
    }

    internal static IEnumerable<ARDB.Curve> ToCurveMany(this NurbsCurve nurbsCurve) => nurbsCurve.ToCurveMany(UnitConverter.ToHostUnits);
    internal static IEnumerable<ARDB.Curve> ToCurveMany(this NurbsCurve nurbsCurve, double factor)
    {
      // Convert to Raw form
      nurbsCurve = nurbsCurve.DuplicateCurve() as NurbsCurve;
      if (factor != 1.0) nurbsCurve.Scale(factor);
      nurbsCurve.CombineShortSegments(Revit.ShortCurveTolerance);

      // Transfer
      if (nurbsCurve.Degree == 1)
      {
        var curvePoints = nurbsCurve.Points;
        int pointCount = curvePoints.Count;
        if (pointCount > 1)
        {
          ARDB.XYZ end, start = curvePoints[0].Location.ToXYZ(UnitConverter.NoScale);
          for (int p = 1; p < pointCount; ++p)
          {
            end = curvePoints[p].Location.ToXYZ(UnitConverter.NoScale);
            if (end.DistanceTo(start) < Revit.ShortCurveTolerance)
              continue;

            yield return ARDB.Line.CreateBound(start, end);
            start = end;
          }
        }
      }
      else if (nurbsCurve.TryGetPolyCurve(out var polyCurve, Revit.AngleTolerance))
      {
        foreach (var segment in ToCurveMany(polyCurve, UnitConverter.NoScale))
          yield return segment;

        yield break;
      }
      else if (nurbsCurve.Degree == 2)
      {
        if (nurbsCurve.IsRational && nurbsCurve.TryGetEllipse(out var ellipse, out var interval, Revit.VertexTolerance))
        {
          // Only degree 2 rational NurbCurves should be transferred as an Arc-Ellipse
          // to avoid unexpected Arcs-Ellipses near linear with gigantic radius.
          yield return ellipse.ToCurve(interval, UnitConverter.NoScale);
        }
        else if (nurbsCurve.SpanCount == 1)
        {
          yield return NurbsSplineEncoder.ToNurbsSpline(nurbsCurve, UnitConverter.NoScale);
        }
        else
        {
          for (int s = 0; s < nurbsCurve.SpanCount; ++s)
          {
            var segment = nurbsCurve.Trim(nurbsCurve.SpanDomain(s)) as NurbsCurve;
            yield return NurbsSplineEncoder.ToNurbsSpline(segment, UnitConverter.NoScale);
          }
        }
      }
      else if (nurbsCurve.IsClosed(Revit.ShortCurveTolerance * 1.01))
      {
        var segments = nurbsCurve.DuplicateSegments();
        if (segments.Length == 1)
        {
          if
          (
            nurbsCurve.NormalizedLengthParameter(0.5, out var mid) &&
            nurbsCurve.Split(mid) is Curve[] half
          )
          {
            yield return NurbsSplineEncoder.ToNurbsSpline(half[0] as NurbsCurve, UnitConverter.NoScale);
            yield return NurbsSplineEncoder.ToNurbsSpline(half[1] as NurbsCurve, UnitConverter.NoScale);
          }
          else throw new ConversionException("Failed to Split closed Edge");
        }
        else
        {
          foreach (var segment in segments)
            yield return NurbsSplineEncoder.ToNurbsSpline(segment as NurbsCurve, UnitConverter.NoScale);
        }
      }
      else
      {
        yield return NurbsSplineEncoder.ToNurbsSpline(nurbsCurve, UnitConverter.NoScale);
      }
    }

    internal static IEnumerable<ARDB.Curve> ToCurveMany(this PolylineCurve polylineCurve) => polylineCurve.ToCurveMany(UnitConverter.ToHostUnits);
    internal static IEnumerable<ARDB.Curve> ToCurveMany(this PolylineCurve polylineCurve, double factor)
    {
      // Convert to Raw form
      polylineCurve = polylineCurve.DuplicateCurve() as PolylineCurve;
      if (factor != 1.0) polylineCurve.Scale(factor);
      polylineCurve.CombineShortSegments(Revit.ShortCurveTolerance);

      // Transfer
      int pointCount = polylineCurve.PointCount;
      if (pointCount > 1)
      {
        ARDB.XYZ end, start = polylineCurve.Point(0).ToXYZ(UnitConverter.NoScale);
        for (int p = 1; p < pointCount; ++p)
        {
          end = polylineCurve.Point(p).ToXYZ(UnitConverter.NoScale);
          if (start.DistanceTo(end) > Revit.ShortCurveTolerance)
          {
            yield return ARDB.Line.CreateBound(start, end);
            start = end;
          }
        }
      }
    }

    internal static IEnumerable<ARDB.Curve> ToCurveMany(this PolyCurve polyCurve) => polyCurve.ToCurveMany(UnitConverter.ToHostUnits);
    internal static IEnumerable<ARDB.Curve> ToCurveMany(this PolyCurve polyCurve, double factor)
    {
      // Convert to Raw form
      polyCurve = polyCurve.DuplicateCurve() as PolyCurve;
      if (factor != 1.0) polyCurve.Scale(factor);
      polyCurve.RemoveNesting();
      polyCurve.CombineShortSegments(Revit.ShortCurveTolerance);

      // Transfer
      int segmentCount = polyCurve.SegmentCount;
      for (int s = 0; s < segmentCount; ++s)
      {
        foreach (var segment in polyCurve.SegmentCurve(s).ToCurveMany(UnitConverter.NoScale))
          yield return segment;
      }
    }

    internal static IEnumerable<ARDB.Curve> ToCurveMany(this Curve curve) => curve.ToCurveMany(UnitConverter.ToHostUnits);
    internal static IEnumerable<ARDB.Curve> ToCurveMany(this Curve curve, double factor)
    {
      switch (curve)
      {
        case LineCurve lineCurve:

          yield return lineCurve.Line.ToLine(factor);
          yield break;

        case PolylineCurve polylineCurve:

          foreach (var line in polylineCurve.ToCurveMany(factor))
            yield return line;
          yield break;

        case ArcCurve arcCurve:

          yield return arcCurve.Arc.ToArc(factor);
          yield break;

        case PolyCurve poly:

          foreach (var segment in poly.ToCurveMany(factor))
            yield return segment;
          yield break;

        case NurbsCurve nurbs:

          foreach (var segment in nurbs.ToCurveMany(factor))
            yield return segment;
          yield break;

        default:
          if (curve.HasNurbsForm() != 0)
          {
            var nurbsForm = curve.ToNurbsCurve();
            foreach (var c in nurbsForm.ToCurveMany(factor))
              yield return c;
          }
          else throw new ConversionException($"Failed to convert {curve} to DB.Curve");

          yield break;
      }
    }
  }
}

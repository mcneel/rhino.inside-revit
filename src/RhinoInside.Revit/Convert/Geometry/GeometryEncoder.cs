using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  using External.DB.Extensions;
  using RhinoInside.Revit.Convert.System.Collections.Generic;

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

    #region Static Properties
    /// <summary>
    /// Default scale factor applied during the geometry encoding to change
    /// from active Rhino document model units to Revit internal units.
    /// </summary>
    /// <remarks>
    /// This factor should be applied to Rhino model length values
    /// in order to obtain Revit internal length values.
    /// <code>
    /// RevitInternalLength = RhinoModelLength * <see cref="GeometryEncoder.ModelScaleFactor"/>
    /// </code>
    /// </remarks>
    /// <since>1.4</since>
    internal static double ModelScaleFactor => UnitConverter.ToInternalLength;
    #endregion

    #region Length
    /// <summary>
    /// Converts the specified length to an equivalent Revit internal length.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Revit internal length that is equivalent to the provided value.</returns>
    /// <since>1.4</since>
    public static double ToInternalLength(double value) => ToInternalLength(value, ModelScaleFactor);
    internal static double ToInternalLength(double value, double factor) => value * factor;
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
    /// revit_uvpoint = rhino_point2f.ToUV()	# type: DB.UV
    /// </code>
    /// 
    /// Using <see cref="ToUV(Point2f)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    /// 
    /// DB.UV revitUVpoint = GeometryEncoder.ToUV(rhinoPoint2f);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    /// 
    /// revit_uvpoint = GE.ToUV(rhino_point2f)	# type: DB.UV
    /// </code>
    ///
    /// </example>
    /// <param name="point">Rhino point to convert.</param>
    /// <returns>Revit point that is equivalent to the provided Rhino point.</returns>
    /// <since>1.0</since>
    public static ARDB::UV ToUV(this Point2f point)
    {
      double factor = ModelScaleFactor;
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
    /// revit_uvpoint = rhino_point2d.ToUV()	# type: DB.UV
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_uvpoint = GE.ToUV(rhino_point2d)	# type: DB.UV
    /// </code>
    ///
    /// </example>
    /// <param name="point">Rhino point to convert.</param>
    /// <returns>Revit point that is equivalent to the provided Rhino point.</returns>
    /// <since>1.0</since>
    public static ARDB::UV ToUV(this Point2d point)
    {
      double factor = ModelScaleFactor;
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
    /// revit_uvvector = rhino_vector2f.ToUV()	# type: DB.UV
    /// </code>
    /// 
    /// Using <see cref="ToUV(Vector2f)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.UV revitUVVector = GeometryEncoder.ToUV(rhinoVector2f);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_uvvector = GE.ToUV(rhino_vector2f)	# type: DB.UV
    /// </code>
    ///
    /// </example>
    /// <param name="vector">Rhino vector to convert.</param>
    /// <returns>Revit vector that is equivalent to the provided Rhino vector.</returns>
    /// <since>1.0</since>
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
    /// DB.UV revitUVVector = rhinoVector2d.ToUV();
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
    /// revit_uvpoint = rhino_vector2d.ToUV()	# type: DB.UV
    /// </code>
    /// 
    /// Using <see cref="ToUV(Vector2d)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.UV revitUVVector = GeometryEncoder.ToUV(rhinoVector2d);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_uvvector = GE.ToUV(rhino_vector2d)	# type: DB.UV
    /// </code>
    ///
    /// </example>
    /// <param name="vector">Rhino vector to convert.</param>
    /// <returns>Revit vector that is equivalent to the provided Rhino vector.</returns>
    /// <since>1.0</since>
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
    /// revit_xyzpoint = rhino_point3f.ToXYZ()	# type: DB.XYZ
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_xyzpoint = GE.ToXYZ(rhino_point3f)	# type: DB.XYZ
    /// </code>
    ///
    /// </example>
    /// <param name="point">Rhino point to convert.</param>
    /// <returns>Revit point that is equivalent to the provided Rhino point.</returns>
    public static ARDB::XYZ ToXYZ(this Point3f point)
    {
      double factor = ModelScaleFactor;
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
    /// revit_xyzpoint = rhino_point3d.ToXYZ()	# type: DB.XYZ
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_xyzpoint = GE.ToXYZ(rhino_point3d)	# type: DB.XYZ
    /// </code>
    ///
    /// </example>
    /// <param name="point">Rhino point to convert.</param>
    /// <returns>Revit point that is equivalent to the provided Rhino point.</returns>
    public static ARDB::XYZ ToXYZ(this Point3d point)
    {
      double factor = ModelScaleFactor;
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
    /// revit_xyzvector = rhino_vector3f.ToXYZ()	# type: DB.XYZ
    /// </code>
    /// 
    /// Using <see cref="ToXYZ(Vector3f)" /> as static method:
    ///
    /// <code language="csharp">
    /// using DB = Autodesk.Revit.DB;
    /// using RhinoInside.Revit.Convert.Geometry;
    ///
    /// DB.XYZ revitXYZVector = GeometryEncoder.ToXYZ(rhinoVector3f);
    /// </code>
    /// 
    /// <code language="Python">
    /// import clr
    /// clr.AddReference("RevitAPI")
    /// clr.AddReference("RhinoInside.Revit")
    /// import Autodesk.Revit.DB as DB
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_xyzpoint = GE.ToXYZ(rhino_vector3f)	# type: DB.XYZ
    /// </code>
    ///
    /// </example>
    /// <param name="vector">Rhino vector to convert.</param>
    /// <returns>Revit vector that is equivalent to the provided Rhino vector.</returns>
    /// <since>1.0</since>
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
    /// revit_xyzpoint = rhino_vector3d.ToXYZ()	# type: DB.XYZ
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_xyzpoint = GE.ToXYZ(rhino_vector3d)	# type: DB.XYZ
    /// </code>
    ///
    /// </example>
    /// <param name="vector">Rhino vector to convert.</param>
    /// <returns>Revit point that is equivalent to the provided Rhino vector.</returns>
    /// <since>1.0</since>
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
    /// revit_plane = rhino_plane.ToPlane()	# type: DB.Plane
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_plane = GE.ToPlane(rhino_plane)	# type: DB.Plane
    /// </code>
    ///
    /// </example>
    /// <param name="plane">Rhino plane to convert.</param>
    /// <returns>Revit Plane that is equivalent to the provided Rhino plane.</returns>
    /// <since>1.0</since>
    public static ARDB.Plane ToPlane(this Plane plane) => ToPlane(plane, ModelScaleFactor);
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
    /// revit_transform = rhino_transform.ToTransform()	# type: DB.Transform
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_transform = GE.ToTransform(rhino_transform)	# type: DB.Transform
    /// </code>
    ///
    /// </example>
    /// <param name="transform">Rhino transform to convert.</param>
    /// <returns>Revit transfrom that is equivalent to the provided Rhino transform.</returns>
    public static ARDB.Transform ToTransform(this Transform transform) => ToTransform(transform, ModelScaleFactor);
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
    /// revit_bbox = rhino_bbox.ToBoundingBoxXYZ()	# type: DB.BoundingBoxXYZ
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_bbox = GE.ToBoundingBoxXYZ(rhino_bbox)	# type: DB.BoundingBoxXYZ
    /// </code>
    /// 
    /// </example>
    /// <param name="boundingBox">Rhino bounding box to convert.</param>
    /// <returns>Revit bounding box that is equivalent to the provided Rhino bounding box.</returns>
    /// <since>1.0</since>
    public static ARDB.BoundingBoxXYZ ToBoundingBoxXYZ(this BoundingBox boundingBox) => ToBoundingBoxXYZ(boundingBox, ModelScaleFactor);
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
    /// revit_bbox = rhino_box.ToBoundingBoxXYZ()	# type: DB.BoundingBoxXYZ
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_bbox = GE.ToBoundingBoxXYZ(rhino_box)	# type: DB.BoundingBoxXYZ
    /// </code>
    /// 
    /// </example>
    /// <param name="box">Rhino box to convert.</param>
    /// <returns>Revit bounding box that is equivalent to the provided Rhino box.</returns>
    /// <since>1.0</since>
    public static ARDB.BoundingBoxXYZ ToBoundingBoxXYZ(this Box box) => ToBoundingBoxXYZ(box, ModelScaleFactor);
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
    /// revit_outline = rhino_bbox.ToOutline()	# type: DB.Outline
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_outline = GE.ToOutline(rhino_bbox)	# type: DB.Outline
    /// </code>
    /// 
    /// </example>
    /// <param name="boundingBox">Rhino bounding box to convert.</param>
    /// <returns>Revit outline that is equivalent to the provided Rhino bounding box.</returns>
    /// <since>1.0</since>
    public static ARDB.Outline ToOutline(this BoundingBox boundingBox) => ToOutline(boundingBox, ModelScaleFactor);
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
    /// revit_line = rhino_line.ToLine()	# type: DB.Line
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_line = GE.ToLine(rhino_line)	# type: DB.Line
    /// </code>
    /// 
    /// </example>
    /// <param name="line">Rhino line to convert.</param>
    /// <returns>Revit line that is equivalent to the provided Rhino line.</returns>
    /// <since>1.0</since>
    public static ARDB.Line ToLine(this Line line) => line.ToLine(ModelScaleFactor);
    internal static ARDB.Line ToLine(this Line line, double factor)
    {
      return ARDB.Line.CreateBound(line.From.ToXYZ(factor), line.To.ToXYZ(factor));
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
    /// revit_polyline = rhino_polyline.ToPolyLine()	# type: DB.PolyLine
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_polyline = GE.ToPolyLine(rhino_polyline)	# type: DB.PolyLine
    /// </code>
    /// 
    /// </example>
    /// <param name="polyline">Rhino polyline to convert.</param>
    /// <returns>Revit polyline that is equivalent to the provided Rhino polyline.</returns>
    /// <since>1.0</since>
    public static ARDB.PolyLine ToPolyLine(this Polyline polyline) => polyline.ToPolyLine(ModelScaleFactor);
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
    /// revit_arc = rhino_arc.ToArc()	# type: DB.Arc
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_arc = GE.ToArc(rhino_arc)	# type: DB.Arc
    /// </code>
    /// 
    /// </example>
    /// <param name="arc">Rhino arc to convert.</param>
    /// <returns>Revit arc that is equivalent to the provided Rhino arc.</returns>
    /// <since>1.0</since>
    public static ARDB.Arc ToArc(this Arc arc) => arc.ToArc(ModelScaleFactor);
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
    /// revit_arc = rhino_circle.ToArc()	# type: DB.Arc
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_arc = GE.ToArc(rhino_circle)	# type: DB.Arc
    /// </code>
    /// 
    /// </example>
    /// <param name="circle">Rhino circle to convert.</param>
    /// <returns>Revit arc that is equivalent to the provided Rhino circle.</returns>
    public static ARDB.Arc ToArc(this Circle circle) => circle.ToArc(ModelScaleFactor);
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
    /// revit_curve = rhino_ellipse.ToCurve()	# type: DB.Curve
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_curve = GE.ToCurve(rhino_ellipse)	# type: DB.Curve
    /// </code>
    /// 
    /// </example>
    /// <param name="ellipse">Rhino ellipse to convert.</param>
    /// <returns>Revit curve that is equivalent to the provided Rhino ellipse.</returns>
    /// <since>1.0</since>
    public static ARDB.Curve ToCurve(this Ellipse ellipse) => ellipse.ToCurve(new Interval(0.0, 2.0 * Math.PI), ModelScaleFactor);
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
    /// revit_curve = rhino_ellipse.ToCurve(interval)	# type: DB.Curve
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// interval = Interval(0.0, 0.5)
    /// revit_curve = GE.ToCurve(rhino_ellipse, interval)	# type: DB.Curve
    /// </code>
    /// 
    /// </example>
    /// <param name="ellipse">Rhino ellipse to convert.</param>
    /// <param name="interval">Interval where the ellipse is defined.</param>
    /// <returns>Revit curve that is equivalent to the provided Rhino ellipse within the given interval</returns>
    /// <since>1.0</since>
    public static ARDB.Curve ToCurve(this Ellipse ellipse, Interval interval) => ellipse.ToCurve(interval, ModelScaleFactor);
    internal static ARDB.Curve ToCurve(this Ellipse ellipse, Interval interval, double factor)
    {
#if REVIT_2018
      return ARDB.Ellipse.CreateCurve(ellipse.Plane.Origin.ToXYZ(factor), ellipse.Radius1 * factor, ellipse.Radius2 * factor, ellipse.Plane.XAxis.ToXYZ(), ellipse.Plane.YAxis.ToXYZ(), interval.Min, interval.Max);
#else
      return ARDB.Ellipse.Create(ellipse.Plane.Origin.ToXYZ(factor), ellipse.Radius1 * factor, ellipse.Radius2 * factor, ellipse.Plane.XAxis.ToXYZ(), ellipse.Plane.YAxis.ToXYZ(), interval.Min, interval.Max);
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
    /// revit_point = rhino_point.ToPoint()	# type: DB.Point
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_point = GE.ToPoint(rhino_point)	# type: DB.Point
    /// </code>
    /// 
    /// </example>
    /// <param name="point">Rhino point to convert.</param>
    /// <returns>Revit Point that is equivalent to the provided Rhino point.</returns>
    public static ARDB.Point ToPoint(this Point point) => point.ToPoint(ModelScaleFactor);
    internal static ARDB.Point ToPoint(this Point point, double factor)
    {
      return ARDB.Point.Create(point.Location.ToXYZ(factor));
    }

    /// <summary>
    /// Converts the specified PointCloudItem to an equivalent Revit Point.
    /// </summary>
    /// <param name="value">A value to convert.</param>
    /// <returns>A Revit Point that is equivalent to the provided value.</returns>
    /// <since>1.4</since>
    public static ARDB.Point ToPoint(this PointCloudItem value) => ToPoint(value, ModelScaleFactor);
    internal static ARDB.Point ToPoint(this PointCloudItem value, double factor)
    {
      return ARDB.Point.Create(value.Location.ToXYZ(factor));
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
    /// revit_curve = rhino_linecurve.ToCurve()	# type: DB.Curve
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_curve = GE.ToCurve(rhino_linecurve)	# type: DB.Curve
    /// </code>
    /// 
    /// </example>
    /// <param name="lineCurve">Rhino lineCurve to convert.</param>
    /// <returns>Revit curve that is equivalent to the provided Rhino lineCurve.</returns>
    /// <since>1.0</since>
    public static ARDB.Curve ToCurve(this LineCurve lineCurve) => lineCurve.Line.ToLine(ModelScaleFactor);
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
    /// revit_curve = rhino_polylinecurve.ToCurve()	# type: DB.Curve
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_curve = GE.ToCurve(rhino_polylinecurve)	# type: DB.Curve
    /// </code>
    /// 
    /// </example>
    /// <param name="polylineCurve">Rhino polylineCurve to convert.</param>
    /// <returns>Revit curve that is equivalent to the provided Rhino polylineCurve.</returns>
    public static ARDB.Curve ToCurve(this PolylineCurve polylineCurve) => ToCurve(polylineCurve, ModelScaleFactor);
    internal static ARDB.Curve ToCurve(this PolylineCurve polylineCurve, double factor)
    {
      if (polylineCurve.TryGetLine(out var line, GeometryObjectTolerance.Internal.VertexTolerance * factor))
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
    /// revit_curve = rhino_arccurve.ToCurve()	# type: DB.Curve
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_curve = GE.ToCurve(rhino_arccurve)	# type: DB.Curve
    /// </code>
    /// 
    /// </example>
    /// <param name="arcCurve">Rhino arcCurve to convert.</param>
    /// <returns>Revit curve that is equivalent to the provided Rhino arcCurve.</returns>
    /// <since>1.0</since>
    public static ARDB.Curve ToCurve(this ArcCurve arcCurve) => arcCurve.Arc.ToArc(ModelScaleFactor);
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
    /// revit_curve = rhino_nurbscurve.ToCurve()	# type: DB.Curve
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_curve = GE.ToCurve(rhino_nurbscurve)	# type: DB.Curve
    /// </code>
    /// 
    /// </example>
    /// <param name="nurbsCurve">Rhino nurbsCurve to convert.</param>
    /// <returns>Revit curve that is equivalent to the provided Rhino nurbsCurve.</returns>
    /// <since>1.0</since>
    public static ARDB.Curve ToCurve(this NurbsCurve nurbsCurve) => nurbsCurve.ToCurve(ModelScaleFactor);
    internal static ARDB.Curve ToCurve(this NurbsCurve nurbsCurve, double factor)
    {
      var tol = GeometryObjectTolerance.Internal;
      if (nurbsCurve.TryGetEllipse(out var ellipse, out var interval, tol.VertexTolerance * factor))
        return ellipse.ToCurve(interval, factor);

      var gap = tol.ShortCurveTolerance * 1.01 / factor;
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
    /// revit_curve = rhino_polycurve.ToCurve()	# type: DB.Curve
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_curve = GE.ToCurve(rhino_polycurve)	# type: DB.Curve
    /// </code>
    /// 
    /// </example>
    /// <param name="polyCurve">Rhino polyCurve to convert.</param>
    /// <returns>Revit curve that is equivalent to the provided Rhino polyCurve.</returns>
    /// <since>1.0</since>
    public static ARDB.Curve ToCurve(this PolyCurve polyCurve) => ToCurve(polyCurve, ModelScaleFactor);
    internal static ARDB.Curve ToCurve(this PolyCurve polyCurve, double factor)
    {
      var tol = GeometryObjectTolerance.Internal;
      var curve = polyCurve.Simplify
      (
        CurveSimplifyOptions.AdjustG1 |
        CurveSimplifyOptions.Merge,
        tol.VertexTolerance * factor,
        tol.AngleTolerance
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
    /// revit_curve = rhino_curve.ToCurve()	# type: DB.Curve
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_curve = GE.ToCurve(rhino_curve)	# type: DB.Curve
    /// </code>
    /// 
    /// </example>
    /// <param name="curve">Rhino curve to convert.</param>
    /// <returns>Revit curve that is equivalent to the provided Rhino curve.</returns>
    /// <since>1.0</since>
    public static ARDB.Curve ToCurve(this Curve curve) => curve.ToCurve(ModelScaleFactor);
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

    internal static ARDB.HermiteSpline ToHermiteSpline(this Curve curve) => ToHermiteSpline(curve, ModelScaleFactor);
    internal static ARDB.HermiteSpline ToHermiteSpline(this Curve curve, double factor)
    {
      if (curve.TryGetHermiteSpline(out var points, out var start, out var end, GeometryObjectTolerance.Internal.VertexTolerance / factor))
      {
        using (var tangents = new ARDB.HermiteSplineTangents() { StartTangent = start.ToXYZ(), EndTangent = end.ToXYZ() })
        {
          var xyz = points.ConvertAll(x => ToXYZ(x, factor));
          return ARDB.HermiteSpline.Create(xyz, curve.IsClosed, tangents);
        }
      }

      return default;
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
    /// revit_curveloop = rhino_curve.ToCurveLoop()	# type: DB.CurveLoop
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_curveloop = GE.ToCurveLoop(rhino_curve)	# type: DB.CurveLoop
    /// </code>
    /// 
    /// </example>
    /// <param name="curve">Rhino curve to convert.</param>
    /// <returns>Revit curveLoop that contains consecutive segments of provided Rhino curve.</returns>
    /// <since>1.0</since>
    public static ARDB.CurveLoop ToCurveLoop(this Curve curve)
    {
      curve = curve.InOtherUnits(ModelScaleFactor);
      curve.CombineShortSegments(GeometryObjectTolerance.Internal.ShortCurveTolerance);

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
    /// revit_curvearray = rhino_curve.ToCurveArray()	# type: DB.CurveArray
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_curvearray = GE.ToCurveArray(rhino_curve)	# type: DB.CurveArray
    /// </code>
    /// 
    /// </example>
    /// <param name="curve">Rhino curve to convert.</param>
    /// <returns>Revit curveArray that contains consecutive segments of provided Rhino curve.</returns>
    /// <since>1.0</since>
    public static ARDB.CurveArray ToCurveArray(this Curve curve)
    {
      curve = curve.InOtherUnits(ModelScaleFactor);
      curve.CombineShortSegments(GeometryObjectTolerance.Internal.ShortCurveTolerance);

      return curve.ToCurveMany(UnitConverter.NoScale).SelectMany(x => x.ToBoundedCurves()).ToCurveArray();
    }

    internal static ARDB.CurveArray ToCurveArray(this IEnumerable<Curve> value)
    {
      var curveArray = new ARDB.CurveArray();
      foreach (var curve in value)
      {
        foreach (var segment in curve.ToCurveMany())
          curveArray.Append(segment);
      }

      return curveArray;
    }

    internal static ARDB.CurveArrArray ToCurveArrArray(this IEnumerable<Curve> value)
    {
      var curveArrayArray = new ARDB.CurveArrArray();
      foreach (var curve in value)
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
    /// revit_solid = rhino_brep.ToSolid()	# type: DB.Solid
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_solid = GE.ToSolid(rhino_brep)	# type: DB.Solid
    /// </code>
    /// 
    /// </example>
    /// <param name="brep">Rhino brep to convert.</param>
    /// <returns>Revit solid that is equivalent to the provided Rhino brep.</returns>
    /// <since>1.0</since>
    public static ARDB.Solid ToSolid(this Brep brep) =>
      BrepEncoder.ToSolid(brep, ModelScaleFactor);

    internal static ARDB.Solid ToSolid(this Brep brep, double factor) =>
      BrepEncoder.ToSolid(brep, factor);

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
    /// revit_solid = rhino_extrusion.ToSolid()	# type: DB.Solid
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_solid = GE.ToSolid(rhino_extrusion)	# type: DB.Solid
    /// </code>
    /// 
    /// </example>
    /// <param name="extrusion">Rhino extrusion to convert.</param>
    /// <returns>Revit solid that is equivalent to the provided Rhino extrusion.</returns>
    /// <since>1.0</since>
    public static ARDB.Solid ToSolid(this Extrusion extrusion) =>
      ExtrusionEncoder.ToSolid(extrusion, ModelScaleFactor);

    internal static ARDB.Solid ToSolid(this Extrusion extrusion, double factor) =>
      ExtrusionEncoder.ToSolid(extrusion, factor);

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
    /// revit_solid = rhino_subd.ToSolid()	# type: DB.Solid
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_solid = GE.ToSolid(rhino_subd)	# type: DB.Solid
    /// </code>
    /// 
    /// </example>
    /// <param name="subd">Rhino subd to convert.</param>
    /// <returns>Revit solid that is equivalent to the provided Rhino subd.</returns>
    /// <since>1.0</since>
    public static ARDB.Solid ToSolid(this SubD subd) =>
      SubDEncoder.ToSolid(subd, ModelScaleFactor);

    internal static ARDB.Solid ToSolid(this SubD subd, double factor) =>
      SubDEncoder.ToSolid(subd, factor);

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
    /// revit_solid = rhino_mesh.ToSolid()	# type: DB.Solid
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_solid = GE.ToSolid(rhino_mesh)	# type: DB.Solid
    /// </code>
    /// 
    /// </example>
    /// <param name="mesh">Rhino mesh to convert.</param>
    /// <returns>Revit solid that is equivalent to the provided Rhino mesh.</returns>
    /// <since>1.0</since>
    public static ARDB.Solid ToSolid(this Mesh mesh) =>
      Raw.RawEncoder.ToHost(MeshEncoder.ToRawBrep(mesh, ModelScaleFactor));

    internal static ARDB.Solid ToSolid(this Mesh mesh, double factor) =>
      Raw.RawEncoder.ToHost(MeshEncoder.ToRawBrep(mesh, factor));
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
    /// revit_mesh = rhino_brep.ToMesh()	# type: DB.Mesh
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_mesh = GE.ToMesh(rhino_brep)	# type: DB.Mesh
    /// </code>
    /// 
    /// </example>
    /// <param name="brep">Rhino brep to convert.</param>
    /// <returns>Revit mesh that is equivalent to the provided Rhino brep.</returns>
    /// <since>1.0</since>
    public static ARDB.Mesh ToMesh(this Brep brep) =>
      BrepEncoder.ToMesh(brep, ModelScaleFactor);

    internal static ARDB.Mesh ToMesh(this Brep brep, double factor) =>
      BrepEncoder.ToMesh(brep, factor);

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
    /// revit_mesh = rhino_extrusion.ToMesh()	# type: DB.Mesh
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_mesh = GE.ToMesh(rhino_extrusion)	# type: DB.Mesh
    /// </code>
    /// 
    /// </example>
    /// <param name="extrusion">Rhino extrusion to convert.</param>
    /// <returns>Revit mesh that is equivalent to the provided Rhino extrusion.</returns>
    /// <since>1.0</since>
    public static ARDB.Mesh ToMesh(this Extrusion extrusion) =>
      ExtrusionEncoder.ToMesh(extrusion, ModelScaleFactor);

    internal static ARDB.Mesh ToMesh(this Extrusion extrusion, double factor) =>
      ExtrusionEncoder.ToMesh(extrusion, factor);

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
    /// revit_mesh = rhino_subd.ToMesh()	# type: DB.Mesh
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_mesh = GE.ToMesh(rhino_subd)	# type: DB.Mesh
    /// </code>
    /// 
    /// </example>
    /// <param name="subd">Rhino subd to convert.</param>
    /// <returns>Revit mesh that is equivalent to the provided Rhino subd.</returns>
    /// <since>1.0</since>
    public static ARDB.Mesh ToMesh(this SubD subd) =>
      SubDEncoder.ToMesh(subd, ModelScaleFactor);

    internal static ARDB.Mesh ToMesh(this SubD subd, double factor) =>
      SubDEncoder.ToMesh(subd, factor);

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
    /// revit_mesh = rhino_mesh.ToMesh()	# type: DB.Mesh
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_mesh = GE.ToMesh(rhino_mesh)	# type: DB.Mesh
    /// </code>
    /// 
    /// </example>
    /// <param name="mesh">Rhino mesh to convert.</param>
    /// <returns>Revit mesh that is equivalent to the provided Rhino mesh.</returns>
    /// <since>1.0</since>
    public static ARDB.Mesh ToMesh(this Mesh mesh) =>
      MeshEncoder.ToMesh(MeshEncoder.ToRawMesh(mesh, ModelScaleFactor));

    internal static ARDB.Mesh ToMesh(this Mesh mesh, double factor) =>
      MeshEncoder.ToMesh(MeshEncoder.ToRawMesh(mesh, factor));
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
    /// revit_geomobj = rhino_geom.ToGeometryObject()	# type: DB.GeometryObject
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
    /// import RhinoInside.Revit.Convert.Geometry.GeometryEncoder as GE
    ///
    /// revit_geomobj = GE.ToGeometryObject(rhino_geom)	# type: DB.GeometryObject
    /// </code>
    /// 
    /// </example>
    /// <param name="geombase">Rhino geometryBase to convert.</param>
    /// <returns>Revit geometryObject that is equivalent to the provided Rhino geometryBase.</returns>
    /// <since>1.4</since>
    public static ARDB.GeometryObject ToGeometryObject(this GeometryBase geombase) =>
      ToGeometryObject(geombase, ModelScaleFactor);

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
      }

      throw new ConversionException($"Unable to convert {geombase} to Autodesk.Revit.DB.GeometryObject");
    }
    #endregion
    internal static IEnumerable<ARDB.Point> ToPointMany(this PointCloud value) =>
      value.ToPointMany(ModelScaleFactor);

    internal static IEnumerable<ARDB.Point> ToPointMany(this PointCloud value, double factor)
    {
      foreach (var point in value)
        yield return point.ToPoint(factor);
    }

    internal static IEnumerable<ARDB.Line> ToLineMany(this Polyline value) =>
      value.ToLineMany(ModelScaleFactor);

    internal static IEnumerable<ARDB.Line> ToLineMany(this Polyline value, double factor)
    {
      value = value.Duplicate();
      value.DeleteShortSegments(GeometryObjectTolerance.Internal.ShortCurveTolerance / factor);

      int count = value.Count;
      if (count > 1)
      {
        var point = value[0];
        ARDB.XYZ end, start = new ARDB.XYZ(point.X * factor, point.Y * factor, point.Z * factor);
        for (int p = 1; p < count; start = end, ++p)
        {
          point = value[p];
          end = new ARDB.XYZ(point.X * factor, point.Y * factor, point.Z * factor);
          yield return ARDB.Line.CreateBound(start, end);
        }
      }
    }

    internal static IEnumerable<ARDB.Curve> ToCurveMany(this NurbsCurve nurbsCurve) =>
      nurbsCurve.ToCurveMany(ModelScaleFactor);

    internal static IEnumerable<ARDB.Curve> ToCurveMany(this NurbsCurve nurbsCurve, double factor)
    {
      // Convert to Raw form
      nurbsCurve = nurbsCurve.DuplicateCurve() as NurbsCurve;
      if (factor != 1.0) nurbsCurve.Scale(factor);
      var tol = GeometryObjectTolerance.Internal;
      nurbsCurve.CombineShortSegments(tol.ShortCurveTolerance);

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
            if (end.DistanceTo(start) < tol.ShortCurveTolerance)
              continue;

            yield return ARDB.Line.CreateBound(start, end);
            start = end;
          }
        }
      }
      else if (nurbsCurve.TryGetPolyCurve(out var polyCurve, tol.AngleTolerance))
      {
        foreach (var segment in ToCurveMany(polyCurve, UnitConverter.NoScale))
          yield return segment;

        yield break;
      }
      else if (nurbsCurve.Degree == 2)
      {
        if (nurbsCurve.IsRational && nurbsCurve.TryGetEllipse(out var ellipse, out var interval, tol.VertexTolerance))
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
      else if (nurbsCurve.IsClosed(tol.ShortCurveTolerance * 1.01))
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

    internal static IEnumerable<ARDB.Curve> ToCurveMany(this PolylineCurve polylineCurve) =>
      polylineCurve.ToCurveMany(ModelScaleFactor);

    internal static IEnumerable<ARDB.Curve> ToCurveMany(this PolylineCurve polylineCurve, double factor)
    {
      // Convert to Raw form
      polylineCurve = polylineCurve.DuplicateCurve() as PolylineCurve;
      if (factor != 1.0) polylineCurve.Scale(factor);
      var tol = GeometryObjectTolerance.Internal;
      polylineCurve.CombineShortSegments(tol.ShortCurveTolerance);

      // Transfer
      int pointCount = polylineCurve.PointCount;
      if (pointCount > 1)
      {
        ARDB.XYZ end, start = polylineCurve.Point(0).ToXYZ(UnitConverter.NoScale);
        for (int p = 1; p < pointCount; ++p)
        {
          end = polylineCurve.Point(p).ToXYZ(UnitConverter.NoScale);
          if (start.DistanceTo(end) > tol.ShortCurveTolerance)
          {
            yield return ARDB.Line.CreateBound(start, end);
            start = end;
          }
        }
      }
    }

    internal static IEnumerable<ARDB.Curve> ToCurveMany(this PolyCurve polyCurve) =>
      polyCurve.ToCurveMany(ModelScaleFactor);

    internal static IEnumerable<ARDB.Curve> ToCurveMany(this PolyCurve polyCurve, double factor)
    {
      // Convert to Raw form
      polyCurve = polyCurve.DuplicateCurve() as PolyCurve;
      if (factor != 1.0) polyCurve.Scale(factor);
      var tol = GeometryObjectTolerance.Internal;
      polyCurve.RemoveNesting();
      polyCurve.CombineShortSegments(tol.ShortCurveTolerance);

      // Transfer
      int segmentCount = polyCurve.SegmentCount;
      for (int s = 0; s < segmentCount; ++s)
      {
        foreach (var segment in polyCurve.SegmentCurve(s).ToCurveMany(UnitConverter.NoScale))
          yield return segment;
      }
    }

    internal static IEnumerable<ARDB.Curve> ToCurveMany(this Curve value) =>
      value.ToCurveMany(ModelScaleFactor);

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

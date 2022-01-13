using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using EDBS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.Convert.Geometry
{
  /// <summary>
  /// Represents a converter for converting measurable values back and forth
  /// the Revit internal unit system and a external Rhino unit system.
  /// </summary>
  class UnitConverter
  {
    #region Default Instances
    /// <summary>
    /// Identity <see cref="UnitConverter"/>.
    /// </summary>
    public static readonly UnitConverter Identity = new UnitConverter(UnitSystem.None);

    /// <summary>
    /// Default <see cref="UnitConverter"/> for converting to and from Rhino model unit system.
    /// </summary>
    public static readonly UnitConverter Model = new UnitConverter(ActiveSpace.ModelSpace, default);

    /// <summary>
    /// Default <see cref="UnitConverter"/> for converting to and from Rhino page unit system.
    /// </summary>
    public static readonly UnitConverter Page = new UnitConverter(ActiveSpace.PageSpace, default);
    #endregion

    #region Implementation Details
    static UnitConverter()
    {
      internalUnits = new double[]
      {
        1.0,              // None
        304800.0,         // Microns
        304.8,            // Millimeters
        30.48,            // Centimeters
        0.3048,           // Meters
        0.0003048,        // Kilometers
        12000000.0,       // Microinches
        12000.0,          // Mils
        12.0,             // Inches
        1.0,              // Feet
        1.0 / 5280.0,     // Miles
        double.NaN,       // CustomUnits
        3048000000.0,     // Angstroms
        304800000.0,      // Nanometers
        3.048,            // Decimeters
        0.03048,          // Dekameters
        0.003048,         // Hectometers
        3.048e-7,         // Megameters
        3.048e-10,        // Gigameters
        1.0 / 3.0,        // Yards
        864.0,            // PrinterPoints
        72.0,             // PrinterPicas
        0.3048 / 1852.0,  // NauticalMiles
        0.3048 / 149597870700.0,  // AstronomicalUnits
        0.3048 / 9460730472580800.0,  // LightYears
        0.3048 / 149597870700.0 * 648000.0 / Math.PI, // Parsecs
      };

      // Length - 1, because we don handle UnitSystem.Unset here.
      Debug.Assert(internalUnits.Length == Enum.GetValues(typeof(UnitSystem)).Length - 1);
    }

    static readonly double[] internalUnits;

    /// <summary>
    /// <see cref="internalUnits"/> accessor with validation.
    /// </summary>
    /// <param name="unitSystem"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double InternalUnits(UnitSystem unitSystem)
    {
      // IsDefined is too slow for the occasion
      //if (!Enum.IsDefined(typeof(UnitSystem), unitSystem) || unitSystem == UnitSystem.Unset)
      //  throw new ArgumentOutOfRangeException(nameof(unitSystem));
      Debug.Assert(Enum.IsDefined(typeof(UnitSystem), unitSystem) && unitSystem != UnitSystem.Unset);

      return UnitSystem.None > unitSystem || unitSystem > UnitSystem.Parsecs ?
        double.NaN :
        internalUnits[(int) unitSystem];
    }

    /// <summary>
    /// Revit Internal Unit System.
    /// </summary>
    /// <remarks>
    /// It returns <see cref="Rhino.UnitSystem.Feet"/>.
    /// </remarks>
    internal const UnitSystem InternalUnitSystem = UnitSystem.Feet;
    #endregion

    #region Internal Factors
    /// <summary>
    /// Factor to do a direct conversion without any unit scaling.
    /// </summary>
    internal const double NoScale = 1.0;

    /// <summary>
    /// Factor for converting a length from Revit internal units to active Rhino document units.
    /// </summary>
    internal static double ToModelLength => internalUnits[(int) Model.UnitSystem];

    /// <summary>
    /// Factor for converting a length from active Rhino document units to Revit internal units.
    /// </summary>
    internal static double ToInternalLength => 1.0 / internalUnits[(int) Model.UnitSystem];
    #endregion

    #region Static Methods
    /// <summary>
    /// Converts a value from Host's internal unit system to <paramref name="target"/> units system.
    /// </summary>
    /// <param name="length">Length value to convert</param>
    /// <param name="target">The unit system to convert to.</param>
    /// <returns>Returns <paramref name="length"/> expressed in <paramref name="target"/> unit system.</returns>
    /// <remarks>
    /// This method may return <see cref="double.NaN"/> if the conversion is not defined.
    /// </remarks>
    public static double ConvertFromInternalUnits(double length, UnitSystem target)
    {
      return length * InternalUnits(target);
    }

    /// <summary>
    /// Converts a value from <paramref name="source"/> unit system to Host's internal unit system.
    /// </summary>
    /// <param name="length">Length value to convert</param>
    /// <param name="source">The unit system to convert from.</param>
    /// <returns>Returns <paramref name="length"/> expressed in Revit internal unit system.</returns>
    /// <remarks>
    /// This method may return <see cref="double.NaN"/> if the conversion is not defined.
    /// </remarks>
    public static double ConvertToInternalUnits(double length, UnitSystem source)
    {
      return length / InternalUnits(source);
    }

    /// <summary>
    /// Converts <paramref name="value"/> from <paramref name="source"/> units to <paramref name="target"/> units.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <param name="dimensionality"> 0 = factor, 1 = length, 2 = area, 3 = volumen </param>
    /// <returns>Returns <paramref name="value"/> expressed in <paramref name="target"/> unit system.</returns>
    /// <remarks>
    /// This method may return <see cref="double.NaN"/> if the conversion is not defined.
    /// </remarks>
    internal static double Convert(double value, UnitSystem source, UnitSystem target, int dimensionality = 1)
    {
      // Fast path.
      switch (dimensionality)
      {
        case -1: return Convert(value, InternalUnits(target), InternalUnits(source));
        case 0: return value;
        case +1: return Convert(value, InternalUnits(source), InternalUnits(target));
      }

      return Convert(value, Math.Pow(InternalUnits(source), dimensionality), Math.Pow(InternalUnits(target), dimensionality));
    }

    static double Convert(double value, double den, double num)
    {
      if (num == den)
        return value;

      if (Math.Abs(num) < Math.Abs(value))
        return num * (value / den);
      else
        return value * (num / den);
    }

    static double Convert(double value, EDBS.SpecType type, UnitSystem from, UnitSystem to)
    {
      return type.TryGetLengthDimensionality(out var dimensionality) ?
        Convert(value, from, to, dimensionality) :
        value;
    }
    #endregion

    #region Methods
    UnitConverter(Func<UnitSystem> unitSystem) => this.unitSystem = unitSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitConverter"/> class to the indicated unit system.
    /// </summary>
    /// <param name="unitSystem">A Rhino unit system to be used in conversions.</param>
    public UnitConverter(UnitSystem unitSystem)
    {
      if (unitSystem == UnitSystem.Unset || !Enum.IsDefined(typeof(UnitSystem), unitSystem))
        throw new ArgumentOutOfRangeException(nameof(unitSystem));

      this.unitSystem = () => unitSystem;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitConverter"/> class referencing the indicated Rhino document.
    /// </summary>
    /// <param name="space">A Rhino space to take unit system from.</param>
    /// <param name="rhinoDoc">A Rhino document to be used in conversions, null to reference <see cref="RhinoDoc.ActiveDoc"/>.</param>
    public UnitConverter(ActiveSpace space, RhinoDoc rhinoDoc = default)
    {
      switch (space)
      {
        case ActiveSpace.ModelSpace: unitSystem = () => (rhinoDoc ?? RhinoDoc.ActiveDoc)?.ModelUnitSystem ?? UnitSystem.Meters; break;
        case ActiveSpace.PageSpace:  unitSystem = () => (rhinoDoc ?? RhinoDoc.ActiveDoc)?.PageUnitSystem  ?? UnitSystem.Millimeters; break;
        default: throw new ArgumentOutOfRangeException(nameof(space));
      }
    }

    /// <summary>
    /// External unit system used to convert.
    /// </summary>
    /// <remarks>
    /// <para>This external unit system use to be the active Rhino document model or page units.</para>
    /// <para>The internal unit system is always the Revit unit system for lengths (feet).</para>
    /// </remarks>
    public UnitSystem UnitSystem => unitSystem();
    readonly Func<UnitSystem> unitSystem;

    /// <summary>
    /// Converts a length from internal to external unit system.
    /// </summary>
    /// <param name="length">Length value to convert</param>
    /// <returns>Returns <paramref name="length"/> expressed in converter external unit system.</returns>
    /// <remarks>
    /// This method may return <see cref="double.NaN"/> if the conversion is not defined.
    /// </remarks>
    public double ConvertFromInternalUnits(double length)
    {
      return length * InternalUnits(UnitSystem);
    }

    /// <summary>
    /// Converts a length from external to internal unit system.
    /// </summary>
    /// <param name="length">Length value to convert</param>
    /// <returns>Returns <paramref name="length"/> expressed in Revit internal unit system.</returns>
    /// <remarks>
    /// This method may return <see cref="double.NaN"/> if the conversion is not defined.
    /// </remarks>
    public double ConvertToInternalUnits(double length)
    {
      return length / InternalUnits(UnitSystem);
    }

    internal double ConvertFromInternalUnits(double value, EDBS.SpecType spec)
    {
      return Convert(value, spec, InternalUnitSystem, UnitSystem);
    }

    internal double ConvertToInternalUnits(double value, EDBS.SpecType spec)
    {
      return Convert(value, spec, UnitSystem, InternalUnitSystem);
    }
    #endregion
  }

  static class UnitConvertible
  {
    #region Scale
    internal static void Scale(ref Interval value, double factor)
    {
      value.T0 *= factor;
      value.T1 *= factor;
    }

    internal static void Scale(ref Point2f value, double factor)
    {
      value.X *= (float) factor;
      value.Y *= (float) factor;
    }
    internal static void Scale(ref Point2d value, double factor)
    {
      value.X *= factor;
      value.Y *= factor;
    }
    internal static void Scale(ref Vector2d value, double factor)
    {
      value.X *= factor;
      value.Y *= factor;
    }
    internal static void Scale(ref Vector2f value, double factor)
    {
      value.X *= (float) factor;
      value.Y *= (float) factor;
    }

    internal static void Scale(ref Point3f value, double factor)
    {
      value.X *= (float) factor;
      value.Y *= (float) factor;
      value.Z *= (float) factor;
    }
    internal static void Scale(ref Point3d value, double factor)
    {
      value.X *= factor;
      value.Y *= factor;
      value.Z *= factor;
    }
    internal static void Scale(ref Vector3d value, double factor)
    {
      value.X *= factor;
      value.Y *= factor;
      value.Z *= factor;
    }
    internal static void Scale(ref Vector3f value, double factor)
    {
      value.X *= (float) factor;
      value.Y *= (float) factor;
      value.Z *= (float) factor;
    }

    internal static void Scale(ref Transform value, double scaleFactor)
    {
      value.M03 *= scaleFactor;
      value.M13 *= scaleFactor;
      value.M23 *= scaleFactor;
    }

    internal static void Scale(ref BoundingBox value, double scaleFactor)
    {
      value.Min *= scaleFactor;
      value.Max *= scaleFactor;
    }

    internal static void Scale(ref Plane value, double scaleFactor)
    {
      value.Origin *= scaleFactor;
    }

    internal static void Scale(ref Line value, double scaleFactor)
    {
      value.From *= scaleFactor;
      value.To   *= scaleFactor;
    }

    internal static void Scale(ref Arc value, double scaleFactor)
    {
      var plane = value.Plane;
      plane.Origin *= scaleFactor;
      value.Plane = plane;
      value.Radius *= scaleFactor;
    }

    internal static void Scale(ref Circle value, double scaleFactor)
    {
      var plane = value.Plane;
      plane.Origin *= scaleFactor;
      value.Plane = plane;
      value.Radius *= scaleFactor;
    }

    internal static void Scale(ref Ellipse value, double scaleFactor)
    {
      var plane = value.Plane;
      plane.Origin *= scaleFactor;
      value.Plane = plane;
      value.Radius1 *= scaleFactor;
      value.Radius2 *= scaleFactor;
    }

    /// <summary>
    /// Scales <paramref name="value"/> instance by <paramref name="factor"/> in place.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="factor"></param>
    /// <seealso cref="InOtherUnits{G}(G, double)"/>
    internal static void Scale<G>(G value, double factor) where G : GeometryBase
    {
      if (factor != 1.0 && value?.Scale(factor) == false)
        throw new InvalidOperationException($"Failed to Change {value} basis");
    }
    #endregion

    #region InOtherUnits
    internal static Interval InOtherUnits(this Interval value, double factor)
    { Scale(ref value, factor); return value; }

    internal static Point3f InOtherUnits(this Point3f value, double factor)
    { Scale(ref value, factor); return value; }

    internal static Point3d InOtherUnits(this Point3d value, double factor)
    { Scale(ref value, factor); return value; }

    internal static Vector3d InOtherUnits(this Vector3d value, double factor)
    { Scale(ref value, factor); return value; }

    internal static Vector3f InOtherUnits(this Vector3f value, double factor)
    { Scale(ref value, factor); return value; }

    internal static Transform InOtherUnits(this Transform value, double factor)
    { Scale(ref value, factor); return value; }

    internal static BoundingBox InOtherUnits(this BoundingBox value, double factor)
    { Scale(ref value, factor); return value; }

    internal static Plane InOtherUnits(this Plane value, double factor)
    { Scale(ref value, factor); return value; }

    internal static Line InOtherUnits(this Line value, double factor)
    { Scale(ref value, factor); return value; }

    internal static Arc InOtherUnits(this Arc value, double factor)
    { Scale(ref value, factor); return value; }

    internal static Circle InOtherUnits(this Circle value, double factor)
    { Scale(ref value, factor); return value; }

    internal static Ellipse InOtherUnits(this Ellipse value, double factor)
    { Scale(ref value, factor); return value; }

    /// <summary>
    /// Duplicates and scales <paramref name="value"/> to be stored in other units.
    /// <para>See <see cref="Scale{G}(G, double)"/> for in place scaling.</para>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="factor"></param>
    /// <returns>Returns a scaled duplicate of the input <paramref name="value"/> in other units.</returns>
    internal static G InOtherUnits<G>(this G value, double factor) where G : GeometryBase
    { value = (G) value.Duplicate(); if(factor != 1.0) Scale(value, factor); return value; }

    static double InOtherUnits(double value, EDBS.SpecType type, UnitSystem from, UnitSystem to)
    {
      return type.TryGetLengthDimensionality(out var dimensionality) ?
        UnitConverter.Convert(value, from, to, dimensionality) :
        value;
    }
    #endregion

    #region InRhinoUnits
    public static Interval InRhinoUnits(this Interval value)
    { Scale(ref value, UnitConverter.ToModelLength); return value; }

    public static Point3f InRhinoUnits(this Point3f value)
    { Scale(ref value, UnitConverter.ToModelLength); return value; }

    public static Point3d InRhinoUnits(this Point3d value)
    { Scale(ref value, UnitConverter.ToModelLength); return value; }

    public static Vector3d InRhinoUnits(this Vector3d value)
    { Scale(ref value, UnitConverter.ToModelLength); return value; }

    public static Vector3f InRhinoUnits(this Vector3f value)
    { Scale(ref value, UnitConverter.ToModelLength); return value; }

    public static Transform InRhinoUnits(this Transform value)
    { Scale(ref value, UnitConverter.ToModelLength); return value; }

    public static BoundingBox InRhinoUnits(this BoundingBox value)
    { Scale(ref value, UnitConverter.ToModelLength); return value; }

    public static Plane InRhinoUnits(this Plane value)
    { Scale(ref value, UnitConverter.ToModelLength); return value; }

    public static Line InRhinoUnits(this Line value)
    { Scale(ref value, UnitConverter.ToModelLength); return value; }

    public static Arc InRhinoUnits(this Arc value)
    { Scale(ref value, UnitConverter.ToModelLength); return value; }

    public static Circle InRhinoUnits(this Circle value)
    { Scale(ref value, UnitConverter.ToModelLength); return value; }

    public static Ellipse InRhinoUnits(this Ellipse value)
    { Scale(ref value, UnitConverter.ToModelLength); return value; }

    /// <summary>
    /// Duplicates and scales <paramref name="value"/> to be stored in Acitve Rhino document units.
    /// <para>See <see cref="Scale{G}(G, double)"/> for in place scaling.</para>
    /// </summary>
    /// <param name="value"></param>
    /// <returns>Returns a scaled duplicate of the input <paramref name="value"/> in Active Rhino document units.</returns>
    public static G InRhinoUnits<G>(this G value) where G : GeometryBase
    { Scale(value = (G) value.Duplicate(), UnitConverter.ToModelLength); return value; }

    internal static double InRhinoUnits(double value, EDBS.SpecType spec) =>
      InOtherUnits(value, spec, UnitConverter.InternalUnitSystem, UnitConverter.Model.UnitSystem);

    internal static double InRhinoUnits(double value, EDBS.SpecType type, RhinoDoc rhinoDoc)
    {
      if (rhinoDoc is null)
        throw new ArgumentNullException(nameof(rhinoDoc));

      return InOtherUnits(value, type, UnitConverter.InternalUnitSystem, rhinoDoc.ModelUnitSystem);
    }
    #endregion

    #region InHostUnits
    public static Interval InHostUnits(this Interval value)
    { Scale(ref value, UnitConverter.ToInternalLength); return value; }

    public static Point3f InHostUnits(this Point3f value)
    { Scale(ref value, UnitConverter.ToInternalLength); return value; }

    public static Point3d InHostUnits(this Point3d value)
    { Scale(ref value, UnitConverter.ToInternalLength); return value; }

    public static Vector3d InHostUnits(this Vector3d value)
    { Scale(ref value, UnitConverter.ToInternalLength); return value; }

    public static Vector3f InHostUnits(this Vector3f value)
    { Scale(ref value, UnitConverter.ToInternalLength); return value; }

    public static Transform InHostUnits(this Transform value)
    { Scale(ref value, UnitConverter.ToInternalLength); return value; }

    public static BoundingBox InHostUnits(this BoundingBox value)
    { Scale(ref value, UnitConverter.ToInternalLength); return value; }

    public static Plane InHostUnits(this Plane value)
    { Scale(ref value, UnitConverter.ToInternalLength); return value; }

    public static Line InHostUnits(this Line value)
    { Scale(ref value, UnitConverter.ToInternalLength); return value; }

    public static Arc InHostUnits(this Arc value)
    { Scale(ref value, UnitConverter.ToInternalLength); return value; }

    public static Circle InHostUnits(this Circle value)
    { Scale(ref value, UnitConverter.ToInternalLength); return value; }

    public static Ellipse InHostUnits(this Ellipse value)
    { Scale(ref value, UnitConverter.ToInternalLength); return value; }

    /// <summary>
    /// Duplicates and scales <paramref name="value"/> to be stored Revit internal units.
    /// <para>See <see cref="Scale{G}(G, double)"/> for in place scaling.</para>
    /// </summary>
    /// <param name="value"></param>
    /// <returns>Returns a duplicate of <paramref name="value"/> in Revit internal units.</returns>
    public static G InHostUnits<G>(this G value) where G : GeometryBase
    { Scale(value = (G) value.Duplicate(), UnitConverter.ToInternalLength); return value; }

    internal static double InHostUnits(double value, EDBS.SpecType spec) =>
      InOtherUnits(value, spec, UnitConverter.Model.UnitSystem, UnitConverter.InternalUnitSystem);

    internal static double InHostUnits(double value, EDBS.SpecType spec, RhinoDoc rhinoDoc)
    {
      if (rhinoDoc is null)
        return double.NaN;

      return InOtherUnits(value, spec, rhinoDoc.ModelUnitSystem, UnitConverter.InternalUnitSystem);
    }
    #endregion
  }

  /// <summary>
  /// Tolerance values to be used on <see cref="Autodesk.Revit.DB.GeometryObject"/> instances.
  /// </summary>
  readonly struct GeometryObjectTolerance
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="GeometryObjectTolerance"/> using to the indicated unit system.
    /// </summary>
    /// <param name="unitSystem">A <see cref="Rhino.UnitSystem"/> to be used in conversions.</param>
    public GeometryObjectTolerance(UnitSystem unitSystem)
    {
      if (unitSystem == UnitSystem.None)
      {
        AngleTolerance = Internal.AngleTolerance;
        VertexTolerance = Internal.VertexTolerance;
        ShortCurveTolerance = Internal.ShortCurveTolerance;
      }
      else
      {
        AngleTolerance = Internal.AngleTolerance;
        VertexTolerance = UnitConverter.ConvertFromInternalUnits(Internal.VertexTolerance, unitSystem);
        ShortCurveTolerance = UnitConverter.ConvertFromInternalUnits(Internal.ShortCurveTolerance, unitSystem);
      }
    }

    internal GeometryObjectTolerance(double angle, double vertex, double curve)
    {
      AngleTolerance = angle;
      VertexTolerance = vertex;
      ShortCurveTolerance = curve;
    }

    /// <summary>
    /// Angle tolerance.
    /// </summary>
    /// <remarks>
    /// Value is in radians. Two angle measurements closer than this value are considered identical.
    /// </remarks>
    public readonly double AngleTolerance;

    /// <summary>
    /// Vertex tolerance.
    /// </summary>
    /// <remarks>
    /// Two points within this distance are considered coincident.
    /// </remarks>
    public readonly double VertexTolerance;

    /// <summary>
    /// Curve length tolerance
    /// </summary>
    /// <remarks>
    /// A curve shorter than this distance is considered degenerated.
    /// </remarks>
    public readonly double ShortCurveTolerance;

    // Initialized to NaN to notice if Internal is used before is assigned.
    //
    //private const double AbsoluteTolerance = (1.0 / 12.0) / 16.0; // 1/16″ in feet
    //public static GeometryObjectTolerance Internal { get; internal set; } = new GeometryObjectTolerance
    //(
    //  angle:  Math.PI / 1800.0, // 0.1° in rad,
    //  vertex: AbsoluteTolerance / 10.0,
    //  curve:  AbsoluteTolerance / 2.0
    //);

    /// <summary>
    /// Default <see cref="GeometryObjectTolerance"/> to be used on <see cref="Autodesk.Revit.DB.GeometryObject"/> instances.
    /// </summary>
    public static GeometryObjectTolerance Internal  { get; internal set; } = new GeometryObjectTolerance(double.NaN, double.NaN, double.NaN);

    /// <summary>
    /// Default <see cref="GeometryObjectTolerance"/> expresed in Rhino model unit system.
    /// </summary>
    public static GeometryObjectTolerance Model     => new GeometryObjectTolerance(UnitConverter.Model.UnitSystem);

    /// <summary>
    /// Default <see cref="GeometryObjectTolerance"/> expresed in Rhino page unit system.
    /// </summary>
    public static GeometryObjectTolerance Page      => new GeometryObjectTolerance(UnitConverter.Page.UnitSystem);
  }
}

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Rhino;
using Rhino.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using EDBS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.Convert.Geometry
{
  public static class UnitConverter
  {
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

    #region Scaling factors
    static readonly double[] internalUnits;

    /// <summary>
    /// <see cref="internalUnits"/> accessor with validation.
    /// </summary>
    /// <param name="unitSystem"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static double InternalUnits(UnitSystem unitSystem)
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
    /// Revit Internal Unit System is Feet
    /// </summary>
    internal const UnitSystem InternalUnitSystem = UnitSystem.Feet;

    /// <summary>
    /// Rhino Unit System
    /// </summary>
    /// <remarks>
    /// It returns <see cref="RhinoDoc.ActiveDoc.ModelUnitSystem"/> or meters if there is no ActiveDoc.
    /// </remarks>
    internal static UnitSystem ExternalUnitSystem => RhinoDoc.ActiveDoc?.ModelUnitSystem ?? UnitSystem.Meters;

    /// <summary>
    /// Converts a value from Host's internal units to a given unit.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="unitSystem"></param>
    /// <returns></returns>
    /// <remarks>
    /// This method may return <see cref="double.NaN"/> if the conversion is not defined.
    /// </remarks>
    public static double ConvertFromHostUnits(double value, UnitSystem unitSystem)
    {
      return value * InternalUnits(unitSystem);
    }

    /// <summary>
    /// Converts a value from a given unit to Host's internal units.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="unitSystem"></param>
    /// <returns></returns>
    /// <remarks>
    /// This method may return <see cref="double.NaN"/> if the conversion is not defined.
    /// </remarks>
    public static double ConvertToHostUnits(double value, UnitSystem unitSystem)
    {
      return value / InternalUnits(unitSystem);
    }

    /// <summary>
    /// Converts a value from Host's internal units to a given unit.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="rhinoModelUnits"></param>
    /// <param name="dimensionality"> 0 = factor, 1 = length, 2 = area, 3 = volumen </param>
    /// <returns></returns>
    /// <remarks>
    /// This method may return <see cref="double.NaN"/> if the conversion is not defined.
    /// </remarks>
    internal static double Convert(double value, UnitSystem from, UnitSystem to, int dimensionality = 1)
    {
      // Fast path.
      switch (dimensionality)
      {
        case -1: return Convert(value, InternalUnits(to), InternalUnits(from));
        case  0: return value;
        case +1: return Convert(value, InternalUnits(from), InternalUnits(to));
      }

      return Convert(value, Math.Pow(InternalUnits(from), dimensionality), Math.Pow(InternalUnits(to), dimensionality));
    }

    static double Convert(double value, double den, double num)
    {
      if (num == den)
        return value;

      // To prevent overflow-underflow and loss of precision
      // we rearrange the product in factors near 1.0.
      if (Math.Abs(value - den) < Math.Abs(num - den))
        return num * (value / den);
      else
        return value * (num / den);
    }

    /// <summary>
    /// Factor to do a direct conversion without any unit scaling.
    /// </summary>
    public const double NoScale = 1.0;

    /// <summary>
    /// Factor for converting a value from Revit internal units to active Rhino document units.
    /// </summary>
    internal static double ToRhinoUnits => internalUnits[(int) ExternalUnitSystem];

    /// <summary>
    /// Factor for converting a value from active Rhino document units to Revit internal units.
    /// </summary>
    internal static double ToHostUnits => 1.0 / internalUnits[(int) ExternalUnitSystem];
    #endregion

    #region Scale
    public static void Scale(ref Interval value, double factor)
    {
      value.T0 *= factor;
      value.T1 *= factor;
    }

    public static void Scale(ref Point2f value, double factor)
    {
      value.X *= (float) factor;
      value.Y *= (float) factor;
    }
    public static void Scale(ref Point2d value, double factor)
    {
      value.X *= factor;
      value.Y *= factor;
    }
    public static void Scale(ref Vector2d value, double factor)
    {
      value.X *= factor;
      value.Y *= factor;
    }
    public static void Scale(ref Vector2f value, double factor)
    {
      value.X *= (float) factor;
      value.Y *= (float) factor;
    }

    public static void Scale(ref Point3f value, double factor)
    {
      value.X *= (float) factor;
      value.Y *= (float) factor;
      value.Z *= (float) factor;
    }
    public static void Scale(ref Point3d value, double factor)
    {
      value.X *= factor;
      value.Y *= factor;
      value.Z *= factor;
    }
    public static void Scale(ref Vector3d value, double factor)
    {
      value.X *= factor;
      value.Y *= factor;
      value.Z *= factor;
    }
    public static void Scale(ref Vector3f value, double factor)
    {
      value.X *= (float) factor;
      value.Y *= (float) factor;
      value.Z *= (float) factor;
    }

    public static void Scale(ref Transform value, double scaleFactor)
    {
      value.M03 *= scaleFactor;
      value.M13 *= scaleFactor;
      value.M23 *= scaleFactor;
    }

    public static void Scale(ref BoundingBox value, double scaleFactor)
    {
      value.Min *= scaleFactor;
      value.Max *= scaleFactor;
    }

    public static void Scale(ref Plane value, double scaleFactor)
    {
      value.Origin *= scaleFactor;
    }

    public static void Scale(ref Line value, double scaleFactor)
    {
      value.From *= scaleFactor;
      value.To   *= scaleFactor;
    }

    public static void Scale(ref Arc value, double scaleFactor)
    {
      var plane = value.Plane;
      plane.Origin *= scaleFactor;
      value.Plane = plane;
      value.Radius *= scaleFactor;
    }

    public static void Scale(ref Circle value, double scaleFactor)
    {
      var plane = value.Plane;
      plane.Origin *= scaleFactor;
      value.Plane = plane;
      value.Radius *= scaleFactor;
    }

    public static void Scale(ref Ellipse value, double scaleFactor)
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
    public static void Scale<G>(G value, double factor) where G : GeometryBase
    {
      if (factor != 1.0 && value?.Scale(factor) == false)
        throw new InvalidOperationException($"Failed to Change {value} basis");
    }
    #endregion

    #region InOtherUnits
    public static Interval InOtherUnits(this Interval value, double factor)
    { Scale(ref value, factor); return value; }

    public static Point3f InOtherUnits(this Point3f value, double factor)
    { Scale(ref value, factor); return value; }

    public static Point3d InOtherUnits(this Point3d value, double factor)
    { Scale(ref value, factor); return value; }

    public static Vector3d InOtherUnits(this Vector3d value, double factor)
    { Scale(ref value, factor); return value; }

    public static Vector3f InOtherUnits(this Vector3f value, double factor)
    { Scale(ref value, factor); return value; }

    public static Transform InOtherUnits(this Transform value, double factor)
    { Scale(ref value, factor); return value; }

    public static BoundingBox InOtherUnits(this BoundingBox value, double factor)
    { Scale(ref value, factor); return value; }

    public static Plane InOtherUnits(this Plane value, double factor)
    { Scale(ref value, factor); return value; }

    public static Line InOtherUnits(this Line value, double factor)
    { Scale(ref value, factor); return value; }

    public static Arc InOtherUnits(this Arc value, double factor)
    { Scale(ref value, factor); return value; }

    public static Circle InOtherUnits(this Circle value, double factor)
    { Scale(ref value, factor); return value; }

    public static Ellipse InOtherUnits(this Ellipse value, double factor)
    { Scale(ref value, factor); return value; }

    /// <summary>
    /// Duplicates and scales <paramref name="value"/> to be stored in other units.
    /// <para>See <see cref="Scale{G}(G, double)"/> for in place scaling.</para>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="factor"></param>
    /// <returns>Returns a scaled duplicate of the input <paramref name="value"/> in other units.</returns>
    public static G InOtherUnits<G>(this G value, double factor) where G : GeometryBase
    { value = (G) value.DuplicateShallow(); if(factor != 1.0) Scale(value, factor); return value; }

    static double InOtherUnits(double value, EDBS.DataType type, UnitSystem from, UnitSystem to)
    {
      if (!type.TryGetLengthDimensionality(out var dimensionality))
        dimensionality = 0;

      return Convert(value, from, to, dimensionality);
    }
    #endregion

    #region InRhinoUnits
    public static Interval InRhinoUnits(this Interval value)
    { Scale(ref value, ToRhinoUnits); return value; }

    public static Point3f InRhinoUnits(this Point3f value)
    { Scale(ref value, ToRhinoUnits); return value; }

    public static Point3d InRhinoUnits(this Point3d value)
    { Scale(ref value, ToRhinoUnits); return value; }

    public static Vector3d InRhinoUnits(this Vector3d value)
    { Scale(ref value, ToRhinoUnits); return value; }

    public static Vector3f InRhinoUnits(this Vector3f value)
    { Scale(ref value, ToRhinoUnits); return value; }

    public static Transform InRhinoUnits(this Transform value)
    { Scale(ref value, ToRhinoUnits); return value; }

    public static BoundingBox InRhinoUnits(this BoundingBox value)
    { Scale(ref value, ToRhinoUnits); return value; }

    public static Plane InRhinoUnits(this Plane value)
    { Scale(ref value, ToRhinoUnits); return value; }

    public static Line InRhinoUnits(this Line value)
    { Scale(ref value, ToRhinoUnits); return value; }

    public static Arc InRhinoUnits(this Arc value)
    { Scale(ref value, ToRhinoUnits); return value; }

    public static Circle InRhinoUnits(this Circle value)
    { Scale(ref value, ToRhinoUnits); return value; }

    public static Ellipse InRhinoUnits(this Ellipse value)
    { Scale(ref value, ToRhinoUnits); return value; }

    /// <summary>
    /// Duplicates and scales <paramref name="value"/> to be stored in Acitve Rhino document units.
    /// <para>See <see cref="Scale{G}(G, double)"/> for in place scaling.</para>
    /// </summary>
    /// <param name="value"></param>
    /// <returns>Returns a scaled duplicate of the input <paramref name="value"/> in Active Rhino document units.</returns>
    public static G InRhinoUnits<G>(this G value) where G : GeometryBase
    { Scale(value = (G) value.DuplicateShallow(), ToRhinoUnits); return value; }

    public static double InRhinoUnits(double value, EDBS.DataType type) =>
      InRhinoUnits(value, type, RhinoDoc.ActiveDoc);
    static double InRhinoUnits(double value, EDBS.DataType type, RhinoDoc rhinoDoc)
    {
      if (rhinoDoc is null)
        return double.NaN;

      return InOtherUnits(value, type, InternalUnitSystem, rhinoDoc.ModelUnitSystem);
    }
    #endregion

    #region InHostUnits
    public static Interval InHostUnits(this Interval value)
    { Scale(ref value, ToHostUnits); return value; }

    public static Point3f InHostUnits(this Point3f value)
    { Scale(ref value, ToHostUnits); return value; }

    public static Point3d InHostUnits(this Point3d value)
    { Scale(ref value, ToHostUnits); return value; }

    public static Vector3d InHostUnits(this Vector3d value)
    { Scale(ref value, ToHostUnits); return value; }

    public static Vector3f InHostUnits(this Vector3f value)
    { Scale(ref value, ToHostUnits); return value; }

    public static Transform InHostUnits(this Transform value)
    { Scale(ref value, ToHostUnits); return value; }

    public static BoundingBox InHostUnits(this BoundingBox value)
    { Scale(ref value, ToHostUnits); return value; }

    public static Plane InHostUnits(this Plane value)
    { Scale(ref value, ToHostUnits); return value; }

    public static Line InHostUnits(this Line value)
    { Scale(ref value, ToHostUnits); return value; }

    public static Arc InHostUnits(this Arc value)
    { Scale(ref value, ToHostUnits); return value; }

    public static Circle InHostUnits(this Circle value)
    { Scale(ref value, ToHostUnits); return value; }

    public static Ellipse InHostUnits(this Ellipse value)
    { Scale(ref value, ToHostUnits); return value; }

    /// <summary>
    /// Duplicates and scales <paramref name="value"/> to be stored Revit internal units.
    /// <para>See <see cref="Scale{G}(G, double)"/> for in place scaling.</para>
    /// </summary>
    /// <param name="value"></param>
    /// <returns>Returns a duplicate of <paramref name="value"/> in Revit internal units.</returns>
    public static G InHostUnits<G>(this G value) where G : GeometryBase
    { Scale(value = (G) value.DuplicateShallow(), ToHostUnits); return value; }

    public static double InHostUnits(double value, EDBS.DataType type) =>
      InHostUnits(value, type, RhinoDoc.ActiveDoc);
    static double InHostUnits(double value, EDBS.DataType type, RhinoDoc rhinoDoc)
    {
      if (rhinoDoc is null)
        return double.NaN;

      return InOtherUnits(value, type, rhinoDoc.ModelUnitSystem, InternalUnitSystem);
    }
    #endregion
  }
}

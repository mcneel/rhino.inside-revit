using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  static class GeometryCache
  {
    enum CachePolicy
    {
      Disabled = 0,     // Caching is disabled
      Memory = 1,       // Memory over performance (Not Implemented)
      Performance = 2,  // Performance over memory
      Extreme = 3       // Does not allow .NET to collect any geometry
    }
#if DEBUG
    static CachePolicy Policy = CachePolicy.Disabled;
#else
    static CachePolicy Policy = CachePolicy.Performance;
#endif

    class SoftReference<T> : WeakReference where T : class
    {
      private object reference;

      public SoftReference(T value) : base(value) { }

      public override object Target
      {
        get => reference is object ? reference : base.Target;
        set
        {
          if (reference is object) throw new InvalidOperationException("Reference is keeping the object alive");
          base.Target = (T) value;
        }
      }

      public T Value
      {
        get => (T) Target;
        set => Target = value;
      }

      public bool KeepAlive
      {
        get => reference is object;
        set => reference = value ? Target : default;
      }

      public bool Hit = false;
    }

    internal struct GeometrySignature : IEquatable<GeometrySignature>
    {
      readonly int HashCode;
      readonly byte[] hash;

      internal GeometrySignature(GeometryBase geometry, double factor)
      {
        using (var stream = new MemoryStream())
        {
          using (var writer = new BinaryWriter(stream))
          {
            switch (IntPtr.Size)
            {
              case sizeof(uint):  writer.Write((uint)  geometry.GetType().TypeHandle.Value); break;
              case sizeof(ulong): writer.Write((ulong) geometry.GetType().TypeHandle.Value); break;
              default: throw new NotImplementedException($"Unexpected sizeof({nameof(IntPtr)})");
            }

            switch (geometry)
            {
              case Curve curve:     Write(writer, curve, factor);   break;
              case Surface surface: Write(writer, surface, factor); break;
              case Brep brep:       Write(writer, brep, factor);    break;
              default: throw new NotImplementedException($"Unexpected type '{geometry}'");
            }

            writer.Flush();
          }

          using (var sha1 = new SHA1Managed())
          {
            var buffer = stream.GetBuffer();
            hash = sha1.ComputeHash(buffer);

            HashCode = buffer.Length;
            for (int i = 0; i < hash.Length; ++i)
              HashCode = HashCode * -1521134295 + (int) hash[i];
          }
        }
      }

      public override string ToString()
      {
        var hex = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
          hex.AppendFormat("{0:x2}", b);

        return hex.ToString();
      }

      public override bool Equals(object obj) => obj is GeometrySignature signature && Equals(signature);

      public bool Equals(GeometrySignature other)
      {
        var x = hash;
        var y = other.hash;

        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        if (x.GetHashCode() != y.GetHashCode()) return false;

        var length = x.Length;
        if (length != y.Length) return false;

        for (int i = 0; i < length; ++i)
          if (x[i] != y[i]) return false;

        return true;
      }

      public override int GetHashCode() => HashCode;

      public static bool operator false(GeometrySignature signature) => signature.hash is null;
      public static bool operator true(GeometrySignature signature) => signature.hash is object;

      public static implicit operator bool(GeometrySignature signature) => signature.hash is object;

      static readonly GeometryObjectTolerance Tolerance = GeometryObjectTolerance.Internal;
      static int RoundNormalizedKnot(double value) => (int) Math.Round(value * 1e+9);
      static double RoundWeight     (double value) => (double) Math.Round(value * 1e+9);
      static double RoundLength     (double value) => (double) Math.Round(value / Tolerance.VertexTolerance);

      static void Write(BinaryWriter writer, Curve curve, double factor)
      {
        IEnumerable<int> RoundCurveKnotList(Rhino.Geometry.Collections.NurbsCurveKnotList knots)
        {
          var count = knots.Count;
          var min = knots[0];
          var max = knots[count - 1];
          var mid = 0.5 * (min + max);
          var alpha = 1.0 / (max - min); // normalize factor

          foreach (var k in knots)
          {
            double normalized = k <= mid ?
              (k - min) * alpha + 0.0 :
              (k - max) * alpha + 1.0;

            yield return RoundNormalizedKnot(normalized);
          }
        }

        var nurbs = curve as NurbsCurve ?? curve.ToNurbsCurve();
        writer.Write(nurbs.Order);
        var rational = nurbs.IsRational;
        writer.Write(nurbs.IsRational);
        writer.Write(nurbs.Points.Count);
        foreach (var point in nurbs.Points)
        {
          var location = point.Location;
          writer.Write(RoundLength(location.X * factor));
          writer.Write(RoundLength(location.Y * factor));
          writer.Write(RoundLength(location.Z * factor));
          if (rational) writer.Write(RoundWeight(point.Weight));
        }
        foreach (var knot in RoundCurveKnotList(nurbs.Knots)) writer.Write(knot);

      }

      static void Write(BinaryWriter writer, Surface surface, double factor)
      {
        IEnumerable<int> RoundSurfaceKnotList(Rhino.Geometry.Collections.NurbsSurfaceKnotList knots)
        {
          var count = knots.Count;
          var min = knots[0];
          var max = knots[count - 1];
          var mid = 0.5 * (min + max);
          var alpha = 1.0 / (max - min); // normalize factor

          foreach (var k in knots)
          {
            double normalized = k <= mid ?
              (k - min) * alpha + 0.0 :
              (k - max) * alpha + 1.0;

            yield return RoundNormalizedKnot(normalized);
          }
        }

        var nurbs = surface as NurbsSurface ?? surface.ToNurbsSurface(Tolerance.VertexTolerance * factor, out var _);
        writer.Write(nurbs.OrderU);
        writer.Write(nurbs.OrderV);
        var rational = nurbs.IsRational;
        writer.Write(nurbs.IsRational);
        writer.Write(nurbs.Points.CountU);
        writer.Write(nurbs.Points.CountV);
        foreach (var point in nurbs.Points)
        {
          var location = point.Location;
          writer.Write(RoundLength(location.X * factor));
          writer.Write(RoundLength(location.Y * factor));
          writer.Write(RoundLength(location.Z * factor));
          if (rational) writer.Write(RoundWeight(point.Weight));
        }

        foreach (var knot in RoundSurfaceKnotList(nurbs.KnotsU)) writer.Write(knot);
        foreach (var knot in RoundSurfaceKnotList(nurbs.KnotsV)) writer.Write(knot);

      }

      static void Write(BinaryWriter writer, Brep brep, double factor)
      {
        writer.Write(brep.Faces.Count);
        writer.Write(brep.Surfaces.Count);
        writer.Write(brep.Edges.Count);
        writer.Write(brep.Curves3D.Count);

        foreach (var face in brep.Faces)
          writer.Write(face.OrientationIsReversed);

        foreach (var surface in brep.Surfaces)
          Write(writer, surface, factor);

        foreach (var edge in brep.Edges)
        {
          var normalizedDomain = edge.EdgeCurve.Domain.NormalizedIntervalAt(edge.Domain);
          writer.Write(RoundNormalizedKnot(normalizedDomain.T0));
          writer.Write(RoundNormalizedKnot(normalizedDomain.T1));
        }

        foreach (var curve in brep.Curves3D)
          Write(writer, curve, factor);
      }
    }

    static readonly Dictionary<GeometrySignature, SoftReference<ARDB.GeometryObject>> GeometryDictionary =
      new Dictionary<GeometrySignature, SoftReference<ARDB.GeometryObject>>();

    internal static void StartKeepAliveRegion()
    {
      if (Policy == CachePolicy.Disabled)
      {
        GeometryDictionary.Clear();
      }
      else if (Policy != CachePolicy.Extreme)
      {
        foreach (var value in GeometryDictionary.Values)
        {
          value.KeepAlive = true;
          value.Hit = false;
        }
      }
    }

    internal static void EndKeepAliveRegion()
    {
      if (Policy == CachePolicy.Disabled) return;

      // Mark non hitted references as collectable
      if (Policy != CachePolicy.Extreme)
      {
        foreach (var entry in GeometryDictionary)
        {
          entry.Value.KeepAlive = entry.Value.Hit;
          entry.Value.Hit = false;
        }

        //GC.Collect();
      }

      // Collect unreferenced entries
      {
        var purge = new List<GeometrySignature>();
        foreach (var entry in GeometryDictionary)
        {
          if (!entry.Value.IsAlive)
            purge.Add(entry.Key);
        }

        foreach (var key in purge)
          GeometryDictionary.Remove(key);
      }

#if DEBUG
      Grasshopper.Instances.DocumentEditor.SetStatusBarEvent
      (
        new Grasshopper.Kernel.GH_RuntimeMessage
        (
          $"'{GeometryDictionary.Count}' solids in cache.",
          Grasshopper.Kernel.GH_RuntimeMessageLevel.Remark
        )
      );
#endif
    }

    internal static void AddExistingGeometry(GeometrySignature signature, ARDB.GeometryObject value)
    {
      if (signature && value is object) GeometryDictionary.Add
      (
        signature,
        new SoftReference<ARDB.GeometryObject>(value) { KeepAlive = true, Hit = true }
      );
    }

    internal static bool TryGetExistingGeometry<R, T>(/*const*/ R key, double factor, out T value, out GeometrySignature signature)
      where R : GeometryBase
      where T : ARDB.GeometryObject
    {
      if (Policy != CachePolicy.Disabled && key is object)
      {
        signature = new GeometrySignature(key, factor);
        if (GeometryDictionary.TryGetValue(signature, out var reference))
        {
          reference.KeepAlive = true;

          if (reference.IsAlive)
          {
            reference.Hit = true;
            value = (T) reference.Value;
            return true;
          }

          GeometryDictionary.Remove(signature);
        }
      }

      value = default;
      signature = default;
      return false;
    }
  }
}

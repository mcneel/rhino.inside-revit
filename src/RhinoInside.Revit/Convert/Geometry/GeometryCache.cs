using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

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

    struct HashComparer : IEqualityComparer<byte[]>
    {
      public bool Equals(byte[] x, byte[] y)
      {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        var length = x.Length;
        if (length != y.Length) return false;

        for (int i = 0; i < length; ++i)
          if (x[i] != y[i]) return false;

        return true;
      }

      public int GetHashCode(byte[] obj)
      {
        int hash = 0;

        if (obj is object)
        {
          for (int i = 0; i < obj.Length; ++i)
          {
            var value = (int) obj[i];
            hash ^= (value << 5) + value;
          }
        }

        return hash;
      }
    }

    static readonly Dictionary<byte[], SoftReference<DB.GeometryObject>> GeometryDictionary =
      new Dictionary<byte[], SoftReference<DB.GeometryObject>>(default(HashComparer));

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
        var purge = new List<byte[]>();
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

    internal static string HashToString(byte[] hash)
    {
      var hex = new StringBuilder(hash.Length * 2);
      foreach (var b in hash)
        hex.AppendFormat("{0:x2}", b);

      return hex.ToString();
    }

    static byte[] GetGeometryHashCode(GeometryBase geometry, double factor)
    {
      float Round(double value) => (float) (value * factor);

      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          switch (geometry)
          {
            case Brep brep:

              writer.Write(typeof(Brep).Name);
              writer.Write(brep.Faces.Count);
              writer.Write(brep.Surfaces.Count);
              writer.Write(brep.Edges.Count);
              writer.Write(brep.Curves3D.Count);

              foreach (var face in brep.Faces)
                writer.Write(face.OrientationIsReversed);

              foreach (var surface in brep.Surfaces)
              {
                var nurbs = surface as NurbsSurface ?? surface.ToNurbsSurface(Revit.VertexTolerance * factor, out var accuracy);
                writer.Write(nurbs.OrderU);
                writer.Write(nurbs.OrderV);
                var rational = nurbs.IsRational;
                writer.Write(nurbs.IsRational);
                writer.Write(nurbs.Points.CountU);
                writer.Write(nurbs.Points.CountV);
                foreach (var point in nurbs.Points)
                {
                  var location = point.Location;
                  writer.Write(Round(location.X));
                  writer.Write(Round(location.Y));
                  writer.Write(Round(location.Z));
                  if (rational) writer.Write(point.Weight);
                }
                foreach (var knot in nurbs.KnotsU) writer.Write(knot);
                foreach (var knot in nurbs.KnotsV) writer.Write(knot);
              }

              foreach (var edge in brep.Edges)
              {
                var domain = edge.Domain;
                writer.Write(domain.T0);
                writer.Write(domain.T1);
              }

              foreach (var curve in brep.Curves3D)
              {
                var nurbs = curve as NurbsCurve ?? curve.ToNurbsCurve();
                writer.Write(nurbs.Order);
                var rational = nurbs.IsRational;
                writer.Write(nurbs.IsRational);
                writer.Write(nurbs.Points.Count);
                foreach (var point in nurbs.Points)
                {
                  var location = point.Location;
                  writer.Write(Round(location.X));
                  writer.Write(Round(location.Y));
                  writer.Write(Round(location.Z));
                  if (rational) writer.Write(point.Weight);
                }
                foreach (var knot in nurbs.Knots) writer.Write(knot);
              }

              break;

            default: throw new NotImplementedException();
          }

          writer.Flush();
        }

        using (var sha1 = new SHA1Managed())
          return sha1.ComputeHash(stream.GetBuffer());
      }
    }

    internal static void AddExistingGeometry(byte[] hash, DB.GeometryObject value)
    {
      if (hash is object && value is object) GeometryDictionary.Add
      (
        hash,
        new SoftReference<DB.GeometryObject>(value) { KeepAlive = true, Hit = true }
      );
    }

    internal static bool TryGetExistingGeometry<R, T>(/*const*/ R key, double factor, out T value, out byte[] hash)
      where R : GeometryBase
      where T : DB.GeometryObject
    {
      if (Policy == CachePolicy.Disabled)
      {
        hash = default;
      }
      else
      {
        hash = GetGeometryHashCode(key, factor);
        if (GeometryDictionary.TryGetValue(hash, out var reference))
        {
          reference.KeepAlive = true;

          if (reference.IsAlive)
          {
            reference.Hit = true;
            value = (T) reference.Value;
            return true;
          }

          GeometryDictionary.Remove(hash);
        }
      }

      value = default;
      return false;
    }
  }
}

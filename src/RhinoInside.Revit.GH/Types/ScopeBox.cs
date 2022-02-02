using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using ARDB_ScopeBox = ARDB.Element;

  [Kernel.Attributes.Name("Scope Box")]
  class ScopeBox : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB_ScopeBox);
    public new ARDB_ScopeBox Value => base.Value as ARDB_ScopeBox;

    protected override bool SetValue(ARDB_ScopeBox element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB_ScopeBox element)
    {
      return element.GetType() == typeof(ARDB_ScopeBox) &&
             element.Category?.Id.IntegerValue == (int) ARDB.BuiltInCategory.OST_VolumeOfInterest;
    }

    public ScopeBox() { }
    public ScopeBox(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public ScopeBox(ARDB_ScopeBox box) : base(box)
    {
      if (!IsValidElement(box))
        throw new ArgumentException("Invalid Element", nameof(box));
    }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB_ScopeBox box)
      {
        using (var options = new ARDB.Options())
        {
          if (box.get_Geometry(options) is ARDB.GeometryElement geometry)
          {
            var points = new List<ARDB.XYZ>();
            foreach (var line in geometry.Cast<ARDB.Line>())
            {
              args.Pipeline.DrawPatternedLine
              (
                line.GetEndPoint(0).ToPoint3d(),
                line.GetEndPoint(1).ToPoint3d(),
                args.Color,
                0x00003333, args.Thickness
              );
            }
          }
        }
      }
    }
    #endregion

    #region Properties
    public override Box Box
    {
      get
      {
        if (Value is ARDB_ScopeBox box)
        {
          using (var options = new ARDB.Options())
          {
            if (box.get_Geometry(options) is ARDB.GeometryElement geometry)
            {
              var points = new List<ARDB.XYZ>(24);
              foreach (var line in geometry.Cast<ARDB.Line>())
              {
                points.Add(line.GetEndPoint(0));
                points.Add(line.GetEndPoint(1));
              }

              var origin = XYZExtension.ComputeMeanPoint(points);
              var cov = XYZExtension.ComputeCovariance(points);
              var basisX = cov.GetPrincipalComponent(0D);
              var basisZ = ARDB.XYZ.BasisZ;
              var basisY = basisZ.CrossProduct(basisX).Normalize(0D);
              var plane = ARDB.Plane.CreateByOriginAndBasis(origin, basisX, basisY);

              var min = new Point3d(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
              var max = new Point3d(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

              foreach (var point in points)
              {
                plane.Project(point, out var uv, out var _); var w = point.Z - origin.Z;
                min.X = Math.Min(min.X, uv.U); max.X = Math.Max(max.X, uv.U);
                min.Y = Math.Min(min.Y, uv.V); max.Y = Math.Max(max.Y, uv.V);
                min.Z = Math.Min(min.Z, w);    max.Z = Math.Max(max.Z, w);
              }

              min *= UnitConverter.ToModelLength;
              max *= UnitConverter.ToModelLength;

              return new Box
              (
                new Plane(origin.ToPoint3d(), basisX.ToVector3d(), basisY.ToVector3d()),
                new Interval(min.X, max.X),
                new Interval(min.Y, max.Y),
                new Interval(min.Z, max.Z)
              );
            }
          }
        }

        return NaN.Box;
      }
    }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB_ScopeBox box)
        {
          using (var options = new ARDB.Options())
          {
            if (box.get_Geometry(options) is ARDB.GeometryElement geometry)
            {
              var points = new List<ARDB.XYZ>();
              foreach (var line in geometry.Cast<ARDB.Line>())
              {
                points.Add(line.GetEndPoint(0));
                points.Add(line.GetEndPoint(1));
              }

              var origin = XYZExtension.ComputeMeanPoint(points);
              var cov = XYZExtension.ComputeCovariance(points);
              var basisX = cov.GetPrincipalComponent(0D);
              var basisZ = ARDB.XYZ.BasisZ;
              var basisY = basisZ.CrossProduct(basisX).Normalize(0D);

              return new Plane(origin.ToPoint3d(), basisX.ToVector3d(), basisY.ToVector3d());
            }
          }
        }

        return NaN.Plane;
      }
    }
    #endregion
  }
}

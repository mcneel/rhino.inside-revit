using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ERDB = RhinoInside.Revit.External.DB;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using External.DB;

  using ARDB_ScopeBox = ARDB.Element;

  [Kernel.Attributes.Name("Scope Box")]
  public class ScopeBox : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB_ScopeBox);
    public new ARDB_ScopeBox Value => base.Value as ARDB_ScopeBox;

    protected override bool SetValue(ARDB_ScopeBox element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB_ScopeBox element)
    {
      return element.GetType() == typeof(ARDB_ScopeBox) &&
             element.Category?.ToBuiltInCategory() == ARDB.BuiltInCategory.OST_VolumeOfInterest;
    }

    public ScopeBox() { }
    public ScopeBox(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public ScopeBox(ARDB_ScopeBox box) : base(box)
    {
      if (!IsValidElement(box))
        throw new ArgumentException("Invalid Element", nameof(box));
    }

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
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

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      var box = Box;
      return box.IsValid ? box.GetBoundingBox(xform) : NaN.BoundingBox;
    }

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
              var lines = geometry.OfType<ARDB.Line>().ToArray();
              if (lines.Length == 12)
              {
                var points = new List<ARDB.XYZ>(lines.Length * 2);
                foreach (var line in lines)
                {
                  points.Add(line.GetEndPoint(0));
                  points.Add(line.GetEndPoint(1));
                }

                var origin = XYZExtension.ComputeMeanPoint(points);
                if (UnitXYZ.Orthonormalize(-lines[2].Direction, -lines[1].Direction, out var basisX, out var basisY, out var basisZ))
                {
                  var coordSystem = ARDB.Transform.Identity; coordSystem.SetCoordSystem(origin, basisX, basisY, basisZ);
                  if (XYZExtension.TryGetBoundingBox(points, out var bbox, coordSystem))
                    return bbox.ToBox();
                }
              }
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
              var lines = geometry.OfType<ARDB.Line>().ToArray();
              if (lines.Length == 12)
              {
                var points = new List<ARDB.XYZ>(lines.Length * 2);
                foreach (var line in lines)
                {
                  points.Add(line.GetEndPoint(0));
                  points.Add(line.GetEndPoint(1));
                }

                var origin = XYZExtension.ComputeMeanPoint(points);
                var basisX = -lines[2].Direction;
                var basisY = -lines[1].Direction;
                return new Plane(origin.ToPoint3d(), basisX.ToVector3d(), basisY.ToVector3d());
              }
            }
          }
        }

        return NaN.Plane;
      }
    }
    #endregion
  }
}

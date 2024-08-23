using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino;
using ERDB = RhinoInside.Revit.External.DB;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using External.DB;

  using ARDB_ScopeBox = ARDB.Element;

  [Kernel.Attributes.Name("Scope Box")]
  public class ScopeBox : GraphicalElement, Bake.IGH_BakeAwareElement
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

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      if (Value is ARDB_ScopeBox sectionBox)
      {
        att = att?.Duplicate() ?? doc.CreateDefaultAttributes();
        att.Name = DisplayName;
        if (Category.BakeElement(idMap, false, doc, att, out var layerGuid))
          att.LayerIndex = doc.Layers.FindId(layerGuid).Index;

        // 2. Check if already exist
        var index = doc.Groups.Find(att.Name);
        if (index >= 0) guid = doc.Groups.FindIndex(index).Id;

        // 3. Update if necessary
        if (index < 0 || overwrite)
        {
          if (index < 0) index = doc.Groups.Add(att.Name);
          if (overwrite && index >= 0)
          {
            var objects = doc.Objects.FindByGroup(index);
            var viewportIds = new Dictionary<int, HashSet<Guid>>();
            foreach (var obj in objects)
            {
              if (obj is ClippingPlaneObject clipper)
                viewportIds[viewportIds.Count] = new HashSet<Guid>(clipper.ClippingPlaneGeometry.ViewportIds());

              doc.Objects.Delete(obj, quiet: true, ignoreModes: true);
            }

            var box = Box;

            // Lines
            {
              var rectangles = new Rectangle3d[]
              {
                new Rectangle3d(new Plane(box.PointAt(0.0, 0.5, 0.5), box.Plane.YAxis, box.Plane.ZAxis), box.Y, box.Z), // left
                new Rectangle3d(new Plane(box.PointAt(1.0, 0.5, 0.5), box.Plane.ZAxis, box.Plane.YAxis), box.Z, box.Y), // right
                new Rectangle3d(new Plane(box.PointAt(0.5, 0.0, 0.5), box.Plane.ZAxis, box.Plane.XAxis), box.Z, box.X), // front
                new Rectangle3d(new Plane(box.PointAt(0.5, 1.0, 0.5), box.Plane.XAxis, box.Plane.ZAxis), box.X, box.Z), // back
                new Rectangle3d(new Plane(box.PointAt(0.5, 0.5, 0.0), box.Plane.XAxis, box.Plane.YAxis), box.X, box.Y), // bottom
                new Rectangle3d(new Plane(box.PointAt(0.5, 0.5, 1.0), box.Plane.YAxis, box.Plane.XAxis), box.Y, box.X)  // top
              };

              using (var rectanglesAtt = att.Duplicate())
              {
                rectanglesAtt.AddToGroup(index);
                rectanglesAtt.PlotColorSource = ObjectPlotColorSource.PlotColorFromDisplay;
                foreach (var rectangle in rectangles)
                  doc.Objects.Add(new PolylineCurve(rectangle.ToPolyline()), rectanglesAtt);
              }
            }

#if RHINO_8
            // Clipping Planes
            {
              ClippingPlaneSurface CreateClippingPlane(Plane plane, Interval u, Interval v) =>
                new ClippingPlaneSurface(new PlaneSurface(plane, u, v));
              
              var planes = new (int Name, ClippingPlaneSurface Surface)[]
              {
                (0, CreateClippingPlane(new Plane(box.PointAt(0.0, 0.5, 0.5), box.Plane.YAxis, box.Plane.ZAxis), box.Y, box.Z)), // left
                (1, CreateClippingPlane(new Plane(box.PointAt(1.0, 0.5, 0.5), box.Plane.ZAxis, box.Plane.YAxis), box.Z, box.Y)), // right
                (2, CreateClippingPlane(new Plane(box.PointAt(0.5, 0.0, 0.5), box.Plane.ZAxis, box.Plane.XAxis), box.Z, box.X)), // front
                (3, CreateClippingPlane(new Plane(box.PointAt(0.5, 1.0, 0.5), box.Plane.XAxis, box.Plane.ZAxis), box.X, box.Z)), // back
                (4, CreateClippingPlane(new Plane(box.PointAt(0.5, 0.5, 0.0), box.Plane.XAxis, box.Plane.YAxis), box.X, box.Y)), // bottom
                (5, CreateClippingPlane(new Plane(box.PointAt(0.5, 0.5, 1.0), box.Plane.YAxis, box.Plane.XAxis), box.Y, box.X))  // top
              };

              using (var planesAtt = att.Duplicate())
              {
                planesAtt.AddToGroup(index);
                planesAtt.ColorSource = ObjectColorSource.ColorFromObject;
                planesAtt.ObjectColor = System.Drawing.Color.FromArgb(0);
                planesAtt.PlotColorSource = ObjectPlotColorSource.PlotColorFromDisplay;
                var name = 0;
                foreach (var plane in planes)
                {
                  plane.Surface.ParticipationListsEnabled = true;
                  plane.Surface.SetClipParticipation(Array.Empty<Guid>(), new int[] { att.LayerIndex }, true);
                  if (viewportIds.TryGetValue(name++, out var viewports))
                  {
                    foreach (var id in viewports)
                      plane.Surface.AddClipViewportId(id);
                  }
                  doc.Objects.Add(plane.Surface, planesAtt);
                }
              }
            }
#endif
          }

          if (index >= 0) guid = doc.Groups.FindIndex(index).Id;
        }

        if (guid != Guid.Empty)
        {
          idMap.Add(Id, guid);
          return true;
        }
      }

      return false;
    }

#endregion
  }
}

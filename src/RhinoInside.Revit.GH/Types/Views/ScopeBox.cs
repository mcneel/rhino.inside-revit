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

      if (Value is ARDB_ScopeBox)
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

            // Instance
            if
            (
              BakeInstanceDefinition(idMap, overwrite, doc, att, out var idef_id) &&
              doc.InstanceDefinitions.FindId(idef_id) is InstanceDefinition idef
            )
            {
              using (var instanceAtt = att.Duplicate())
              {
                var xform = Transform.PlaneToPlane(Plane.WorldXY, box.Plane) *
                            Transform.Scale(Plane.WorldXY, box.X.Length, box.Y.Length, box.Z.Length);

                instanceAtt.AddToGroup(index);
                guid = doc.Objects.AddInstanceObject(idef.Index, xform, instanceAtt);
              }
            }

            // Clipping Planes
            if (guid != Guid.Empty)
            {
              using (var planesAtt = att.Duplicate())
              {
                var parent = doc.Layers[att.LayerIndex];
                var layer = doc.Layers.FirstOrDefault(x => x.ParentLayerId == parent.Id && x.Name == "Planes");
                if (layer is null)
                {
                  planesAtt.LayerIndex = doc.Layers.Add
                  (
                    new Layer
                    {
                      ParentLayerId = parent.Id,
                      Name = "Planes",
                      IsVisible = true,
#if RHINO_8
                      IsLocked = true
#else
                      IsLocked = false
#endif
                    }
                  );
                  layer = doc.Layers[index];
                }
                else
                {
                  planesAtt.LayerIndex = layer.Index;
#if !RHINO_8
                  if (layer.IsLocked) layer.IsLocked = false; // Allow me to center the planes please!!
#endif
                }

                var planes = new (int Name, Plane Plane, Interval U, Interval V)[]
                {
                  (0, new Plane(box.PointAt(0.0, 0.5, 0.5), box.Plane.YAxis, box.Plane.ZAxis), box.Y, box.Z), // left
                  (1, new Plane(box.PointAt(1.0, 0.5, 0.5), -box.Plane.YAxis, box.Plane.ZAxis), box.Y, box.Z), // right
                  (2, new Plane(box.PointAt(0.5, 0.0, 0.5), -box.Plane.XAxis, box.Plane.ZAxis), box.X, box.Z), // front
                  (3, new Plane(box.PointAt(0.5, 1.0, 0.5), box.Plane.XAxis, box.Plane.ZAxis), box.X, box.Z), // back
                  (4, new Plane(box.PointAt(0.5, 0.5, 0.0), box.Plane.XAxis, box.Plane.YAxis), box.X, box.Y), // bottom
                  (5, new Plane(box.PointAt(0.5, 0.5, 1.0), -box.Plane.XAxis, box.Plane.YAxis), box.X, box.Y)  // top
                };

                planesAtt.AddToGroup(index);
                planesAtt.ColorSource = ObjectColorSource.ColorFromObject;
                planesAtt.ObjectColor = System.Drawing.Color.FromArgb(0); // Make planes "invisible"
                planesAtt.PlotColorSource = ObjectPlotColorSource.PlotColorFromDisplay;
                var name = 0;
                foreach (var plane in planes)
                {
                  viewportIds.TryGetValue(name++, out var viewports);
                  AddClippingPlane(doc, plane.Plane, viewports, planesAtt);
                }

                // This makes the gumball ignore the Planes even are visible.
                layer.SetPersistentLocking(true);
                layer.IsLocked = true;
              }
            }
          }
        }

        if (guid != Guid.Empty)
        {
          idMap.Add(Id, guid);
          return true;
        }
      }

      return false;
    }

    private static ClippingPlaneObject AddClippingPlane(RhinoDoc document, Plane plane, IEnumerable<Guid> viewportIds, ObjectAttributes attributes)
    {
      var interval = new Interval(-Revit.ModelUnits * 3, +Revit.ModelUnits * 3);
#if RHINO_8
      var surface = new PlaneSurface(plane, interval, interval);
      var clippingSurface = new ClippingPlaneSurface(surface);

      foreach (var vport in viewportIds ?? Array.Empty<Guid>())
        clippingSurface.AddClipViewportId(vport);

      clippingSurface.ParticipationListsEnabled = true;
      clippingSurface.SetClipParticipation(Array.Empty<Guid>(), new int[] { attributes.LayerIndex }, true);
      var id = document.Objects.Add(clippingSurface, attributes);
      var clippingPlane = document.Objects.Find(id) as ClippingPlaneObject;
#else
      var id = document.Objects.AddClippingPlane(plane, 3.0 * Revit.ModelUnits, 3.0 * Revit.ModelUnits, viewportIds ?? new Guid[] { Guid.Empty }, attributes);

      var clippingPlane = document.Objects.Find(id) as ClippingPlaneObject;
      var clippingSurface = clippingPlane.ClippingPlaneGeometry;

      // Centered on the plane please!!
      clippingSurface.Extend(0, interval);
      clippingSurface.Extend(1, interval);
      clippingPlane.CommitChanges();
#endif

      return clippingPlane;
    }

    public bool BakeInstanceDefinition
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      guid = Guid.Empty;
      overwrite = false;

      var box = new Box(Plane.WorldXY, new Interval(-0.5, +0.5), new Interval(-0.5, +0.5), new Interval(-0.5, +0.5));
      box.Inflate(-0.001);

      var name = "*Revit::Annotation::Scope Box";
      var idef = doc.InstanceDefinitions.Find(name);
      if (idef is object)
      {
        guid = idef.Id;
        var objs = idef.GetObjects();
        var bbox = BoundingBox.Empty;
        foreach ( var obj in objs )
          bbox.Union(obj.Geometry.GetBoundingBox(accurate: true));

        overwrite |= (bbox.Min.X != box.X.T0 || bbox.Max.X != box.X.T1);
        overwrite |= (bbox.Min.Y != box.Y.T0 || bbox.Max.Y != box.Y.T1);
        overwrite |= (bbox.Min.Z != box.Z.T0 || bbox.Max.Z != box.Z.T1);
      }
      else overwrite = true;

      if (overwrite)
      {
        var ibox = box;
        var rectangles = new Rectangle3d[]
        {
          new Rectangle3d(new Plane(ibox.PointAt(0.0, 0.5, 0.5), ibox.Plane.YAxis, ibox.Plane.ZAxis), ibox.Y, ibox.Z), // left
          new Rectangle3d(new Plane(ibox.PointAt(1.0, 0.5, 0.5), ibox.Plane.ZAxis, ibox.Plane.YAxis), ibox.Z, ibox.Y), // right
          new Rectangle3d(new Plane(ibox.PointAt(0.5, 0.0, 0.5), ibox.Plane.ZAxis, ibox.Plane.XAxis), ibox.Z, ibox.X), // front
          new Rectangle3d(new Plane(ibox.PointAt(0.5, 1.0, 0.5), ibox.Plane.XAxis, ibox.Plane.ZAxis), ibox.X, ibox.Z), // back
          new Rectangle3d(new Plane(ibox.PointAt(0.5, 0.5, 0.0), ibox.Plane.XAxis, ibox.Plane.YAxis), ibox.X, ibox.Y), // bottom
          new Rectangle3d(new Plane(ibox.PointAt(0.5, 0.5, 1.0), ibox.Plane.YAxis, ibox.Plane.XAxis), ibox.Y, ibox.X)  // top
        };

        var geometry = rectangles.Select(x => new PolylineCurve(x.ToPolyline())).ToArray();

        var attributes = new ObjectAttributes()
        {
          LayerIndex = att.LayerIndex,
          ColorSource = ObjectColorSource.ColorFromParent,
          LinetypeSource = ObjectLinetypeSource.LinetypeFromParent,
          PlotColorSource = ObjectPlotColorSource.PlotColorFromParent,
          MaterialSource = ObjectMaterialSource.MaterialFromParent,
        };

        if (idef is null)
        {
          var index = doc.InstanceDefinitions.Add(name, "Revit Scope Box", Point3d.Origin, geometry, Enumerable.Repeat(attributes, geometry.Length));
          if (index >=0) guid = doc.InstanceDefinitions[index].Id;
        }
        else
        {
          if (doc.InstanceDefinitions.ModifyGeometry(idef.Index, geometry, Enumerable.Repeat(attributes, geometry.Length)))
            guid = idef.Id;
        }
      }

      return guid != Guid.Empty;
    }

#endregion
  }
}

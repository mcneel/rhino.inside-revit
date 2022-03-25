using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using static Rhino.RhinoMath;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Shared Site")]
  public class ProjectLocation : Instance, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.ProjectLocation);
    public new ARDB.ProjectLocation Value => base.Value as ARDB.ProjectLocation;

    public ProjectLocation() { }
    public ProjectLocation(ARDB.ProjectLocation instance) : base(instance) { }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      var location = Location;
      if (location.IsValid)
      {
        return new BoundingBox
        (
          new Point3d[]
          {
              location.Origin,
              location.Origin
          },
          xform
        );
      }

      return NaN.BoundingBox;
    }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var location = Location;
      if (location.IsValid)
      {
        var strokeColor = (System.Drawing.Color) Rhino.Display.ColorRGBA.ApplyGamma(new Rhino.Display.ColorRGBA(args.Color), 2.0);
        args.Pipeline.DrawPoint(location.Origin, Rhino.Display.PointStyle.Pin, strokeColor, args.Color, 12.0f, 2.0f, 7.0f, 0.0f, true, true);
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

      if (Value is ARDB.ProjectLocation projectLocation)
      {
        var name = ToString();

        // 2. Check if already exist
        var index = doc.NamedConstructionPlanes.Find(name);

        // 3. Update if necessary
        if (index < 0 || overwrite)
        {
          if (projectLocation.GetSiteLocation() is ARDB.SiteLocation siteLocation)
          {
            var location = Location;
            var anchorPoint = new EarthAnchorPoint()
            {
              Name = projectLocation.Name,
              Description = siteLocation.PlaceName,
              ModelBasePoint = location.Origin,
              ModelEast = location.XAxis,
              ModelNorth = location.YAxis,
              EarthBasepointElevation = siteLocation.Elevation,
              EarthBasepointLatitude = ToDegrees(siteLocation.Latitude),
              EarthBasepointLongitude = ToDegrees(siteLocation.Longitude),
            };

            doc.EarthAnchorPoint = anchorPoint;
          }

          var cplane = CreateConstructionPlane(name, Location, doc);

          if (index < 0) index = doc.NamedConstructionPlanes.Add(cplane);
          else if (overwrite) doc.NamedConstructionPlanes.Modify(cplane, index, true);
        }

        // TODO: Create a V5 Uuid out of the name
        //guid = new Guid(0, 0, 0, BitConverter.GetBytes((long) index));
        //idMap.Add(Id, guid);

        return true;
      }

      return false;
    }
    #endregion
  }

  [Kernel.Attributes.Name("Site Location")]
  public class SiteLocation : ElementType
  {
    protected override Type ValueType => typeof(ARDB.SiteLocation);
    public new ARDB.SiteLocation Value => base.Value as ARDB.SiteLocation;

    public SiteLocation() { }
    public SiteLocation(ARDB.SiteLocation value) : base(value) { }

    public override string DisplayName =>
      Value?.PlaceName is string placeName && placeName.Length > 0?
      placeName : base.DisplayName;
  }
}

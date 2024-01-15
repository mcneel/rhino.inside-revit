using System;
using OS = System.Environment;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;
  using RhinoInside.Revit.Convert.Units;

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

    public SiteLocation SiteLocation => SiteLocation.FromElement(Value?.GetSiteLocation()) as SiteLocation;

    internal bool GetEarthAnchor(EarthAnchorPoint anchorPoint)
    {
      if (Value is ARDB.ProjectLocation)
      {
        var location = Location;
        anchorPoint.ModelBasePoint = location.Origin;
        anchorPoint.ModelEast = location.XAxis;
        anchorPoint.ModelNorth = location.YAxis;

        return true;
      }

      return false;
    }

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
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

      if (Value is ARDB.ProjectLocation)
      {
        // 3. Update if necessary
        if (overwrite)
        {
          var anchorPoint = doc.EarthAnchorPoint;

          GetEarthAnchor(anchorPoint);              // Location on the model
          SiteLocation.GetEarthAnchor(anchorPoint); // Location on earth

          doc.EarthAnchorPoint = anchorPoint;
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
  public class SiteLocation : ElementType, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.SiteLocation);
    public new ARDB.SiteLocation Value => base.Value as ARDB.SiteLocation;

    public SiteLocation() { }
    public SiteLocation(ARDB.SiteLocation value) : base(value) { }

    public override string DisplayName =>
      Value?.PlaceName is string placeName && placeName.Length > 0?
      placeName : base.DisplayName;

    internal bool GetEarthAnchor(EarthAnchorPoint anchorPoint)
    {
      if (Value is ARDB.SiteLocation siteLocation)
      {
        anchorPoint.Name = siteLocation.PlaceName;
        //anchorPoint.Description =
        //"{" +
        //$"\"geo-coordinate-system-id\": \"{siteLocation.GeoCoordinateSystemId}\", " +
        //$"\"geo-coordinate-system-definition\": \"{siteLocation.GeoCoordinateSystemDefinition}\", " +
        //$"\"time-zone\": {siteLocation.TimeZone}, " +
        //$"\"weather-station-name\": \"{siteLocation.WeatherStationName}\"" +
        //"}";
        anchorPoint.EarthBasepointElevation = UnitScale.Convert(siteLocation.Elevation, UnitScale.Internal, UnitScale.Meters);
        anchorPoint.EarthBasepointLatitude = RhinoMath.ToDegrees(siteLocation.Latitude);
        anchorPoint.EarthBasepointLongitude = RhinoMath.ToDegrees(siteLocation.Longitude);

        return true;
      }

      return false;
    }

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

      if (Value is ARDB.SiteLocation)
      {
        // 3. Update if necessary
        if (overwrite)
        {
          var anchorPoint = doc.EarthAnchorPoint;
          GetEarthAnchor(anchorPoint);
          doc.EarthAnchorPoint = anchorPoint;
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
}

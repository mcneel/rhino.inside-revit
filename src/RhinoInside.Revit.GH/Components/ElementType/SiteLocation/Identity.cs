using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Site
{
  using static Rhino.RhinoMath;

  public class SiteLocationIdentity : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("6820C2DC-4BE5-4A54-884F-5D402CF556B3");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ID";

    public SiteLocationIdentity()
    : base
    (
      "Site Location Identity",
      "Identity",
      "Site location identity Data.",
      "Revit",
      "Site"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.ElementType>("Site Location", "SL"),
      ParamDefinition.Create<Param_String>("Place Name", "PN", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Number>("Time Zone", "TZ", "Hours ranging from -12 to +12. 0 represents GMT.", optional: true, relevance: ParamRelevance.Primary),
      new ParamDefinition
      (
        new Param_Angle() { Name = "Latitude", NickName = "LAT", Optional = true, AngleParameter = true, UseDegrees = true },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Angle() { Name = "Longitude", NickName = "LON", Optional = true, AngleParameter = true, UseDegrees = true },
        ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ElementType>("Site Location", "SL", relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Number>("Elevation", "E", "The elevation of the site location", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Weather Station", "WS", "The name of the weather station at the site location", relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Place Name", "PN", "The place name of the site", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Number>("Time Zone", "TZ", "The time-zone for the site", relevance: ParamRelevance.Primary),
      new ParamDefinition
      (
        new Param_Angle() { Name = "Latitude", NickName = "LAT", Description = "The latitude of the site location", AngleParameter = true, UseDegrees = true },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Angle() { Name = "Longitude", NickName = "LON", Description = "The longitude of the site location", AngleParameter = true, UseDegrees = true },
        ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Site Location", out Types.SiteLocation location, x => x.IsValid)) return;

      bool update = false;
      update |= Params.GetData(DA, "Place Name", out string placeName);
      update |= Params.GetData(DA, "Time Zone", out double? timeZone);
      update |= Params.GetData(DA, "Latitude", out double? latitude);
      update |= Params.GetData(DA, "Longitude", out double? longitude);

      if (update)
      {
        StartTransaction(location.Document);

        if (placeName != null) location.Value.PlaceName = placeName;
        if (timeZone != null) location.Value.TimeZone = timeZone.Value;
        if (latitude != null)
        {
          if (Params.Input<Param_Number>("Latitude").UseDegrees)
          {
            if (Math.Abs(latitude.Value) < 90.0)
              location.Value.Latitude = ToRadians(latitude.Value);
            else
              throw new Exceptions.RuntimeArgumentException("Latitude", "Value is out of range. It must be between -90 and 90.");
          }
          else
          {
            if (Math.Abs(latitude.Value) < Math.PI / 2.0)
              location.Value.Latitude = latitude.Value;
            else
              throw new Exceptions.RuntimeArgumentException("Latitude", "Value is out of range. It must be between -PI/2 and PI/2.");
          }
        }

        if (longitude != null)
        {
          location.Value.Longitude = Params.Input<Param_Number>("Longitude").UseDegrees ?
            ToRadians(longitude.Value) % (2.0 * Math.PI) :
            longitude.Value;
        }
      }

      Params.TrySetData(DA, "Site Location", () => location);
      Params.TrySetData(DA, "Place Name", () => location.Value.PlaceName);
      Params.TrySetData(DA, "Weather Station", () => location.Value.WeatherStationName);
      Params.TrySetData(DA, "Time Zone", () => location.Value.TimeZone);
      Params.TrySetData(DA, "Latitude", () => Params.Output<Param_Number>("Latitude").UseDegrees ? ToDegrees(location.Value.Latitude) : location.Value.Latitude);
      Params.TrySetData(DA, "Longitude", () => Params.Output<Param_Number>("Longitude").UseDegrees ? ToDegrees(location.Value.Longitude) : location.Value.Longitude);
      Params.TrySetData(DA, "Elevation", () => location.Value.Elevation * Revit.ModelUnits);
    }
  }
}

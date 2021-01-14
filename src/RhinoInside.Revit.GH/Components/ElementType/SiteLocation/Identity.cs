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
      ParamDefinition.Create<Param_String>("Place Name", "PN", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_Number>("Time Zone", "TZ", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.FromParam
      (
        new Param_Number() { Name = "Latitude", NickName = "LAT", Optional = true, AngleParameter = true, UseDegrees = true },
        ParamVisibility.Default
      ),
      ParamDefinition.FromParam
      (
        new Param_Number() { Name = "Longitude", NickName = "LON", Optional = true, AngleParameter = true, UseDegrees = true },
        ParamVisibility.Default
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ElementType>("Site Location", "SL", relevance: ParamVisibility.Voluntary),
      ParamDefinition.Create<Param_Number>("Elevation", "E", "The elevation of the site location", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_String>("Weather Station", "WS", "The name of the weather station at the site location", relevance: ParamVisibility.Voluntary),
      ParamDefinition.Create<Param_String>("Place Name", "PN", "The place name of the site", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_Number>("Time Zone", "TZ", "The time-zone for the site", relevance: ParamVisibility.Default),
      ParamDefinition.FromParam
      (
        new Param_Number() { Name = "Latitude", NickName = "LAT", Description = "The latitude of the site location", AngleParameter = true, UseDegrees = true },
        ParamVisibility.Default
      ),
      ParamDefinition.FromParam
      (
        new Param_Number() { Name = "Longitude", NickName = "LON", Description = "The longitude of the site location", AngleParameter = true, UseDegrees = true },
        ParamVisibility.Default
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Site Location", out Types.SiteLocation location, x => x.IsValid)) return;

      bool update = false;
      update |= Params.GetData(DA, "Place Name", out string placeName);
      update |= Params.GetData(DA, "TimeZone", out double? timeZone);
      update |= Params.GetData(DA, "Latitude", out double? latitude);
      update |= Params.GetData(DA, "TimeZone", out double? longitude);

      if (update)
      {
        StartTransaction(location.Document);

        if (placeName != null) location.Value.PlaceName = placeName;
        if (timeZone != null) location.Value.TimeZone = timeZone.Value;
        if (latitude != null)
        {
          location.Value.Latitude = Params.Input<Param_Number>("Latitude").UseDegrees ?
            ToRadians(latitude.Value) :
            latitude.Value;
        }
        if (longitude != null)
        {
          location.Value.Longitude = Params.Input<Param_Number>("Longitude").UseDegrees ?
            ToRadians(longitude.Value) :
            longitude.Value;
        }
      }

      Params.TrySetData(DA, "Place Name", () => location.Value.PlaceName);
      Params.TrySetData(DA, "Weather Station", () => location.Value.WeatherStationName);
      Params.TrySetData(DA, "Time Zone", () => location.Value.TimeZone);
      Params.TrySetData(DA, "Latitude", () => Params.Output<Param_Number>("Latitude").UseDegrees ? ToDegrees(location.Value.Latitude) : location.Value.Latitude);
      Params.TrySetData(DA, "Longitude", () => Params.Output<Param_Number>("Longitude").UseDegrees ? ToDegrees(location.Value.Longitude) : location.Value.Longitude);
      Params.TrySetData(DA, "Elevation", () => location.Value.Elevation);
    }
  }
}

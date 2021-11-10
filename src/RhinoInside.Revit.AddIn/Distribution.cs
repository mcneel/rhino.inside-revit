using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace RhinoInside.Revit.AddIn.Distribution
{
  public class ReleaseVersion : IXmlSerializable
  {
    public ReleaseVersion() { }

    [XmlIgnore]
    public Version Version { get; private set; } = null;

    public void WriteXml(XmlWriter writer) => writer.WriteString(this.ToString());
    public void ReadXml(XmlReader reader) => Version = new Version(reader.ReadElementContentAsString());
    public XmlSchema GetSchema() => null;
    public override string ToString() => Version?.ToString();

    public static bool operator ==(ReleaseVersion rv, Version v) => rv.Version == v;
    public static bool operator !=(ReleaseVersion rv, Version v) => rv.Version != v;
    public static bool operator >(ReleaseVersion rv, Version v) => rv.Version > v;
    public static bool operator <(ReleaseVersion rv, Version v) => rv.Version < v;
    public static bool operator >=(ReleaseVersion rv, Version v) => rv.Version >= v;
    public static bool operator <=(ReleaseVersion rv, Version v) => rv.Version <= v;
    public override bool Equals(object obj) => Version.Equals(obj);
    public override int GetHashCode() => Version.GetHashCode();
  }

  [XmlRoot("release")]
  public class ReleaseInfo
  {
    [XmlElement("title")]
    public string Title { get; set; }

    [XmlElement("releaseDate")]
    public DateTime ReleaseDate { get; set; }

    [XmlElement("version")]
    public ReleaseVersion Version { get; set; }

    [XmlElement("downloadUrl")]
    public string DownloadUrl { get; set; }

    [XmlElement("releaseNotesUrl")]
    public string ReleaseNotesUrl { get; set; }

    [XmlElement("sha256")]
    public string Signature { get; set; }

    internal UpdateChannel Source { get; set; }
  }

  internal class UpdateChannel
  {
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string ReleaseName { get; set; }
    public string Description { get; set; }
    public string IconResource { get; set; }
    public string Url { get; set; }
    public bool IsStable { get; set; } = false;

    public ReleaseInfo GetLatestRelease()
    {
      // capture all errors and return null if any
      try
      {
        var xml = new XmlSerializer(typeof(ReleaseInfo));
        string releaseInfo = new WebClient().DownloadString(Url);
        using (var reader = new StringReader(releaseInfo))
        {
          var rinfo = (ReleaseInfo) xml.Deserialize(reader);
          rinfo.Source = this;
          return rinfo;
        }
      }
      catch
      {
        // TODO: log error message for debug
        return null;
      }
    }
  }

  internal static class Updater
  {
    // Note:
    // - It is expected that this list does not include any channels that do
    //   not belong to the major version of this addon.
    //   Any addon should only know about its own channels
    //   e.g. No 2.0/ channel on an addon with major version 1.0
    // - Order is important. Top to bottom from most public to least public
    public static readonly UpdateChannel[] Channels = new UpdateChannel[]
    {
      new UpdateChannel
      {
        Id = new Guid("0b10351c-25e3-4680-9135-6b86cd27bcda"),
        Name = "Public Releases (Official)",
        ReleaseName = "Public Release",
        Description = "Official and stable public releases downloadable from website",
        IconResource = $"{typeof(Loader).Namespace}.Resources.ChannelStable-icon.png",
        Url = $@"https://files.mcneel.com/rhino.inside/revit/update/{Core.Version.Major}.x/stable.xml",
        IsStable = true
      },
      new UpdateChannel
      {
        Id =             new Guid("c63def46-e63d-41e3-8f82-9b5ee1d88251"),
        Name =           "Release Candidates (Pre-release Bug Fixes)",
        ReleaseName =    "Release Candidate",
        Description =    "Release candidates are product releases being stabilized for final release and may still contain bugs",
        IconResource =   $"{typeof(Loader).Namespace}.Resources.ChannelRC-icon.png",
        Url =            $@"https://files.mcneel.com/rhino.inside/revit/update/{Core.Version.Major}.x/rc.xml"
      },
      new UpdateChannel
      {
        Id =             new Guid("7fc1e535-c7cd-47d8-a969-e01435bacd65"),
        Name =           "Daily Builds (Work in Progress)",
        ReleaseName =    "Daily Build",
        Description =    "Daily Builds are most recent builds of the development branch and might contain bugs and unfinished features",
        IconResource =   $"{typeof(Loader).Namespace}.Resources.ChannelDaily-icon.png",
        Url =            $@"https://files.mcneel.com/rhino.inside/revit/update/{Core.Version.Major}.x/daily.xml"
      }
    };

    public static UpdateChannel DefaultChannel => Channels[0];

    public static UpdateChannel ActiveChannel
    {
      get
      {
        if (Properties.AddInOptions.Current.UpdateChannel is string activeChannelId)
        {
          var channelGuid = new Guid(activeChannelId);
          return Channels.Where(x => x.Id == channelGuid).FirstOrDefault();
        }

        return null;
      }
    }

    public static async Task<ReleaseInfo> GetReleaseInfoAsync()
        => await Task.Run(() => ActiveChannel?.GetLatestRelease());

    public static async Task<ReleaseInfo> GetReleaseInfoAsync(UpdateChannel channel)
        => await Task.Run(() => channel?.GetLatestRelease());
  }
}

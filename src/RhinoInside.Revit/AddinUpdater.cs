using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.IO;
using System.Net;

using Autodesk.Revit.UI;

using RhinoInside.Revit.Settings;

namespace RhinoInside.Revit
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
  }

  internal class AddinUpdateChannel
  {
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Version TargetVersion { get; set; }
    public string Url { get; set; }

    public AddinUpdateChannel(Guid id, string name, string description, Version target, string url)
    {
      Id = id; Name = name; Description = description; TargetVersion = target; Url = url;
    }

    public ReleaseInfo GetLatestRelease()
    {
      // capture all errors and return null if any
      try
      {
        var xml = new XmlSerializer(typeof(ReleaseInfo));
        string releaseInfo = new WebClient().DownloadString(this.Url);
        using (TextReader reader = new StringReader(releaseInfo))
        {
          return (ReleaseInfo) xml.Deserialize(reader);
        }
      }
      catch (Exception updateCheckEx)
      {
        // TODO: log error message for debug
        return null;
      }
    }
  }

  internal static class AddinUpdater
  {
    static public readonly AddinUpdateChannel DefaultChannel = new AddinUpdateChannel
    (
      id:           new Guid("0b10351c-25e3-4680-9135-6b86cd27bcda"),
      name:         "Public Releases",
      description:  "Official and tested public releases downloadable from website",
      target:       new Version(1, 0),
      url:          @"https://files.mcneel.com/rhino.inside.revit/updates/1.0/stable.xml"
    );

    /* Note:
     * It is expected that this list does not include any channels that do not belong to the major
     * version of this addon. Any addon should only know about its own channels
     * e.g. No 2.0/ channel on an addon with major version 1.0
     */
    static public readonly List<AddinUpdateChannel> Channels = new List<AddinUpdateChannel>
    {
      DefaultChannel,
      // TODO: this channel is not setup yet. activate when ready
      //new AddinUpdateChannel
      //(
      //  id:           new Guid("c63def46-e63d-41e3-8f82-9b5ee1d88251"),
      //  name:         "Release Candidates",
      //  description:  "Release candidates are product releases being cleaned up for release and may still contain bugs",
      //  target:       new Version(1, 0),
      //  url:          @"https://files.mcneel.com/rhino.inside.revit/updates/1.0/rc.xml"
      //),
        new AddinUpdateChannel
      (
        id:           new Guid("7fc1e535-c7cd-47d8-a969-e01435bacd65"),
        name:         "Daily Builds (Work in Progress)",
        description:  "Daily Builds are most recent builds of the development branch and might contain bugs and unfinished features",
        target:       new Version(1, 0),
        url:          @"https://files.mcneel.com/rhino.inside.revit/updates/1.0/daily.xml"
      )
    };

    static AddinUpdateChannel ActiveChannel
    {
      get
      {
        if (AddinOptions.UpdateChannel is string activeChannelId)
        {
          var channelGuid = new Guid(activeChannelId);
          return Channels.Where(x => x.Id == channelGuid).FirstOrDefault();
        }
        return null;
      }
    }

    static public void GetReleaseInfo(Action<ReleaseInfo> callBack)
      => GetReleaseInfo(ActiveChannel, callBack);

    static public async void GetReleaseInfo(AddinUpdateChannel channel, Action<ReleaseInfo> callBack)
    {
      if (callBack != null)
        callBack(
          await Task.Run(() => channel.GetLatestRelease())
          );
    }
  }
}

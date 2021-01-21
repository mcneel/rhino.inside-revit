using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

using Autodesk.Revit.UI;

using RhinoInside.Revit.Settings;

namespace RhinoInside.Revit
{
  class AddinReleaseInfo
  {
    public string Title { get; set; }
    //public string Description { get; set; }
    public DateTime ReleaseDate { get; set; }
    public Version Version { get; set; }
    public string DownloadUrl { get; set; }
    public string ReleaseNotesUrl { get; set; }
    public SHA256 Signature { get; set; }
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

    public AddinReleaseInfo GetLatestRelease()
    {
      // TODO
      return new AddinReleaseInfo
      {
        Version = new Version("2.0")
      };
    }
  }

  static class AddinUpdater
  {
    static public readonly AddinUpdateChannel DefaultChannel = new AddinUpdateChannel
    (
      id:           new Guid("0b10351c-25e3-4680-9135-6b86cd27bcda"),
      name:         "Public Releases",
      description:  "Official and tested public releases downloadable from website",
      target:       new Version(1, 0),
      url:          @""
    );

    static public readonly List<AddinUpdateChannel> Channels = new List<AddinUpdateChannel>
    {
      DefaultChannel,
      new AddinUpdateChannel
      (
        id:           new Guid("c63def46-e63d-41e3-8f82-9b5ee1d88251"),
        name:         "Release Candidates",
        description:  "Release candidates are product releases being cleaned up for release and may still contain bugs",
        target:       new Version(1, 0),
        url:          @""
      ),
        new AddinUpdateChannel
      (
        id:           new Guid("7fc1e535-c7cd-47d8-a969-e01435bacd65"),
        name:         "Daily Builds (Work in Progress)",
        description:  "Daily Builds are most recent builds of the development branch and might contain bugs and unfinished features",
        target:       new Version(1, 0),
        url:          @""
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

    static public async void CheckUpdates(Action<AddinReleaseInfo> callBack)
    {
      if (callBack != null)
        callBack(
          await Task.Run(() => ActiveChannel.GetLatestRelease())
          );
    }
  }
}

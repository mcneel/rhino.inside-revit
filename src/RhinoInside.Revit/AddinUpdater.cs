using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Autodesk.Revit.UI;

using RhinoInside.Revit.Settings;

namespace RhinoInside.Revit
{
  class AddinReleaseInfo
  {
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime ReleaseDate { get; set; }
    public Version Version { get; set; }
    public string DownloadUrl { get; set; }
  }

  abstract class AddinUpdateChannel
  {
    abstract public Guid Id { get; }
    abstract public string Name { get; }
    abstract public string Description { get; }
    abstract public int MajorVersion { get; }
    abstract public string Url { get; }

    public AddinReleaseInfo GetLatestRelease()
    {
      return new AddinReleaseInfo
      {
        Version = new Version("2.0")
      };
    }
  }

  class UpdatePublicChannel : AddinUpdateChannel
  {
    public override Guid Id => new Guid("0b10351c-25e3-4680-9135-6b86cd27bcda");
    public override string Name => "Public Releases";
    public override string Description => "Official and tested public releases downloadable from website";
    public override int MajorVersion => 1;
    public override string Url => @"";
  }

  class UpdateReleaseCandidateChannel : AddinUpdateChannel
  {
    public override Guid Id => new Guid("c63def46-e63d-41e3-8f82-9b5ee1d88251");
    public override string Name => "Release Candidates";
    public override string Description => "Release candidates are product releases being cleaned up for release and may still contain bugs";
    public override int MajorVersion => 1;
    public override string Url => @"";
  }

  class UpdateDailyChannel : AddinUpdateChannel
  {
    public override Guid Id => new Guid("7fc1e535-c7cd-47d8-a969-e01435bacd65");
    public override string Name => "Daily Builds (Work in Progress)";
    public override string Description => "Daily Builds are most recent builds of the development branch and might contain bugs and unfinished features";
    public override int MajorVersion => 1;
    public override string Url => @"";
  }

  static class AddinUpdater
  {
    static public readonly AddinUpdateChannel DefaultChannel = new UpdatePublicChannel();
    static public readonly List<AddinUpdateChannel> Channels = new List<AddinUpdateChannel>
    {
      DefaultChannel,
      new UpdateReleaseCandidateChannel(),
      new UpdateDailyChannel()
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

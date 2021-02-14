using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Interop;

using Eto.Forms;
using Eto.Drawing;
using Forms = Eto.Forms;

using Autodesk.Revit.UI;

using RhinoInside.Revit.Settings;
using System.Diagnostics;

namespace RhinoInside.Revit.UI
{
  internal class OptionsWindow : BaseDialog
  {
    public UpdatesPanel UpdatesPanel { get; } = new UpdatesPanel();

    private TabPage _general;
    private TabPage _updates;
    private TabPage _scripts;
    private TabControl _tabs;

    public OptionsWindow(UIApplication uiApp) : base(uiApp, initialSize: new Size(450, -1))
    {
      Title = "Options";
      InitLayout();
    }

    public void ActivateGeneralTab() => _tabs.SelectedPage = _general;
    public void ActivateUpdatesTab() => _tabs.SelectedPage = _updates;
    public void ActivateScriptsTab() => _tabs.SelectedPage = _scripts;

    void InitLayout()
    {
      // apply settings button
      var applyButton = new Button { Text = "Apply", Height = 25 };
      applyButton.Click += ApplyButton_Click;

    _general = new TabPage { Text = "General", Padding = new Padding(5), Content = new GeneralPanel() };
    _updates = new TabPage { Text = "Updates", Padding = new Padding(5), Content = UpdatesPanel };
    _scripts = new TabPage { Text = "Scripts", Padding = new Padding(5), Content = new Panel() };
    _tabs = new TabControl
    {
      TabPosition = Forms.DockPosition.Top,
      Pages = { _general, _updates, _scripts }
    };

    // setup contents
    Content = new TableLayout
      {
        Spacing = new Size(5, 10),
        Padding = new Padding(5),
        Rows = {
          new TableRow {
            Cells = { new TableCell { ScaleWidth = true, Control = _tabs }}
          },
          null,
          new TableRow { Cells = { applyButton } },
        }
      };
    }

    private void ApplyButton_Click(object sender, EventArgs e)
    {
      UpdatesPanel.ApplyChanges();
      AddinOptions.Save();
      Close();
    }
  }

  internal class GeneralPanel: Panel
  {
    public GeneralPanel() => InitLayout();

    CheckBox _loadOnStartup = new CheckBox { Text = "Load Rhino on Startup" };

    void InitLayout()
    {
      _loadOnStartup.Checked = AddinOptions.Current.LoadOnStartup;

      Content = new TableLayout
      {
        Spacing = new Size(5, 10),
        Padding = new Padding(5),
        Rows = {
          new TableRow {
            ScaleHeight = true,
            Cells = { new TableCell(_loadOnStartup, true) }
          },
        }
      };
    }

    internal void ApplyChanges()
    {
      if (_loadOnStartup.Checked.HasValue)
        AddinOptions.Current.LoadOnStartup = _loadOnStartup.Checked.Value;
    }
  }

  internal class UpdatesPanel : Panel
  {
    public UpdatesPanel() => InitLayout();

    CheckBox _checkUpdatesOnStartup = new CheckBox { Text = "Check Updates on Startup" };
    Label _channelDescription = new Label { Visible = false, Wrap = WrapMode.Word, Height = 36 };
    Forms.ComboBox _updateChannelSelector = new Forms.ComboBox();
    Button _releaseNotesBtn = new Button { Text = "Release Notes", Height = 25 };
    Button _downloadBtn = new Button { Text = "Download Installer", Height = 25 };

    internal ReleaseInfo ReleaseInfo = null;

    void InitLayout()
    {
      // setup update options
      _checkUpdatesOnStartup.Checked = AddinOptions.Current.CheckForUpdatesOnStartup;

      // setup update channel selector
      _updateChannelSelector.SelectedIndexChanged += _updateChannelSelector_SelectedIndexChanged;
      foreach (AddinUpdateChannel chnl in AddinUpdater.Channels)
      {
        _updateChannelSelector.Items.Add(chnl.Name);
      }

      if (AddinOptions.Current.UpdateChannel is string activeChannelId)
      {
        var channelGuid = new Guid(activeChannelId);
        var updaterChannel = AddinUpdater.Channels.Where(x => x.Id == channelGuid).First();
        _updateChannelSelector.SelectedIndex = AddinUpdater.Channels.IndexOf(updaterChannel);
      }
      else
        _updateChannelSelector.SelectedIndex = AddinUpdater.Channels.IndexOf(AddinUpdater.DefaultChannel);


      // setup update options groupbox
      var spacing = new Size(5, 10);

      Content = new TableLayout
      {
        Spacing = spacing,
        Padding = new Padding(5),
        Rows = {
          new TableRow {
            ScaleHeight = true,
            Cells = { new TableCell(_checkUpdatesOnStartup, true) }
          },
          new TableLayout
          {
            Height = 25,
            Spacing = spacing,
            Rows =
            {
              new TableRow {
                ScaleHeight = true,
                Cells =
                {
                  new Label {
                    Text = $"Update Channel v{Addin.Version.Major}.*",
                    Height = 25,
                    VerticalAlignment = VerticalAlignment.Center
                  },
                  _updateChannelSelector
                }
              }
            }
          },
          _channelDescription,
        }
      };

      // setup release info controls
      _releaseNotesBtn.Click += _releaseNotesBtn_Click;
      _downloadBtn.Click += _downloadBtn_Click;
    }

    private void _downloadBtn_Click(object sender, EventArgs e)
    {
      if (ReleaseInfo != null)
        Process.Start(ReleaseInfo.DownloadUrl);
    }

    private void _releaseNotesBtn_Click(object sender, EventArgs e)
    {
      if (ReleaseInfo != null)
        Process.Start(ReleaseInfo.ReleaseNotesUrl);
    }

    private void _updateChannelSelector_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is Forms.ComboBox channelSelector)
      {
        var updaterChannel = AddinUpdater.Channels[channelSelector.SelectedIndex];
        _channelDescription.Text = updaterChannel.Description;
        _channelDescription.Visible = true;
      }
    }

    internal void SetReleaseInfo(ReleaseInfo releaseInfo)
    {
      if (releaseInfo != null)
      {
        ReleaseInfo = releaseInfo;

        var updateGroup = ((TableLayout)Content);
        updateGroup.Rows.Insert(0,
          new TableRow
          {
            Cells = { new Panel
              {
                Content = new TableLayout
                {
                  Spacing = new Size(5, 10),
                  Padding = new Padding(5),
                  Rows =
                  {
                    new TableRow {
                      Cells =
                      {
                        new TableCell {
                          Control = new ImageView {
                            Image = Icon.FromResource("RhinoInside.Revit.Resources.NewRelease.png", assembly: Assembly.GetExecutingAssembly()),
                          }
                        },
                        new TableLayout
                        {
                          Spacing = new Size(5, 10),
                          Rows = {
                            new Label {
                              Text = "New Release Available!\n"
                                  + $"Version: {releaseInfo.Version}\n"
                                  + $"Release Date: {releaseInfo.ReleaseDate}",
                              Width = 150
                            },
                            _downloadBtn,
                            _releaseNotesBtn,
                            null
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        );
      }
    }

    internal void ApplyChanges()
    {
      // update settings
      if (_checkUpdatesOnStartup.Checked.HasValue)
        AddinOptions.Current.CheckForUpdatesOnStartup = _checkUpdatesOnStartup.Checked.Value;

      AddinOptions.Current.UpdateChannel =
        AddinUpdater.Channels[_updateChannelSelector.SelectedIndex].Id.ToString();
    }
  }
}

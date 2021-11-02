using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Eto.Drawing;
using Eto.Forms;

using Autodesk.Revit.UI;

using RhinoInside.Revit.Settings;

namespace RhinoInside.Revit.UI
{
  internal class AddInOptionsDialog : ModalDialog
  {
    public GeneralPanel GeneralPanel { get; } = new GeneralPanel();
    public UpdatesPanel UpdatesPanel { get; } = new UpdatesPanel();
    public ScriptsPanel ScriptsPanel { get; } = new ScriptsPanel();

    private TabPage _general;
    private TabPage _updates;
    private TabPage _scripts;
    private TabControl _tabs;

    public AddInOptionsDialog(UIApplication uiApp) : base(uiApp, initialSize: new Size(470, 450))
    {
      Title = "Options";
      DefaultButton.Click += OkButton_Click;

      InitLayout();
    }

    public void ActivateGeneralTab() => _tabs.SelectedPage = _general;
    public void ActivateUpdatesTab() => _tabs.SelectedPage = _updates;
    public void ActivateScriptsTab() => _tabs.SelectedPage = _scripts;

    void InitLayout()
    {
      _general = new TabPage { Text = "General", Padding = new Padding(5), Content = GeneralPanel };
      _updates = new TabPage { Text = "Updates", Padding = new Padding(5), Content = UpdatesPanel };
      _scripts = new TabPage { Text = "Scripts", Padding = new Padding(5), Content = ScriptsPanel };
      _tabs = new TabControl
      {
        TabPosition = Eto.Forms.DockPosition.Top,
        Pages = { _general, _updates, _scripts },
      };

      // setup contents
      Content = new TableLayout
      {
        Rows =
        {
          new TableRow
          {
            ScaleHeight = true,
            Cells = { new TableCell { ScaleWidth = true, Control = _tabs }}
          }
        }
      };
    }

    private void OkButton_Click(object sender, EventArgs e)
    {
      GeneralPanel.ApplyChanges();
      UpdatesPanel.ApplyChanges();
      ScriptsPanel.ApplyChanges();

      AddinOptions.Save();
      Close(DialogResult.Ok);
    }
  }

  internal class GeneralPanel : Panel
  {
    public GeneralPanel() => InitLayout();

    readonly CheckBox _loadOnStartup = new CheckBox { Text = "Start Rhino on startup", ToolTip = "Restart Revit" };
    readonly CheckBox _isolateSettings = new CheckBox { Text = "Isolate Rhino settings", ToolTip = "Rhino will use a separate set of settings in Revit" };
    readonly CheckBox _useHostLanguage = new CheckBox { Text = "Use Revit UI language", ToolTip = "Rhino UI will be same language as Revit" };
    readonly CheckBox _keepUIOnTop = new CheckBox { Text = "Keep UI always on top", ToolTip = "Rhino UI will display always on top of Revit window" };
    readonly CheckBox _compactTab = new CheckBox { Text = "Compact Revit tabs", ToolTip = "Load into Add-ins tab - Restart Revit" };
    readonly CheckBox _compactRibbon = new CheckBox { Text = "Compact Ribbon", ToolTip = "Collapse Rhino and Grasshopper panels" };

    void InitLayout()
    {
      _loadOnStartup.Checked = AddinOptions.Current.LoadOnStartup;
      _isolateSettings.Checked = AddinOptions.Current.IsolateSettings;
      _useHostLanguage.Checked = AddinOptions.Current.UseHostLanguage;
      _keepUIOnTop.Checked = AddinOptions.Current.KeepUIOnTop;
      _compactTab.Checked = AddinOptions.Current.CompactTab;
      _compactRibbon.Checked = AddinOptions.Current.CompactRibbon;

      Content = new TableLayout
      {
        Spacing = new Size(5, 10),
        Padding = new Padding(5),
        Rows =
        {
          new GroupBox
          {
            Text = "Startup",
            Content = new TableLayout
            {
              Spacing = new Size(5, 10),
              Padding = new Padding(5),
              Rows =
              {
                _loadOnStartup,
                _isolateSettings,
                _useHostLanguage,
                _keepUIOnTop
              }
            }
          },
          new GroupBox
          {
            Text = "User Interface",
            Content = new TableLayout
            {
              Spacing = new Size(5, 10),
              Padding = new Padding(5),
              Rows =
              {
                _compactTab,
                _compactRibbon,
              }
            }
          },
          null
        }
      };
    }

    internal void ApplyChanges()
    {
      if (_loadOnStartup.Checked.HasValue)
        AddinOptions.Current.LoadOnStartup = _loadOnStartup.Checked.Value;

      if (_compactTab.Checked.HasValue)
        AddinOptions.Current.CompactTab = _compactTab.Checked.Value;

      if (_compactRibbon.Checked.HasValue)
        AddinOptions.Current.CompactRibbon = _compactRibbon.Checked.Value;

      if (_isolateSettings.Checked.HasValue)
        AddinOptions.Current.IsolateSettings = _isolateSettings.Checked.Value;

      if (_useHostLanguage.Checked.HasValue)
        AddinOptions.Current.UseHostLanguage = _useHostLanguage.Checked.Value;

      if (_keepUIOnTop.Checked.HasValue)
        AddinOptions.Current.KeepUIOnTop = _keepUIOnTop.Checked.Value;
    }
  }

  internal class UpdatesPanel : Panel
  {
    public UpdatesPanel() => InitLayout();

    readonly CheckBox _checkUpdatesOnStartup = new CheckBox { Text = "Check Updates on Startup" };
    readonly Label _channelDescription = new Label { Visible = false, Wrap = WrapMode.Word, Height = 36, VerticalAlignment = VerticalAlignment.Top };
    readonly DropDown _updateChannelSelector = new DropDown() { Height = 25 };
    readonly Button _releaseNotesBtn = new Button { Text = "Release Notes", Height = 25 };
    readonly Button _downloadBtn = new Button { Text = "Download Installer", Height = 25 };
    readonly Panel _prevChannelsInfo = new Panel();
    static readonly Spinner _prevChannelsInfoSpinner = new Spinner { Visible = false, Enabled = true };
    readonly StackLayout _prevChannelsInfoLabel = new StackLayout
    {
      Orientation = Orientation.Horizontal,
      Items =
      {
        new Label { Text = "Latest In Other Channels:  " },
        _prevChannelsInfoSpinner
      }
    };

    TableRow _releaseInfoPanel = new TableRow();
    internal ReleaseInfo ReleaseInfo = null;

    void InitLayout()
    {
      // setup update options
      _checkUpdatesOnStartup.Checked = AddinOptions.Current.CheckForUpdatesOnStartup;

      // setup update channel selector
      _updateChannelSelector.SelectedIndexChanged += UpdateChannelSelector_SelectedIndexChanged;
      var execAssm = Assembly.GetExecutingAssembly();
      foreach (AddinUpdateChannel channel in AddinUpdater.Channels)
      {
        _updateChannelSelector.Items.Add(
          new ImageListItem
          {
            Image = Icon.FromResource(channel.IconResource, execAssm).WithSize(16, 16),
            Text = channel.Name
          }
        );
      }

      if (AddinOptions.Current.UpdateChannel is string activeChannelId)
      {
        var channelGuid = new Guid(activeChannelId);
        var updateChannel = AddinUpdater.Channels.Where(x => x.Id == channelGuid).First();
        _updateChannelSelector.SelectedIndex = Array.IndexOf(AddinUpdater.Channels, updateChannel);
      }
      else
        _updateChannelSelector.SelectedIndex = Array.IndexOf(AddinUpdater.Channels, AddinUpdater.DefaultChannel);

      // setup update options groupbox
      var spacing = new Size(5, 10);

      Content = new TableLayout
      {
        Spacing = spacing,
        Padding = new Padding(5),
        Rows = {
          new TableRow {
            Cells = { new TableCell(_checkUpdatesOnStartup, true) }
          },
          new TableLayout {
            Height = 25,
            Spacing = spacing,
            Rows = {
              new TableRow {
                Cells = {
                  new Label {
                    Text = $"Update Channel v{AddIn.Version.Major}.*",
                    Height = 25,
                    VerticalAlignment = VerticalAlignment.Center
                  },
                  _updateChannelSelector
                }
              }
            }
          },
          new TableRow {
            Cells = { _channelDescription }
          },
           new TableRow {
            Cells = { _prevChannelsInfoLabel }
          },
           new TableRow {
            Cells = { _prevChannelsInfo }
          },
           null
        }
      };

      // setup release info controls
      _releaseNotesBtn.Click += ReleaseNotesBtn_Click;
      _downloadBtn.Click += DownloadBtn_Click;
    }

    private void DownloadBtn_Click(object sender, EventArgs e)
    {
      if (ReleaseInfo != null)
        Process.Start(ReleaseInfo.DownloadUrl);
    }

    private void ReleaseNotesBtn_Click(object sender, EventArgs e)
    {
      if (ReleaseInfo != null)
        Process.Start(ReleaseInfo.ReleaseNotesUrl);
    }

    private void UpdateChannelSelector_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is DropDown channelSelector)
      {
        var updateChannel = AddinUpdater.Channels[channelSelector.SelectedIndex];
        _channelDescription.Text = updateChannel.Description;
        _channelDescription.Visible = true;

        // build the table for other channel information
        UpdatePreviousChannelsInfo(
          AddinUpdater.Channels.Where(c => c.Id != updateChannel.Id).ToList()
          );
      }
    }

    private async void UpdatePreviousChannelsInfo(List<AddinUpdateChannel> prevChannels)
    {
      if (prevChannels.Count > 0)
      {
        _prevChannelsInfoSpinner.Visible = true;

        var infoTable = new TableLayout();

        foreach (AddinUpdateChannel channel in prevChannels)
        {
          var releaseInfo = await AddinUpdater.GetReleaseInfoAsync(channel);
          if (releaseInfo is ReleaseInfo)
          {
            var downloadBtn = new LinkButton { Text = $"v{releaseInfo.Version}" };
            downloadBtn.Click += (s, e) => Process.Start(releaseInfo.DownloadUrl);
            var whatsNewButton = new LinkButton { Text = $"Release Notes" };
            whatsNewButton.Click += (s, e) => Process.Start(releaseInfo.ReleaseNotesUrl);

            infoTable.Rows.Add(
              new TableRow
              {
                Cells =
                {
                new ImageView
                {
                  Width = 18,
                  Image = Icon.FromResource(
                    channel.IconResource, assembly: Assembly.GetExecutingAssembly()
                    ).WithSize(16, 16),
                },
                new TableCell
                {
                  ScaleWidth = true,
                  Control = new Label { Text = channel.ReleaseName },
                },
                downloadBtn,
                new Panel { Width = 20 },
                whatsNewButton
                }
              });
          }
        }

        _prevChannelsInfo.Content = infoTable;
        _prevChannelsInfoSpinner.Visible = false;
      }
      else
      {
        _prevChannelsInfoLabel.Visible = false;
        _prevChannelsInfo.Visible = false;
      }
    }

    internal void SetReleaseInfo(ReleaseInfo releaseInfo)
    {
      var updateGroup = ((TableLayout) Content);
      if (releaseInfo != null)
      {
        ReleaseInfo = releaseInfo;

        _releaseInfoPanel = new TableRow
        {
          Cells = {
            new Panel {
              Content = new TableLayout {
                Spacing = new Size(5, 10),
                Padding = new Padding(5),
                Rows = {
                  new TableRow {
                    Cells = {
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
                            Text = $"New {releaseInfo.Source.ReleaseName} Available!\n"
                                 + $"Version: {releaseInfo.Version}\n"
                                 + $"Release Date: {releaseInfo.ReleaseDate}",
                            Width = 150
                          },
                          new Label {
                            Text = "Close Revit and run the installer to update",
                            TextAlignment = TextAlignment.Center
                          },
                          _downloadBtn,
                          _releaseNotesBtn
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        };

        updateGroup.Rows.Insert(0, _releaseInfoPanel);
      }
      else if (_releaseInfoPanel is TableRow)
      {
        updateGroup.Rows.Remove(_releaseInfoPanel);
        _releaseInfoPanel = null;
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

  internal class ScriptsPanel : Panel
  {
    public ScriptsPanel() => InitLayout();

    CheckBox _loadScriptPackagesOnStartup = new CheckBox { Text = "Add Installed Packages to Ribbon" };
    CheckBox _loadScriptsOnStartup = new CheckBox { Text = "Add User Scripts to Ribbon" };
    ListBox _scriptLocations = new ListBox();
    Button _addButton = new Button { Text = "Add Script Location", Height = 25 };
    Button _delButton = new Button { Text = "Remove Selected", Height = 25, Enabled = false };

    void InitLayout()
    {
      _loadScriptsOnStartup.Checked = AddinOptions.Current.LoadUserScriptPackages;
      _loadScriptPackagesOnStartup.Checked = AddinOptions.Current.LoadInstalledScriptPackages;

      foreach (var location in AddinOptions.Current.ScriptLocations)
        _scriptLocations.Items.Add(location);
      _scriptLocations.SelectedIndexChanged += ScriptLocations_SelectedIndexChanged;

      _addButton.Click += AddButton_Click;
      _delButton.Click += DelButton_Click;

      Content = new TableLayout
      {
        Spacing = new Size(5, 10),
        Padding = new Padding(5),
        Rows = {
          new StackLayout
          {
            Spacing = 10,
            Items = {
              _loadScriptPackagesOnStartup,
              _loadScriptsOnStartup,
            }
          },
          new TableRow {
            Cells = { new Label { Text = "User Script Locations" } }
          },
          new TableRow {
            ScaleHeight = true,
            Cells = { _scriptLocations }
          },
          new TableLayout
          {
            Spacing = new Size(5, 10),
            Padding = new Padding(5),
            Rows = {
              new TableRow {
                Cells = {
                  new TableCell(_addButton, true),
                  new TableCell(_delButton, true),
                }
              },
            }
          }
        }
      };
    }

    private void ScriptLocations_SelectedIndexChanged(object sender, EventArgs e)
    {
      _delButton.Enabled = _scriptLocations.SelectedIndex > -1;
    }

    private void AddButton_Click(object sender, EventArgs e)
    {
      var sfdlg = new SelectFolderDialog();
      sfdlg.ShowDialog(this);

      if (sfdlg.Directory is string location)
      {
        foreach (var item in _scriptLocations.Items)
          if (item.Text == location)
          {
            _scriptLocations.SelectedIndex = _scriptLocations.Items.IndexOf(item);
            return;
          }

        _scriptLocations.Items.Add(location);
      }
    }

    private void DelButton_Click(object sender, EventArgs e)
    {
      if (_scriptLocations.SelectedIndex > -1)
      {
        _scriptLocations.Items.RemoveAt(_scriptLocations.SelectedIndex);
      }
    }

    internal void ApplyChanges()
    {
      if (_loadScriptsOnStartup.Checked.HasValue)
        AddinOptions.Current.LoadUserScriptPackages = _loadScriptsOnStartup.Checked.Value;

      if (_loadScriptPackagesOnStartup.Checked.HasValue)
        AddinOptions.Current.LoadInstalledScriptPackages = _loadScriptPackagesOnStartup.Checked.Value;

      var scriptLocs = new HashSet<string>();
      foreach (var item in _scriptLocations.Items)
        scriptLocs.Add(item.Text);
      AddinOptions.Current.ScriptLocations = scriptLocs;
    }
  }
}

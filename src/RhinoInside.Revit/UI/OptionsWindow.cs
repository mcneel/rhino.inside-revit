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

namespace RhinoInside.Revit.UI
{
  class OptionsWindow : BaseWindow
  {
    CheckBox _checkUpdatesOnStartup = new CheckBox { Text = "Check Updates on Startup" };
    Forms.ComboBox _updateChannelSelector = new Forms.ComboBox();

    public OptionsWindow(UIApplication uiApp) : base(uiApp, width: 400, height: 200)
    {
      Title = "Options";
      InitLayout();
    }

    void InitLayout()
    {
      // setup update options
      _checkUpdatesOnStartup.Checked = AddinOptions.CheckForAddinUpdates;

      // setup update channel selector
      foreach (AddinUpdateChannel chnl in AddinUpdater.Channels)
        _updateChannelSelector.Items.Add(chnl.Name);

      if (AddinOptions.UpdateChannel is string activeChannelId)
      {
        var channelGuid = new Guid(activeChannelId);
        var updaterChannel = AddinUpdater.Channels.Where(x => x.Id == channelGuid).First();
        _updateChannelSelector.SelectedIndex = AddinUpdater.Channels.IndexOf(updaterChannel);
      }
      else
        _updateChannelSelector.SelectedIndex = AddinUpdater.Channels.IndexOf(AddinUpdater.DefaultChannel);

      // apply settings button
      var applyButton = new Button { Text = "Apply" };
      applyButton.Click += ApplyButton_Click;
      applyButton.Height = 25;

      // setup update options groupbox
      var spacing = new Size(5, 10);

      var updateOpts = new GroupBox
      {
        Text = "Updates",
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
            }
          }
        }
      };

      // setup contents
      Content = new TableLayout
      {
        Spacing = spacing,
        Padding = new Padding(5),
        Rows = {
          new TableRow {
            Cells = { new TableCell { ScaleWidth = true, Control = updateOpts } }
          },
          null,
          new TableRow { Cells = { applyButton } },
        }
      };
    }

    private void ApplyButton_Click(object sender, EventArgs e)
    {
      // update settings
      if (_checkUpdatesOnStartup.Checked.HasValue)
        AddinOptions.CheckForAddinUpdates = _checkUpdatesOnStartup.Checked.Value;

      AddinOptions.UpdateChannel =
        AddinUpdater.Channels[_updateChannelSelector.SelectedIndex].Id.ToString();

      Close();
    }
  }
}

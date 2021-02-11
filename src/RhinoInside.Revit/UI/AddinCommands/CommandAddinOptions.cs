using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using RhinoInside.Revit.Native;
using RhinoInside.Revit.Settings;
using UIX = RhinoInside.Revit.External.UI;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandAddinOptions : Command
  {
    static ReleaseInfo LatestReleaseInfo = null;

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      const string CommandName = "Options";

      var buttonData = NewPushButtonData<CommandAddinOptions, AlwaysAvailable>(CommandName, "Resources.Options.png", "");
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        // setup button
        StoreButton("Options", pushButton);

        // disable if startup mode is disabled
        if (Addin.StartupMode == AddinStartupMode.Disabled)
        {
          pushButton.Enabled = false;
          pushButton.ToolTip = "Addin Disabled";
        }

        // disable the button if options are readonly
        pushButton.Enabled = !AddinOptions.IsReadOnly && Addin.IsRhinoUIFrameworkReady;
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      // try opening options window
      if (!AddinOptions.IsReadOnly)
      {
        var optWindow = new OptionsWindow(data.Application);
        if (LatestReleaseInfo != null)
          optWindow.SetReleaseInfo(LatestReleaseInfo);
        optWindow.ShowModal();
      }
      else
        TaskDialog.Show("Options", "Contact your system admin to change the options");

      return Result.Succeeded;
    }

    /// <summary>
    /// Mark button with highlighter dot using Autodesk.Windows api
    /// </summary>
    static public void NotifyUpdateAvailable(ReleaseInfo releaseInfo)
    {
      // button gets deactivated if options are readonly
      if (!AddinOptions.IsReadOnly)
      {
        if (RestoreButton("Options") is RibbonButton button)
        {
          HighlightButton(button);
          button.ToolTip = "New Release Available for Download!\n"
                         + $"Version: {releaseInfo.Version}\n"
                         + button.ToolTip;
        }
        LatestReleaseInfo = releaseInfo;
      }
    }
  }

}

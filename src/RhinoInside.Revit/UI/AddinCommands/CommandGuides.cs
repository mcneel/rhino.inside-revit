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
  class CommandGuides : Command
  {
    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      const string CommandName = "Guides";

      var buttonData = NewPushButtonData<CommandGuides, AlwaysAvailable>(CommandName, "Resources.Guides-icon.png", "");
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, @"https://www.rhino3d.com/inside/revit/beta/"));
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      using (System.Diagnostics.Process.Start(@"https://www.rhino3d.com/inside/revit/beta/guides")) { }

      return Result.Succeeded;
    }
  }

}

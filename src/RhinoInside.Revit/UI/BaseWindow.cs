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

using RhinoInside.Revit.External.UI.Extensions;

namespace RhinoInside.Revit.UI
{
  /// <summary>
  /// Rhino UI framework base window type for this addon
  /// Current implementation is centered on Revit window and uses the generic Rhino icon
  /// </summary>
  class BaseWindow : Form
  {
    public BaseWindow(UIApplication uiApp, int width, int height)
    {
      Owner = Eto.Forms.WpfHelpers.ToEtoWindow(uiApp.MainWindowHandle);
      Icon = Icon.FromResource("RhinoInside.Revit.Resources.Rhino-logo.ico", assembly: Assembly.GetExecutingAssembly());
      Resizable = false;

      // set window size and center on the parent window
      var size = new Size(width, height);
      MinimumSize = size;
      var windowLocation = uiApp.GetChildWindowCenterLocation(size.Width, size.Height);
      Location = new Point(windowLocation.X, windowLocation.Y);

      // styling
      Padding = new Padding(10, 0, 10, 10);
      BackgroundColor = Colors.White;
    }
  }
}

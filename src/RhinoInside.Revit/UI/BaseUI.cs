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

using Rhino.UI;

using Autodesk.Revit.UI;

using RhinoInside.Revit.External.UI.Extensions;

namespace RhinoInside.Revit.UI
{
  /// <summary>
  /// Rhino UI framework base form (non-modal) type for this addon
  /// Current implementation is centered on Revit window and uses the generic Rhino icon
  /// </summary>
  class BaseForm : Form
  {
    private UIApplication _uiApp = null;

    public BaseForm(UIApplication uiApp, Size initialSize)
    {
      BaseUIUtils.SetupWindow(this, uiApp, initialSize);
      _uiApp = uiApp;
      SizeChanged += BaseForm_SizeChanged;
    }

    private void BaseForm_SizeChanged(object sender, EventArgs e)
      => BaseUIUtils.CenterWindow(this, _uiApp);
  }

  /// <summary>
  /// Rhino UI framework base dialog (modal) type for this addon
  /// Current implementation is centered on Revit window and uses the generic Rhino icon
  /// </summary>
  class BaseDialog : Dialog
  {
    private UIApplication _uiApp = null;

    public BaseDialog(UIApplication uiApp, Size initialSize)
    {
      BaseUIUtils.SetupWindow(this, uiApp, initialSize);
      _uiApp = uiApp;
      SizeChanged += BaseDialog_SizeChanged;
    }

    private void BaseDialog_SizeChanged(object sender, EventArgs e)
      => BaseUIUtils.CenterWindow(this, _uiApp);
  }


  /// <summary>
  /// Set of utility methods for the base form and dialog
  /// </summary>
  internal static class BaseUIUtils
  {
    internal static void SetupWindow(Window wnd, UIApplication uiApp, Size initialSize)
    {
      // set Revit window as parent
#if REVIT_2019
      wnd.Owner = RhinoEtoApp.MainWindow;
#else
      wnd.Owner = Eto.Forms.WpfHelpers.ToEtoWindow(Autodesk.Windows.ComponentManager.ApplicationWindow);
#endif
      // set the default Rhino icon
      wnd.Icon = Icon.FromResource("RhinoInside.Revit.Resources.Rhino-logo.ico", assembly: Assembly.GetExecutingAssembly());

      // set window size and center on the parent window
      wnd.ClientSize = initialSize;

      // assign size handler to always center window
      wnd.Resizable = false;

      // styling
      wnd.Padding = new Padding(10, 0, 10, 10);
      wnd.BackgroundColor = Colors.White;
    }

    internal static void CenterWindow(Window wnd, UIApplication uiApp)
    {
      var loc = uiApp.GetChildWindowCenterLocation(wnd.Width, wnd.Height);
      wnd.Location = new Point(loc.X, loc.Y);
    }

  }
}

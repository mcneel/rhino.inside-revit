using System.Reflection;
using Autodesk.Revit.UI;
using Eto.Drawing;
using Eto.Forms;

namespace RhinoInside.Revit.UI
{
  /// <summary>
  /// Set of utility methods for the base form and dialog
  /// </summary>
  internal static class BaseWindowUtils
  {
    internal static void SetupWindow(Window wnd, UIApplication uiApp, Size initialSize)
    {
      // set Revit window as parent
#if REVIT_2019
      wnd.Owner = Eto.Forms.WpfHelpers.ToEtoWindow(uiApp.MainWindowHandle);
#else
      wnd.Owner = Eto.Forms.WpfHelpers.ToEtoWindow(Autodesk.Windows.ComponentManager.ApplicationWindow);
#endif
      // set the default Rhino icon
      wnd.Icon = Icon.FromResource("RhinoInside.Revit.Resources.RIR-logo.ico", assembly: Assembly.GetExecutingAssembly());

      // set window size and center on the parent window
      wnd.ClientSize = initialSize;

      // assign size handler to always center window
      wnd.Resizable = false;

      // styling
      wnd.Padding = new Padding(10, 10, 10, 10);
      wnd.BackgroundColor = Colors.White;
    }
  }
}

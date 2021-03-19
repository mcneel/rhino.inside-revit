using System;
using Autodesk.Revit.UI;
using Eto.Drawing;
using Eto.Forms;

namespace RhinoInside.Revit.UI
{
  /// <summary>
  /// Rhino UI framework base dialog (modal) type for this addon
  /// Current implementation is centered on Revit window and uses the generic Rhino icon
  /// </summary>
  public abstract class BaseDialog : Dialog
  {
    private UIApplication _uiApp = null;

    public BaseDialog(UIApplication uiApp, Size initialSize)
    {
      BaseWindowUtils.SetupWindow(this, uiApp, initialSize);
      _uiApp = uiApp;
      SizeChanged += BaseDialog_SizeChanged;
    }

    private void BaseDialog_SizeChanged(object sender, EventArgs e)
      => BaseWindowUtils.CenterWindow(this, _uiApp);
  }
}

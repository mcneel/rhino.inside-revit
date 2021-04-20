using System;
using Eto.Drawing;
using Eto.Forms;

namespace RhinoInside.Revit.UI
{
  /// <summary>
  /// Rhino UI framework base form (non-modal) type for this addon
  /// Current implementation is centered on Revit window and uses the generic Rhino icon
  /// </summary>
  public abstract class BaseForm : Form
  {
    private Autodesk.Revit.UI.UIApplication _uiApp = null;

    public BaseForm(Autodesk.Revit.UI.UIApplication uiApp, Size initialSize)
    {
      BaseWindowUtils.SetupWindow(this, uiApp, initialSize);
      _uiApp = uiApp;
      SizeChanged += BaseForm_SizeChanged;
    }

    private void BaseForm_SizeChanged(object sender, EventArgs e)
      => BaseWindowUtils.CenterWindow(this, _uiApp);
  }
}

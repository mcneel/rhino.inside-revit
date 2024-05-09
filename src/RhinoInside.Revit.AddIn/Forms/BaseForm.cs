using System;
using Eto.Drawing;
using Eto.Forms;

namespace RhinoInside.Revit.AddIn.Forms
{
  /// <summary>
  /// Rhino UI framework base form (non-modal) type for this addon
  /// Current implementation is centered on Revit window and uses the generic Rhino icon
  /// </summary>
  abstract class BaseForm : Form
  {
    protected BaseForm(Autodesk.Revit.UI.UIApplication uiApp, Size initialSize)
    {
      BaseWindowUtils.SetupWindow(this, uiApp, initialSize);
    }
  }
}

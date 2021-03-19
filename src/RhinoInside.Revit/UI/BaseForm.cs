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

using Autodesk.Revit.UI;

using RhinoInside.Revit.External.UI.Extensions;

namespace RhinoInside.Revit.UI
{
  /// <summary>
  /// Rhino UI framework base form (non-modal) type for this addon
  /// Current implementation is centered on Revit window and uses the generic Rhino icon
  /// </summary>
  abstract public class BaseForm : Form
  {
    private UIApplication _uiApp = null;

    public BaseForm(UIApplication uiApp, Size initialSize)
    {
      BaseWindowUtils.SetupWindow(this, uiApp, initialSize);
      _uiApp = uiApp;
      SizeChanged += BaseForm_SizeChanged;
    }

    private void BaseForm_SizeChanged(object sender, EventArgs e)
      => BaseWindowUtils.CenterWindow(this, _uiApp);
  }
}

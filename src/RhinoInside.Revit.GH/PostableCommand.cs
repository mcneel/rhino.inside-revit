using System.Windows.Forms;
using Grasshopper.Kernel;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH
{
  using External.UI.Extensions;

  static class ToolStripExtensions
  {
    public static void AppendPostableCommand(this ToolStrip menu, ARUI.PostableCommand postableCommand, string text)
    {
      var activeApp = Revit.ActiveUIApplication;
      var postable = activeApp.ActiveUIDocument.TryGetPostableCommandId(postableCommand, out var commandId);
      GH_DocumentObject.Menu_AppendItem
      (
        menu, text,
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        postable, false
      );
    }
  }
}
